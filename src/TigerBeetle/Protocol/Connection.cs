using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;

namespace TigerBeetle.Protocol
{
	internal sealed class Connection
	{
		#region Fields

		/// The peer is determined by inspecting the first message header
		/// received.

		private ConnectionPeer peer;

		private byte replica;

		private ConnectionState state;

		/// This is guaranteed to be valid only while state is connected.
		/// It will be reset to null during the shutdown process and is always null if the
		/// connection is unused (i.e. peer == .none).
		private Socket? socket = null;

		private Timer? connectWithExponentialBackoffTimer;

		/// True exactly when the recv_completion has been submitted to the IO abstraction
		/// but the callback has not yet been run.
		private bool recvSubmitted = false;

		/// The Message with the buffer passed to the kernel for recv operations.
		private Message? recvMessage;

		/// The number of bytes in `recv_message` that have been received and need parsing.
		private int recvProgress;

		/// The number of bytes in `recv_message` that have been parsed.
		private int recvParsed;

		/// True if we have already checked the header checksum of the message we
		/// are currently receiving/parsing.
		private bool recvCheckedHeader = false;

		/// True exactly when the send_completion has been submitted to the IO abstraction
		/// but the callback has not yet been run.
		/// 
		private bool sendSubmitted = false;

		/// Number of bytes of the current message that have already been sent.
		private int sendProgress;

		/// The queue of messages to send to the client or replica peer.
		private readonly Queue<Message> sendQueue;

		#endregion Fields

		#region Constructor

		public Connection()
		{
			sendQueue = new Queue<Message>(Config.ConnectionSendQueueMax);
		}

		#endregion Constructor

		#region Properties

		public ConnectionState State => state;

		public ConnectionPeer Peer => peer;

		#endregion Properties

		#region Methods

		/// Attempt to connect to a replica.
		/// The slot in the Message.replicas slices is immediately reserved.
		/// Failure is silent and returns the connection to an unused state.
		public void ConnectToReplica(MessageBus bus, byte replica)
		{
			Trace.Assert(peer == ConnectionPeer.None);
			Trace.Assert(state == ConnectionState.Free);
			Trace.Assert(socket == null);

			// The first replica's network address family determines the
			// family for all other replicas:
			var family = bus.Configuration[0].AddressFamily;

			socket = new Socket(family, SocketType.Stream, ProtocolType.Tcp);
			if (Config.TcpRcvbuf > 0) socket.ReceiveBufferSize = Config.TcpRcvbuf;
			if (Config.TcpSndbuf > 0) socket.SendBufferSize = Config.TcpSndbuf;
			if (Config.TcpKeepalive) socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, Config.TcpKeepalive);
			if (Config.TcpNodelay) socket.NoDelay = Config.TcpNodelay;

			socket.UseOnlyOverlappedIO = true;

			peer = ConnectionPeer.Replica;
			this.replica = replica;

			state = ConnectionState.Connecting;
			bus.ConnectionsUsed += 1;

			Trace.Assert(bus.Replicas[replica] == null);
			bus.Replicas[replica] = this;

			var ms = (int)Timeout.ExponentialBackoffWithJitter(bus.Prgn, Config.ConnectionDelayMinMs, Config.ConnectionDelayMaxMs, bus.ReplicasConnectAttempts[replica]);

			bus.ReplicasConnectAttempts[replica] += 1;
			Trace.WriteLine($"connecting to replica {replica} in {ms}ms...");

			Trace.Assert(!recvSubmitted);
			recvSubmitted = true;

			Trace.Assert(connectWithExponentialBackoffTimer == null);

			const int INFINITE = -1;
			connectWithExponentialBackoffTimer = new Timer(OnConnectWithExponentialBackoffResult, bus, ms, INFINITE);
		}

		private void OnConnectWithExponentialBackoffResult(object asyncState)
		{
			Trace.Assert(connectWithExponentialBackoffTimer != null);
			connectWithExponentialBackoffTimer.Dispose();
			connectWithExponentialBackoffTimer = null;

			Trace.Assert(recvSubmitted);
			recvSubmitted = false;

			var bus = (MessageBus)asyncState ?? throw new NullReferenceException("Invalid callback state");
			if (state == ConnectionState.Terminating)
			{
				MaybeClose(bus);
				return;
			}

			Trace.Assert(state == ConnectionState.Connecting);
			Trace.Assert(socket != null);

			Trace.TraceInformation($"connecting to replica {replica}...");

			Trace.Assert(!recvSubmitted);
			recvSubmitted = true;

			var address = bus.Configuration[replica];
			bus.io.Connect(socket, OnConnectResult, bus, address);
		}

		private void OnConnectResult(SocketAsyncEventArgs e)
		{
			Trace.Assert(recvSubmitted);
			recvSubmitted = false;

			var bus = (MessageBus)e.UserToken ?? throw new NullReferenceException("Invalid callback state");
			if (state == ConnectionState.Terminating)
			{
				MaybeClose(bus);
				return;
			}

			Trace.Assert(state == ConnectionState.Connecting);
			Trace.Assert(socket != null);
			state = ConnectionState.Connected;

			if (e.SocketError != SocketError.Success)
			{
				Trace.TraceInformation($"error connecting to replica {replica}: {e.SocketError}");
				Terminate(bus, shutdown: false);
				return;
			}

			Trace.TraceInformation($"connected to replica {replica}");
			bus.ReplicasConnectAttempts[replica] = 0;

			AssertRecvSendInitialState(bus);

			// This will terminate the connection if there are no messages available:
			GetRecvMessageAndRecv(bus);

			// A message may have been queued for sending while we were connecting:
			// TODO Should we relax recv() and send() to return if `connection.state != .connected`?

			if (state == ConnectionState.Connected) Send(bus);
		}

		private void AssertRecvSendInitialState(MessageBus bus)
		{
			Trace.Assert(bus.ConnectionsUsed > 0);

			Trace.Assert(peer == ConnectionPeer.Unknown || peer == ConnectionPeer.Replica);
			Trace.Assert(state == ConnectionState.Connected);
			Trace.Assert(socket != null);

			Trace.Assert(recvSubmitted == false);
			Trace.Assert(recvMessage == null);
			Trace.Assert(recvProgress == 0);
			Trace.Assert(recvParsed == 0);

			Trace.Assert(sendSubmitted == false);
			Trace.Assert(sendProgress == 0);
		}

		private void GetRecvMessageAndRecv(MessageBus bus)
		{
			if (recvMessage != null && recvParsed == 0)
			{
				Recv(bus);
				return;
			}

			var newMessage = bus.GetMessage();
			if (newMessage == null)
			{
				// TODO Decrease the probability of this happening by:
				// 1. using a header-only message if possible.
				// 2. determining a true upper limit for static allocation.
				Trace.TraceError($"no free buffer available to recv message from {replica}");
				Terminate(bus, shutdown: true);
				return;
			}

			try
			{
				if (recvMessage != null)
				{
					try
					{
						Trace.Assert(recvProgress > 0);
						Trace.Assert(recvParsed > 0);

						var data = recvMessage.Buffer.AsSpan<byte>(recvParsed..recvProgress);
						data.CopyTo(newMessage.Buffer);

						recvProgress = data.Length;
						recvParsed = 0;
					}
					finally
					{
						bus.Unref(recvMessage);
					}
				}
				else
				{
					Trace.Assert(recvProgress == 0);
					Trace.Assert(recvParsed == 0);
				}

				recvMessage = newMessage.Ref();
				Recv(bus);
			}
			finally
			{
				bus.Unref(newMessage);
			}
		}

		private void Recv(MessageBus bus)
		{
			Trace.Assert(peer != ConnectionPeer.None);
			Trace.Assert(state == ConnectionState.Connected);
			Trace.Assert(socket != null);

			Trace.Assert(!recvSubmitted);
			recvSubmitted = true;

			Trace.Assert(recvProgress < Config.MessageSizeMax);
			Trace.Assert(recvMessage != null);

			var buffer = recvMessage.Buffer.AsMemory(recvProgress..Config.MessageSizeMax);
			bus.io.Receive(socket, OnRecvResult, bus, buffer);
		}

		private void OnRecvResult(SocketAsyncEventArgs e)
		{
			Trace.Assert(recvSubmitted);
			recvSubmitted = false;

			var bus = (MessageBus)e.UserToken ?? throw new NullReferenceException("Invalid callback state");
			if (state == ConnectionState.Terminating)
			{
				MaybeClose(bus);
				return;
			}

			Trace.Assert(state == ConnectionState.Connected);
			Trace.Assert(socket != null);

			if (e.SocketError != SocketError.Success)
			{
				// TODO: maybe don't need to close on *every* error
				Trace.TraceError($"error receiving from {replica}: {e.SocketError}");
				Terminate(bus, shutdown: true);
				return;
			}

			var bytesReceived = e.BytesTransferred;

			// No bytes received means that the peer closed its side of the connection.
			if (bytesReceived == 0)
			{
				//Trace.TraceInformation($"peer performed an orderly shutdown: {replica}");
				//Terminate(bus, shutdown: false);
				return;
			}

			recvProgress += bytesReceived;
			ParseMessages(bus);
		}

		private void ParseMessages(MessageBus bus)
		{
			Trace.Assert(peer != ConnectionPeer.None);
			Trace.Assert(state == ConnectionState.Connected);
			Trace.Assert(socket != null);

			for (; ; )
			{
				var message = ParseMessage(bus);
				if (message == null) break;

				OnMessage(bus, message);
			}
		}

		private Message? ParseMessage(MessageBus bus)
		{
			Trace.Assert(recvMessage != null);

			var data = recvMessage.Buffer.AsSpan(recvParsed..recvProgress);

			if (data.Length < HeaderData.SIZE)
			{
				GetRecvMessageAndRecv(bus);
				return null;
			}

			var header = recvMessage.Header;
			if (!recvCheckedHeader)
			{
				if (!header.IsValidChecksum())
				{
					Trace.TraceError($"invalid header checksum received from {replica}");
					Terminate(bus, shutdown: true);
					return null;
				}

				if (header.Size < HeaderData.SIZE || header.Size > Config.MessageSizeMax)
				{
					Trace.TraceError($"header with invalid size {header.Size} received from peer {replica}");
					Terminate(bus, shutdown: true);
					return null;
				}

				if (header.Cluster != bus.Cluster)
				{
					Trace.TraceError($"message addressed to the wrong cluster: {header.Cluster}");
					Terminate(bus, shutdown: true);
					return null;
				}

				Trace.Assert(peer == ConnectionPeer.Replica);
				recvCheckedHeader = true;
			}

			if (data.Length < header.Size)
			{
				GetRecvMessageAndRecv(bus);
				return null;
			}

			// At this point we know that we have the full message in our buffer.
			// We will now either deliver this message or terminate the connection
			// due to an error, so reset recv_checked_header for the next message.
			Trace.Assert(recvCheckedHeader);
			recvCheckedHeader = false;

			if (!header.IsValidChecksumBody(recvMessage.GetBody<byte>()))
			{
				Trace.TraceError($"invalid body checksum received from {replica}");
				Terminate(bus, shutdown: true);
				return null;
			}

			recvParsed += header.Size;

			// Return the parsed message using zero-copy if we can, or copy if the client is
			// pipelining:
			// If this is the first message but there are messages in the pipeline then we
			// copy the message so that its sector padding (if any) will not overwrite the
			// front of the pipeline.  If this is not the first message then we must copy
			// the message to a new message as each message needs to have its own unique
			// `references` and `header` metadata.
			if (recvProgress == header.Size) return recvMessage.Ref();

			var message = bus.GetMessage();
			if (message == null)
			{
				// TODO Decrease the probability of this happening by:
				// 1. using a header-only message if possible.
				// 2. determining a true upper limit for static allocation.
				Trace.TraceError($"no free buffer available to deliver message from {peer}");
				Terminate(bus, shutdown: true);
				return null;
			};

			data[0..header.Size].CopyTo(message.Buffer);
			return message;
		}

		private void OnMessage(MessageBus bus, Message message)
		{
			if (message == recvMessage)
			{
				Trace.Assert(recvParsed == message.Header.Size);
				Trace.Assert(recvParsed == recvProgress);
			}
			else if (recvParsed == message.Header.Size)
			{
				Trace.Assert(recvParsed < recvProgress);
			}
			else
			{
				Trace.Assert(recvParsed > message.Header.Size);
				Trace.Assert(recvParsed <= recvProgress);
			}

			if (message.Header.Command == Command.Request || message.Header.Command == Command.Prepare)
			{
				//var sector_ceil = vsr.sector_ceil(message.header.size);
				//if (message.Header.Size != sector_ceil)
				//{
				//Trace.Assert(message.header.size < sector_ceil);
				//Trace.Assert(message.buffer.len == config.message_size_max + config.sector_size);
				// mem.set(u8, message.buffer[message.header.size..sector_ceil], 0);
				//}
			}

			bus.OnMessage(message);

		}

		public void SendMessage(MessageBus bus, Message message)
		{
			Trace.Assert(peer == ConnectionPeer.Client || peer == ConnectionPeer.Replica);
			switch (state)
			{
				case ConnectionState.Connected:
				case ConnectionState.Connecting:
					break;

				case ConnectionState.Terminating:
					return;

				case ConnectionState.Free:
				case ConnectionState.Accepting:
				default:
					throw new InvalidOperationException();
			}

			sendQueue.Enqueue(message.Ref());

			// If the connection has not yet been established we can't send yet.
			// Instead on_connect() will call send().
			if (state == ConnectionState.Connecting)
			{
				Trace.Assert(peer == ConnectionPeer.Replica);
				return;
			}

			// If there is no send operation currently in progress, start one.
			if (!sendSubmitted) Send(bus);
		}

		private void Send(MessageBus bus)
		{
			Trace.Assert(peer == ConnectionPeer.Client || peer == ConnectionPeer.Replica);
			Trace.Assert(state == ConnectionState.Connected);
			Trace.Assert(socket != null);

			if (sendQueue.Count == 0) return;
			var message = sendQueue.Peek();

			Trace.Assert(!sendSubmitted);
			sendSubmitted = true;

			var buffer = message.Buffer.AsMemory(sendProgress..message.Header.Size);
			bus.io.Send(socket, OnSendResult, bus, buffer);
		}

		private void OnSendResult(SocketAsyncEventArgs e)
		{
			Trace.Assert(socket != null);

			Trace.Assert(sendSubmitted);
			sendSubmitted = false;

			Trace.Assert(peer == ConnectionPeer.Client || peer == ConnectionPeer.Replica);

			var bus = (MessageBus)e.UserToken ?? throw new NullReferenceException("Invalid callback state");
			if (state == ConnectionState.Terminating)
			{
				MaybeClose(bus);
				return;
			}

			Trace.Assert(state == ConnectionState.Connected);
			Trace.Assert(socket != null);

			if (e.SocketError != SocketError.Success)
			{
				Trace.TraceError($"error sending message to replica at {replica}: {e.SocketError}");
				Terminate(bus, shutdown: true);
				return;
			}

			var result = e.BytesTransferred;
			if (sendQueue.Count == 0) throw new NullReferenceException("No current message");

			var message = sendQueue.Peek();
			sendProgress += result;

			Trace.Assert(sendProgress <= message.Header.Size);

			// If the message has been fully sent, move on to the next one.
			if (sendProgress == message.Header.Size)
			{
				sendProgress = 0;
				_ = sendQueue.Dequeue();
				bus.Unref(message);
			}

			Send(bus);
		}

		/// Clean up an active connection and reset it to its initial, unused, state.
		/// This reset does not happen instantly as currently in progress operations
		/// must first be stopped. The `how` arg allows the caller to specify if a
		/// shutdown syscall should be made or not before proceeding to wait for
		/// currently in progress operations to complete and close the socket.
		/// I'll be back! (when the Connection is reused after being fully closed)

		private void Terminate(MessageBus bus, bool shutdown)
		{
			Trace.Assert(peer != ConnectionPeer.None);
			Trace.Assert(state != ConnectionState.Free);
			Trace.Assert(socket != null);

			if (shutdown)
			{
				try
				{
					socket.Shutdown(SocketShutdown.Both);
				}
				catch (SocketException)
				{
					// This should only happen if we for some reason decide to terminate
					// a connection while a connect operation is in progress.
					// This is fine though, we simply continue with the logic below and
					// wait for the connect operation to finish.

					// TODO: This currently happens in other cases if the
					// connection was closed due to an error. We need to intelligently
					// decide whether to shutdown or close directly based on the error
					// before these assertions may be re-enabled.

					//assert(connection.state == .connecting);
					//assert(connection.recv_submitted);
					//assert(!connection.send_submitted);
				}
			}

			Trace.Assert(state != ConnectionState.Terminating);
			state = ConnectionState.Terminating;
			MaybeClose(bus);
		}

		private void MaybeClose(MessageBus bus)
		{
			Trace.Assert(peer != ConnectionPeer.None);
			Trace.Assert(state == ConnectionState.Terminating);

			// If a recv or send operation is currently submitted to the kernel,
			// submitting a close would cause a race. Therefore we must wait for
			// any currently submitted operation to complete.
			if (recvSubmitted || sendSubmitted) return;

			sendSubmitted = true;
			recvSubmitted = true;

			// We can free resources now that there is no longer any I/O in progress.
			while (sendQueue.Count > 0)
			{
				var message = sendQueue.Dequeue();
				bus.Unref(message);
			}

			if (this.recvMessage != null)
			{
				bus.Unref(recvMessage);
				recvMessage = null;
			}

			Trace.Assert(socket != null);
			try
			{
				// It's OK to use the send completion here as we know that no send
				// operation is currently in progress.
				socket.Close();
				OnCloseResult(bus);
			}
			finally
			{
				socket = null;
			}
		}

		private void OnCloseResult(object asyncState)
		{
			Trace.Assert(sendSubmitted);
			Trace.Assert(recvSubmitted);

			Trace.Assert(peer != ConnectionPeer.None);
			Trace.Assert(state == ConnectionState.Terminating);

			var bus = (MessageBus)asyncState ?? throw new NullReferenceException("Invalid callback state");

			Trace.Assert(recvMessage == null);
			Trace.Assert(sendQueue.Count == 0);

			bus.ConnectionsUsed -= 1;

			switch (peer)
			{
				case ConnectionPeer.None:
				case ConnectionPeer.Unknown:
				case ConnectionPeer.Client:
				default:
					throw new InvalidOperationException();

				case ConnectionPeer.Replica:

					// A newer replica connection may have replaced this one:
					if (bus.Replicas[replica] == this)
					{
						bus.Replicas[replica] = null;
					}
					break;
			}

			peer = default;
			replica = default;
			state = default;
			socket = default;
			recvSubmitted = default;
			recvMessage = default;
			recvProgress = default;
			recvParsed = default;
			recvCheckedHeader = default;
			sendSubmitted = default;
			sendProgress = default;
		}

		#endregion Methods
	}
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TigerBeetle.Protocol;

namespace TigerBeetle
{
	public sealed class Client
	{
		#region InnerTypes

		private delegate void ClientCallback(UInt128 userData, Operation action, ReadOnlySpan<byte> reply);

		private struct RequestData
		{
			public UInt128 userData;
			public ClientCallback? callback;
			public Message message;
		}

		#endregion InnerTypes

		#region Fields

		private readonly MessageBus bus;

		/// A universally unique identifier for the client (must not be zero).
		/// Used for routing replies back to the client via any network path (multi-path routing).
		/// The client ID must be ephemeral and random per process, and never persisted, so that
		/// lingering or zombie deployment processes cannot break correctness and/or liveness.
		/// A cryptographic random number generator must be used to ensure these properties.
		private readonly UInt128 id;

		/// The identifier for the cluster that this client intends to communicate with.
		private readonly uint cluster;

		/// The number of replicas in the cluster.
		private readonly byte replicaCount;

		/// We hash-chain request/reply checksums to verify linearizability within a client session:
		/// * so that the parent of the next request is the checksum of the latest reply, and
		/// * so that the parent of the next reply is the checksum of the latest request.
		private UInt128 parent;

		/// The session number for the client, zero when registering a session, non-zero thereafter.
		private ulong session;

		/// The request number of the next request.
		private uint requestNumber;

		/// The highest view number seen by the client in messages exchanged with the cluster.
		/// Used to locate the current leader, and provide more information to a partitioned leader.
		private uint view;

		/// The number of ticks without a reply before the client resends the inflight request.
		/// Dynamically adjusted as a function of recent request round-trip time.
		private Protocol.Timeout requestTimeout;

		/// The number of ticks before the client broadcasts a ping to the cluster.
		/// Used for end-to-end keepalive, and to discover a new leader between requests.
		private Protocol.Timeout pingTimeout;

		private ulong ticks;

		/// Used to calculate exponential backoff with random jitter.
		/// Seeded with the client's ID.
		private readonly Random prng;

		/// A client is allowed at most one inflight request at a time at the protocol layer.
		/// We therefore queue any further concurrent requests made by the application layer.
		/// We must leave one message free to receive with.
		private readonly Queue<RequestData> requestQueue;

		#endregion Fields

		#region Constructor

		public Client(uint cluster, IPEndPoint[] configuration)
		{
			if (configuration == null || configuration.Length == 0 || configuration.Length > Config.ReplicasMax) throw new ArgumentException(nameof(configuration));

			id = Guid.NewGuid();
			this.cluster = cluster;

			replicaCount = (byte)configuration.Length;
			prng = new Random(this.id.AsReadOnlySpan<int>()[0]);

			bus = new MessageBus(prng, cluster, configuration, OnMessageReceived);

			requestTimeout = new Protocol.Timeout
			{
				Id = id,
				Name = "request_timeout",
				After = Config.RttTicks * Config.RttMultiple
			};

			pingTimeout = new Protocol.Timeout
			{
				Id = id,
				Name = "ping_timeout",
				After = 30_000 / Config.TickMs
			};

			requestQueue = new Queue<RequestData>(Config.MessageBusMessagesMax - 1);

			//TODO:
			//pingTimeout.Start();
			//tickTimer = new Task(async () =>
			//{
			//    for (; ; )
			//    {
			//        Tick();
			//        await Task.Delay((int)Config.TickMs);
			//    }
			//}, TaskCreationOptions.LongRunning);
			//tickTimer.Start();
		}

		#endregion Constructor

		#region Methods

		public CreateAccountResult CreateAccount(Account account)
		{
			var ret = CallRequest<CreateAccountsResult, Account>(Operation.CreateAccounts, new[] { account });
			return ret.Length == 0 ? CreateAccountResult.Ok : ret[0].Result;
		}

		public CreateAccountsResult[] CreateAccounts(IEnumerable<Account> batch)
		{
			return CallRequest<CreateAccountsResult, Account>(Operation.CreateAccounts, batch);
		}

		public Task<CreateAccountResult> CreateAccountAsync(Account account)
		{
			var task = CallRequestAsync<CreateAccountsResult, Account>(Operation.CreateAccounts, new[] { account });
			return task.ContinueWith<CreateAccountResult>(x => x.Result.Length == 0 ? CreateAccountResult.Ok : x.Result[0].Result);
		}

		public Task<CreateAccountsResult[]> CreateAccountsAsync(IEnumerable<Account> batch)
		{
			return CallRequestAsync<CreateAccountsResult, Account>(Operation.CreateAccounts, batch);
		}

		public CreateTransferResult CreateTransfer(Transfer transfer)
		{
			var ret = CallRequest<CreateTransfersResult, Transfer>(Operation.CreateTransfers, new[] { transfer });
			return ret.Length == 0 ? CreateTransferResult.Ok : ret[0].Result;
		}

		public CreateTransfersResult[] CreateTransfers(IEnumerable<Transfer> batch)
		{
			return CallRequest<CreateTransfersResult, Transfer>(Operation.CreateTransfers, batch);
		}

		public Task<CreateTransferResult> CreateTransferAsync(Transfer transfer)
		{
			var task = CallRequestAsync<CreateTransfersResult, Transfer>(Operation.CreateTransfers, new[] { transfer });
			return task.ContinueWith<CreateTransferResult>(x => x.Result.Length == 0 ? CreateTransferResult.Ok : x.Result[0].Result);
		}

		public Task<CreateTransferResult[]> CreateTransfersAsync(IEnumerable<Transfer> batch)
		{
			return CallRequestAsync<CreateTransferResult, Transfer>(Operation.CreateTransfers, batch);
		}

		public CommitTransferResult CommitTransfer(Commit commit)
		{
			var ret = CallRequest<CommitTransfersResult, Commit>(Operation.CommitTransfers, new[] { commit });
			return ret.Length == 0 ? CommitTransferResult.Ok : ret[0].Result;
		}

		public CommitTransfersResult[] CommitTransfers(IEnumerable<Commit> batch)
		{
			return CallRequest<CommitTransfersResult, Commit>(Operation.CommitTransfers, batch);
		}

		public Task<CommitTransferResult> CommitTransferAsync(Commit commit)
		{
			var task = CallRequestAsync<CommitTransfersResult, Commit>(Operation.CommitTransfers, new[] { commit });
			return task.ContinueWith<CommitTransferResult>(x => x.Result.Length == 0 ? CommitTransferResult.Ok : x.Result[0].Result);
		}

		public Task<CommitTransfersResult[]> CommitTransfersAsync(IEnumerable<Commit> batch)
		{
			return CallRequestAsync<CommitTransfersResult, Commit>(Operation.CommitTransfers, batch);
		}

		public Account? LookupAccount(UInt128 id)
		{
			var ret = CallRequest<AccountData, UInt128>(Operation.LookupAccounts, new[] { id });
			return ret.Length == 0 ? null : new Account(ret[0]);
		}

		public Account[] LookupAccounts(IEnumerable<UInt128> ids)
		{
			var ret = CallRequest<AccountData, UInt128>(Operation.LookupAccounts, ids);
			return ret.Select(data => new Account(data)).ToArray();
		}

		public Task<Account?> LookupAccountAsync(UInt128 id)
		{
			var task = CallRequestAsync<AccountData, UInt128>(Operation.LookupAccounts, new[] { id });
			return task.ContinueWith<Account?>(x => x.Result.Length == 0 ? null : new Account(x.Result[0]));
		}

		public Task<Account[]> LookupAccountsAsync(IEnumerable<UInt128> ids)
		{
			var task = CallRequestAsync<AccountData, UInt128>(Operation.LookupAccounts, ids);
			return task.ContinueWith(x => x.Result.Select(data => new Account(data)).ToArray());
		}

		private TResult[] CallRequest<TResult, TBody>(Operation operation, IEnumerable<TBody> batch)
			where TBody : IData
			where TResult : struct
		{
			UInt128 actionId = Guid.NewGuid();
			TResult[]? result = null;
			void callback(UInt128 userData, Protocol.Operation action, ReadOnlySpan<byte> reply)
			{
				lock (this)
				{
					Trace.Assert(userData == actionId);
					Trace.Assert(action == operation);

					result = reply.Length == 0 ? Array.Empty<TResult>() : MemoryMarshal.Cast<byte, TResult>(reply).ToArray();
					Monitor.Pulse(this);
				}
			}

			Request(actionId, operation, callback, batch);

			lock (this)
			{
				Monitor.Wait(this);

				Trace.Assert(result != null);
				return result!;
			}
		}

		private Task<TResult[]> CallRequestAsync<TResult, TBody>(Operation operation, IEnumerable<TBody> batch)
			where TBody : IData
			where TResult : struct
		{
			UInt128 actionId = Guid.NewGuid();

			var completionSource = new TaskCompletionSource<TResult[]>();
			void callback(UInt128 userData, Protocol.Operation action, ReadOnlySpan<byte> reply)
			{
				Trace.Assert(userData == actionId);
				Trace.Assert(action == operation);

				var result = reply.Length == 0 ? Array.Empty<TResult>() : MemoryMarshal.Cast<byte, TResult>(reply).ToArray();
				completionSource!.SetResult(result);
			}

			Request(actionId, operation, callback, batch);
			return completionSource.Task;
		}

		public void Tick()
		{
			ticks += 1;

			bus.Tick();

			pingTimeout.Tick();
			requestTimeout.Tick();

			if (pingTimeout.Fired()) OnPingTimeout();
			if (requestTimeout.Fired()) OnRequestTimeout();
		}

		private void OnRequestTimeout()
		{
			requestTimeout.Backoff(prng);

			var message = requestQueue.Peek().message;
			Trace.Assert(message.Header.Command == Command.Request);
			Trace.Assert(message.Header.Request < requestNumber);
			Trace.Assert(message.Header.Checksum == parent);
			Trace.Assert(message.Header.Context == session);

			Trace.TraceInformation($"{id}: on_request_timeout: resending request={message.Header.Request} checksum={message.Header.Checksum}");

			// We assume the leader is down and round-robin through the cluster:
			var roundRobin = unchecked((byte)((view + requestTimeout.Attempts) % replicaCount));
			SendMessageToReplica(roundRobin, message);
		}

		private void OnPingTimeout()
		{
			pingTimeout.Reset();

			Message? ping = null;

			try
			{
				ping = bus.GetHeaderOnlyMessage();
				Trace.Assert(ping.References == 1);

				ping.Header.Command = Command.Ping;
				ping.Header.Cluster = cluster;
				ping.Header.Client = id;
				ping.Header.Size = HeaderData.SIZE;
				ping.Header.SetChecksumBody(ping.GetBody<byte>());
				ping.Header.SetChecksum();

				// TODO If we haven't received a pong from a replica since our last ping, then back off.
				SendHeaderToReplicas(ping);
			}
			finally
			{
				if (ping != null)
				{
					Trace.Assert(ping.References > 1);
					bus.Unref(ping);
				}
			}
		}

		private void SendHeaderToReplicas(Message message)
		{
			for (byte replica = 0; replica < replicaCount; replica++)
			{
				SendMessageToReplica(replica, message);
			}
		}

		private void Request<TBody>(UInt128 userData, Operation operation, ClientCallback callback, IEnumerable<TBody> body)
			where TBody : IData
		{
			Register();

			Message? message = null;
			try
			{
				message = bus.GetMessage();
				Trace.Assert(message.References == 1);

				// We will set parent, context, view and checksums only when sending for the first time:
				message.Header.Client = id;
				message.Header.Request = requestNumber;
				message.Header.Cluster = cluster;
				message.Header.Command = Command.Request;
				message.Header.Operation = (Operation)operation;
				message.SetBody(body);
				message.Header.SetChecksumBody(message.GetBody<byte>());
				message.Header.SetChecksum();

				Trace.Assert(requestNumber > 0);
				requestNumber += 1;

				Trace.TraceInformation($"{id}: request: user_data={userData} request={message.Header.Request} size={message.Header.Size} {operation}");

				var wasEmpty = requestQueue.Count == 0;

				requestQueue.Enqueue(new()
				{
					userData = userData,
					callback = callback,
					message = message.Ref(),
				});

				// If the queue was empty, then there is no request inflight and we must send this one:
				if (wasEmpty) SendRequestForTheFirstTime(message);
			}
			finally
			{
				if (message != null)
				{
					Trace.Assert(message.References > 1);
					bus.Unref(message);
				}
			}

		}

		private void OnMessageReceived(Message message)
		{
			Trace.TraceInformation($"{id}: on_message: {message.Header}");

			if (message.Header.GetInvalidMessage() is string reason)
			{
				Trace.TraceInformation($"{id}: on_message: invalid ({reason})");
				return;
			}

			if (message.Header.Cluster != cluster)
			{
				Trace.TraceWarning($"{id}: on_message: wrong cluster (cluster should be {cluster}, not {message.Header.Cluster})");
				return;
			}

			switch (message.Header.Command)
			{
				case Command.Pong:
					OnPong(message);
					break;

				case Command.Reply:
					OnReply(message);
					break;

				case Command.Eviction:
					OnEviction(message);
					break;

				default:
					Trace.TraceWarning($"{id}: on_message: ignoring misdirected {message.Header.Command} message");
					break;
			}
		}

		private void OnPong(Message pong)
		{
			Trace.Assert(pong.Header.Command == Command.Pong);
			Trace.Assert(pong.Header.Cluster == cluster);

			if (pong.Header.Client != 0)
			{
				Trace.TraceInformation($"{id}: on_pong: ignoring (client != 0)");
				return;
			}

			if (pong.Header.View > view)
			{
				Trace.TraceInformation($"{id}: on_pong: newer view={view}..{pong.Header.View}");
				view = pong.Header.View;
			}

			// Now that we know the view number, it's a good time to register if we haven't already:
			Register();
		}

		private void OnReply(Message reply)
		{
			// We check these checksums again here because this is the last time we get to downgrade
			// a correctness bug into a liveness bug, before we return data back to the application.
			Trace.Assert(reply.Header.IsValidChecksum());
			Trace.Assert(reply.Header.IsValidChecksumBody(reply.GetBody<byte>()));
			Trace.Assert(reply.Header.Command == Command.Reply);

			if (reply.Header.Client != id)
			{
				Trace.TraceInformation($"{id}: on_reply: ignoring (wrong client={reply.Header.Client})");
				return;
			}

			if (requestQueue.TryPeek(out RequestData inflight))
			{
				if (reply.Header.Request < inflight.message.Header.Request)
				{
					Trace.TraceInformation($"{id}: on_reply: ignoring (request {reply.Header.Request} < {inflight.message.Header.Request})");
					return;
				}
			}
			else
			{
				Trace.TraceInformation($"{id}: on_reply: ignoring (no inflight request)");
				return;
			}

			try
			{
				inflight = requestQueue.Dequeue();

				Trace.TraceInformation($"{id}: on_reply: user_data={inflight.userData} request={reply.Header.Request} size={reply.Header.Size} {reply.Header.Operation}");

				Trace.Assert(reply.Header.Parent == parent);
				Trace.Assert(reply.Header.Client == id);
				Trace.Assert(reply.Header.Context == 0);
				Trace.Assert(reply.Header.Request == inflight.message.Header.Request);
				Trace.Assert(reply.Header.Cluster == cluster);
				Trace.Assert(reply.Header.Op == reply.Header.Commit);
				Trace.Assert(reply.Header.Operation == inflight.message.Header.Operation);

				// The checksum of this reply becomes the parent of our next request:
				parent = reply.Header.Checksum;

				if (reply.Header.View > view)
				{
					Trace.TraceInformation($"{id}: on_reply: newer view={view}..{reply.Header.View}");
					view = reply.Header.View;
				}

				//TODO
				//self.request_timeout.stop();

				if (inflight.message.Header.Operation == Operation.Register)
				{
					Trace.Assert(session == 0);
					Trace.Assert(reply.Header.Commit > 0);

					session = reply.Header.Commit; // The commit number becomes the session number.
				}

				// We must process the next request before releasing control back to the callback.
				// Otherwise, requests may run through send_request_for_the_first_time() more than once.
				if (requestQueue.TryPeek(out RequestData nextRequest))
				{
					SendRequestForTheFirstTime(nextRequest.message);
				}

				if (inflight.message.Header.Operation != Operation.Register)
				{
					Trace.Assert(inflight.callback != null);
					inflight.callback!(inflight.userData, inflight.message.Header.Operation, reply.GetBody<byte>());
				}
			}
			finally
			{
				bus.Unref(inflight.message);
			}

		}

		private void OnEviction(Message eviction)
		{
			Trace.Assert(eviction.Header.Command == Command.Eviction);
			Trace.Assert(eviction.Header.Cluster == cluster);

			if (eviction.Header.Client != id)
			{
				Trace.TraceWarning($"{id}: on_eviction: ignoring (wrong client={eviction.Header.Client})");
				return;
			}

			if (eviction.Header.View < view)
			{
				Trace.TraceInformation($"{id}: on_eviction: ignoring (older view={eviction.Header.View})");
				return;
			}

			Trace.Assert(eviction.Header.Client == id);
			Trace.Assert(eviction.Header.View >= view);

			Trace.TraceError($"{id}: session evicted: too many concurrent client sessions");
			throw new InvalidOperationException("session evicted: too many concurrent client sessions");
		}

		/// Registers a session with the cluster for the client, if this has not yet been done.
		private void Register()
		{
			if (requestNumber > 0) return;

			Message? message = null;
			try
			{
				message = bus.GetMessage();
				message.Header.Client = id;
				message.Header.Request = requestNumber;
				message.Header.Cluster = cluster;
				message.Header.Command = Command.Request;
				message.Header.Operation = Operation.Register;

				Trace.Assert(requestNumber == 0);
				requestNumber += 1;

				Trace.TraceInformation($"{id}: register: registering a session with the cluster");

				Trace.Assert(requestQueue.Count == 0);

				requestQueue.Enqueue(new()
				{
					userData = UInt128.Zero,
					callback = null,
					message = message.Ref(),
				});

				SendRequestForTheFirstTime(message);
			}
			finally
			{
				if (message != null)
				{
					Trace.Assert(message.References > 1);
					bus.Unref(message);
				}
			}
		}

		private void SendRequestForTheFirstTime(Message message)
		{
			Trace.Assert(requestQueue.Count > 0);
			Trace.Assert(requestQueue.Peek().message == message);

			Trace.Assert(message.Header.Command == Command.Request);
			Trace.Assert(message.Header.Parent == 0);
			Trace.Assert(message.Header.Context == 0);
			Trace.Assert(message.Header.Request < requestNumber);
			Trace.Assert(message.Header.View == 0);
			Trace.Assert(message.Header.Size <= Config.MessageSizeMax);

			// We set the message checksums only when sending the request for the first time,
			// which is when we have the checksum of the latest reply available to set as `parent`,
			// and similarly also the session number if requests were queued while registering:
			message.Header.Parent = parent;
			message.Header.Context = session;

			// We also try to include our highest view number, so we wait until the request is ready
			// to be sent for the first time. However, beyond that, it is not necessary to update
			// the view number again, for example if it should change between now and resending.
			message.Header.View = view;
			message.Header.SetChecksumBody(message.GetBody<byte>());
			message.Header.SetChecksum();

			// The checksum of this request becomes the parent of our next reply:
			parent = message.Header.Checksum;

			Trace.TraceInformation($"{id}: send_request_for_the_first_time: request={message.Header.Request} checksum={message.Header.Checksum}");

			//TODO:
			//Trace.Assert(!requestTimeout.ticking);
			//self.request_timeout.start();

			// If our view number is out of date, then the old leader will forward our request.
			// If the leader is offline, then our request timeout will fire and we will round-robin.
			SendMessageToReplica(unchecked((byte)(view % replicaCount)), message);
		}

		private void SendMessageToReplica(byte replica, Message message)
		{
			Trace.TraceInformation($"{id}: sending {message.Header.Command} to replica {replica}: {message.Header}");

			Trace.Assert(replica < replicaCount);
			Trace.Assert(message.Header.IsValidChecksum());
			Trace.Assert(message.Header.Client == id);
			Trace.Assert(message.Header.Cluster == cluster);

			bus.SendToReplica(replica, message);
		}

		#endregion Methods
	}
}

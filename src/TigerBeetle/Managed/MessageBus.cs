using System;
using System.Diagnostics;
using System.Net;

namespace TigerBeetle.Managed
{
	internal sealed class MessageBus
	{
		#region Fields

		internal readonly IO io;

		private readonly MessagePool pool;

		private readonly uint cluster;

		private readonly IPEndPoint[] configuration;

		/// This slice is allocated with a fixed size in the init function and never reallocated.
		private readonly Connection[] connections;

		/// Number of connections currently in use (i.e. connection.peer != .none).
		private int connectionsUsed;

		/// Map from replica index to the currently active connection for that replica, if any.
		/// The connection for the process replica if any will always be null.

		private readonly Connection?[] replicas;

		/// The number of outgoing `connect()` attempts for a given replica:
		/// Reset to zero after a successful `on_connect()`.

		private readonly ulong[] replicasConnectAttempts;

		private readonly Random prng;

		private readonly Action<Message> messageReceivedCallback;

		#endregion Fields

		#region Properties

		public uint Cluster => cluster;

		public Random Prgn => prng;

		public IPEndPoint[] Configuration => configuration;

		/// Number of connections currently in use (i.e. connection.peer != .none).
		public int ConnectionsUsed { get => connectionsUsed; internal set => connectionsUsed = value; }

		internal Connection?[] Replicas => replicas;

		internal ulong[] ReplicasConnectAttempts => replicasConnectAttempts;

		#endregion Properties

		#region Constructor

		public MessageBus(Random prgn, uint cluster, IPEndPoint[] configuration, Action<Message> messageReceivedCallback)
		{
			Trace.Assert(Config.ConnectionsMax > configuration.Length);

			this.cluster = cluster;
			connections = new Connection[Config.ConnectionsMax];

			for (int i = 0; i < connections.Length; i++)
			{
				connections[i] = new Connection();
			}

			replicas = new Connection[configuration.Length];
			replicasConnectAttempts = new ulong[configuration.Length];

			pool = new MessagePool();

			this.configuration = configuration;
			this.prng = prgn;

			this.messageReceivedCallback = messageReceivedCallback;
			this.io = new IO();
		}

		#endregion Constructor

		#region Methods

		public Message GetMessage() => pool.GetMessage();

		public Message GetHeaderOnlyMessage() => pool.GetHeaderOnlyMessage();

		public void Tick()
		{
			// The client connects to all replicas.
			for (byte replica = 0; replica < replicas.Length; replica++)
			{
				MaybeConnectToReplica(replica);
			}
		}

		public void SendToReplica(byte replica, Message message)
		{
			if (replicas[replica] is Connection connection)
			{
				connection.SendMessage(this, message);
			}
			else
			{
				Trace.TraceInformation($"no active connection to replica {replica}");
			}
		}

		public void MaybeConnectToReplica(byte replica)
		{
			// We already have a connection to the given replica.
			if (replicas[replica] != null)
			{
				Trace.Assert(connectionsUsed > 0);
				return;
			}

			// Obtain a connection struct for our new replica connection.
			// If there is a free connection, use that. Otherwise drop
			// a client or unknown connection to make space. Prefer dropping
			// a client connection to an unknown one as the unknown peer may
			// be a replica. Since shutting a connection down does not happen
			// instantly, simply return after starting the shutdown and try again
			// on the next tick().
			foreach (var connection in connections)
			{
				if (connection!.State == ConnectionState.Free)
				{
					Trace.Assert(connection.Peer == ConnectionPeer.None);

					// This will immediately add the connection to bus.replicas,
					// or else will return early if a socket file descriptor cannot be obtained:
					// TODO See if we can clean this up to remove/expose the early return branch.
					connection.ConnectToReplica(this, (byte)replica);
					return;
				}
			}
		}

		public void Unref(Message message) => pool.Unref(message);

		public void OnMessage(Message message) => messageReceivedCallback(message);

		#endregion Methods
	}


}


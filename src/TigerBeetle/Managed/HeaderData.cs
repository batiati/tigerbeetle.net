using System.Runtime.InteropServices;

namespace TigerBeetle.Managed
{
	/// Network message and journal entry header:
	/// We reuse the same header for both so that prepare messages from the leader can simply be
	/// journalled as is by the followers without requiring any further modification.

	[StructLayout(LayoutKind.Explicit, Size = SIZE)]
	internal struct HeaderData
	{
		#region Fields

		public const int SIZE = 128;

		#region Documentation

		/// A checksum covering only the remainder of this header.
		/// This allows the header to be trusted without having to recv() or read() the associated body.
		/// This checksum is enough to uniquely identify a network message or journal entry.

		#endregion Documentation

		[FieldOffset(0)]
		public UInt128 checksum;

		#region Documentation

		/// A checksum covering only the associated body after this header.

		#endregion Documentation

		[FieldOffset(16)]
		public UInt128 checksumBody;

		#region Documentation

		/// A backpointer to the previous request or prepare checksum for hash chain verification.
		/// This provides a cryptographic guarantee for linearizability:
		/// 1. across our distributed log of prepares, and
		/// 2. across a client's requests and our replies.
		/// This may also be used as the initialization vector for AEAD encryption at rest, provided
		/// that the leader ratchets the encryption key every view change to ensure that prepares
		/// reordered through a view change never repeat the same IV for the same encryption key.

		#endregion Documentation

		[FieldOffset(32)]
		public UInt128 parent;

		#region Documentation

		/// Each client process generates a unique, random and ephemeral client ID at initialization.
		/// The client ID identifies connections made by the client to the cluster for the sake of
		/// routing messages back to the client.
		///
		/// With the client ID in hand, the client then registers a monotonically increasing session
		/// number (committed through the cluster) to allow the client's session to be evicted safely
		/// from the client table if too many concurrent clients cause the client table to overflow.
		/// The monotonically increasing session number prevents duplicate client requests from being
		/// replayed.
		///
		/// The problem of routing is therefore solved by the 128-bit client ID, and the problem of
		/// detecting whether a session has been evicted is solved by the session number.

		#endregion Documentation

		[FieldOffset(48)]
		public UInt128 client;

		#region Documentation

		/// The checksum of the message to which this message refers, or a unique recovery nonce.
		///
		/// We use this cryptographic context in various ways, for example:
		///
		/// * A `request` sets this to the client's session number.
		/// * A `prepare` sets this to the checksum of the client's request.
		/// * A `prepare_ok` sets this to the checksum of the prepare being acked.
		/// * A `commit` sets this to the checksum of the latest committed prepare.
		/// * A `request_prepare` sets this to the checksum of the prepare being requested.
		/// * A `nack_prepare` sets this to the checksum of the prepare being nacked.
		///
		/// This allows for cryptographic guarantees beyond request, op, and commit numbers, which have
		/// low entropy and may otherwise collide in the event of any correctness bugs.

		#endregion Documentation

		[FieldOffset(64)]
		public UInt128 context;

		#region Documentation

		/// Each request is given a number by the client and later requests must have larger numbers
		/// than earlier ones. The request number is used by the replicas to avoid running requests more
		/// than once; it is also used by the client to discard duplicate responses to its requests.
		/// A client is allowed to have at most one request inflight at a time.

		#endregion Documentation

		[FieldOffset(80)]
		public uint request;

		#region Documentation

		/// The cluster number binds intention into the header, so that a client or replica can indicate
		/// the cluster it believes it is speaking to, instead of accidentally talking to the wrong
		/// cluster (for example, staging vs production).

		#endregion Documentation

		[FieldOffset(84)]
		public uint cluster;

		#region Documentation

		/// The cluster reconfiguration epoch number (for future use).

		#endregion Documentation

		[FieldOffset(88)]
		public uint epoch;

		#region Documentation

		/// Every message sent from one replica to another contains the sending replica's current view.
		/// A `u32` allows for a minimum lifetime of 136 years at a rate of one view change per second.

		#endregion Documentation

		[FieldOffset(92)]
		public uint view;

		#region Documentation

		/// The op number of the latest prepare that may or may not yet be committed. Uncommitted ops
		/// may be replaced by different ops if they do not survive through a view change.

		#endregion Documentation

		[FieldOffset(96)]
		public ulong op;

		#region Documentation

		/// The commit number of the latest committed prepare. Committed ops are immutable.

		#endregion Documentation

		[FieldOffset(104)]
		public ulong commit;

		#region Documentation

		/// The journal offset to which this message relates. This enables direct access to a prepare in
		/// storage, without yet having any previous prepares. All prepares are of variable size, since
		/// a prepare may contain any number of data structures (even if these are of fixed size).

		#endregion Documentation

		[FieldOffset(112)]
		public ulong offset;

		#region Documentation

		/// The size of the Header structure (always), plus any associated body.

		#endregion Documentation

		[FieldOffset(120)]
		public int size;

		#region Documentation

		/// The index of the replica in the cluster configuration array that authored this message.
		/// This identifies only the ultimate author because messages may be forwarded amongst replicas.

		#endregion Documentation

		[FieldOffset(124)]
		public byte replica;

		#region Documentation

		/// The Viewstamped Replication protocol command for this message.

		#endregion Documentation

		[FieldOffset(125)]
		public Command command;

		#region Documentation

		/// The state machine operation to apply.

		#endregion Documentation

		[FieldOffset(126)]
		public Operation operation;

		#region Documentation

		/// The version of the protocol implementation that originated this message.

		#endregion Documentation

		[FieldOffset(127)]
		public byte version;

		#endregion Fields
	}
}


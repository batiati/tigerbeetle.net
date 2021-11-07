using System;
using System.Diagnostics;

namespace TigerBeetle.Protocol
{
	internal unsafe class Header
	{
		#region Fields

		public const int VERSION = 0;

		private HeaderData* ptr;

		#endregion Fields

		#region Constructor

		public Header(ref HeaderData header)
		{
			fixed (HeaderData* ptr = &header)
			{
				this.ptr = ptr;
			}
		}

		#endregion Constructor

		#region Properties

		public UInt128 Checksum { get => ptr->checksum; set => ptr->checksum = value; }

		public UInt128 ChecksumBody { get => ptr->checksumBody; set => ptr->checksumBody = value; }

		public UInt128 Parent { get => ptr->parent; set => ptr->parent = value; }

		public UInt128 Client { get => ptr->client; set => ptr->client = value; }

		public UInt128 Context { get => ptr->context; set => ptr->context = value; }

		public uint Request { get => ptr->request; set => ptr->request = value; }

		public uint Cluster { get => ptr->cluster; set => ptr->cluster = value; }

		public uint Epoch { get => ptr->epoch; set => ptr->epoch = value; }

		public uint View { get => ptr->view; set => ptr->view = value; }

		public ulong Op { get => ptr->op; set => ptr->op = value; }

		public ulong Commit { get => ptr->commit; set => ptr->commit = value; }

		public ulong Offset { get => ptr->offset; set => ptr->offset = value; }

		public int Size { get => ptr->size; set => ptr->size = value; }

		public byte Replica { get => ptr->replica; set => ptr->replica = value; }

		public Command Command { get => ptr->command; set => ptr->command = value; }

		public Operation Operation { get => ptr->operation; set => ptr->operation = value; }

		public byte Version { get => ptr->version; set => ptr->version = value; }

		#endregion Properties

		#region Methods

		public void SetChecksum()
		{
			ptr->checksum = CalculateChecksum();
		}

		public void SetChecksumBody(ReadOnlySpan<byte> body)
		{
			ptr->checksumBody = CalculateChecksumBody(body);
		}

		private UInt128 CalculateChecksum()
		{
			var input = new ReadOnlySpan<byte>(ptr, HeaderData.SIZE).Slice(UInt128.SIZE);
			return CalculateBlake3(input);
		}

		private UInt128 CalculateBlake3(ReadOnlySpan<byte> input)
		{
			using var blake3 = new Blake3Core.Blake3();

			Span<byte> output = stackalloc byte[64];
			var success = blake3.TryComputeHash(input, output, out int _);
			if (!success) throw new InvalidOperationException("Hash error");

			return new UInt128(output);
		}

		private UInt128 CalculateChecksumBody(ReadOnlySpan<byte> body) => CalculateBlake3(body);

		public bool IsValidChecksum()
		{
			return Checksum == CalculateChecksum();
		}

		public bool IsValidChecksumBody(ReadOnlySpan<byte> body)
		{
			return ChecksumBody == CalculateChecksumBody(body);
		}

		public PeerType GetPeerType()
		{
			switch (Command)
			{
				case Command.Reserved: throw new InvalidOperationException();

				// These messages cannot always identify the peer as they may be forwarded:
				case Command.Request:

					return Operation switch
					{
						// However, we do not forward the first .register request sent by a client:
						Operation.Register => PeerType.Client,
						_ => PeerType.Unknown,
					};

				case Command.Prepare: return PeerType.Unknown;

				// These messages identify the peer as either a replica or a client:
				// TODO Assert that pong responses from a replica do not echo the pinging client's ID.
				case Command.Ping:
				case Command.Pong:

					if (Client != UInt128.Zero)
					{
						Trace.Assert(Replica == 0);
						return PeerType.Client;
					}
					else
					{
						return PeerType.Replica;
					}

				// All other messages identify the peer as a replica:
				default: return PeerType.Replica;

			}
		}

		#region Comments

		/// Returns null if all fields are set correctly according to the command, or else a warning.
		/// This does not verify that checksum is valid, and expects that this has already been done.

		#endregion Comments

		public string? GetInvalidMessage()
		{
			if (Version != VERSION) return "version != Version";
			if (Size < HeaderData.SIZE) return "size < @sizeOf(Header)";
			if (Epoch != 0) return "epoch != 0";
			return Command switch
			{
				Command.Reserved => GetInvalidReservedMessage(),
				Command.Request => GetInvalidRequestMessage(),
				Command.Prepare => GetInvalidPrepareMessage(),
				Command.PrepareOk => GetInvalidPrepareOkMessage(),
				_ => null // TODO Add validators for all commands.
			};
		}

		private string? GetInvalidReservedMessage()
		{
			if (Parent != UInt128.Zero) return "parent != 0";
			if (Client != UInt128.Zero) return "client != 0";
			if (Context != UInt128.Zero) return "context != 0";
			if (Request != 0) return "request != 0";
			if (Cluster != 0) return "cluster != 0";
			if (View != 0) return "view != 0";
			if (Op != 0) return "op != 0";
			if (Commit != 0) return "commit != 0";
			if (Offset != 0) return "offset != 0";
			if (Replica != 0) return "replica != 0";
			if (Operation != Operation.Reserved) return "operation != .reserved";
			return null;
		}

		private string? GetInvalidRequestMessage()
		{
			if (Client == UInt128.Zero) return "client == 0";
			if (Op != 0) return "op != 0";
			if (Commit != 0) return "commit != 0";
			if (Offset != 0) return "offset != 0";
			if (Replica != 0) return "replica != 0";

			switch (Operation)
			{
				case Operation.Reserved: return "operation == .reserved";
				case Operation.Init: return "operation == .init";
				case Operation.Register:

					// The first request a client makes must be to register with the cluster:
					if (Parent != UInt128.Zero) return "parent != 0";
					if (Context != UInt128.Zero) return "context != 0";
					if (Request != 0) return "request != 0";
					// The .register operation carries no payload:
					if (Size != HeaderData.SIZE) return "size != @sizeOf(Header)";
					break;

				default:

					// Thereafter, the client must provide the session number in the context:
					// These requests should set `parent` to the `checksum` of the previous reply.
					if (Context == UInt128.Zero) return "context == 0";
					if (Request == 0) return "request == 0";
					break;
			}

			return null;
		}

		private string? GetInvalidPrepareMessage()
		{
			switch (Operation)
			{
				case Operation.Reserved: return "operation == .reserved";
				case Operation.Init:
					if (Parent != UInt128.Zero) return "init: parent != 0";
					if (Client != UInt128.Zero) return "init: client != 0";
					if (Context != UInt128.Zero) return "init: context != 0";
					if (Request != 0) return "init: request != 0";
					if (View != 0) return "init: view != 0";
					if (Op != 0) return "init: op != 0";
					if (Commit != 0) return "init: commit != 0";
					if (Offset != 0) return "init: offset != 0";
					if (Size != HeaderData.SIZE) return "init: size != @sizeOf(Header)";
					if (Replica != 0) return "init: replica != 0";
					break;

				default:
					if (Client == UInt128.Zero) return "client == 0";
					if (Op == 0) return "op == 0";
					if (Op <= Commit) return "op <= commit";
					if (Operation == Operation.Register)
					{
						// Client session numbers are replaced by the reference to the previous prepare.
						if (Request != 0) return "request != 0";
					}
					else
					{
						// Client session numbers are replaced by the reference to the previous prepare.
						if (Request == 0) return "request == 0";
					}
					break;
			}

			return null;
		}

		private string? GetInvalidPrepareOkMessage()
		{
			if (Size != HeaderData.SIZE) return "size != @sizeOf(Header)";

			switch (Operation)
			{
				case Operation.Reserved: return "operation == .reserved";
				case Operation.Init:

					if (Parent != UInt128.Zero) return "init: parent != 0";
					if (Client != UInt128.Zero) return "init: client != 0";
					if (Context != UInt128.Zero) return "init: context != 0";
					if (Request != 0) return "init: request != 0";
					if (View != 0) return "init: view != 0";
					if (Op != 0) return "init: op != 0";
					if (Commit != 0) return "init: commit != 0";
					if (Offset != 0) return "init: offset != 0";
					if (Replica != 0) return "init: replica != 0";
					break;

				default:
					if (Client == UInt128.Zero) return "client == 0";
					if (Op == 0) return "op == 0";
					if (Op <= Commit) return "op <= commit";
					if (Operation == Operation.Register)
					{
						if (Request != 0) return "request != 0";
					}
					else
					{
						if (Request == 0) return "request == 0";
					}
					break;
			}

			return null;
		}

		#endregion Methods
	}
}


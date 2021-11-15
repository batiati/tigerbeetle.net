using System;

namespace TigerBeetle
{
	public sealed class Transfer : IData
	{
		#region Fields

		private TransferData data;

		#endregion Fields

		#region Constructor

		public Transfer()
		{
			data = new TransferData();
		}

		internal Transfer(TransferData data)
		{
			this.data = data;
		}

		#endregion Constructor

		#region Properties

		public UInt128 Id
		{
			get => data.id;
			set => data.id = value;
		}

		public UInt128 DebitAccountId
		{
			get => data.debitAccountId;
			set => data.debitAccountId = value;
		}

		public UInt128 CreditAccountId
		{
			get => data.creditAccountId;
			set => data.creditAccountId = value;
		}

		public UInt128 UserData
		{
			get => data.userData;
			set => data.userData = value;
		}

		public ReadOnlySpan<byte> Reserved
		{
			get => data.reserved.AsReadOnlySpan<byte>();
			set => value.CopyTo(data.reserved.AsSpan<byte>());
		}

		public ulong Timeout
		{
			get => data.timeout;
			set => data.timeout = value;
		}

		public uint Code
		{
			get => data.code;
			set => data.code = value;
		}

		public TransferFlags Flags
		{
			get => data.flags;
			set => data.flags = value;
		}

		public ulong Amount
		{
			get => data.amount;
			set => data.amount = value;
		}

		public ulong Timestamp
		{
			get => data.timestamp;
			set => data.timestamp = value;
		}

		#endregion Properties

		#region Methods

		#region IReadBuffer

		public ReadOnlySpan<byte> AsReadOnlySpan()
		{
			unsafe
			{
				fixed (void* ptr = &data)
				{
					return new ReadOnlySpan<byte>(ptr, TransferData.SIZE);
				}
			}
		}

		#endregion IReadBuffer

		#endregion Methods
	}
}

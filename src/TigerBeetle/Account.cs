using System;

namespace TigerBeetle
{
	public sealed class Account : IData
	{
		#region Fields

		private AccountData data;

		#endregion Fields

		#region Constructor

		public Account()
		{
			data = new AccountData();
		}

		internal Account(AccountData data)
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

		public UInt128 UserData
		{
			get => data.userData;
			set => data.userData = value;
		}

		public ReadOnlySpan<byte> Reserved
		{
			get => data.reserved.ToReadOnlySpan<byte>();
			set => value.CopyTo(data.reserved.ToSpan<byte>());
		}

		public ushort Unit
		{
			get => data.unit;
			set => data.unit = value;
		}

		public ushort Code
		{
			get => data.code;
			set => data.code = value;
		}

		public AccountFlags Flags
		{
			get => data.flags;
			set => data.flags = value;
		}

		public ulong DebitsReserved
		{
			get => data.debitsReserved;
			set => data.debitsReserved = value;
		}

		public ulong DebitsAccepted
		{
			get => data.debitsAccepted;
			set => data.debitsAccepted = value;
		}

		public ulong CreditsReserved
		{
			get => data.creditsReserved;
			set => data.creditsReserved = value;
		}

		public ulong CreditsAccepted
		{
			get => data.creditsAccepted;
			set => data.creditsAccepted = value;
		}

		public ulong Timestamp
		{
			get => data.timestamp;
			set => data.timestamp = value;
		}

		#endregion Properties

		#region Methods

		public override bool Equals(object? obj)
		{
			return obj is Account account &&
					AsReadOnlySpan().SequenceEqual(account.AsReadOnlySpan());
		}

		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}

		#endregion Methods

		#region IReadBuffer

		public ReadOnlySpan<byte> AsReadOnlySpan()
		{
			unsafe
			{
				fixed (void* ptr = &data)
				{
					return new ReadOnlySpan<byte>(ptr, AccountData.SIZE);
				}
			}
		}

		#endregion IReadBuffer
	}
}

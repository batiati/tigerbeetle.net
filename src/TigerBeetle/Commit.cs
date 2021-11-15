using System;

namespace TigerBeetle
{
	public sealed class Commit : IData
	{
		#region Fields

		private CommitData data;

		#endregion Fields

		#region Constructor

		public Commit()
		{
			data = new CommitData();
		}

		internal Commit(CommitData data)
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

		public ReadOnlySpan<byte> Reserved
		{
			get => data.reserved.AsReadOnlySpan<byte>();
			set => value.CopyTo(data.reserved.AsSpan<byte>());
		}

		public uint Code
		{
			get => data.code;
			set => data.code = value;
		}

		public CommitFlags Flags
		{
			get => data.flags;
			set => data.flags = value;
		}

		public ulong Timestamp
		{
			get => data.timestamp;
			set => data.timestamp = value;
		}

		#endregion Properties

		#region IReadBuffer

		public ReadOnlySpan<byte> AsReadOnlySpan()
		{
			unsafe
			{
				fixed (void* ptr = &data)
				{
					return new ReadOnlySpan<byte>(ptr, CommitData.SIZE);
				}
			}
		}

		#endregion IReadBuffer
	}
}

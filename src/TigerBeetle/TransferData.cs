using System;
using System.Runtime.InteropServices;

namespace TigerBeetle
{
	[StructLayout(LayoutKind.Explicit, Size = SIZE)]
	internal struct TransferData
	{
		#region InnerTypes

		[StructLayout(LayoutKind.Explicit, Size = SIZE)]
		public unsafe struct ReservedData
		{
			public const int SIZE = 32;

			[FieldOffset(0)]
			private fixed byte raw[32];

			public ReadOnlySpan<T> AsReadOnlySpan<T>()
			{
				fixed (void* ptr = raw)
				{
					return new ReadOnlySpan<T>(ptr, SIZE);
				}
			}

			public Span<T> AsSpan<T>()
			{
				fixed (void* ptr = raw)
				{
					return new Span<T>(ptr, SIZE);
				}
			}
		}

		#endregion

		#region Fields

		public const int SIZE = 128;

		[FieldOffset(0)]
		public UInt128 id;

		[FieldOffset(16)]
		public UInt128 debitAccountId;

		[FieldOffset(32)]
		public UInt128 creditAccountId;

		[FieldOffset(48)]
		public UInt128 userData;

		[FieldOffset(64)]
		public ReservedData reserved;

		[FieldOffset(96)]
		public ulong timeout;

		[FieldOffset(104)]
		public uint code;

		[FieldOffset(108)]
		public TransferFlags flags;

		[FieldOffset(112)]
		public ulong amount;

		[FieldOffset(120)]
		public ulong timestamp;

		#endregion Fields
	}
}

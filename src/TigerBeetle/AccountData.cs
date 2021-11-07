using System;
using System.Runtime.InteropServices;

namespace TigerBeetle
{
	[StructLayout(LayoutKind.Explicit, Size = SIZE)]
	internal struct AccountData
	{
		#region InnerTypes

		[StructLayout(LayoutKind.Explicit, Size = SIZE)]
		public unsafe struct ReservedData
		{
			public const int SIZE = 48;

			[FieldOffset(0)]
			private fixed byte raw[SIZE];

			public ReadOnlySpan<T> ToReadOnlySpan<T>()
			{
				fixed (void* ptr = &this)
				{
					return new ReadOnlySpan<T>(ptr, SIZE);
				}
			}

			public Span<T> ToSpan<T>()
			{
				fixed (void* ptr = &this)
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
		public UInt128 userData;

		[FieldOffset(32)]
		public ReservedData reserved;

		[FieldOffset(80)]
		public ushort unit;

		[FieldOffset(82)]
		public ushort code;

		[FieldOffset(84)]
		public AccountFlags flags;

		[FieldOffset(88)]
		public ulong debitsReserved;

		[FieldOffset(96)]
		public ulong debitsAccepted;

		[FieldOffset(104)]
		public ulong creditsReserved;

		[FieldOffset(112)]
		public ulong creditsAccepted;

		[FieldOffset(120)]
		public ulong timestamp;

		#endregion Fields
	}

}

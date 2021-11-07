using System;
using System.Runtime.InteropServices;

namespace TigerBeetle
{
	[StructLayout(LayoutKind.Explicit, Size = SIZE)]
	internal struct CommitData
	{
		#region InnerTypes

		[StructLayout(LayoutKind.Explicit, Size = SIZE)]
		public unsafe struct ReservedData
		{
			public const int SIZE = 32;

			[FieldOffset(0)]
			private fixed byte raw[SIZE];

			public ReadOnlySpan<T> AsReadOnlySpan<T>()
			{
				fixed (void* ptr = &this)
				{
					return new ReadOnlySpan<T>(ptr, SIZE);
				}
			}

			public Span<T> AsSpan<T>()
			{
				fixed (void* ptr = &this)
				{
					return new Span<T>(ptr, SIZE);
				}
			}
		}

		#endregion InnerTypes

		#region Fields

		public const int SIZE = 64;

		[FieldOffset(0)]
		public UInt128 id;

		[FieldOffset(16)]
		public ReservedData reserved;

		[FieldOffset(48)]
		public uint code;

		[FieldOffset(52)]
		public CommitFlags flags;

		[FieldOffset(56)]
		public ulong timestamp;

		#endregion Fields
	}
}

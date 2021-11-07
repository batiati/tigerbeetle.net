using System;
using System.Runtime.InteropServices;

namespace TigerBeetle
{
	[StructLayout(LayoutKind.Explicit, Size = SIZE)]
	public unsafe struct UInt128 : Protocol.IData
	{
		#region Fields

		public const int SIZE = 16;

		public static readonly UInt128 Zero = new UInt128();

		[FieldOffset(0)]
		private ulong _0;

		[FieldOffset(8)]
		private ulong _1;

		#endregion Fields

		#region Constructor

		public UInt128(ReadOnlySpan<byte> bytes)
		{
			var values = MemoryMarshal.Cast<byte, ulong>(bytes);
			_0 = values[0];
			_1 = values[1];
		}

		public UInt128(Guid guid)
		{
			_0 = 0;
			_1 = 0;

			FromGuid(guid);
		}

		public UInt128(long a, long b = 0)
		{
			_0 = (ulong)a;
			_1 = (ulong)b;
		}

		public UInt128(ulong a, ulong b = 0)
		{
			_0 = a;
			_1 = b;
		}

		#endregion Constructor

		#region Methods

		public Guid ToGuid() => new(AsReadOnlySpan<byte>());

		public (long, long) ToInt64()
		{
			return ((long)_0, (long)_1);
		}

		public (ulong, ulong) ToUInt64()
		{
			return (_0, _1);
		}

		internal Span<T> AsSpan<T>()
		{
			fixed (void* ptr = &this)
			{
				return new Span<T>(ptr, SIZE / Marshal.SizeOf<T>());
			}
		}

		internal void FromGuid(Guid guid) => guid.TryWriteBytes(AsSpan<byte>());

		public override bool Equals(object obj)
		{
			if (obj is UInt128 other)
			{
				return _0 == other._0 && _1 == other._1;
			}
			else
			{
				return false;
			}

		}

		public override int GetHashCode()
		{
			return HashCode.Combine(_0, _1);
		}

		public override string ToString()
		{
			return ToGuid().ToString();
		}

		public static bool operator ==(UInt128 left, UInt128 right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(UInt128 left, UInt128 right)
		{
			return !(left == right);
		}

		public static implicit operator UInt128(Guid guid)
		{
			return new UInt128(guid);
		}

		public static implicit operator UInt128(long value)
		{
			return new UInt128(value, 0);
		}

		public static implicit operator UInt128(ulong value)
		{
			return new UInt128(value, 0);
		}

		public static implicit operator UInt128(int value)
		{
			return new UInt128(value, 0);
		}

		public static implicit operator UInt128(uint value)
		{
			return new UInt128(value, 0);
		}

		public ReadOnlySpan<byte> AsReadOnlySpan()
		{
			unsafe
			{
				fixed (void* ptr = &this)
				{
					return new ReadOnlySpan<byte>(ptr, SIZE);
				}
			}
		}

		internal ReadOnlySpan<T> AsReadOnlySpan<T>()
			where T : struct
		{
			return MemoryMarshal.Cast<byte, T>(AsReadOnlySpan());
		}

		#endregion Methods
	}
}

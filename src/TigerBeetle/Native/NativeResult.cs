namespace TigerBeetle.Native
{
	internal enum NativeResult : uint
	{
		SUCCESS = 0,
		ALREADY_INITIALIZED = 1,
		IO_URING_FAILED = 2,
		INVALID_ADDRESS = 3,
		ADDRESS_LIMIT_EXCEEDED = 4,
		INVALID_HANDLE = 5,
		MESSAGE_POOL_EXHAUSTED = 6,
		TICK_FAILED = 8,
		OUT_OF_MEMORY = 9,
	}
}

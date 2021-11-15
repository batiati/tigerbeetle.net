namespace TigerBeetle.Managed
{

	internal enum Command : byte
	{
		Reserved,

		Ping,
		Pong,

		Request,
		Prepare,
		PrepareOk,
		Reply,
		Commit,

		StartViewChange,
		DoViewChange,
		StartView,

		Recovery,
		RecoveryResponse,

		RequestStartView,
		RequestHeaders,
		RequestPrepare,
		Headers,
		NackPrepare,

		Eviction,
	}
}


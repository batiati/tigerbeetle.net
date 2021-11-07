using System;

namespace TigerBeetle
{
	[Flags]
	public enum CommitFlags : uint
	{
		None = 0x00,
		Linked = 0x01,
		Reject = 0x02,
		Preimage = 0x04
	}
}

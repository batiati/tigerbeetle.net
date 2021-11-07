using System;

namespace TigerBeetle
{
	[Flags]
	public enum TransferFlags : uint
	{
		None = 0x00,
		Linked = 0x01,
		TwoPhaseCommit = 0x02,
		Condition = 0x04,
	}
}

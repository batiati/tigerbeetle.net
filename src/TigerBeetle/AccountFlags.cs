using System;

namespace TigerBeetle
{
	[Flags]
	public enum AccountFlags : uint
	{
		None = 0x00,
		Linked = 0x01,
		DebitsMustNotExceedCredits = 0x02,
		CreditsMustNotExceedDebits = 0x04,
	}
}

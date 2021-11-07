namespace TigerBeetle
{
	public enum CreateAccountResult : uint
	{
		Ok,
		LinkedEventFailed,
		Exists,
		ExistsWithDifferentUserData,
		ExistsWithDifferentReservedField,
		ExistsWithDifferentUnit,
		ExistsWithDifferentCode,
		ExistsWithDifferentFlags,
		ExceedsCredits,
		ExceedsDebits,
		ReservedField,
		ReservedFlagPadding,
	}
}

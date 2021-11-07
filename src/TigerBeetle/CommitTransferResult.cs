namespace TigerBeetle
{
	public enum CommitTransferResult : uint
	{
		Ok,
		LinkedEventFailed,
		ReservedField,
		ReservedFlagPadding,
		TransferNotFound,
		TransferNotTwoPhaseCommit,
		TransferExpired,
		AlreadyCommitted,
		AlreadyCommittedButAccepted,
		AlreadyCommittedButRejected,
		DebitAccountNotFound,
		CreditAccountNotFound,
		DebitAmountWasNotReserved,
		CreditAmountWasNotReserved,
		ExceedsCredits,
		ExceedsDebits,
		ConditionRequiresPreimage,
		PreimageRequiresCondition,
		PreimageInvalid,
	}
}

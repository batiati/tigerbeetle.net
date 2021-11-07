﻿namespace TigerBeetle
{
	public enum CreateTransferResult : uint
	{
		Ok,
		LinkedEventFailed,
		Exists,
		ExistsWithDifferentDebitAccountId,
		ExistsWithDifferentCreditAccountId,
		ExistsWithDifferentUserData,
		ExistsWithDifferentReservedField,
		ExistsWithDifferentCode,
		ExistsWithDifferentAmount,
		ExistsWithDifferentTimeout,
		ExistsWithDifferentFlags,
		ExistsAndAlreadyCommittedAndAccepted,
		ExistsAndAlreadyCommittedAndRejected,
		ReservedField,
		ReservedFlagPadding,
		DebitAccountNotFound,
		CreditAccountNotFound,
		AccountsAreTheSame,
		AccountsHaveDifferentUnits,
		AmountIsZero,
		ExceedsCredits,
		ExceedsDebits,
		TwoPhaseCommitMustTimeout,
		TimeoutReservedForTwoPhaseCommit,
	}
}

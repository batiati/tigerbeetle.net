using System.Runtime.InteropServices;

namespace TigerBeetle
{
	[StructLayout(LayoutKind.Explicit, Size = 8)]
	public struct CommitTransfersResult
	{
		#region Fields

		[FieldOffset(0)]
		private readonly int index;

		[FieldOffset(4)]
		private readonly CommitTransferResult result;

		#endregion Fields

		#region Constructor

		internal CommitTransfersResult(int index, CommitTransferResult result = CommitTransferResult.Ok)
		{
			this.index = index;
			this.result = result;
		}

		#endregion Constructor

		#region Properties

		public int Index => index;

		public CommitTransferResult Result => result;

		#endregion Properties
	}
}

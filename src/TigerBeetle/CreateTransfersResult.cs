using System.Runtime.InteropServices;

namespace TigerBeetle
{
	[StructLayout(LayoutKind.Explicit, Size = 8)]
	public struct CreateTransfersResult
	{
		#region Fields

		[FieldOffset(0)]
		private readonly int index;

		[FieldOffset(4)]
		private readonly CreateTransferResult result;

		#endregion Fields

		#region Constructor

		internal CreateTransfersResult(int index, CreateTransferResult result = CreateTransferResult.Ok)
		{
			this.index = index;
			this.result = result;
		}

		#endregion Constructor

		#region Properties

		public int Index => index;

		public CreateTransferResult Result => result;

		#endregion Properties
	}
}

using System.Runtime.InteropServices;

namespace TigerBeetle
{
	[StructLayout(LayoutKind.Explicit, Size = 8)]
	public struct CreateAccountsResult
	{
		#region Fields

		[FieldOffset(0)]
		private readonly int index;

		[FieldOffset(4)]
		private readonly CreateAccountResult result;

		#endregion Fields

		#region Constructor

		internal CreateAccountsResult(int index, CreateAccountResult result = CreateAccountResult.Ok)
		{
			this.index = index;
			this.result = result;
		}

		#endregion Constructor

		#region Properties

		public int Index => index;

		public CreateAccountResult Result => result;

		#endregion Properties
	}
}

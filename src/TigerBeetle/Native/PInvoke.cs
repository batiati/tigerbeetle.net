using System;
using System.Runtime.InteropServices;

namespace TigerBeetle.Native
{
	internal static class PInvoke
	{
		#region InnerTypes

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void NativeCallback(Operation operation, IntPtr data, nint size);

		#endregion InnerTypes

		#region Fields

		private const string LIB_NAME = "tigerbeetle";

		#endregion Fields

		#region Methods

		[DllImport(LIB_NAME)]
		public static extern NativeResult TB_Init();

		[DllImport(LIB_NAME)]
		public static extern NativeResult TB_Deinit();

		[DllImport(LIB_NAME)]
		public static extern NativeResult TB_CreateClient(UInt128 clientId, uint cluster, [MarshalAs(UnmanagedType.LPStr)] string addresses_raw, ref IntPtr handle);

		[DllImport(LIB_NAME)]
		public static extern NativeResult TB_DestroyClient(IntPtr handle);

		[DllImport(LIB_NAME)]
		public static extern NativeResult TB_GetMessage(IntPtr handle, out IntPtr messageHandle, out IntPtr messageBodyBuffer, out nint messageBodyBufferLen);

		[DllImport(LIB_NAME)]
		public static extern NativeResult TB_UnrefMessage(IntPtr handle, IntPtr messageHandle);

		[DllImport(LIB_NAME)]
		public static extern NativeResult TB_Request(IntPtr handle, byte operation, IntPtr messageHandle, nint messageBodySize, [MarshalAs(UnmanagedType.FunctionPtr)] NativeCallback callback);

		[DllImport(LIB_NAME)]
		public static extern NativeResult TB_Tick(IntPtr handle);

		[DllImport(LIB_NAME)]
		public static extern NativeResult TB_RunFor(IntPtr handle, uint ms);

		#endregion Methods
	}
}

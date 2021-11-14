using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TigerBeetle.Protocol;

namespace TigerBeetle
{
	internal sealed class NativeClientImpl : IClientImpl
	{
		#region InnerTypes

		private static class PInvoke
		{
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

		private enum NativeResult : uint
		{
			SUCCESS = 0,
			ALREADY_INITIALIZED = 1,
			IO_URING_FAILED = 2,
			INVALID_ADDRESS = 3,
			ADDRESS_LIMIT_EXCEEDED = 4,
			INVALID_HANDLE = 5,
			MESSAGE_POOL_EXHAUSTED = 6,
			TICK_FAILED = 8,
			OUT_OF_MEMORY = 9,
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void NativeCallback(Operation operation, IntPtr data, nint size);

		#region Documentation

		/// <summary>
		/// An nnmanaged pointer to a message and body buffer
		/// </summary>

		#endregion Documentation

		private ref struct MessageHandle
		{
			#region Fields

			public readonly IntPtr handle;
			public readonly Span<byte> body;

			#endregion Fields

			#region Constructor

			public MessageHandle(IntPtr handle, Span<byte> body)
			{
				this.handle = handle;
				this.body = body;
			}

			#endregion Constructor
		}

		#region Documentation

		/// <summary>
		/// Just a holder to prevent GC on callback
		/// It is stored in the Task's asyncState
		/// </summary>

		#endregion Documentation

		private sealed class StateHolder
		{
			public NativeCallback? callback;
		}

		#endregion InnerTypes

		#region Fields

		private readonly IntPtr handle;
		private readonly UInt128 id;
		private readonly uint cluster;
		private readonly Thread tickTimer;

		#endregion Fields

		#region Properties

		public UInt128 Id => id;

		public uint Cluster => cluster;

		#endregion Properties

		#region Constructor

		public NativeClientImpl(uint cluster, IPEndPoint[] configuration)
		{
			if (configuration == null || configuration.Length == 0 || configuration.Length > Config.ReplicasMax) throw new ArgumentException(nameof(configuration));

			id = Guid.NewGuid();
			this.cluster = cluster;

			var addresses_raw = string.Join(',', configuration.Select(x => x.ToString()));

			NativeResult ret;

			ret = PInvoke.TB_Init();
			CheckResult(ret);

			ret = PInvoke.TB_CreateClient(id, cluster, addresses_raw, ref handle);
			CheckResult(ret);

			tickTimer = new Thread(OnTick);
			tickTimer.IsBackground = true;
			tickTimer.Start();
		}

		#endregion Constructor

		#region Methods

		public TResult[] CallRequest<TResult, TBody>(Operation operation, IEnumerable<TBody> batch)
			where TBody : IData
			where TResult : struct
		{
			TResult[]? result = null;
			void callback(Operation action, IntPtr ptr, nint len)
			{
				lock (this)
				{
					Trace.Assert(action == operation);

					unsafe
					{
						var reply = new ReadOnlySpan<byte>(ptr.ToPointer(), (int)len);
						result = reply.Length == 0 ? Array.Empty<TResult>() : MemoryMarshal.Cast<byte, TResult>(reply).ToArray();
					}

					Monitor.Pulse(this);
				}
			}

			Request(operation, callback, batch);

			lock (this)
			{
				Monitor.Wait(this);

				Trace.Assert(result != null);
				return result!;
			}
		}

		public Task<TResult[]> CallRequestAsync<TResult, TBody>(Operation operation, IEnumerable<TBody> batch)
			where TBody : IData
			where TResult : struct
		{
			var state = new StateHolder();
			var completionSource = new TaskCompletionSource<TResult[]>(state);

			void callback(Operation action, IntPtr ptr, nint len)
			{
				Trace.Assert(action == operation);

				unsafe
				{
					var reply = new ReadOnlySpan<byte>(ptr.ToPointer(), (int)len);
					var result = reply.Length == 0 ? Array.Empty<TResult>() : MemoryMarshal.Cast<byte, TResult>(reply).ToArray();
					completionSource!.SetResult(result);
				}
			}

			state.callback = callback;
			Request(operation, state.callback, batch);

			return completionSource.Task;
		}

		private void Request<TBody>(Operation operation, NativeCallback callback, IEnumerable<TBody> body)
			where TBody : IData
		{
			MessageHandle message = default;
			int size = 0;

			try
			{
				message = GetMessage();
				foreach (var item in body)
				{
					var bodySpan = item.AsReadOnlySpan();
					bodySpan.CopyTo(message.body.Slice(size));
					size += bodySpan.Length;
				}
			}
			catch
			{
				if (message.handle != IntPtr.Zero) PInvoke.TB_UnrefMessage(handle, message.handle);
			}

			var ret = PInvoke.TB_Request(handle, (byte)operation, message.handle, size, callback);
			CheckResult(ret);
		}

		private MessageHandle GetMessage()
		{
			unsafe
			{
				var ret = PInvoke.TB_GetMessage(handle, out IntPtr messageHandle, out IntPtr messageBodyBuffer, out nint messageBodyBufferLen);
				CheckResult(ret);

				return new MessageHandle(messageHandle, new Span<byte>(messageBodyBuffer.ToPointer(), (int)messageBodyBufferLen));
			}
		}

		private void CheckResult(NativeResult ret)
		{
			if (ret != NativeResult.SUCCESS) throw new Exception($"Invalid operation: result={ret}");
		}

		private void OnTick(object _)
		{
			for (; ; )
			{
				try
				{
					PInvoke.TB_Tick(handle);
					PInvoke.TB_RunFor(handle, 5);
				}
				catch (ThreadAbortException)
				{
					break;
				}
			}
		}

		#endregion Methods
	}
}

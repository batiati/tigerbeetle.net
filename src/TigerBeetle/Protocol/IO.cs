using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TigerBeetle.Protocol
{
	internal sealed class IO
	{
		#region InnerTypes

		private sealed class CompletionEventArgs : SocketAsyncEventArgs
		{
			#region Fields

			private readonly ManualResetEventSlim ev = new();

			#endregion Fields

			#region Properties

			public WaitHandle WaitHandle => ev.WaitHandle;

			public bool IsCompleted => ev.IsSet;

			#endregion Properties

			#region Methods

			public void Reset()
			{
				ev.Reset();
			}

			public void SyncSet()
			{
				ev.Set();
			}

			protected override void OnCompleted(SocketAsyncEventArgs e)
			{
				ev.Set();
			}

			#endregion Methods
		}

		private sealed class Completion
		{
			#region Fields

			private SocketAsyncOperation asyncOperation;
			private Socket? socket;
			private Action<SocketAsyncEventArgs>? callback;
			private readonly CompletionEventArgs e = new();

			#endregion Fields

			public SocketAsyncOperation AsyncOperation => asyncOperation;

			public WaitHandle WaitHandle => e.WaitHandle;

			public bool IsCompleted => e.IsCompleted;

			public void Submit(Socket socket, SocketAsyncOperation asyncOperation, Action<SocketAsyncEventArgs> callback, object? asyncState, Memory<byte> buffer = default, IPEndPoint? address = null)
			{
				Trace.Assert(this.socket == null);
				if (socket == null) throw new NullReferenceException();

				this.socket = socket;
				this.asyncOperation = asyncOperation;
				this.callback = callback;

				e.UserToken = asyncState;
				e.RemoteEndPoint = address;
				e.SetBuffer(buffer);

				bool pending;

				switch (asyncOperation)
				{
					case SocketAsyncOperation.Connect:
						pending = socket.ConnectAsync(e);
						break;

					case SocketAsyncOperation.Send:
						pending = socket.SendAsync(e);
						break;

					case SocketAsyncOperation.Receive:
						pending = socket.ReceiveAsync(e);
						break;

					default:
						throw new NotImplementedException();
				}

				if (!pending) e.SyncSet();
			}

			public void Complete()
			{
				if (callback == null) throw new NullReferenceException();

				callback(e);

				asyncOperation = SocketAsyncOperation.None;
				socket = null;
				callback = null;
				e.RemoteEndPoint = null;
				e.SetBuffer(default);
				e.Reset();
			}
		}

		#endregion InnerTypes

		#region Fields

		private const int POOL_SIZE = 128;
		private const int MAX_HAIT_HANDLES = 64 - 1;

		private readonly LinkedList<Completion> pool = new();
		private readonly LinkedList<Completion> submited = new();
		private readonly LinkedList<Completion> completed = new();

		private ManualResetEventSlim submitedEvent;
		private WaitHandle[][] waitHandlesPool;


		public void SetSubmissionWaitHandle(ManualResetEventSlim ev)
		{
			this.submitedEvent = ev;
			waitHandlesPool = new WaitHandle[MAX_HAIT_HANDLES][];

			for (int i = 0; i < MAX_HAIT_HANDLES; i++)
			{
				waitHandlesPool[i] = new WaitHandle[i + 1];
				waitHandlesPool[i][0] = ev.WaitHandle;
			}
		}

		#endregion Fields

		#region Constructor

		public IO()
		{
			for (int i = 0; i < POOL_SIZE; i++)
			{
				pool.AddLast(new Completion());
			}
		}

		#endregion Constructor

		#region Methods

		public void Connect(Socket socket, Action<SocketAsyncEventArgs> callback, object asyncState, IPEndPoint address)
		{
			Enqueue(socket, SocketAsyncOperation.Connect, callback, asyncState, default, address);
		}

		public void Send(Socket socket, Action<SocketAsyncEventArgs> callback, object asyncState, Memory<byte> buffer)
		{
			Enqueue(socket, SocketAsyncOperation.Send, callback, asyncState, buffer);
		}

		public void Receive(Socket socket, Action<SocketAsyncEventArgs> callback, object asyncState, Memory<byte> buffer)
		{
			Enqueue(socket, SocketAsyncOperation.Receive, callback, asyncState, buffer);
		}

		private void Enqueue(Socket socket, SocketAsyncOperation operation, Action<SocketAsyncEventArgs> callback, object? asyncState, Memory<byte> buffer = default, IPEndPoint? address = null)
		{
			var completion = Get();
			completion.Submit(socket, operation, callback, asyncState, buffer, address);
			submited.AddLast(completion);
		}

		public void Tick()
		{
			CheckSubmited();
			FlushCompleted();
		}

		public void RunFor(int ms)
		{
			var now = DateTime.Now;

			var waitHandles = GetWaitHandles();
			WaitHandle.WaitAny(waitHandles, ms, exitContext: false);
			submitedEvent.Reset();
		}

		private WaitHandle[] GetWaitHandles()
		{
			var handleIndex = submited.Count < waitHandlesPool.Length ? submited.Count : waitHandlesPool.Length - 1;
			var waitHandles = waitHandlesPool[handleIndex];

			if (submited.Count > 0)
			{
				int i = 1;
				var node = submited.First;
				while (node != null)
				{
					waitHandles[i] = node.Value.WaitHandle;
					i += 1;

					if (i == waitHandles.Length) break;
					node = node.Next;
				}
			}

			return waitHandles;
		}

		private void CheckSubmited()
		{
			var node = submited.First;
			for (; ; )
			{
				if (node == null) break;

				if (node.Value.IsCompleted)
				{
					submited.Remove(node);
					completed.AddLast(node.Value);
				}

				node = node.Next;
			}
		}

		private void FlushCompleted()
		{
			var node = completed.First;
			for (; ; )
			{
				if (node == null) break;

				completed.RemoveFirst();
				node.Value.Complete();

				Return(node.Value);

				node = node.Next;
			}
		}

		private Completion Get()
		{
			var first = pool.First;
			pool.RemoveFirst();
			return first.Value;
		}

		private void Return(Completion completion)
		{
			pool.AddLast(completion);
		}

		#endregion Methods

	}
}

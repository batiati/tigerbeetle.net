using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TigerBeetle.Managed
{
	#region Documentation

	/// <summary>
	/// An io_uring mimic to provide the same interface for both native and managed clients
	/// </summary>

	#endregion Documentation

	internal sealed class IO
	{
		#region InnerTypes

		private sealed class CompletionEventArgs : SocketAsyncEventArgs
		{
			#region Fields

			private readonly ManualResetEventSlim ev;
			private bool isCompleted;

			#endregion Fields

			#region Constructor

			public CompletionEventArgs(ManualResetEventSlim ev)
			{
				this.ev = ev;
			}

			#endregion Constructor

			#region Properties

			public bool IsCompleted => isCompleted;

			#endregion Properties

			#region Methods

			public void Reset()
			{
				isCompleted = false;
			}

			public void Set()
			{
				isCompleted = true;
				ev.Set();
			}

			protected override void OnCompleted(SocketAsyncEventArgs e)
			{
				Set();
			}

			#endregion Methods
		}

		private sealed class Completion
		{
			#region Fields

			private SocketAsyncOperation asyncOperation;
			private Socket? socket;
			private Action<SocketAsyncEventArgs>? callback;
			private readonly CompletionEventArgs e;

			#endregion Fields

			#region Constructor

			public Completion(ManualResetEventSlim ev)
			{
				e = new CompletionEventArgs(ev);
			}

			#endregion Constructor

			#region Properties

			public SocketAsyncOperation AsyncOperation => asyncOperation;

			public bool IsCompleted => e.IsCompleted;

			#endregion Properties

			#region Methods

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

				if (!pending) e.Set();
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

			#endregion Methods
		}

		#endregion InnerTypes

		#region Fields

		private const int POOL_SIZE = 128;

		// TODO: implement a intrusive LinkedList in order to reduce node allocations and GC

		private readonly LinkedList<Completion> pool = new();
		private readonly LinkedList<Completion> submited = new();
		private readonly LinkedList<Completion> completed = new();
		private readonly ManualResetEventSlim submitedEvent = new();

		#endregion Fields

		#region Constructor

		public IO()
		{
			for (int i = 0; i < POOL_SIZE; i++)
			{
				pool.AddLast(new Completion(submitedEvent));
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
			if (submitedEvent.Wait(ms))
			{
				submitedEvent.Reset();
			}
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

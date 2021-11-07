using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

namespace TigerBeetle.Protocol
{
	public sealed class IO
	{
		private readonly LinkedList<Operation> pool = new LinkedList<Operation>();
		private readonly LinkedList<Operation> submited = new LinkedList<Operation>();
		private readonly LinkedList<Operation> completed = new LinkedList<Operation>();

		public enum IOOperation
		{
			None,
			Connect,
			Read,
			Write,
			Timeout,
		}

		public class Operation 
		{
			public IOOperation IOOperation { get; private set; }

			private NetworkStream? stream;
			private Action<object?>? objectCallback;
			private Action<int, object?>? intObjectCallback;
			private IAsyncResult? asyncResult;
			private object? asyncState;

			public void Write(Socket socket, byte[] buffer, int offset, int size, Action<object?> action, object? asyncState)
			{
				IOOperation = IOOperation.Write;

				this.objectCallback = action;
				this.intObjectCallback = null;
				this.asyncState = asyncState;
				
				stream = new NetworkStream(socket, ownsSocket: false);
				asyncResult = stream.BeginWrite(buffer, offset, size, null, null);
			}

			public void Read(Socket socket, byte[] buffer, int offset, int size, Action<int, object?> action, object? asyncState)
			{
				IOOperation = IOOperation.Read;

				intObjectCallback = action;
				objectCallback = null;
				this.asyncState = asyncState;

				stream = new NetworkStream(socket, ownsSocket: false);
				asyncResult = stream.BeginRead(buffer, offset, size, null, null);
			}

			public bool Flush()
			{
				if (asyncResult != null && asyncResult.IsCompleted)
				{
					switch (IOOperation)
					{
						case IOOperation.Read:

							Trace.Assert(intObjectCallback != null);
							var result = stream.EndRead(asyncResult);
							intObjectCallback(result, asyncState);
							break;

						case IOOperation.Write:

							Trace.Assert(objectCallback != null);

							stream.EndWrite(asyncResult);
							objectCallback(asyncState);
							break;
						
						default:
							throw new NotImplementedException();
					}

					stream.Dispose();
					stream = null;

					IOOperation = IOOperation.None;
					intObjectCallback = null;
					objectCallback = null;
					asyncResult = null;
					asyncState = null;

					return true;
				}

				return false;
			}
		}

		public IO()
		{
			for (int i = 0; i < 1024; i++)
			{
				pool.AddLast(new Operation());
			}
		}

		public void Write(Socket socket, byte[] buffer, int offset, int size, Action<object?> action, object asyncState)
		{
			var op = GetOperation();
			op.Write(socket, buffer, offset, size, action, asyncState);

			submited.AddLast(op);
		}

		public void Read(Socket socket, byte[] buffer, int offset, int size, Action<int, object?> action, object asyncState)
		{
			var op = GetOperation();
			op.Read(socket, buffer, offset, size, action, asyncState);

			submited.AddLast(op);
		}

		private Operation GetOperation()
		{
			var first = pool.First.Value;
			pool.RemoveFirst();

			return first;
		}

		public void Tick()
		{
			if (submited.Count == 0) return;

			var node = submited.First;
			for (; ; )
			{
				if (node.Value.Flush())
				{
					pool.AddLast(node.Value);
				}

				node = node.Next;
				if (node == null) break;
			}
		}


	}
}

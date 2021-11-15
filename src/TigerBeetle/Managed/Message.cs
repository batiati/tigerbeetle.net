using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TigerBeetle.Managed
{
	internal sealed class Message
	{
		#region Fields

		private readonly Header header;
		private readonly byte[] buffer;

		private int references;
		internal Message? next;

		#endregion Fields

		#region Constructor

		public Message(bool isHeaderOnly = false)
		{
			this.buffer = new byte[isHeaderOnly ? HeaderData.SIZE : Config.MessageSizeMaxPadded];
			header = new Header(ref MemoryMarshal.Cast<byte, HeaderData>(buffer.AsSpan(0..HeaderData.SIZE))[0])
			{
				Checksum = UInt128.Zero,
				ChecksumBody = UInt128.Zero,
				Parent = UInt128.Zero,
				Client = UInt128.Zero,
				Context = UInt128.Zero,
				Request = 0,
				Epoch = 0,
				View = 0,
				Op = 0,
				Commit = 0,
				Offset = 0,
				Size = HeaderData.SIZE,
				Replica = 0,
				Operation = Operation.Reserved,
				Version = Header.VERSION
			};
		}

		#endregion Constructor

		#region Properties

		public Header Header => header;

		public byte[] Buffer => buffer;

		public int References => references;

		#endregion Properties

		#region Methods

		public Message Ref()
		{
			references += 1;
			return this;
		}

		public bool Unref()
		{
			references -= 1;
			return references == 0;
		}

		public bool IsFree()
		{
			return references == 0;
		}

		public ReadOnlySpan<T> GetBody<T>()
			where T : struct
		{
			if (IsHeaderOnly() || header.Size <= HeaderData.SIZE)
			{
				return ReadOnlySpan<T>.Empty;
			}
			else
			{
				return MemoryMarshal.Cast<byte, T>(buffer.AsSpan(HeaderData.SIZE..Header.Size));
			}
		}

		public void SetBody(ReadOnlySpan<byte> body)
		{
			body.CopyTo(buffer.AsSpan(HeaderData.SIZE..));
			header.Size = HeaderData.SIZE + body.Length;
		}

		public void SetBody<T>(T body)
			where T : IData
		{
			var bodySpan = body.AsReadOnlySpan();
			bodySpan.CopyTo(buffer.AsSpan(HeaderData.SIZE..));
			header.Size = HeaderData.SIZE + bodySpan.Length;
		}

		public void SetBody<T>(IEnumerable<T> body)
			where T : IData
		{
			int size = HeaderData.SIZE;
			foreach (var item in body)
			{
				var bodySpan = item.AsReadOnlySpan();
				bodySpan.CopyTo(buffer.AsSpan(size..));
				size += bodySpan.Length;
			}

			header.Size = size;
		}

		public bool IsHeaderOnly()
		{
			var ret = buffer.Length == HeaderData.SIZE;
			Trace.Assert(ret || buffer.Length == Config.MessageSizeMaxPadded);
			return ret;
		}

		#endregion Methods
	}


}


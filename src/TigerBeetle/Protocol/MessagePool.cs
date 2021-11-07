using System.Diagnostics;

namespace TigerBeetle.Protocol
{
	#region Documentation

	/// <summary>
	/// A pool of reference-counted Messages, memory for which is allocated only once
	/// during initialization and reused thereafter. The config.message_bus_messages_max
	/// and config.message_bus_headers_max values determine the size of this pool.
	/// </summary>

	#endregion Documentation

	internal sealed class MessagePool
	{
		#region Fields

		/// List of currently unused messages of message_size_max_padded
		private Message? freeList;

		/// List of currently usused header-sized messages
		private Message? headerOnlyFreeList;

		#endregion Fields

		#region Constructor

		public MessagePool()
		{
			for (int i = 0; i < Config.MessageBusMessagesMax; i++)
			{
				var message = new Message(isHeaderOnly: false);
				message.next = freeList;
				freeList = message;
			}

			for (int i = 0; i < Config.MessageBusHeadersMax; i++)
			{
				var message = new Message(isHeaderOnly: true);
				message.next = headerOnlyFreeList;
				headerOnlyFreeList = message;
			}
		}

		#endregion Constructor

		#region Methods

		#region Documentation

		/// <summary>
		/// Get an unused message with a buffer of config.message_size_max. If no such message is
		/// available, an error is returned. The returned message has exactly one reference.
		/// </summary>

		#endregion Documentation

		public Message GetMessage()
		{
			if (freeList == null)
			{
				// Creates a new message if there is none free
				var newMessage = new Message(isHeaderOnly: false);
				return newMessage.Ref();
			}
			else
			{
				var ret = freeList;
				freeList = ret.next;
				ret.next = null;

				Trace.Assert(!ret.IsHeaderOnly());
				Trace.Assert(ret.IsFree());

				return ret.Ref();
			}
		}

		#region Documentation

		/// <summary>
		/// Get an unused message with a buffer only large enough to hold a header. If no such message
		/// is available, an error is returned. The returned message has exactly one reference.
		/// </summary>

		#endregion Documentation		

		public Message GetHeaderOnlyMessage()
		{
			if (headerOnlyFreeList == null)
			{
				// Creates a new message if there is none free
				var newMessage = new Message(isHeaderOnly: true);
				return newMessage.Ref();
			}
			else
			{
				var ret = headerOnlyFreeList;
				headerOnlyFreeList = ret.next;
				ret.next = null;

				Trace.Assert(ret.IsHeaderOnly());
				Trace.Assert(ret.IsFree());

				return ret.Ref();
			}
		}

		public void Unref(Message message)
		{
			if (message.Unref())
			{
				if (message.IsHeaderOnly())
				{
					message.next = headerOnlyFreeList;
					headerOnlyFreeList = message;
				}
				else
				{
					message.next = freeList;
					freeList = message;
				}
			}
		}

		#endregion Methods
	}
}


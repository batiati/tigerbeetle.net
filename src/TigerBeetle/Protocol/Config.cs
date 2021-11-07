namespace TigerBeetle.Protocol
{
	internal static class Config
	{

		/// The number of milliseconds between each replica tick, the basic unit of time in TigerBeetle.
		/// Used to regulate heartbeats, retries and timeouts, all specified as multiples of a tick.
		public const uint TickMs = 10;

		/// The conservative round-trip time at startup when there is no network knowledge.
		/// Adjusted dynamically thereafter for RTT-sensitive timeouts according to network congestion.
		/// This should be set higher rather than lower to avoid flooding the network at startup.
		public const ulong RttTicks = 300 / TickMs;

		/// The multiple of round-trip time for RTT-sensitive timeouts.
		public const byte RttMultiple = 2;

		/// The min/max bounds of exponential backoff (and jitter) to add to RTT-sensitive timeouts.
		public const ulong BackoffMinTicks = 100 / TickMs;
		public const ulong BackoffMaxTicks = 10000 / TickMs;

		/// The maximum size of a message in bytes:
		/// This is also the limit of all inflight data across multiple pipelined requests per connection.
		/// We may have one request of up to 2 MiB inflight or 2 pipelined requests of up to 1 MiB inflight.
		/// This impacts sequential disk write throughput, the larger the buffer the better.
		/// 2 MiB is 16,384 transfers, and a reasonable choice for sequential disk write throughput.
		/// However, this impacts bufferbloat and head-of-line blocking latency for pipelined requests.
		/// For a 1 Gbps NIC = 125 MiB/s throughput: 2 MiB / 125 * 1000ms = 16ms for the next request.
		/// This impacts the amount of memory allocated at initialization by the server.
		public const int MessageSizeMax = 1 * 1024 * 1024;

		/// The minimum size of an aligned kernel page and an Advanced Format disk sector:
		/// This is necessary for direct I/O without the kernel having to fix unaligned pages with a copy.
		/// The new Advanced Format sector size is backwards compatible with the old 512 byte sector size.
		/// This should therefore never be less than 4 KiB to be future-proof when server disks are swapped.
		public const int SectorSize = 4096;

		/// The maximum number of replicas allowed in a cluster.
		public const int ReplicasMax = 6;

		/// The maximum number of clients allowed per cluster, where each client has a unique 128-bit ID.
		/// This impacts the amount of memory allocated at initialization by the server.
		/// This determines the size of the VR client table used to cache replies to clients by client ID.
		/// Each client has one entry in the VR client table to store the latest `message_size_max` reply.
		public const int ClientsMax = 32;

		/// The maximum number of connections that can be held open by the server at any time:
		public const int ConnectionsMax = ReplicasMax; // ReplicasMax + ClientsMax;

		/// The number of full-sized messages allocated at initialization by the message bus.
		public const int MessageBusMessagesMax = ReplicasMax * 4; //ConnectionsMax * 4;

		/// The number of header-sized messages allocated at initialization by the message bus.
		/// These are much smaller/cheaper and we can therefore have many of them.
		public const int MessageBusHeadersMax = ReplicasMax * ConnectionSendQueueMax * 2; //ConnectionsMax * ConnectionSendQueueMax * 2;

		/// The maximum number of Viewstamped Replication prepare messages that can be inflight at a time.
		/// This is immutable once assigned per cluster, as replicas need to know how many operations might
		/// possibly be uncommitted during a view change, and this must be constant for all replicas.
		public const int PipeliningMax = ClientsMax;

		/// The maximum number of outgoing messages that may be queued on a connection.
		public const int ConnectionSendQueueMax = PipeliningMax;

		/// Add an extra sector_size bytes to allow a partially received subsequent
		/// message to be shifted to make space for 0 padding to vsr.sector_ceil.
		public const int MessageSizeMaxPadded = MessageSizeMax + SectorSize;

		/// The maximum size of a kernel socket receive buffer in bytes (or 0 to use the system default):
		/// This sets SO_RCVBUF as an alternative to the auto-tuning range in /proc/sys/net/ipv4/tcp_rmem.
		/// The value is limited by /proc/sys/net/core/rmem_max, unless the CAP_NET_ADMIN privilege exists.
		/// The kernel doubles this value to allow space for packet bookkeeping overhead.
		/// The receive buffer should ideally exceed the Bandwidth-Delay Product for maximum throughput.
		/// At the same time, be careful going beyond 4 MiB as the kernel may merge many small TCP packets,
		/// causing considerable latency spikes for large buffer sizes:
		/// https://blog.cloudflare.com/the-story-of-one-latency-spike/
		public const int TcpRcvbuf = 4 * 1024 * 1024;

		/// The maximum size of a kernel socket send buffer in bytes (or 0 to use the system default):
		/// This sets SO_SNDBUF as an alternative to the auto-tuning range in /proc/sys/net/ipv4/tcp_wmem.
		/// The value is limited by /proc/sys/net/core/wmem_max, unless the CAP_NET_ADMIN privilege exists.
		/// The kernel doubles this value to allow space for packet bookkeeping overhead.
		public const int TcpSndbuf = 4 * 1024 * 1024;

		/// Whether to enable TCP keepalive:
		public const bool TcpKeepalive = true;

		/// Whether to disable Nagle's algorithm to eliminate send buffering delays:
		public const bool TcpNodelay = true;

		/// The minimum and maximum amount of time in milliseconds to wait before initiating a connection.
		/// Exponential backoff and jitter are applied within this range.
		public const ulong ConnectionDelayMinMs = 50;
		public const ulong ConnectionDelayMaxMs = 1000;

	}


}


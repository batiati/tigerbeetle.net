namespace TigerBeetle.Managed
{
	internal enum ConnectionState
	{
		Free,

		/// The connection has been reserved for an in progress accept operation,
		/// with peer set to `.none`.
		Accepting,

		/// The peer is a replica and a connect operation has been started
		/// but not yet competed.
		Connecting,

		/// The peer is fully connected and may be a client, replica, or unknown.
		Connected,

		/// The connection is being terminated but cleanup has not yet finished.
		Terminating,
	}
}

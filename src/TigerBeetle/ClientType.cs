namespace TigerBeetle
{
	public enum ClientType
	{
		#region Documentation

		/// <summary>
		/// Pure C# client implementation
		/// </summary>

		#endregion Documentation

		Managed = 0,

		#region Documentation

		/// <summary>
		/// Native implementation through P/Invoke
		/// </summary>

		#endregion Documentation

		Native = 1
	}
}
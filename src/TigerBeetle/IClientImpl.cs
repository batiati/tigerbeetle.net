using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TigerBeetle.Managed;

namespace TigerBeetle
{
	internal interface IClientImpl : IDisposable
	{
		#region Properties

		uint Cluster { get; }
		UInt128 Id { get; }

		#endregion Properties

		#region Methods

		TResult[] CallRequest<TResult, TBody>(Operation operation, IEnumerable<TBody> batch)
			where TResult : struct
			where TBody : IData;
		Task<TResult[]> CallRequestAsync<TResult, TBody>(Operation operation, IEnumerable<TBody> batch)
			where TResult : struct
			where TBody : IData;

		#endregion Methods
	}
}
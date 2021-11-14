﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TigerBeetle.Protocol;

namespace TigerBeetle
{
	public sealed class Client
	{
		#region Fields

		private readonly IClientImpl impl;

		#endregion Fields

		#region Constructor

		public Client(ClientType type, uint cluster, IPEndPoint[] configuration)
		{
			if (configuration == null || configuration.Length == 0 || configuration.Length > Config.ReplicasMax) throw new ArgumentException(nameof(configuration));

			impl = type switch
			{
				ClientType.Managed => new ManagedClientImpl(cluster, configuration),
				ClientType.Native => new NativeClientImpl(cluster, configuration),
				_ => throw new NotImplementedException(),
			};
		}

		#endregion Constructor

		public UInt128 Id => impl.Id;

		public uint Cluster => impl.Cluster;

		public ClientType ClientType => impl is NativeClientImpl ? ClientType.Native : ClientType.Managed;

		#region Methods

		public CreateAccountResult CreateAccount(Account account)
		{
			var ret = CallRequest<CreateAccountsResult, Account>(Operation.CreateAccounts, new[] { account });
			return ret.Length == 0 ? CreateAccountResult.Ok : ret[0].Result;
		}

		public CreateAccountsResult[] CreateAccounts(IEnumerable<Account> batch)
		{
			return CallRequest<CreateAccountsResult, Account>(Operation.CreateAccounts, batch);
		}

		public Task<CreateAccountResult> CreateAccountAsync(Account account)
		{
			var task = CallRequestAsync<CreateAccountsResult, Account>(Operation.CreateAccounts, new[] { account });
			return task.ContinueWith<CreateAccountResult>(x => x.Result.Length == 0 ? CreateAccountResult.Ok : x.Result[0].Result);
		}

		public Task<CreateAccountsResult[]> CreateAccountsAsync(IEnumerable<Account> batch)
		{
			return CallRequestAsync<CreateAccountsResult, Account>(Operation.CreateAccounts, batch);
		}

		public CreateTransferResult CreateTransfer(Transfer transfer)
		{
			var ret = CallRequest<CreateTransfersResult, Transfer>(Operation.CreateTransfers, new[] { transfer });
			return ret.Length == 0 ? CreateTransferResult.Ok : ret[0].Result;
		}

		public CreateTransfersResult[] CreateTransfers(IEnumerable<Transfer> batch)
		{
			return CallRequest<CreateTransfersResult, Transfer>(Operation.CreateTransfers, batch);
		}

		public Task<CreateTransferResult> CreateTransferAsync(Transfer transfer)
		{
			var task = CallRequestAsync<CreateTransfersResult, Transfer>(Operation.CreateTransfers, new[] { transfer });
			return task.ContinueWith<CreateTransferResult>(x => x.Result.Length == 0 ? CreateTransferResult.Ok : x.Result[0].Result);
		}

		public Task<CreateTransferResult[]> CreateTransfersAsync(IEnumerable<Transfer> batch)
		{
			return CallRequestAsync<CreateTransferResult, Transfer>(Operation.CreateTransfers, batch);
		}

		public CommitTransferResult CommitTransfer(Commit commit)
		{
			var ret = CallRequest<CommitTransfersResult, Commit>(Operation.CommitTransfers, new[] { commit });
			return ret.Length == 0 ? CommitTransferResult.Ok : ret[0].Result;
		}

		public CommitTransfersResult[] CommitTransfers(IEnumerable<Commit> batch)
		{
			return CallRequest<CommitTransfersResult, Commit>(Operation.CommitTransfers, batch);
		}

		public Task<CommitTransferResult> CommitTransferAsync(Commit commit)
		{
			var task = CallRequestAsync<CommitTransfersResult, Commit>(Operation.CommitTransfers, new[] { commit });
			return task.ContinueWith<CommitTransferResult>(x => x.Result.Length == 0 ? CommitTransferResult.Ok : x.Result[0].Result);
		}

		public Task<CommitTransfersResult[]> CommitTransfersAsync(IEnumerable<Commit> batch)
		{
			return CallRequestAsync<CommitTransfersResult, Commit>(Operation.CommitTransfers, batch);
		}

		public Account? LookupAccount(UInt128 id)
		{
			var ret = CallRequest<AccountData, UInt128>(Operation.LookupAccounts, new[] { id });
			return ret.Length == 0 ? null : new Account(ret[0]);
		}

		public Account[] LookupAccounts(IEnumerable<UInt128> ids)
		{
			var ret = CallRequest<AccountData, UInt128>(Operation.LookupAccounts, ids);
			return ret.Select(data => new Account(data)).ToArray();
		}

		public Task<Account?> LookupAccountAsync(UInt128 id)
		{
			var task = CallRequestAsync<AccountData, UInt128>(Operation.LookupAccounts, new[] { id });
			return task.ContinueWith<Account?>(x => x.Result.Length == 0 ? null : new Account(x.Result[0]));
		}

		public Task<Account[]> LookupAccountsAsync(IEnumerable<UInt128> ids)
		{
			var task = CallRequestAsync<AccountData, UInt128>(Operation.LookupAccounts, ids);
			return task.ContinueWith(x => x.Result.Select(data => new Account(data)).ToArray());
		}

		private TResult[] CallRequest<TResult, TBody>(Operation operation, IEnumerable<TBody> batch)
			where TBody : IData
			where TResult : struct
		{

			return impl.CallRequest<TResult, TBody>(operation, batch);
		}

		private Task<TResult[]> CallRequestAsync<TResult, TBody>(Operation operation, IEnumerable<TBody> batch)
			where TBody : IData
			where TResult : struct
		{
			return impl.CallRequestAsync<TResult, TBody>(operation, batch);
		}

		#endregion Methods
	}
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace TigerBeetle.Benchmarks
{
	public static class Benchmark
	{
		#region InnerTypes

		private class TimedQueue
		{
			#region Fields

			private Stopwatch timer = new Stopwatch();

			#endregion Fields

			#region Properties

			public Queue<(Func<Task> action, bool isCommit)> Batches { get; } = new();

			public long MaxTransfersLatency { get; private set; }

			public long MaxCommitsLatency { get; private set; }

			public long TotalTime { get; private set; }

			#endregion Properties

			#region Methods

			public async Task Execute()
			{
				while (Batches.TryPeek(out (Func<Task> action, bool isCommit) tuple))
				{
					timer.Restart();
					await tuple.action();
					timer.Stop();

					TotalTime += timer.ElapsedMilliseconds;

					_ = Batches.Dequeue();

					if (tuple.isCommit)
					{
						MaxCommitsLatency = Math.Max(timer.ElapsedMilliseconds, MaxCommitsLatency);
					}
					else
					{
						MaxTransfersLatency = Math.Max(timer.ElapsedMilliseconds, MaxTransfersLatency);
					}
				}
			}

			public void Reset()
			{
				MaxCommitsLatency = 0;
				MaxTransfersLatency = 0;
				timer.Reset();
				TotalTime = 0;
				Batches.Clear();
			}

			#endregion Methods
		}

		#endregion InnerTypes

		#region Fields

		private const int MAX_TRANSFERS = 1_000_000;
		private const bool IS_TWO_PHASE_COMMIT = false;
		private const int BATCH_SIZE = 5_000;

		#endregion Fields

		#region Methods

		public static async Task Main()
		{
			var clientType = ClientType.Managed;

			string[] args = Environment.GetCommandLineArgs();
			if (args.Length > 1) Enum.TryParse<ClientType>(args.Last(), ignoreCase: true, out clientType);

			Console.WriteLine($"Benchmarking.Net ... {clientType}");

			var queue = new TimedQueue();
			var client = new Client(clientType, 0, new IPEndPoint[] { IPEndPoint.Parse("127.0.0.1:3001") });
			
			WaitForConnect();

			var accounts = new[] {
				new Account
				{
					Id = 1,
					UserData = 0,
					Unit = Currency.ZAR.Code,
				},
				new Account
				{
					Id = 2,
					UserData = 0,
					Unit = Currency.ZAR.Code,
				}
			};

			// Pre-allocate a million transfers:
			var transfers = new Transfer[MAX_TRANSFERS];
			for (int i = 0; i < transfers.Length; i++)
			{
				transfers[i] = new Transfer
				{
					Id = i,
					DebitAccountId = accounts[0].Id,
					CreditAccountId = accounts[1].Id,
					UserData = 0,
					Code = 0,
					Amount = Currency.ZAR.ToUInt64(0.01M),
					Flags = IS_TWO_PHASE_COMMIT ? TransferFlags.TwoPhaseCommit : TransferFlags.None,
					Timeout = IS_TWO_PHASE_COMMIT ? int.MaxValue : 0,
				};
			}

			var commits = new Commit[IS_TWO_PHASE_COMMIT ? MAX_TRANSFERS : 0];
			for (int i = 0; i < commits.Length; i++)
			{
				commits[i] = new Commit
				{
					Id = i,
					Code = 0,
					Flags = CommitFlags.None,
				};
			}

			Console.WriteLine("creating accounts...");

			async Task createAccounts() => _ = await client.CreateAccountsAsync(accounts);

			queue.Batches.Enqueue((createAccounts, false));
			await queue.Execute();
			Trace.Assert(queue.Batches.Count == 0);

			Console.WriteLine("batching transfers...");
			queue.Reset();

			int batchCount = 0;
			int count = 0;

			for (; ; )
			{
				var batch = transfers.Skip(batchCount * BATCH_SIZE).Take(BATCH_SIZE);
				if (batch.Count() == 0) break;

				async Task createTransfers()
				{
					var ret = await client.CreateTransfersAsync(batch);
					if (ret.Length > 0) throw new Exception("Invalid transfer results");
				}

				queue.Batches.Enqueue((createTransfers, false));

				if (IS_TWO_PHASE_COMMIT)
				{
					#pragma warning disable CS0162 // Unreachable code detected
					
					var commitBatch = commits.Skip(batchCount * BATCH_SIZE).Take(BATCH_SIZE);
					async Task commitTransfers()
					{
						var ret = await client.CommitTransfersAsync(commitBatch);
						if (ret.Length > 0) throw new Exception("Invalid commit results");
					}
					queue.Batches.Enqueue((commitTransfers, true));

					#pragma warning restore CS0162
				}

				batchCount += 1;
				count += BATCH_SIZE;
			}

			Trace.Assert(count == MAX_TRANSFERS);

			Console.WriteLine("starting benchmark...");
			await queue.Execute();
			Trace.Assert(queue.Batches.Count == 0);

			Console.WriteLine("============================================");

			var result = (long)((transfers.Length * 1000) / queue.TotalTime);

			Console.WriteLine($"{result} {(IS_TWO_PHASE_COMMIT ? "two-phase commit " : "")}transfers per second\n");
			Console.WriteLine($"create_transfers max p100 latency per {BATCH_SIZE} transfers = {queue.MaxTransfersLatency}ms");
			Console.WriteLine($"commit_transfers max p100 latency per {BATCH_SIZE} transfers = {queue.MaxCommitsLatency}ms");
		}

		private static void WaitForConnect()
		{
			System.Threading.Thread.Sleep(100);
		}

		#endregion Methods
	}
}

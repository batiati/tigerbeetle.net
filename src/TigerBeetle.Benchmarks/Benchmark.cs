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
			private Stopwatch globalTimer = new Stopwatch();

			#endregion Fields

			#region Properties

			public Queue<(Func<Task> action, bool isCommit)> Batches { get; } = new();

			public long MaxTransfersLatency { get; private set; }

			public long MaxCommitsLatency { get; private set; }

			public long TotalTime => globalTimer.ElapsedMilliseconds;

			#endregion Properties

			#region Methods

			public async Task Execute()
			{
				Console.WriteLine("executing batches...");

				while (Batches.TryPeek(out (Func<Task> action, bool isCommit) tuple))
				{
					globalTimer.Start();
					timer.Restart();
					await tuple.action();
					timer.Stop();
					globalTimer.Stop();

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
				globalTimer.Reset();
				Batches.Clear();
			}

			#endregion Methods
		}

		#endregion InnerTypes

		#region Fields

		private const int MAX_TRANSFERS = 50_000;
		private const bool IS_TWO_PHASE_COMMIT = false;
		private const int BATCH_SIZE = 500;

		#endregion Fields

		#region Methods

		public static async Task Main()
		{

			Console.WriteLine("Benchmarking.Net ...");
			var queue = new TimedQueue();
			var client = new Client(0, new IPEndPoint[] { IPEndPoint.Parse("192.168.105.95:3001") });
			WaitForConnect(client);

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

				async Task createTransfers() => _ = await client.CreateTransfersAsync(batch);
				queue.Batches.Enqueue((createTransfers, false));

				if (IS_TWO_PHASE_COMMIT)
				{
					var commitBatch = commits.Skip(batchCount * BATCH_SIZE).Take(BATCH_SIZE);
					async Task commitTransfers() => _ = await client.CommitTransfersAsync(commitBatch);
					queue.Batches.Enqueue((commitTransfers, true));
				}

				batchCount += 1;
				count += BATCH_SIZE;
			}

			Trace.Assert(count == MAX_TRANSFERS);

			Console.Write("starting benchmark...");
			await queue.Execute();
			Trace.Assert(queue.Batches.Count == 0);

			Console.WriteLine("============================================");

			var result = (long)((transfers.Length * 1000) / queue.TotalTime);

			Console.WriteLine($"{result} {(IS_TWO_PHASE_COMMIT ? "two-phase commit " : "")}transfers per second\n");
			Console.WriteLine($"create_transfers max p100 latency per {BATCH_SIZE} transfers = {queue.MaxTransfersLatency}ms");
			Console.WriteLine($"commit_transfers max p100 latency per {BATCH_SIZE} transfers = {queue.MaxCommitsLatency}ms");
		}

		private static void WaitForConnect(Client client)
		{
			for (int i = 0; i < 20; i++)
			{
				System.Threading.Thread.Sleep(10);
			}
		}

		#endregion Methods
	}
}

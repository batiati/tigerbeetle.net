using System;
using System.Diagnostics;

namespace TigerBeetle.Protocol
{
	internal sealed class Timeout
	{
		#region Fields

		private string name;
		private UInt128 id;
		private ulong after;
		private byte attempts = 0;
		private ulong rtt = Config.RttTicks;
		private byte rttMultiple = Config.RttMultiple;
		private ulong ticks = 0;
		private bool ticking = false;

		#endregion Fields

		public UInt128 Id { get => id; set => id = value; }

		public string Name { get => name; set => name = value; }

		public ulong After { get => after; set => after = value; }

		public byte Attempts { get => attempts; }

		public bool Ticking { get => ticking; }

		public static ulong ExponentialBackoffWithJitter(Random prng, ulong min, ulong max, ulong attempt)
		{
			var range = max - min;
			Trace.Assert(range > 0);

			// Do not use `@truncate(u6, attempt)` since that only discards the high bits:
			// We want a saturating exponent here instead.
			const byte MAX_U6 = 63;
			var exponent = Math.Min(MAX_U6, (byte)attempt);

			// A "1" shifted left gives any power of two:
			// 1<<0 = 1, 1<<1 = 2, 1<<2 = 4, 1<<3 = 8
			var power = unchecked(1UL << exponent);

			// Calculate the capped exponential backoff component, `min(range, min * 2 ^ attempt)`:
			var backoff = Math.Min(range, Math.Max(1, min) * power);
			var jitter = (ulong)(prng.NextDouble() * backoff);

			var result = min + jitter;

			Trace.Assert(result >= min);
			Trace.Assert(result <= max);

			return result;
		}

		/// Increments the attempts counter and resets the timeout with exponential backoff and jitter.
		/// Allows the attempts counter to wrap from time to time.
		/// The overflow period is kept short to surface any related bugs sooner rather than later.
		/// We do not saturate the counter as this would cause round-robin retries to get stuck.
		public void Backoff(Random prng)
		{
			Trace.Assert(ticking);

			unchecked
			{
				ticks = 0;
				attempts += 1;
			}

			//log.debug("{}: {s} backing off", .{ self.id, self.name
			SetAfterForRttAndAttempts(prng);
		}

		/// It's important to check that when fired() is acted on that the timeout is stopped/started,
		/// otherwise further ticks around the event loop may trigger a thundering herd of messages.
		public bool Fired()
		{
			if (ticking && ticks >= after)
			{
				//log.debug("{}: {s} fired", .{ self.id, self.name });
				if (ticks > after)
				{
					//log.emerg("{}: {s} is firing every tick", .{ self.id, self.name });
					throw new InvalidOperationException("timeout was not reset correctly");
				}

				return true;
			}
			else
			{
				return false;
			}
		}

		public void Reset()
		{
			attempts = 0;
			ticks = 0;

			Trace.Assert(ticking);
			// TODO Use prng to adjust for rtt and attempts.
			//log.debug("{}: {s} reset", .{ self.id, self.name
		}

		/// Sets the value of `after` as a function of `rtt` and `attempts`.
		/// Adds exponential backoff and jitter.
		/// May be called only after a timeout has been stopped or reset, to prevent backward jumps.
		public void SetAfterForRttAndAttempts(Random prng)
		{
			// If `after` is reduced by this function to less than `ticks`, then `fired()` will panic:
			Trace.Assert(ticks == 0);
			Trace.Assert(rtt > 0);

			var after = (rtt * rttMultiple) + ExponentialBackoffWithJitter(prng, Config.BackoffMinTicks, Config.BackoffMaxTicks, attempts);

			// TODO Clamp `after` to min/max tick bounds for timeout.

			//log.debug("{}: {s} after={}..{} (rtt={} min={} max={} attempts={})", .{
			//    self.id,
			//    self.name,
			//    self.after,
			//    after,
			//    self.rtt,
			//    config.backoff_min_ticks,
			//    config.backoff_max_ticks,
			//    self.attempts,
			//});

			this.after = after;
			Trace.Assert(after > 0);
		}

		public void SetRtt(ulong rttTicks)
		{
			Trace.Assert(rtt > 0);
			Trace.Assert(rttTicks > 0);

			//log.debug("{}: {s} rtt={}..{}", .{
			//    self.id,
			//    self.name,
			//    self.rtt,
			//    rtt_ticks,
			//});

			rtt = rttTicks;
		}

		public void Start()
		{
			attempts = 0;
			ticks = 0;
			ticking = true;

			// TODO Use self.prng to adjust for rtt and attempts.
			//log.debug("{}: {s} started", .{ self.id, self.name
		}

		public void Stop()
		{
			attempts = 0;
			ticks = 0;
			ticking = false;

			//log.debug("{}: {s} stopped", .{ self.id, self.name });
		}

		public void Tick()
		{
			if (ticking) ticks += 1;
		}

	}
}


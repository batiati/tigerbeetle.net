using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TigerBeetle.Tests
{
	[TestClass]
	public class ValueUnitTests
	{
		[TestMethod]
		public void Convertion()
		{
			var usd = Currency.USD;
			Assert.AreEqual(9.99M, usd.FromUInt64((ulong)999));
			Assert.AreEqual((ulong)999, usd.ToUInt64(9.99M));
		}

		[TestMethod]
		public void Truncate()
		{
			var usd = Currency.USD;
			Assert.AreEqual((ulong)999, usd.ToUInt64(9.99987654321M));
		}

		[TestMethod]
		public void Custom()
		{
			var btc = new Currency(0, "BTC", 8, "Bitcoin");
			Assert.AreEqual(0.00000999M, btc.FromUInt64((ulong)999));
			Assert.AreEqual((ulong)999, btc.ToUInt64(0.00000999M));
		}

		[TestMethod]
		public void CustomTruncate()
		{
			var btc = new Currency(0, "BTC", 8, "Bitcoin");
			Assert.AreEqual((ulong)999, btc.ToUInt64(0.00000999987654321M));
		}
	}
}

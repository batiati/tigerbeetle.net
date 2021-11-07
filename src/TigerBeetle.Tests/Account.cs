using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace TigerBeetle.Tests
{
	[TestClass]
	public class AccountTests
	{
		[TestMethod]
		public void Creation()
		{
			var account = new Account();
			Assert.AreEqual(account.Id, UInt128.Zero);
			Assert.AreEqual(account.UserData, UInt128.Zero);
			Assert.IsTrue(account.Reserved.Length == 48);
			Assert.AreEqual(account.Unit, (ushort)0);
			Assert.AreEqual(account.Code, (ushort)0);
			Assert.AreEqual(account.DebitsReserved, (ulong)0);
			Assert.AreEqual(account.CreditsReserved, (ulong)0);
			Assert.AreEqual(account.DebitsAccepted, (ulong)0);
			Assert.AreEqual(account.CreditsAccepted, (ulong)0);
			Assert.AreEqual(account.Timestamp, (ulong)0);
		}

		[TestMethod]
		public void SetId()
		{
			var account = new Account();
			var value = Guid.NewGuid();
			account.Id = value;

			Assert.AreEqual(account.Id, value);
			Assert.AreEqual(value, account.Id);
		}

		[TestMethod]
		public void SetUserData()
		{
			var account = new Account();
			var value = Guid.NewGuid();
			account.UserData = value;

			Assert.AreEqual(account.UserData, value);
			Assert.AreEqual(value, account.UserData);
		}

		[TestMethod]
		public void SetReserved()
		{
			var account = new Account();
			var value = new byte[] { 0, 1, 2, 3, 4, 5 };
			account.Reserved = value;

			Assert.IsTrue(account.Reserved.StartsWith(value));
		}
	}
}

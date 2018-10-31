﻿using ICD.Connect.Conferencing.Comparers;
using NUnit.Framework;
using System.Linq;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Conferencing.Tests.Comparers
{
	[TestFixture]
	public sealed class ContactNameComparerTest
	{
		[Test]
		public void CompareTest()
		{
			string[] names =
			{
				"Test A A",
				"Test A B",
				"Test A",
				"A"
			};

			string[] expected =
			{
				"A",
				"Test A A",
				"Test A",
				"Test A B"
			};

			string[] sorted = names.Order(ContactNameComparer.Instance).ToArray();

			Assert.IsTrue(expected.SequenceEqual(sorted));
		}
	}
}

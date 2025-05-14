using NUnit.Framework;
using System;

namespace SEE.Utils
{
    /// <summary>
    /// Tests for <see cref="SEEDate"/>.
    /// </summary>
    internal class TestSEEDate
    {
        private const string aDate = "2023/02/28";

        /// <summary>
        /// Tests <see cref="SEEDate.ToDate"/>.
        /// </summary>
        [Test]
        public void TestToDate()
        {
            Assert.AreEqual(new DateTime(2023, 2, 28), SEEDate.ToDate(aDate));
        }

        /// <summary>
        /// Tests <see cref="SEEDate.ToDate"/> with an invalid date.
        /// </summary>
        [Test]
        public void TestInvalidDate()
        {
            Assert.Throws<ArgumentException>(() => SEEDate.ToDate("2023-10-01"));
        }

        /// <summary>
        /// Tests <see cref="SEEDate.ToDate"/> with an impossible date (no leap year).
        /// </summary>
        [Test]
        public void TestImpossibleDate1()
        {
            Assert.Throws<ArgumentException>(() => SEEDate.ToDate("2023/02/29"));
        }

        /// <summary>
        /// Tests <see cref="SEEDate.ToDate"/> with an impossible date (wrong
        /// number of days of month).
        /// </summary>
        [Test]
        public void TestImpossibleDate2()
        {
            Assert.Throws<ArgumentException>(() => SEEDate.ToDate("2023/04/31"));
        }
    }
}

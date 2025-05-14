using NUnit.Framework;
using System;
using System.Globalization;

namespace SEE.Utils
{
    /// <summary>
    /// Test for <see cref="SEEDate"/>.
    /// </summary>
    internal class TestSEEDate
    {
        private const string aDate = "2023/10/01";

        /// <summary>
        /// Tests the empty constructor.
        /// </summary>
        [Test]
        public void TestEmptyConstructor()
        {
            DateTime now = DateTime.Now;
            SEEDate date = new();
            Assert.AreEqual(now, date.Get());
        }

        /// <summary>
        /// Tests the constructor.
        /// </summary>
        [Test]
        public void TestConstructor()
        {
            SEEDate date = new(aDate);
            Assert.AreEqual(new DateTime(2023, 10, 1), date.Get());
        }

        /// <summary>
        /// Tests the setter.
        /// </summary>
        [Test]
        public void TestSet()
        {
            SEEDate date = new();
            date.Set(aDate);
            Assert.AreEqual(new DateTime(2023, 10, 1), date.Get());
        }
        /// <summary>
        /// Tests the setter with an invalid date.
        /// </summary>
        [Test]
        public void TestSetInvalidDate()
        {
            SEEDate date = new();
            Assert.Throws<ArgumentException>(() => date.Set("2023-10-01"));
        }

        [Test]
        public void TestToString1()
        {
            SEEDate date = new(aDate);
            Assert.AreEqual(aDate, date.ToString());
        }

        [Test]
        public void TestToString2()
        {
            DateTime now = DateTime.Now;
            SEEDate date = new();
            Assert.AreEqual(now.ToString(SEEDate.DateFormat, CultureInfo.InvariantCulture), date.ToString());
            UnityEngine.Debug.Log(date.ToString());
        }

        [Test]
        public void TestFormat()
        {
            Assert.IsTrue(DateTime.TryParseExact("30/01/2009", "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime _));
            Assert.IsTrue(DateTime.TryParseExact("2009/01/30", "yyyy/MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime _));
        }
    }
}

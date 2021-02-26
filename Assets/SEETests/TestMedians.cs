using System.Collections.Generic;
using NUnit.Framework;

namespace SEE.Utils
{
    /// <summary>
    /// Test for median calculation in class Medians.
    /// </summary>
    internal class TestMedians
    {
        [Test]
        public void TestMedianNull()
        {
            ICollection<float> values = null;
            Assert.Throws<System.ArgumentException>(() => Medians.Median(values));
        }

        [Test]
        public void TestMedianEmpty()
        {
            ICollection<float> values = new List<float>();
            Assert.Throws<System.ArgumentException>(() => Medians.Median(values));
        }

        [Test]
        public void TestMedianOne()
        {
            ICollection<float> values = new List<float>() { 1 };
            Assert.AreEqual(1, Medians.Median(values));
        }

        [Test]
        public void TestMedianTwo()
        {
            ICollection<float> values = new List<float>() { 1, 3};
            Assert.AreEqual(2, Medians.Median(values));
        }

        [Test]
        public void TestMedianThree()
        {
            ICollection<float> values = new List<float>() { 1, 2, 3 };
            Assert.AreEqual(2, Medians.Median(values));
        }

        [Test]
        public void TestMedianFour()
        {
            ICollection<float> values = new List<float>() { 1, 2, 4, 5};
            Assert.AreEqual(3, Medians.Median(values));
        }
    }
}

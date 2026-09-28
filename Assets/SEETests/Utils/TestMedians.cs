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
            Assert.That(() => Medians.Median(values), Throws.TypeOf<System.ArgumentException>());
        }

        [Test]
        public void TestMedianEmpty()
        {
            ICollection<float> values = new List<float>();
            Assert.That(() => Medians.Median(values), Throws.TypeOf<System.ArgumentException>());
        }

        [Test]
        public void TestMedianOne()
        {
            ICollection<float> values = new List<float>() { 1 };
            Assert.That(Medians.Median(values), Is.EqualTo(1f));
        }

        [Test]
        public void TestMedianTwo()
        {
            ICollection<float> values = new List<float>() { 1, 3};
            Assert.That(Medians.Median(values), Is.EqualTo(2f));
        }

        [Test]
        public void TestMedianThree()
        {
            ICollection<float> values = new List<float>() { 1, 2, 3 };
            Assert.That(Medians.Median(values), Is.EqualTo(2f));
        }

        [Test]
        public void TestMedianFour()
        {
            ICollection<float> values = new List<float>() { 1, 2, 4, 5};
            Assert.That(Medians.Median(values), Is.EqualTo(3f));
        }
    }
}

using NUnit.Framework;
using UnityEngine;

namespace SEE.Game.Drawable.Configurations
{
    /// <summary>
    /// Tests the cloning semantics of <see cref="LineCapConf"/>.
    /// </summary>
    [TestFixture]
    public class TestLineCapConfClone
    {
        /// <summary>
        /// Verifies that cloning creates an independent line cap configuration with the same values.
        /// </summary>
        [Test]
        public void CloneCreatesIndependentConfiguration()
        {
            LineCapConf original = new()
            {
                PrimaryColor = Color.red,
                SecondaryColor = Color.blue,
                Thickness = 2.0f,
                Tiling = 3.0f,
                FillOutStatus = true,
                FillOutColor = Color.green,
                UseOwnVisuals = true
            };

            LineCapConf clone = original.Clone();

            Assert.That(clone, Is.Not.SameAs(original));
            Assert.That(clone.Equals(original), Is.True);

            clone.Thickness = 5.0f;
            clone.PrimaryColor = Color.black;

            Assert.That(original.Thickness, Is.EqualTo(2.0f));
            Assert.That(original.PrimaryColor, Is.EqualTo(Color.red));
        }
    }
}

using NUnit.Framework;
using UnityEngine;

namespace SEE.Game.Drawable.Configurations
{
    /// <summary>
    /// Tests the cloning semantics of <see cref="TextConf"/>.
    /// </summary>
    [TestFixture]
    public class TestTextConfClone
    {
        /// <summary>
        /// Verifies that cloning creates an independent text configuration with the same values.
        /// </summary>
        [Test]
        public void CloneCreatesIndependentConfiguration()
        {
            TextConf original = new()
            {
                ID = "Text",
                Text = "Original",
                FontColor = Color.red,
                FontSize = 12.0f
            };

            TextConf clone = original.Clone();

            Assert.That(clone, Is.Not.SameAs(original));
            Assert.That(clone.ID, Is.EqualTo(original.ID));
            Assert.That(clone.Text, Is.EqualTo(original.Text));
            Assert.That(clone.FontColor, Is.EqualTo(original.FontColor));
            Assert.That(clone.FontSize, Is.EqualTo(original.FontSize));

            clone.Text = "Changed";
            clone.FontSize = 24.0f;

            Assert.That(original.Text, Is.EqualTo("Original"));
            Assert.That(original.FontSize, Is.EqualTo(12.0f));
        }
    }
}

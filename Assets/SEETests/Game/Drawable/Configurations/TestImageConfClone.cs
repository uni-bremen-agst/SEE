using NUnit.Framework;

namespace SEE.Game.Drawable.Configurations
{
    /// <summary>
    /// Tests the cloning semantics of <see cref="ImageConf"/>.
    /// </summary>
    [TestFixture]
    public class TestImageConfClone
    {
        /// <summary>
        /// Verifies that image file data is copied into an independent array.
        /// </summary>
        [Test]
        public void CloneCopiesFileData()
        {
            ImageConf original = new()
            {
                FileData = new byte[] { 1, 2, 3 }
            };

            ImageConf clone = original.Clone();

            Assert.That(clone.FileData, Is.Not.SameAs(original.FileData));
            Assert.That(clone.FileData, Is.EqualTo(original.FileData));

            clone.FileData[0] = 9;

            Assert.That(original.FileData[0], Is.EqualTo(1));
        }

        /// <summary>
        /// Verifies that a null file data array remains null in the cloned configuration.
        /// </summary>
        [Test]
        public void ClonePreservesNullFileData()
        {
            ImageConf original = new()
            {
                FileData = null
            };

            ImageConf clone = original.Clone();

            Assert.That(clone.FileData, Is.Null);
        }
    }
}

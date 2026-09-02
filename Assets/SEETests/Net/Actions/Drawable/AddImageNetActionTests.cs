using NUnit.Framework;
using SEE.Game.Drawable.Configurations;
using System;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// Tests the image configuration validation of <see cref="AddImageNetAction"/>.
    /// </summary>
    [TestFixture]
    public class AddImageNetActionTests
    {
        /// <summary>
        /// Verifies that an image configuration without file data is rejected.
        /// </summary>
        [Test]
        public void ValidateImageConfigurationRejectsImageWithoutFileData()
        {
            ImageConf image = new()
            {
                FileData = null
            };

            Assert.Throws<ArgumentException>(() => AddImageNetAction.ValidateImageConfiguration(image));
        }

        /// <summary>
        /// Verifies that an image configuration with file data is accepted.
        /// </summary>
        [Test]
        public void ValidateImageConfigurationAcceptsImageWithFileData()
        {
            ImageConf image = new()
            {
                FileData = new byte[] { 1, 2, 3 }
            };

            Assert.DoesNotThrow(() => AddImageNetAction.ValidateImageConfiguration(image));
        }

        /// <summary>
        /// Verifies that an image configuration with empty file data is rejected.
        /// </summary>
        [Test]
        public void ValidateImageConfigurationRejectsImageWithEmptyFileData()
        {
            ImageConf image = new()
            {
                FileData = Array.Empty<byte>()
            };

            Assert.Throws<ArgumentException>(() => AddImageNetAction.ValidateImageConfiguration(image));
        }
    }
}

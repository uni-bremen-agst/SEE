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
        /// Verifies that a local image configuration without file data is rejected.
        /// </summary>
        [Test]
        public void ValidateImageConfigurationRejectsLocalImageWithoutFileData()
        {
            ImageConf image = new()
            {
                FileData = null,
                URL = string.Empty
            };

            Assert.Throws<ArgumentException>(() => AddImageNetAction.ValidateImageConfiguration(image));
        }

        /// <summary>
        /// Verifies that a web image configuration does not require file data.
        /// </summary>
        [Test]
        public void ValidateImageConfigurationAcceptsWebImageWithoutFileData()
        {
            ImageConf image = new()
            {
                FileData = null,
                URL = "https://example.com/image.png"
            };

            Assert.DoesNotThrow(() => AddImageNetAction.ValidateImageConfiguration(image));
        }
    }
}

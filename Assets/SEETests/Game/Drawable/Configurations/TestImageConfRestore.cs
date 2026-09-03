using NUnit.Framework;
using System.Collections.Generic;

namespace SEE.Game.Drawable.Configurations
{
    /// <summary>
    /// Tests restoring <see cref="ImageConf"/> configurations.
    /// </summary>
    [TestFixture]
    public class TestImageConfRestore
    {
        /// <summary>
        /// Verifies that the image URL is restored from its own configuration entry
        /// instead of the image path entry.
        /// </summary>
        [Test]
        public void RestoreUsesUrlLabel()
        {
            const string path = "local/image.png";
            const string url = "https://example.com/image.png";

            Dictionary<string, object> attributes = new()
            {
                { "PathLabel", path },
                { "UrlLabel", url }
            };

            ImageConf image = new();

            image.Restore(attributes);

            Assert.That(image.Path, Is.EqualTo(path));
            Assert.That(image.URL, Is.EqualTo(url));
        }
    }
}

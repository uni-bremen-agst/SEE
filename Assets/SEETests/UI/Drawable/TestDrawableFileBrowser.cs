using NUnit.Framework;
using System.Reflection;
using UnityEngine;

namespace SEE.UI.Drawable
{
    /// <summary>
    /// Tests the lifecycle behavior of <see cref="DrawableFileBrowser"/>.
    /// </summary>
    [TestFixture]
    public class TestDrawableFileBrowser
    {
        /// <summary>
        /// The game object hosting the file browser component.
        /// </summary>
        private GameObject gameObject;

        /// <summary>
        /// The file browser under test.
        /// </summary>
        private DrawableFileBrowser browser;

        /// <summary>
        /// Creates a fresh file browser for each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("DrawableFileBrowserTest");
            browser = gameObject.AddComponent<DrawableFileBrowser>();
        }

        /// <summary>
        /// Destroys the temporary game object after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (gameObject != null)
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        /// <summary>
        /// Verifies that closing the file browser discards a file path
        /// that has not yet been consumed.
        /// </summary>
        [Test]
        public void TestCloseDiscardsPendingFilePath()
        {
            SetPrivateField("gotFilePath", true);
            SetPrivateField("path", "pending-image.png");

            browser.Close();

            bool hasPath = browser.TryGetFilePath(out string filePath);

            Assert.That(hasPath, Is.False);
            Assert.That(filePath, Is.Empty);
        }

        /// <summary>
        /// Sets a private instance field of the file browser.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="fieldName">The name of the field.</param>
        /// <param name="value">The value to assign.</param>
        private void SetPrivateField<T>(string fieldName, T value)
        {
            FieldInfo field = typeof(DrawableFileBrowser).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null,
                $"Could not find field '{fieldName}'.");

            field.SetValue(browser, value);
        }
    }
}

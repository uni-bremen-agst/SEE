using Michsky.UI.ModernUIPack;
using NUnit.Framework;
using UnityEngine;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Tests the image source selection menu.
    /// </summary>
    [TestFixture]
    public class TestImageSourceMenu : DrawableMenuTestBase
    {
        /// <summary>
        /// Destroys the image source menu after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            ImageSourceMenu.Instance.Destroy();
        }

        /// <summary>
        /// Verifies that selecting the local source can be consumed.
        /// </summary>
        [Test]
        public void TestLocalSourceCanBeSelected()
        {
            ImageSourceMenu.Instance.Enable();

            FindButton("Local").clickEvent.Invoke();

            Assert.That(
                ImageSourceMenu.Instance.TryGetSource(
                    out ImageSourceMenu.Source source),
                Is.True);

            Assert.That(source, Is.EqualTo(ImageSourceMenu.Source.Local));
        }

        /// <summary>
        /// Verifies that selecting the web source can be consumed.
        /// </summary>
        [Test]
        public void TestWebSourceCanBeSelected()
        {
            ImageSourceMenu.Instance.Enable();

            FindButton("Web").clickEvent.Invoke();

            Assert.That(
                ImageSourceMenu.Instance.TryGetSource(
                    out ImageSourceMenu.Source source),
                Is.True);

            Assert.That(source, Is.EqualTo(ImageSourceMenu.Source.Web));
        }

        /// <summary>
        /// Verifies that a source selection can only be consumed once.
        /// </summary>
        [Test]
        public void TestSourceCanOnlyBeConsumedOnce()
        {
            ImageSourceMenu.Instance.Enable();
            FindButton("Local").clickEvent.Invoke();

            Assert.That(
                ImageSourceMenu.Instance.TryGetSource(out _),
                Is.True);

            Assert.That(
                ImageSourceMenu.Instance.TryGetSource(
                    out ImageSourceMenu.Source source),
                Is.False);

            Assert.That(source, Is.EqualTo(ImageSourceMenu.Source.None));
        }

        /// <summary>
        /// Verifies that destroying the menu discards an unconsumed source.
        /// </summary>
        [Test]
        public void TestDestroyDiscardsPendingSource()
        {
            ImageSourceMenu.Instance.Enable();
            FindButton("Web").clickEvent.Invoke();

            ImageSourceMenu.Instance.Destroy();

            Assert.That(
                ImageSourceMenu.Instance.TryGetSource(
                    out ImageSourceMenu.Source source),
                Is.False);

            Assert.That(source, Is.EqualTo(ImageSourceMenu.Source.None));
        }

        /// <summary>
        /// Verifies that canceling the menu does not produce a source selection.
        /// </summary>
        [Test]
        public void TestCancelDoesNotSelectSource()
        {
            ImageSourceMenu.Instance.Enable();

            FindButton("Cancel").clickEvent.Invoke();

            Assert.That(
                ImageSourceMenu.Instance.TryGetSource(
                    out ImageSourceMenu.Source source),
                Is.False);

            Assert.That(source, Is.EqualTo(ImageSourceMenu.Source.None));
        }

        /// <summary>
        /// Verifies that reopening the menu does not retain a previous selection.
        /// </summary>
        [Test]
        public void TestReopeningStartsWithoutPreviousSelection()
        {
            ImageSourceMenu.Instance.Enable();
            FindButton("Local").clickEvent.Invoke();

            ImageSourceMenu.Instance.Destroy();
            ImageSourceMenu.Instance.Enable();

            Assert.That(
                ImageSourceMenu.Instance.TryGetSource(
                    out ImageSourceMenu.Source source),
                Is.False);

            Assert.That(source, Is.EqualTo(ImageSourceMenu.Source.None));
        }

        /// <summary>
        /// Finds a button of the currently instantiated image source menu.
        /// </summary>
        /// <param name="name">The button name.</param>
        /// <returns>The matching button manager.</returns>
        private static ButtonManagerBasic FindButton(string name)
        {
            foreach (ButtonManagerBasic candidate
                     in Object.FindObjectsByType<ButtonManagerBasic>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (candidate.gameObject.name == name)
                {
                    return candidate;
                }
            }

            Assert.Fail(
                $"Could not find the button '{name}' of the image source menu.");
            return null;
        }
    }
}

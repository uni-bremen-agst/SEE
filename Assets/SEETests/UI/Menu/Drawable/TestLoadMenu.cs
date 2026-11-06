using Michsky.UI.ModernUIPack;
using NUnit.Framework;
using UnityEngine;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Tests the drawable load menu.
    /// </summary>
    [TestFixture]
    public class TestLoadMenu : DrawableMenuTestBase
    {
        /// <summary>
        /// Destroys the load menu after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            LoadMenu.Instance.Destroy();
        }

        /// <summary>
        /// Verifies that all load-menu buttons invoke the callbacks
        /// registered for the current menu instance.
        /// </summary>
        [Test]
        public void TestEnableRegistersButtonCallbacks()
        {
            bool loadCalled = false;
            bool loadSpecificCalled = false;
            bool loadCurrentPageCalled = false;

            LoadMenu.Instance.Enable(
                () => loadCalled = true,
                () => loadSpecificCalled = true,
                () => loadCurrentPageCalled = true);

            FindButton("Load").clickEvent.Invoke();
            FindButton("LoadSpecific").clickEvent.Invoke();
            FindButton("LoadSpecificCurrentPage").clickEvent.Invoke();

            Assert.That(loadCalled, Is.True);
            Assert.That(loadSpecificCalled, Is.True);
            Assert.That(loadCurrentPageCalled, Is.True);
        }

        /// <summary>
        /// Finds a button of the currently instantiated load menu.
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

            Assert.Fail($"Could not find the button '{name}' of the load menu.");
            return null;
        }
    }
}

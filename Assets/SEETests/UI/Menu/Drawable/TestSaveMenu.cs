using Michsky.MUIP;
using NUnit.Framework;
using UnityEngine;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Tests the drawable save menu.
    /// </summary>
    [TestFixture]
    public class TestSaveMenu : DrawableMenuTestBase
    {
        [TearDown]
        public void TearDown()
        {
            SaveMenu.Instance.Destroy();
        }

        [Test]
        public void TestEnableRegistersButtonCallbacks()
        {
            bool saveCalled = false;
            bool saveCurrentPageCalled = false;
            bool saveAllCalled = false;

            SaveMenu.Instance.Enable(
                () => saveCalled = true,
                () => saveCurrentPageCalled = true,
                () => saveAllCalled = true);

            FindButton("Save").onClick.Invoke();
            FindButton("SaveCurrentPage").onClick.Invoke();
            FindButton("SaveAll").onClick.Invoke();

            Assert.That(saveCalled, Is.True);
            Assert.That(saveCurrentPageCalled, Is.True);
            Assert.That(saveAllCalled, Is.True);
        }

        private static ButtonManager FindButton(string name)
        {
            foreach (ButtonManager candidate
                     in Object.FindObjectsByType<ButtonManager>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (candidate.gameObject.name == name)
                {
                    return candidate;
                }
            }

            Assert.Fail($"Could not find the button '{name}' of the save menu.");
            return null;
        }
    }
}

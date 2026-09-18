using Michsky.UI.ModernUIPack;
using NUnit.Framework;
using SEE.Game;
using SEE.Game.Drawable;
using UnityEngine;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Tests the sticky note move menu.
    /// </summary>
    [TestFixture]
    public class TestStickyNoteMoveMenu : DrawableMenuTestBase
    {
        /// <summary>
        /// The sticky note holder used by the tests.
        /// </summary>
        private GameObject stickyNoteHolder;

        /// <summary>
        /// Creates the sticky note hierarchy required by each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            stickyNoteHolder = new GameObject("StickyNote");

            GameObject surface = new GameObject("Surface");
            surface.tag = Tags.Drawable;
            surface.transform.SetParent(stickyNoteHolder.transform);
        }

        /// <summary>
        /// Destroys the move menu and sticky note hierarchy after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            StickyNoteMoveMenu.Instance.Destroy();

            if (stickyNoteHolder != null)
            {
                Object.DestroyImmediate(stickyNoteHolder);
            }
        }

        /// <summary>
        /// Verifies that the menu initially uses the normal movement speed.
        /// </summary>
        [Test]
        public void TestDefaultSpeedIsNormalMoveSpeed()
        {
            StickyNoteMoveMenu.Instance.Enable(stickyNoteHolder, true);

            Assert.That(StickyNoteMoveMenu.Instance.GetSpeed(), Is.EqualTo(ValueHolder.Move));
        }

        /// <summary>
        /// Verifies that the speed switch changes the movement speed.
        /// </summary>
        [Test]
        public void TestSpeedSwitchChangesMovementSpeed()
        {
            StickyNoteMoveMenu.Instance.Enable(stickyNoteHolder, true);

            SwitchManager speedSwitch = FindSpeedSwitch();

            speedSwitch.OnEvents.Invoke();
            Assert.That(StickyNoteMoveMenu.Instance.GetSpeed(), Is.EqualTo(ValueHolder.MoveFast));

            speedSwitch.OffEvents.Invoke();
            Assert.That(StickyNoteMoveMenu.Instance.GetSpeed(), Is.EqualTo(ValueHolder.Move));
        }

        /// <summary>
        /// Verifies that completing the move operation can only be consumed once.
        /// </summary>
        [Test]
        public void TestFinishCanOnlyBeConsumedOnce()
        {
            StickyNoteMoveMenu.Instance.Enable(stickyNoteHolder, true);

            ButtonManagerBasic finishButton = FindFinishButton();
            finishButton.clickEvent.Invoke();

            Assert.That(StickyNoteMoveMenu.Instance.TryGetFinish(out bool firstFinish), Is.True);
            Assert.That(firstFinish, Is.True);

            Assert.That(StickyNoteMoveMenu.Instance.TryGetFinish(out bool secondFinish), Is.False);
            Assert.That(secondFinish, Is.False);
        }

        /// <summary>
        /// Finds the speed switch of the sticky note move menu.
        /// </summary>
        /// <returns>The speed switch.</returns>
        private static SwitchManager FindSpeedSwitch()
        {
            foreach (SwitchManager candidate in Object.FindObjectsByType<SwitchManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.gameObject.name == "SpeedSwitch")
                {
                    return candidate;
                }
            }

            Assert.Fail("Could not find the speed switch of the sticky note move menu.");
            return null;
        }

        /// <summary>
        /// Finds the finish button of the sticky note move menu.
        /// </summary>
        /// <returns>The finish button.</returns>
        private static ButtonManagerBasic FindFinishButton()
        {
            foreach (ButtonManagerBasic candidate in Object.FindObjectsByType<ButtonManagerBasic>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.gameObject.name == "Finish")
                {
                    return candidate;
                }
            }

            Assert.Fail("Could not find the finish button of the sticky note move menu.");
            return null;
        }
    }
}

using Michsky.UI.ModernUIPack;
using NUnit.Framework;
using SEE.Game;
using SEE.Game.Drawable;
using SEE.UI.Menu.Drawable;
using UnityEngine;

namespace SEE.UI.Menu.Drawable.StickyNoteRotation
{
    /// <summary>
    /// Tests the interaction and navigation behavior of
    /// <see cref="StickyNoteRotationMenu"/>.
    /// </summary>
    [TestFixture]
    public class TestStickyNoteRotationMenu : DrawableMenuTestBase
    {
        /// <summary>
        /// The maximum allowed angular difference when comparing rotations.
        /// </summary>
        private const float angleTolerance = 0.01f;

        /// <summary>
        /// The sticky note holder rotated by the menu.
        /// </summary>
        private GameObject stickyNote;

        /// <summary>
        /// The object on which the sticky note is considered to have been placed.
        /// </summary>
        private GameObject hitObject;

        /// <summary>
        /// Creates the Drawable hierarchy required by each test and resets the
        /// static menu state.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            StickyNoteRotationMenu.Destroy();
            StickyNoteRotationMenu.TryGetFinish(out _);

            stickyNote = new GameObject("StickyNote");

            GameObject surface = new GameObject("Surface");
            surface.tag = Tags.Drawable;
            surface.transform.SetParent(stickyNote.transform);

            hitObject = new GameObject("Wall");
        }

        /// <summary>
        /// Destroys the rotation menus and objects created for the current test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            StickyNoteRotationMenu.Destroy();
            StickyNoteRotationMenu.TryGetFinish(out _);

            if (stickyNote != null)
            {
                Object.DestroyImmediate(stickyNote);
            }

            if (hitObject != null)
            {
                Object.DestroyImmediate(hitObject);
            }
        }

        /// <summary>
        /// Verifies that the predefined x rotation buttons apply the expected
        /// rotation to the sticky note.
        /// </summary>
        /// <param name="buttonName">The name of the rotation button.</param>
        /// <param name="expectedDegree">The expected x rotation in degrees.</param>
        [TestCase("Laying", 90.0f)]
        [TestCase("Hanging", 0.0f)]
        public void TestXRotationButtons(string buttonName, float expectedDegree)
        {
            stickyNote.transform.localEulerAngles = new Vector3(25.0f, 0.0f, 0.0f);

            StickyNoteRotationMenu.Enable(stickyNote, hitObject);

            FindActiveButton(buttonName).clickEvent.Invoke();

            AssertAngle(stickyNote.transform.localEulerAngles.x, expectedDegree);
        }

        /// <summary>
        /// Verifies that the next and back buttons switch between the x and y
        /// rotation menus.
        /// </summary>
        [Test]
        public void TestNavigationBetweenRotationMenus()
        {
            StickyNoteRotationMenu.Enable(stickyNote, hitObject);

            Assert.That(StickyNoteRotationMenu.IsYActive(), Is.False);

            FindActiveButton("Next").clickEvent.Invoke();

            Assert.That(StickyNoteRotationMenu.IsYActive(), Is.True);

            FindActiveButton("Back").clickEvent.Invoke();

            Assert.That(StickyNoteRotationMenu.IsYActive(), Is.False);
        }

        /// <summary>
        /// Verifies that every predefined y rotation button applies the expected
        /// rotation to the sticky note.
        /// </summary>
        /// <param name="buttonName">The name of the rotation button.</param>
        /// <param name="expectedDegree">The expected y rotation in degrees.</param>
        [TestCase("0", 0.0f)]
        [TestCase("90", 90.0f)]
        [TestCase("180", 180.0f)]
        [TestCase("270", 270.0f)]
        public void TestYRotationButtons(string buttonName, float expectedDegree)
        {
            stickyNote.transform.localEulerAngles = new Vector3(0.0f, 13.0f, 0.0f);

            StickyNoteRotationMenu.Enable(stickyNote, hitObject);
            FindActiveButton("Next").clickEvent.Invoke();

            FindActiveButton(buttonName).clickEvent.Invoke();

            AssertAngle(stickyNote.transform.localEulerAngles.y, expectedDegree);
        }

        /// <summary>
        /// Verifies that assigning a value through the public y slider interface
        /// applies the corresponding rotation to the sticky note.
        /// </summary>
        [Test]
        public void TestAssignValueToYSliderChangesRotation()
        {
            StickyNoteRotationMenu.Enable(stickyNote, hitObject);
            FindActiveButton("Next").clickEvent.Invoke();

            StickyNoteRotationMenu.AssignValueToYSlider(123.4f);

            AssertAngle(stickyNote.transform.localEulerAngles.y, 123.4f);
        }

        /// <summary>
        /// Verifies that the return callback can be invoked from the x rotation menu.
        /// </summary>
        [Test]
        public void TestXRotationReturnInvokesCallback()
        {
            bool callbackInvoked = false;

            StickyNoteRotationMenu.Enable(stickyNote, hitObject, () => callbackInvoked = true);

            FindActiveButton("ReturnBtn").clickEvent.Invoke();

            Assert.That(callbackInvoked, Is.True);
        }

        /// <summary>
        /// Verifies that the return callback remains available after switching to
        /// the y rotation menu.
        /// </summary>
        [Test]
        public void TestYRotationReturnInvokesCallback()
        {
            bool callbackInvoked = false;

            StickyNoteRotationMenu.Enable(stickyNote, hitObject, () => callbackInvoked = true);
            FindActiveButton("Next").clickEvent.Invoke();

            FindActiveButton("ReturnBtn").clickEvent.Invoke();

            Assert.That(callbackInvoked, Is.True);
        }

        /// <summary>
        /// Verifies that finishing the rotation configuration produces exactly one
        /// consumable finish state.
        /// </summary>
        [Test]
        public void TestFinishCanOnlyBeConsumedOnce()
        {
            StickyNoteRotationMenu.Enable(stickyNote, hitObject);
            FindActiveButton("Next").clickEvent.Invoke();

            FindActiveButton("Finish").clickEvent.Invoke();

            Assert.That(StickyNoteRotationMenu.TryGetFinish(out bool finish), Is.True);
            Assert.That(finish, Is.True);

            Assert.That(StickyNoteRotationMenu.TryGetFinish(out finish), Is.False);
            Assert.That(finish, Is.False);
        }

        /// <summary>
        /// Finds an active button with the given name in the currently instantiated
        /// rotation menu.
        /// </summary>
        /// <param name="name">The name of the requested button.</param>
        /// <returns>The active button manager.</returns>
        private static ButtonManagerBasic FindActiveButton(string name)
        {
            foreach (ButtonManagerBasic candidate in Object.FindObjectsByType<ButtonManagerBasic>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.gameObject.name == name && candidate.gameObject.activeInHierarchy)
                {
                    return candidate;
                }
            }

            Assert.Fail($"Could not find active button '{name}' in the sticky note rotation menu.");
            return null;
        }

        /// <summary>
        /// Verifies that two angles are equal within the test tolerance.
        /// </summary>
        /// <param name="actual">The actual angle in degrees.</param>
        /// <param name="expected">The expected angle in degrees.</param>
        private static void AssertAngle(float actual, float expected)
        {
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(actual, expected)), Is.LessThan(angleTolerance));
        }
    }
}

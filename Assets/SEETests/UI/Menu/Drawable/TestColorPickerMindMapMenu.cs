using Michsky.UI.ModernUIPack;
using NUnit.Framework;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Tests the color picker menu used for Mind Map nodes.
    /// </summary>
    [TestFixture]
    public class TestColorPickerMindMapMenu : DrawableMenuTestBase
    {
        /// <summary>
        /// Destroys the menu after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            ColorPickerMindMapMenu.Instance.Destroy();
        }

        /// <summary>
        /// Verifies that the primary border color can be selected.
        /// </summary>
        [Test]
        public void TestPrimaryBorderColorCanBeSelected()
        {
            MindMapNodeConf configuration = CreateConfiguration();
            ColorPickerMindMapMenu.Instance.Enable(configuration, true);

            FindButton("Border").clickEvent.Invoke();

            Assert.That(ColorPickerMindMapMenu.Instance.TryGetColor(out Color color), Is.True);
            Assert.That(color, Is.EqualTo(configuration.BorderConf.PrimaryColor));
        }

        /// <summary>
        /// Verifies that the secondary border color can be selected for a non-monochrome border.
        /// </summary>
        [Test]
        public void TestSecondaryBorderColorCanBeSelected()
        {
            MindMapNodeConf configuration = CreateConfiguration();
            ColorPickerMindMapMenu.Instance.Enable(configuration, false);

            FindButton("Border").clickEvent.Invoke();

            Assert.That(ColorPickerMindMapMenu.Instance.TryGetColor(out Color color), Is.True);
            Assert.That(color, Is.EqualTo(configuration.BorderConf.SecondaryColor));
        }

        /// <summary>
        /// Verifies that a monochrome border returns its primary color when a secondary color is requested.
        /// </summary>
        [Test]
        public void TestMonochromeBorderUsesPrimaryColorForSecondarySelection()
        {
            MindMapNodeConf configuration = CreateConfiguration();
            configuration.BorderConf.ColorKind = ColorKind.Monochrome;

            ColorPickerMindMapMenu.Instance.Enable(configuration, false);
            FindButton("Border").clickEvent.Invoke();

            Assert.That(ColorPickerMindMapMenu.Instance.TryGetColor(out Color color), Is.True);
            Assert.That(color, Is.EqualTo(configuration.BorderConf.PrimaryColor));
        }

        /// <summary>
        /// Verifies that the text outline color can be selected.
        /// </summary>
        [Test]
        public void TestTextOutlineColorCanBeSelected()
        {
            MindMapNodeConf configuration = CreateConfiguration();
            ColorPickerMindMapMenu.Instance.Enable(configuration, false);

            FindButton("NodeText").clickEvent.Invoke();

            Assert.That(ColorPickerMindMapMenu.Instance.TryGetColor(out Color color), Is.True);
            Assert.That(color, Is.EqualTo(configuration.TextConf.OutlineColor));
        }

        /// <summary>
        /// Verifies that the branch-line button is disabled for a node without a parent.
        /// </summary>
        [Test]
        public void TestBranchLineButtonIsDisabledWithoutParent()
        {
            MindMapNodeConf configuration = CreateConfiguration();
            configuration.BranchLineToParent = "";

            ColorPickerMindMapMenu.Instance.Enable(configuration, true);

            ButtonManagerBasic branchButton = FindButton("BranchLine");
            Assert.That(branchButton.enabled, Is.False);
            Assert.That(branchButton.GetComponent<Button>().interactable, Is.False);
        }

        /// <summary>
        /// Verifies that destroying the menu discards an unconsumed color selection.
        /// </summary>
        [Test]
        public void TestDestroyDiscardsPendingColor()
        {
            MindMapNodeConf configuration = CreateConfiguration();
            ColorPickerMindMapMenu.Instance.Enable(configuration, true);

            FindButton("Border").clickEvent.Invoke();
            ColorPickerMindMapMenu.Instance.Destroy();

            Assert.That(ColorPickerMindMapMenu.Instance.TryGetColor(out Color color), Is.False);
            Assert.That(color, Is.EqualTo(Color.clear));
        }

        /// <summary>
        /// Creates a Mind Map node configuration containing distinct colors for all tested elements.
        /// </summary>
        /// <returns>The created configuration.</returns>
        private static MindMapNodeConf CreateConfiguration()
        {
            return new MindMapNodeConf
            {
                BorderConf = new LineConf
                {
                    PrimaryColor = Color.red,
                    SecondaryColor = Color.blue,
                    ColorKind = ColorKind.Gradient
                },
                TextConf = new TextConf
                {
                    FontColor = Color.green,
                    OutlineColor = Color.yellow
                },
                BranchLineToParent = "BranchLine",
                BranchLineConf = new LineConf
                {
                    PrimaryColor = Color.magenta,
                    SecondaryColor = Color.cyan,
                    ColorKind = ColorKind.Gradient
                }
            };
        }

        /// <summary>
        /// Finds a button of the currently instantiated Mind Map color picker menu.
        /// </summary>
        /// <param name="name">The name of the button.</param>
        /// <returns>The matching button manager.</returns>
        private static ButtonManagerBasic FindButton(string name)
        {
            foreach (ButtonManagerBasic candidate in Object.FindObjectsByType<ButtonManagerBasic>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.gameObject.name == name)
                {
                    return candidate;
                }
            }

            Assert.Fail($"Could not find the button '{name}' of the Mind Map color picker menu.");
            return null;
        }
    }
}

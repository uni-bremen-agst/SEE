using NUnit.Framework;
using SEE.Controls.Actions;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using System.Reflection;
using UnityEngine;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Tests menu transitions handled by <see cref="DrawableMenuLifecycle"/>.
    /// </summary>
    [TestFixture]
    internal class TestDrawableMenuLifecycle : DrawableMenuTestBase
    {
        /// <summary>
        /// Cleans up all color picker menus after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            ColorPickerMenu.Instance.Destroy();
            ColorPickerMindMapMenu.Instance.Destroy();
            ColorPickerLineMenu.Instance.Destroy();
        }

        /// <summary>
        /// Verifies that entering the color picker action opens its main menu.
        /// </summary>
        [Test]
        public void TestEnteringColorPickerOpensMenu()
        {
            Assert.That(ColorPickerMenu.Instance.IsOpen(), Is.False);

            InvokeLifecycle(ActionStateTypes.Edit, ActionStateTypes.ColorPicker);

            Assert.That(ColorPickerMenu.Instance.IsOpen(), Is.True);
        }

        /// <summary>
        /// Verifies that leaving the color picker action destroys its main menu.
        /// </summary>
        [Test]
        public void TestLeavingColorPickerDestroysMenu()
        {
            ColorPickerMenu.Instance.Enable();

            InvokeLifecycle(ActionStateTypes.ColorPicker, ActionStateTypes.Edit);

            Assert.That(ColorPickerMenu.Instance.IsOpen(), Is.False);
        }

        /// <summary>
        /// Verifies that leaving the color picker action also destroys an open Mind Map helper menu.
        /// </summary>
        [Test]
        public void TestLeavingColorPickerDestroysMindMapHelperMenu()
        {
            ColorPickerMindMapMenu.Instance.Enable(CreateMindMapConfiguration(), true);

            Assert.That(ColorPickerMindMapMenu.Instance.IsOpen(), Is.True);

            InvokeLifecycle(ActionStateTypes.ColorPicker, ActionStateTypes.Edit);

            Assert.That(ColorPickerMindMapMenu.Instance.IsOpen(), Is.False);
        }

        /// <summary>
        /// Verifies that leaving the color picker action destroys an open line helper menu
        /// and discards its unfinished selection.
        /// </summary>
        [Test]
        public void TestLeavingColorPickerDestroysLineHelperMenu()
        {
            LineConf configuration = new LineConf
            {
                PrimaryColor = Color.red,
                SecondaryColor = Color.blue,
                ColorKind = ColorKind.Monochrome,
                FillOutStatus = true,
                FillOutColor = Color.green,
                LineCapStart = LineCapConf.CreateNone(),
                LineCapEnd = LineCapConf.CreateNone()
            };

            ColorPickerLineMenu.Instance.BeginSelection(configuration, true);

            Assert.That(ColorPickerLineMenu.Instance.IsOpen(), Is.True);

            InvokeLifecycle(ActionStateTypes.ColorPicker, ActionStateTypes.Edit);

            Assert.That(ColorPickerLineMenu.Instance.IsOpen(), Is.False);
            Assert.That(ColorPickerLineMenu.Instance.TryGetColor(out Color color), Is.False);
            Assert.That(color, Is.EqualTo(Color.clear));
        }

        /// <summary>
        /// Invokes the action-state-change handler of <see cref="DrawableMenuLifecycle"/>.
        /// </summary>
        /// <param name="previousActionState">The action state that was left.</param>
        /// <param name="newActionState">The newly entered action state.</param>
        private static void InvokeLifecycle(ActionStateType previousActionState, ActionStateType newActionState)
        {
            MethodInfo method = typeof(DrawableMenuLifecycle).GetMethod(
                "OnActionStateChanged", BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(method, Is.Not.Null);

            method.Invoke(null, new object[] { previousActionState, newActionState });
        }

        /// <summary>
        /// Creates the minimal Mind Map configuration required to open its color picker helper menu.
        /// </summary>
        /// <returns>The created Mind Map configuration.</returns>
        private static MindMapNodeConf CreateMindMapConfiguration()
        {
            return new MindMapNodeConf
            {
                BorderConf = new LineConf(),
                TextConf = new TextConf(),
                BranchLineToParent = ""
            };
        }
    }
}

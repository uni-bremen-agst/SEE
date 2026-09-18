using NUnit.Framework;
using SEE.Controls.Actions;
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
            ColorPickerLineMenu.Disable();
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
        /// Verifies that leaving the color picker action clears a pending line helper selection.
        /// </summary>
        [Test]
        public void TestLeavingColorPickerClearsLineHelperState()
        {
            GameObject lineMenu = new GameObject("ColorPickerLine");

            SetPrivateStaticField(typeof(ColorPickerLineMenu), "instance", lineMenu);
            SetPrivateStaticField(typeof(ColorPickerLineMenu), "gotColor", true);
            SetPrivateStaticField(typeof(ColorPickerLineMenu), "chosenColor", Color.red);

            InvokeLifecycle(ActionStateTypes.ColorPicker, ActionStateTypes.Edit);

            Assert.That(lineMenu == null, Is.True);
            Assert.That(ColorPickerLineMenu.TryGetColor(out Color color), Is.False);
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

        /// <summary>
        /// Sets a private static field for lifecycle test preparation.
        /// </summary>
        /// <typeparam name="T">The type of the field value.</typeparam>
        /// <param name="type">The type declaring the field.</param>
        /// <param name="fieldName">The name of the field.</param>
        /// <param name="value">The value to assign.</param>
        private static void SetPrivateStaticField<T>(System.Type type, string fieldName, T value)
        {
            FieldInfo field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(field, Is.Not.Null);

            field.SetValue(null, value);
        }
    }
}

using NUnit.Framework;
using SEE.Controls.ReversibleActions;
using SEE.Controls.ReversibleActions.Drawable;
using SEE.UI.Menu.Drawable.Line;
using SEE.UI.Menu.Drawable.Shapes;
using SEE.UI.Menu.Drawable.Text;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.Line;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Play-mode integration tests for <see cref="DrawableMenuLifecycle"/>.
    /// </summary>
    [TestFixture]
    internal class TestDrawableMenuLifecycle : TestUI
    {
        /// <summary>
        /// Ensures that the UI canvas exists, restores the parents of
        /// persistent menu objects, and closes all tool-session menus
        /// before each test.
        /// </summary>
        /// <returns>
        /// An enumerator that waits for UI initialization.
        /// </returns>
        [UnitySetUp]
        public IEnumerator SetUpMenus()
        {
            EnsureUICanvas();

            Transform canvasTransform = UICanvas.Canvas.transform;
            ShapeMenuControls shapeControls = GetShapeControls();

            /// Restore persistent menu objects to the current canvas.
            /// These objects may have survived a previous test canvas.
            shapeControls.SwitchObject.transform.SetParent(
                canvasTransform, false);

            shapeControls.MenuObject.transform.SetParent(
                canvasTransform, false);

            GetTextMenuObject().transform.SetParent(
                canvasTransform, false);

            LineMenu.Instance.Disable();
            ShapeMenu.Disable();
            TextMenu.Instance.Disable();

            yield return null;
        }

        /// <summary>
        /// Closes all tool-session menus and preserves persistent
        /// menu objects before the shared test base destroys a
        /// temporary UI canvas.
        /// </summary>
        /// <returns>
        /// An enumerator that waits for menu cleanup.
        /// </returns>
        [UnityTearDown]
        public IEnumerator TearDownMenus()
        {
            ShapeMenu.Disable();
            TextMenu.Instance.Disable();
            LineMenu.Instance.Disable();

            ColorPickerMindMapMenu.Instance.Destroy();
            ColorPickerLineMenu.Instance.Destroy();

            if (OwnsUICanvas)
            {
                ShapeMenuControls shapeControls = GetShapeControls();

                shapeControls.MenuObject.transform.SetParent(null);
                shapeControls.SwitchObject.transform.SetParent(null);

                GetTextMenuObject().transform.SetParent(null);

                LineMenu.Instance.GameObject.transform.SetParent(null);
            }

            yield return null;
        }

        /// <summary>
        /// Verifies that entering freehand drawing opens the line menu.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTest]
        public IEnumerator TestDrawFreehandOpensLineMenu()
        {
            InvokeLifecycleTransition(
                null,
                ActionStateTypes.DrawFreehand);

            yield return null;

            Assert.That(LineMenu.Instance.IsOpen(), Is.True);
            Assert.That(LineMenu.Instance.IsInDrawingMode(), Is.True);
        }

        /// <summary>
        /// Verifies that leaving freehand drawing closes the line menu.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTest]
        public IEnumerator TestLeavingDrawFreehandClosesLineMenu()
        {
            InvokeLifecycleTransition(
                null,
                ActionStateTypes.DrawFreehand);

            InvokeLifecycleTransition(
                ActionStateTypes.DrawFreehand,
                ActionStateTypes.Edit);

            yield return null;

            Assert.That(LineMenu.Instance.IsOpen(), Is.False);
        }

        /// <summary>
        /// Verifies that entering shape drawing opens the shape menu.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTest]
        public IEnumerator TestDrawShapesOpensShapeMenu()
        {
            InvokeLifecycleTransition(
                null,
                ActionStateTypes.DrawShapes);

            yield return null;

            Assert.That(
                GetShapeSwitch().activeInHierarchy,
                Is.True);
        }

        /// <summary>
        /// Verifies that leaving shape drawing closes the shape menu.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTest]
        public IEnumerator TestLeavingDrawShapesClosesShapeMenu()
        {
            InvokeLifecycleTransition(
                null,
                ActionStateTypes.DrawShapes);

            InvokeLifecycleTransition(
                ActionStateTypes.DrawShapes,
                ActionStateTypes.Edit);

            yield return null;

            Assert.That(
                GetShapeSwitch().activeInHierarchy,
                Is.False);
        }

        /// <summary>
        /// Verifies that entering text writing opens the text menu.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTest]
        public IEnumerator TestWriteTextOpensTextMenu()
        {
            InvokeLifecycleTransition(
                null,
                ActionStateTypes.WriteText);

            yield return null;

            Assert.That(TextMenu.Instance.IsOpen(), Is.True);
        }

        /// <summary>
        /// Verifies that leaving text writing closes the text menu.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTest]
        public IEnumerator TestLeavingWriteTextClosesTextMenu()
        {
            InvokeLifecycleTransition(
                null,
                ActionStateTypes.WriteText);

            InvokeLifecycleTransition(
                ActionStateTypes.WriteText,
                ActionStateTypes.Edit);

            yield return null;

            Assert.That(TextMenu.Instance.IsOpen(), Is.False);
        }

        /// <summary>
        /// Verifies that switching from freehand drawing to shape drawing closes
        /// the line menu and opens the shape menu.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTest]
        public IEnumerator TestSwitchFromFreehandToShapes()
        {
            InvokeLifecycleTransition(
                null,
                ActionStateTypes.DrawFreehand);

            InvokeLifecycleTransition(
                ActionStateTypes.DrawFreehand,
                ActionStateTypes.DrawShapes);

            yield return null;

            Assert.That(LineMenu.Instance.IsOpen(), Is.False);
            Assert.That(GetShapeSwitch().activeInHierarchy, Is.True);
        }

        /// <summary>
        /// Verifies that switching from shape drawing to text writing closes the
        /// shape menu and opens the text menu.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTest]
        public IEnumerator TestSwitchFromShapesToWriteText()
        {
            InvokeLifecycleTransition(
                null,
                ActionStateTypes.DrawShapes);

            InvokeLifecycleTransition(
                ActionStateTypes.DrawShapes,
                ActionStateTypes.WriteText);

            yield return null;

            Assert.That(
                GetShapeSwitch().activeInHierarchy,
                Is.False);

            Assert.That(TextMenu.Instance.IsOpen(), Is.True);
        }

        /// <summary>
        /// Verifies that stopping a color-picker action discards a pending
        /// line helper menu even when no action-state transition occurs.
        /// The selection state is cleared immediately, while the menu
        /// object is destroyed after the current Unity frame.
        /// </summary>
        /// <returns>
        /// An enumerator that waits for deferred menu destruction.
        /// </returns>
        [UnityTest]
        public IEnumerator TestStoppingColorPickerDestroysLineHelperMenu()
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

            IReversibleAction action =
                ColorPickerAction.CreateReversibleAction();

            action.Stop();

            /// The pending color selection must be invalidated immediately.
            Assert.That(
                ColorPickerLineMenu.Instance.TryGetColor(out Color color),
                Is.False);
            Assert.That(color, Is.EqualTo(Color.clear));

            /// Unity destroys game objects at the end of the frame.
            yield return null;

            Assert.That(ColorPickerLineMenu.Instance.IsOpen(), Is.False);
        }

        /// <summary>
        /// Invokes the menu-lifecycle transition handler.
        /// Action-history event semantics themselves are covered by
        /// <c>TestActionHistory</c>.
        /// </summary>
        /// <param name="previousActionState">The action state being left.</param>
        /// <param name="newActionState">The newly entered action state.</param>
        private static void InvokeLifecycleTransition(
            ActionStateType previousActionState,
            ActionStateType newActionState)
        {
            MethodInfo method = typeof(DrawableMenuLifecycle).GetMethod(
                "OnActionStateChanged",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(method, Is.Not.Null);

            method.Invoke(
                null,
                new object[]
                {
                    previousActionState,
                    newActionState
                });
        }

        /// <summary>
        /// Gets the switch object managed by <see cref="ShapeMenu"/>.
        /// </summary>
        /// <returns>The shape-menu switch object.</returns>
        private static GameObject GetShapeSwitch()
        {
            return GetShapeControls().SwitchObject;
        }

        /// <summary>
        /// Gets the controls owned by the static shape menu.
        /// </summary>
        /// <returns>The shape-menu controls.</returns>
        private static ShapeMenuControls GetShapeControls()
        {
            FieldInfo field = typeof(ShapeMenu).GetField(
                "controls",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(field, Is.Not.Null);

            ShapeMenuControls controls =
                field.GetValue(null) as ShapeMenuControls;

            Assert.That(controls, Is.Not.Null);

            return controls;
        }

        /// <summary>
        /// Gets the persistent game object of the text-menu singleton.
        /// </summary>
        /// <returns>The text-menu game object.</returns>
        private static GameObject GetTextMenuObject()
        {
            FieldInfo field = typeof(Menu).GetField(
                "gameObject",
                BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.That(field, Is.Not.Null);

            GameObject result =
                field.GetValue(TextMenu.Instance) as GameObject;

            Assert.That(result, Is.Not.Null);

            return result;
        }
    }
}

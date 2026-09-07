using NUnit.Framework;
using SEE.Controls.Actions;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Play-mode integration tests for <see cref="DrawableMenuLifecycle"/>.
    /// </summary>
    /// <remarks>
    /// These tests are currently blocked by issue #983 because the play-mode
    /// test environment does not provide the SEE scene objects required by
    /// drawable menus.
    /// </remarks>
    [Ignore("Blocked by #983: required SEE UI scene objects are unavailable in play-mode tests.")]
    internal class TestDrawableMenuLifecycle : TestUI
    {
        /// <summary>
        /// Disables all tool-session menus before every test.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnitySetUp]
        public IEnumerator SetUpMenus()
        {
            LineMenu.Instance.Disable();
            ShapeMenu.Disable();
            TextMenu.Instance.Disable();

            yield return null;
        }

        /// <summary>
        /// Disables all tool-session menus after every test.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTearDown]
        public IEnumerator TearDownMenus()
        {
            ShapeMenu.Disable();
            TextMenu.Instance.Disable();
            LineMenu.Instance.Disable();

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
            FieldInfo field = typeof(ShapeMenu).GetField(
                "drawableSwitch",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(field, Is.Not.Null);

            GameObject result = field.GetValue(null) as GameObject;

            Assert.That(result, Is.Not.Null);

            return result;
        }
    }
}

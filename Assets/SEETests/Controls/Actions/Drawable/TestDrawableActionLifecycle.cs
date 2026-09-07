using NUnit.Framework;
using SEE.Controls.Actions;
using SEE.Utils.History;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// Tests the state resets performed by <see cref="DrawableActionLifecycle"/>.
    /// </summary>
    [TestFixture]
    internal class TestDrawableActionLifecycle
    {
        /// <summary>
        /// Binding flags used to access private static lifecycle and action state.
        /// </summary>
        private const BindingFlags PrivateStatic =
            BindingFlags.NonPublic | BindingFlags.Static;

        /// <summary>
        /// A temporary object used as a marker for static selection state.
        /// </summary>
        private GameObject marker;

        /// <summary>
        /// Restores all static action state after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            EditAction.Reset();
            CutCopyPasteAction.Reset();
            ScaleAction.Reset();
            LayerChangeAction.Reset();

            if (marker != null)
            {
                UnityEngine.Object.DestroyImmediate(marker);
                marker = null;
            }
        }

        /// <summary>
        /// Verifies that initializing the lifecycle repeatedly registers exactly
        /// one action-state-change handler.
        /// </summary>
        [Test]
        public void TestInitializeRegistersOnlyOnce()
        {
            DrawableActionLifecycle.Initialize();
            DrawableActionLifecycle.Initialize();

            Assert.That(
                CountLifecycleSubscriptions(),
                Is.EqualTo(1));
        }

        /// <summary>
        /// Verifies that leaving the edit action resets its shared selection state.
        /// </summary>
        [Test]
        public void TestLeavingEditResetsState()
        {
            marker = new GameObject("EditActionMarker");

            SetPrivateStaticField(
                typeof(EditAction), "oldSelectedObj", marker);
            SetPrivateStaticField(
                typeof(EditAction), "mouseWasReleased", false);

            InvokeLifecycle(
                ActionStateTypes.Edit,
                ActionStateTypes.DrawFreehand);

            Assert.That(
                GetPrivateStaticField<GameObject>(
                    typeof(EditAction), "oldSelectedObj"),
                Is.Null);

            Assert.That(
                GetPrivateStaticField<bool>(
                    typeof(EditAction), "mouseWasReleased"),
                Is.True);
        }

        /// <summary>
        /// Verifies that leaving the cut-copy-paste action resets its shared
        /// selection state.
        /// </summary>
        [Test]
        public void TestLeavingCutCopyPasteResetsState()
        {
            marker = new GameObject("CutCopyPasteActionMarker");

            SetPrivateStaticField(
                typeof(CutCopyPasteAction), "oldSelectedObj", marker);
            SetPrivateStaticField(
                typeof(CutCopyPasteAction), "mouseWasReleased", false);

            InvokeLifecycle(
                ActionStateTypes.CutCopyPaste,
                ActionStateTypes.DrawFreehand);

            Assert.That(
                GetPrivateStaticField<GameObject>(
                    typeof(CutCopyPasteAction), "oldSelectedObj"),
                Is.Null);

            Assert.That(
                GetPrivateStaticField<bool>(
                    typeof(CutCopyPasteAction), "mouseWasReleased"),
                Is.True);
        }

        /// <summary>
        /// Verifies that leaving the scale action resets its shared selection state.
        /// </summary>
        [Test]
        public void TestLeavingScaleResetsState()
        {
            marker = new GameObject("ScaleActionMarker");

            SetPrivateStaticField(
                typeof(ScaleAction), "oldSelectedObj", marker);
            SetPrivateStaticField(
                typeof(ScaleAction), "mouseWasReleased", false);

            InvokeLifecycle(
                ActionStateTypes.Scale,
                ActionStateTypes.DrawFreehand);

            Assert.That(
                GetPrivateStaticField<GameObject>(
                    typeof(ScaleAction), "oldSelectedObj"),
                Is.Null);

            Assert.That(
                GetPrivateStaticField<bool>(
                    typeof(ScaleAction), "mouseWasReleased"),
                Is.True);
        }

        /// <summary>
        /// Verifies that leaving the layer changer resets the information state.
        /// </summary>
        [Test]
        public void TestLeavingLayerChangerResetsState()
        {
            SetPrivateStaticField(
                typeof(LayerChangeAction), "showInfo", true);

            InvokeLifecycle(
                ActionStateTypes.LayerChanger,
                ActionStateTypes.DrawFreehand);

            Assert.That(
                GetPrivateStaticField<bool>(
                    typeof(LayerChangeAction), "showInfo"),
                Is.False);
        }

        /// <summary>
        /// Verifies that an unrelated action transition does not reset edit state.
        /// </summary>
        [Test]
        public void TestUnrelatedTransitionDoesNotResetEditState()
        {
            marker = new GameObject("UnrelatedTransitionMarker");

            SetPrivateStaticField(
                typeof(EditAction), "oldSelectedObj", marker);
            SetPrivateStaticField(
                typeof(EditAction), "mouseWasReleased", false);

            InvokeLifecycle(
                ActionStateTypes.DrawFreehand,
                ActionStateTypes.DrawShapes);

            Assert.That(
                GetPrivateStaticField<GameObject>(
                    typeof(EditAction), "oldSelectedObj"),
                Is.SameAs(marker));

            Assert.That(
                GetPrivateStaticField<bool>(
                    typeof(EditAction), "mouseWasReleased"),
                Is.False);
        }

        /// <summary>
        /// Invokes the state-change handler of the drawable action lifecycle.
        /// The action-history event itself is covered separately by
        /// <c>TestActionHistory</c>.
        /// </summary>
        /// <param name="previousActionState">The action state being left.</param>
        /// <param name="newActionState">The newly entered action state.</param>
        private static void InvokeLifecycle(
            ActionStateType previousActionState,
            ActionStateType newActionState)
        {
            MethodInfo method = typeof(DrawableActionLifecycle).GetMethod(
                "OnActionStateChanged",
                PrivateStatic);

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
        /// Sets a private static field.
        /// </summary>
        /// <typeparam name="T">The type of the field value.</typeparam>
        /// <param name="type">The declaring type.</param>
        /// <param name="fieldName">The field name.</param>
        /// <param name="value">The value to assign.</param>
        private static void SetPrivateStaticField<T>(
            Type type,
            string fieldName,
            T value)
        {
            FieldInfo field = type.GetField(fieldName, PrivateStatic);

            Assert.That(field, Is.Not.Null);

            field.SetValue(null, value);
        }

        /// <summary>
        /// Gets a private static field.
        /// </summary>
        /// <typeparam name="T">The expected field type.</typeparam>
        /// <param name="type">The declaring type.</param>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The current field value.</returns>
        private static T GetPrivateStaticField<T>(
            Type type,
            string fieldName)
        {
            FieldInfo field = type.GetField(fieldName, PrivateStatic);

            Assert.That(field, Is.Not.Null);

            return (T)field.GetValue(null);
        }

        /// <summary>
        /// Counts the action-state-change handlers registered by
        /// <see cref="DrawableActionLifecycle"/>.
        /// </summary>
        /// <returns>The number of registered handlers.</returns>
        private static int CountLifecycleSubscriptions()
        {
            FieldInfo historyField = typeof(GlobalActionHistory).GetField(
                "history",
                PrivateStatic);

            Assert.That(historyField, Is.Not.Null);

            ActionHistory history =
                (ActionHistory)historyField.GetValue(null);

            FieldInfo eventField = typeof(ActionHistory).GetField(
                "ActionStateChanged",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(eventField, Is.Not.Null);

            Delegate handlers = eventField.GetValue(history) as Delegate;

            if (handlers == null)
            {
                return 0;
            }

            return handlers
                .GetInvocationList()
                .Count(handler =>
                    handler.Method.DeclaringType
                    == typeof(DrawableActionLifecycle));
        }
    }
}

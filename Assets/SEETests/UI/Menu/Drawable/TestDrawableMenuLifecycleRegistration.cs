using NUnit.Framework;
using SEE.Controls.Actions;
using SEE.Utils.History;
using System;
using System.Linq;
using System.Reflection;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Tests registration of <see cref="DrawableMenuLifecycle"/>.
    /// </summary>
    [TestFixture]
    internal class TestDrawableMenuLifecycleRegistration
    {
        /// <summary>
        /// Verifies that repeated lifecycle initialization registers exactly one
        /// action-state-change handler.
        /// </summary>
        [Test]
        public void TestInitializeRegistersOnlyOnce()
        {
            DrawableMenuLifecycle.Initialize();
            DrawableMenuLifecycle.Initialize();

            Assert.That(
                CountLifecycleSubscriptions(),
                Is.EqualTo(1));
        }

        /// <summary>
        /// Counts the action-state-change handlers registered by
        /// <see cref="DrawableMenuLifecycle"/>.
        /// </summary>
        /// <returns>The number of registered lifecycle handlers.</returns>
        private static int CountLifecycleSubscriptions()
        {
            const BindingFlags privateStatic =
                BindingFlags.NonPublic | BindingFlags.Static;

            FieldInfo historyField = typeof(GlobalActionHistory).GetField(
                "history",
                privateStatic);

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
                    == typeof(DrawableMenuLifecycle));
        }
    }
}

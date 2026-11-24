
using System.Collections.Generic;
using NUnit.Framework;
using SEE.Controls.ReversibleActions;
using UnityEngine;

namespace SEE.ReversibleActionHistory
{
    /// <summary>
    /// Tests the interaction between context-menu actions, regular actions,
    /// and the active tool during undo and redo operations.
    /// </summary>
    [TestFixture]
    internal class TestContextMenuActionHistory
    {
        /// <summary>
        /// A reversible test action that completes on its first update.
        /// It resets the context-menu execution flag when completed, as
        /// some production actions do.
        /// </summary>
        private sealed class TestAction : AbstractPlayerAction
        {
            /// <summary>
            /// The action state represented by this test action.
            /// </summary>
            private readonly ActionStateType actionType;

            /// <summary>
            /// Creates a reversible test action of the given type.
            /// </summary>
            /// <param name="actionType">The action state type.</param>
            public TestAction(ActionStateType actionType)
            {
                this.actionType = actionType;
            }

            /// <summary>
            /// Completes the action and clears the temporary context-menu flag.
            /// </summary>
            /// <returns>True because the action has completed.</returns>
            public override bool Update()
            {
                ExecuteViaContextMenu = false;
                CurrentState = IReversibleAction.Progress.Completed;
                return true;
            }

            /// <summary>
            /// Creates a new unused instance of the same action type.
            /// </summary>
            /// <returns>A new reversible test action.</returns>
            public override IReversibleAction NewInstance()
            {
                return new TestAction(actionType);
            }

            /// <summary>
            /// Returns the action state type.
            /// </summary>
            /// <returns>The action state type of this action.</returns>
            public override ActionStateType GetActionStateType()
            {
                return actionType;
            }

            /// <summary>
            /// Returns the identifiers of objects changed by this action.
            /// The test action does not modify actual game objects.
            /// </summary>
            /// <returns>An empty set of object identifiers.</returns>
            public override HashSet<string> GetChangedObjects()
            {
                return new HashSet<string>();
            }
        }

        /// <summary>
        /// Creates an unregistered action state type whose factory produces
        /// reversible test actions.
        /// </summary>
        /// <param name="name">The name of the action state type.</param>
        /// <returns>A new action state type.</returns>
        private static ActionStateType CreateActionType(string name)
        {
            ActionStateType actionType = null;
            actionType = new ActionStateType(
                name,
                string.Empty,
                Color.white,
                ' ',
                () => new TestAction(actionType),
                register: false);

            return actionType;
        }

        /// <summary>
        /// Verifies that redoing an action restores its action type in the
        /// history, even when the action originated from the context menu.
        /// Tool selection is managed separately by PlayerMenu.
        /// </summary>
        [Test]
        public void ContextMenuRedoRestoresHistoryAction()
        {
            ActionHistory history = new(syncOverNetwork: false);
            ActionStateType moveType = CreateActionType("Move");
            ActionStateType deleteType = CreateActionType("Delete");

            history.Execute(moveType.CreateReversible());

            TestAction deleteAction = new(deleteType);
            history.Execute(deleteAction);
            deleteAction.ExecuteViaContextMenu = true;
            history.Update();

            Assert.That(deleteAction.ExecuteViaContextMenu, Is.False);

            history.Execute(moveType.CreateReversible());
            history.Undo();

            Assert.That(history.RedoCount(), Is.EqualTo(1));

            history.Execute(moveType.CreateReversible());
            history.Redo();

            Assert.That(
                history.CurrentAction.GetActionStateType(),
                Is.SameAs(deleteType),
                "The history must retain the type of the redone action.");
        }

        /// <summary>
        /// Verifies that undoing a completed context-menu action resumes
        /// the preceding effective action in the history.
        /// The visible tool selection is managed separately by PlayerMenu.
        /// </summary>
        [Test]
        public void ContextMenuUndoRestoresPrecedingHistoryAction()
        {
            ActionHistory history = new(syncOverNetwork: false);
            ActionStateType moveType = CreateActionType("Move");
            ActionStateType deleteType = CreateActionType("Delete");
            ActionStateType editType = CreateActionType("Edit Node");

            history.Execute(moveType.CreateReversible());
            history.Update();

            TestAction deleteAction = new(deleteType);
            history.Execute(deleteAction);
            deleteAction.ExecuteViaContextMenu = true;
            history.Update();

            history.Execute(editType.CreateReversible());
            history.Undo();

            Assert.That(history.RedoCount(), Is.EqualTo(1));
            Assert.That(
                history.CurrentAction.GetActionStateType(),
                Is.SameAs(moveType),
                "Undo must resume the preceding effective history action.");
        }

        /// <summary>
        /// Verifies that redoing a regular tool-based action retains the
        /// existing behavior of activating the redone action's tool.
        /// </summary>
        [Test]
        public void RegularActionRedoActivatesRedoneTool()
        {
            ActionHistory history = new(syncOverNetwork: false);
            ActionStateType moveType = CreateActionType("Move");
            ActionStateType deleteType = CreateActionType("Delete");

            history.Execute(moveType.CreateReversible());

            TestAction deleteAction = new(deleteType);
            history.Execute(deleteAction);
            history.Update();

            history.Execute(moveType.CreateReversible());
            history.Undo();

            Assert.That(history.RedoCount(), Is.EqualTo(1));

            history.Execute(moveType.CreateReversible());
            history.Redo();

            Assert.That(
                history.CurrentAction.GetActionStateType(),
                Is.SameAs(deleteType),
                "Redoing a regular Delete action must retain its existing tool-selection behavior.");
        }
    }
}

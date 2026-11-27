using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SEE.Controls.ReversibleActions;
using SEE.Game;
using SEE.ReversibleActionHistory;
using SEE.UI.Menu;
using SEE.UserSettings;
using SEE.XR;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;

namespace SEE.UI
{
    /// <summary>
    /// PlayMode regression tests verifying how the player menu responds
    /// to undoing and redoing regular and context-menu actions.
    /// </summary>
    [TestFixture]
    internal sealed class TestPlayerMenuContextHistory : TestUI
    {
        /// <summary>
        /// The player object created for the test.
        /// </summary>
        private GameObject playerObject;

        /// <summary>
        /// The original local player.
        /// </summary>
        private GameObject previousLocalPlayer;

        /// <summary>
        /// Original state of the VR undo shortcut.
        /// </summary>
        private bool previousUndoToggle;

        /// <summary>
        /// Original state of the VR redo shortcut.
        /// </summary>
        private bool previousRedoToggle;

        /// <summary>
        /// The player menu under test.
        /// </summary>
        private PlayerMenu playerMenu;

        /// <summary>
        /// The actual selection menu managed by the player menu.
        /// </summary>
        private SelectionMenu selectionMenu;

        /// <summary>
        /// The optional event system created by the fixture.
        /// </summary>
        private GameObject createdEventSystem;

        /// <summary>
        /// The global action history used by the player menu.
        /// </summary>
        private ActionHistory history;

        /// <summary>
        /// The undo stack to restore after the test.
        /// </summary>
        private Stack<IReversibleAction> undoStack;

        /// <summary>
        /// The redo stack to restore after the test.
        /// </summary>
        private Stack<IReversibleAction> redoStack;

        /// <summary>
        /// The global entries to restore after the test.
        /// </summary>
        private List<ActionHistory.GlobalHistoryEntry> globalEntries;

        /// <summary>
        /// Saved undo stack entries, ordered from top to bottom.
        /// </summary>
        private IReversibleAction[] originalUndoActions;

        /// <summary>
        /// Saved redo stack entries, ordered from top to bottom.
        /// </summary>
        private IReversibleAction[] originalRedoActions;

        /// <summary>
        /// Saved global history entries.
        /// </summary>
        private List<ActionHistory.GlobalHistoryEntry> originalGlobalEntries;

        /// <summary>
        /// Initializes the real player menu and saves the existing history
        /// so that the tests do not leave their actions behind.
        /// </summary>
        /// <returns>An enumerator waiting for Unity initialization.</returns>
        [UnitySetUp]
        public IEnumerator SetUpPlayerMenu()
        {
            FieldInfo historyField = typeof(GlobalActionHistory).GetField(
                "history",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(historyField, Is.Not.Null);

            history = (ActionHistory)historyField.GetValue(null);

            undoStack = GetHistoryStack("UndoHistory");
            redoStack = GetHistoryStack("RedoHistory");

            FieldInfo globalEntriesField = typeof(ActionHistory).GetField(
                "globalHistory",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(globalEntriesField, Is.Not.Null);

            globalEntries =
                (List<ActionHistory.GlobalHistoryEntry>)
                globalEntriesField.GetValue(history);

            originalUndoActions = undoStack.ToArray();
            originalRedoActions = redoStack.ToArray();

            originalGlobalEntries =
                new List<ActionHistory.GlobalHistoryEntry>(globalEntries);

            previousLocalPlayer = LocalPlayer.Instance;

            previousUndoToggle = XRSEEActions.UndoToggle;
            previousRedoToggle = XRSEEActions.RedoToggle;

            // Stop the previous action before isolating the test history.
            history.CurrentAction?.Stop();
            undoStack.Clear();
            redoStack.Clear();
            globalEntries.Clear();

            EnsureUICanvas();

            if (EventSystem.current == null)
            {
                createdEventSystem = new GameObject(
                    "PlayerMenu test event system",
                    typeof(EventSystem),
                    typeof(InputSystemUIInputModule));
            }

            // The input module must be initialized before PlayerMenu
            // starts executing MoveAction.Update().
            yield return null;

            Assert.That(EventSystem.current, Is.Not.Null);

            Assert.That(
                EventSystem.current.currentInputModule,
                Is.InstanceOf<InputSystemUIInputModule>(),
                "PlayerMenu tests require an active InputSystemUIInputModule.");

            playerObject = new GameObject("PlayerMenu test player");
            LocalPlayer.Instance = playerObject;

            playerMenu = playerObject.AddComponent<PlayerMenu>();

            yield return null;

            playerMenu.enabled = false;

            selectionMenu = playerObject.GetComponent<SelectionMenu>();

            Assert.That(selectionMenu, Is.Not.Null);
            Assert.That(selectionMenu.ActiveEntry, Is.Not.Null);

            selectionMenu.ShowMenu = false;

            Assert.That(
                selectionMenu.ActiveEntry.Title,
                Is.EqualTo(ActionStateTypes.Move.Name));
        }

        /// <summary>
        /// Restores the original player and history after each test.
        /// </summary>
        /// <returns>An enumerator waiting for Unity object destruction.</returns>
        [UnityTearDown]
        public IEnumerator TearDownPlayerMenu()
        {
            XRSEEActions.UndoToggle = previousUndoToggle;
            XRSEEActions.RedoToggle = previousRedoToggle;

            if (playerMenu != null)
            {
                playerMenu.enabled = false;
            }

            if (history != null && undoStack != null && redoStack != null)
            {
                history.CurrentAction?.Stop();

                RestoreStack(undoStack, originalUndoActions);
                RestoreStack(redoStack, originalRedoActions);

                globalEntries.Clear();
                globalEntries.AddRange(originalGlobalEntries);
            }

            LocalPlayer.Instance = previousLocalPlayer;

            if (playerObject != null)
            {
                UnityEngine.Object.Destroy(playerObject);
            }

            if (createdEventSystem != null)
            {
                UnityEngine.Object.Destroy(createdEventSystem);
            }

            if (history != null && undoStack != null)
            {
                history.CurrentAction?.Start();
            }

            yield return null;
        }

        /// <summary>
        /// Verifies that redoing an interrupted context-menu action preserves the selected Move tool.
        /// </summary>
        /// <returns>An enumerator for the Unity Test Runner.</returns>
        [UnityTest]
        [LoadScene]
        public IEnumerator ContextMenuUndoRedoPreservesSelectedTool()
        {
            SimulatedAction action = PrepareAction(fromContextMenu: true);

            AssertSelectedTool(ActionStateTypes.Move);
            Assert.That(action.CurrentProgress(), Is.EqualTo(IReversibleAction.Progress.InProgress));

            InvokeHistoryShortcut(undo: true);

            Assert.That(action.IsUndone, Is.True);
            AssertSelectedTool(ActionStateTypes.Move);
            Assert.That(redoStack.Count, Is.EqualTo(1));
            Assert.That(redoStack.Peek(), Is.SameAs(action));

            InvokeHistoryShortcut(undo: false);

            Assert.That(action.IsUndone, Is.True);
            Assert.That(history.CurrentAction, Is.Not.SameAs(action));
            Assert.That(redoStack.Count, Is.EqualTo(0));
            AssertSelectedTool(ActionStateTypes.Move);

            yield return null;
        }

        /// <summary>
        /// Verifies that redoing an interrupted regular action activates a fresh instance of its tool.
        /// </summary>
        /// <returns>An enumerator for the Unity Test Runner.</returns>
        [UnityTest]
        [LoadScene]
        public IEnumerator RegularActionRedoActivatesRedoneTool()
        {
            SimulatedAction action = PrepareAction(fromContextMenu: false);

            AssertSelectedTool(ActionStateTypes.Move);

            InvokeHistoryShortcut(undo: true);

            Assert.That(action.IsUndone, Is.True);
            Assert.That(redoStack.Count, Is.EqualTo(1));
            Assert.That(redoStack.Peek(), Is.SameAs(action));

            InvokeHistoryShortcut(undo: false);

            Assert.That(action.IsUndone, Is.True);
            Assert.That(redoStack.Count, Is.EqualTo(0));
            Assert.That(GlobalActionHistory.CurrentAction(), Is.InstanceOf<SimulatedAction>());

            SimulatedAction restarted = (SimulatedAction)GlobalActionHistory.CurrentAction();
            Assert.That(restarted, Is.Not.SameAs(action));
            Assert.That(restarted.IsUndone, Is.False);
            Assert.That(restarted.CurrentProgress(), Is.EqualTo(IReversibleAction.Progress.NoEffect));
            AssertSelectedTool(ActionStateTypes.Delete);

            yield return null;
        }

        /// <summary>
        /// Creates an in-progress simulated Delete action and selects Move afterward.
        /// The action has a preliminary effect but remains incomplete for the undo/redo scenario.
        /// </summary>
        /// <param name="fromContextMenu">Whether the simulated action originates from the context menu.</param>
        /// <returns>The original simulated action retained in the undo history.</returns>
        private SimulatedAction PrepareAction(bool fromContextMenu)
        {
            ActionStateType simulatedType = new(
                name: ActionStateTypes.Delete.Name,
                description: "Simulated reversible action",
                color: Color.white,
                icon: ' ',
                createReversible: () => new SimulatedAction(),
                register: false);

            GlobalActionHistory.Execute(simulatedType);

            SimulatedAction action = (SimulatedAction)GlobalActionHistory.CurrentAction();
            action.WasExecutedViaContextMenu = fromContextMenu;
            action.SimulateEffect();

            GlobalActionHistory.Execute(ActionStateTypes.Move);
            playerMenu.UpdateActiveEntry();

            return action;
        }

        /// <summary>
        /// Invokes the actual player-menu update with an Undo or Redo
        /// request, using SEE's existing VR input toggles.
        /// </summary>
        /// <param name="undo">True for Undo; false for Redo.</param>
        private void InvokeHistoryShortcut(bool undo)
        {
            MethodInfo updateMethod = typeof(PlayerMenu).GetMethod(
                "Update",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(updateMethod, Is.Not.Null);

            PlayerInputType originalInputType = UserSetting.Instance.InputType;

            try
            {
                UserSetting.Instance.InputType = PlayerInputType.VRPlayer;

                XRSEEActions.UndoToggle = undo;
                XRSEEActions.RedoToggle = !undo;

                updateMethod.Invoke(playerMenu, null);
            }
            finally
            {
                XRSEEActions.UndoToggle = false;
                XRSEEActions.RedoToggle = false;

                UserSetting.Instance.InputType = originalInputType;
            }
        }

        /// <summary>
        /// Checks both the visible menu selection and the current
        /// action represented by the history.
        /// </summary>
        /// <param name="expected">The expected action type.</param>
        private void AssertSelectedTool(ActionStateType expected)
        {
            Assert.That(selectionMenu.ActiveEntry, Is.Not.Null);

            Assert.That(
                selectionMenu.ActiveEntry.Title,
                Is.EqualTo(expected.Name));

            Assert.That(
                GlobalActionHistory.Current(),
                Is.EqualTo(expected));
        }

        /// <summary>
        /// Retrieves one of the action-history stacks.
        /// </summary>
        /// <param name="propertyName">The private stack property name.</param>
        /// <returns>The requested stack.</returns>
        private Stack<IReversibleAction> GetHistoryStack(string propertyName)
        {
            PropertyInfo property = typeof(ActionHistory).GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(property, Is.Not.Null);

            return (Stack<IReversibleAction>)property.GetValue(history);
        }

        /// <summary>
        /// Restores the original content and order of a history stack.
        /// </summary>
        /// <param name="stack">The stack to restore.</param>
        /// <param name="entries">The original entries, topmost first.</param>
        private static void RestoreStack(
            Stack<IReversibleAction> stack,
            IReversibleAction[] entries)
        {
            stack.Clear();

            for (int i = entries.Length - 1; i >= 0; i--)
            {
                stack.Push(entries[i]);
            }
        }

        /// <summary>
        /// A reversible test action representing an ongoing modification.
        /// It has no scene or network effects.
        /// </summary>
        private sealed class SimulatedAction : AbstractPlayerAction
        {
            /// <summary>
            /// Whether this action has been undone.
            /// </summary>
            public bool IsUndone { get; private set; }

            /// <summary>
            /// Initializes the simulated action without any preliminary effect.
            /// </summary>
            public override void Awake()
            {
                CurrentState = IReversibleAction.Progress.NoEffect;
            }

            /// <summary>
            /// Simulates the first modification without completing the action.
            /// </summary>
            public void SimulateEffect()
            {
                CurrentState = IReversibleAction.Progress.InProgress;
            }

            /// <summary>
            /// Keeps the simulated action running.
            /// </summary>
            /// <returns>False because it does not finish automatically.</returns>
            public override bool Update()
            {
                return false;
            }

            /// <summary>
            /// Creates a fresh simulated action.
            /// </summary>
            /// <returns>A new simulated action.</returns>
            public override IReversibleAction NewInstance()
            {
                return new SimulatedAction();
            }

            /// <summary>
            /// Returns the Delete action type represented by this test action.
            /// </summary>
            /// <returns>The Delete action type.</returns>
            public override ActionStateType GetActionStateType()
            {
                return ActionStateTypes.Delete;
            }

            /// <summary>
            /// Reverts the simulated modification.
            /// </summary>
            public override void Undo()
            {
                base.Undo();
                IsUndone = true;
            }

            /// <summary>
            /// Reapplies a completed simulated modification and validates the base action contract.
            /// </summary>
            public override void Redo()
            {
                base.Redo();
                IsUndone = false;
            }

            /// <summary>
            /// Returns the identifiers of modified objects.
            /// </summary>
            /// <returns>An empty set because no scene objects are modified.</returns>
            public override HashSet<string> GetChangedObjects()
            {
                return new HashSet<string>();
            }
        }
    }
}

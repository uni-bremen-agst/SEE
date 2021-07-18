using System;
using System.Collections.Generic;
using System.Linq;
using SEE.Controls;
using SEE.Controls.Actions;
using SEE.Game.UI.Menu;
using SEE.Game.UI.Notification;
using SEE.Game.UI.StateIndicator;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace SEE.GO.Menu
{
    /// <summary>
    /// Implements the behaviour of the in-game player menu, in which action states can be selected.
    /// </summary>
    public class PlayerMenu : MonoBehaviour
    {
        /// <summary>
        /// The UI object representing the menu the user chooses the action state from.
        /// </summary>
        private SelectionMenu ModeMenu;

        /// <summary>
        /// The UI object representing the indicator, which displays the current action state on the screen.
        /// </summary>
        private ActionStateIndicator Indicator;

        /// <summary>
        /// This creates and returns the mode menu, with which you can select the active game mode.
        ///
        /// Available modes can be found in <see cref="ActionStateType"/>.
        /// </summary>
        /// <param name="attachTo">The game object the menu should be attached to. If <c>null</c>, a
        /// new game object will be created.</param>
        /// <returns>the newly created mode menu component.</returns>
        private static SelectionMenu CreateModeMenu(GameObject attachTo = null)
        {
            Assert.IsTrue(ActionStateType.AllTypes.Count == 12);

            // Note: A ?? expression can't be used here, or Unity's overloaded null-check will be overridden.
            GameObject modeMenuGO = attachTo ? attachTo : new GameObject { name = "Mode Menu" };

            // IMPORTANT NOTE: Because an ActionState.Type value will be used as an index into
            // the following field of menu entries, the rank of an entry in this field of entry
            // must correspond to the ActionState.Type value. If this is not the case, we will
            // run into an endless recursion.

            List<ToggleMenuEntry> entries = new List<ToggleMenuEntry>();
            bool first = true;
            foreach (ActionStateType type in ActionStateType.AllTypes)
            {
                UnityAction entryAction = () => GlobalActionHistory.Execute(type);
                UnityAction exitAction = null;

                //FIXME This is a bad hack and should be replaced with something proper for non-reversible actions.
                // This currently just attaches the ShowCodeAction to the menu and registers entry/exitActions which
                // will enable/disable the component. It should be replaced with something more generalizable.
                // The current behavior also has a bug which doesn't properly leave an action when switching
                // to the code action.
                if (Equals(type, ActionStateType.ShowCode))
                {
                    // Attach ShowCodeAction
                    ShowCodeAction action = modeMenuGO.AddComponent<ShowCodeAction>();
                    entryAction = () => action.enabled = true;
                    exitAction = () => action.enabled = false;
                    action.enabled = false;
                }
                entries.Add(new ToggleMenuEntry(
                    active: first,
                    entryAction: entryAction,
                    exitAction: exitAction,
                    title: type.Name,
                    description: type.Description,
                    entryColor: type.Color,
                    icon: Resources.Load<Sprite>(type.IconPath)
                    ));
                if (first)
                {
                    GlobalActionHistory.Execute(type);
                    first = false;
                }
            }

            SelectionMenu modeMenu = modeMenuGO.AddComponent<SelectionMenu>();
            modeMenu.Title = "Mode Selection";
            modeMenu.Description = "Please select the mode you want to activate.";
            foreach (ToggleMenuEntry entry in entries)
            {
                modeMenu.AddEntry(entry);
            }
            return modeMenu;
        }

        /// <summary>
        /// This creates and returns the <see cref="StateIndicator.ActionStateIndicator"/>, which displays the current mode.
        /// The indicator will either be attached to the given GameObject or to a new GameObject if
        /// <paramref name="attachTo"/> is null.
        /// </summary>
        /// <param name="attachTo">The GameObject the indicator shall be attached to.
        /// If <c>null</c>, a new one will be created.</param>
        /// <returns>The newly created ActionStateIndicator.</returns>
        private static ActionStateIndicator CreateActionStateIndicator(GameObject attachTo = null)
        {
            // Note: A ?? expression can't be used here, or Unity's overloaded null-check will be overridden.
            GameObject actionStateGO = attachTo ? attachTo : new GameObject { name = "Action State Indicator" };
            ActionStateIndicator indicator = actionStateGO.AddComponent<ActionStateIndicator>();
            return indicator;
        }

        private void Start()
        {
            ModeMenu = CreateModeMenu(gameObject);
            Indicator = CreateActionStateIndicator(gameObject);
            // Whenever the state is changed, the action state indicator should reflect that
            ModeMenu.OnMenuEntrySelected.AddListener(SetIndicatorStateToEntry);
            // Initialize action state indicator to current action state
            SetIndicatorStateToEntry(ModeMenu.ActiveEntry);

            void SetIndicatorStateToEntry(ToggleMenuEntry entry)
            {
                Indicator.ChangeActionState(ActionStateType.FromID(ModeMenu.Entries.IndexOf(entry)));
            }
        }

        /// <summary>
        /// The menu can be enabled/disabled by pressing the space bar.
        /// Additionally, the action state can be selected via number keys.
        /// </summary>
        private void Update()
        {
            try
            {
                // Select action state via numbers on the keyboard
                for (int i = 0; i < ModeMenu.Entries.Count; i++)
                {
                    if (SEEInput.DigitKeyPressed(i))
                    {
                        ModeMenu.SelectEntry(i);
                        break;
                    }
                }
                if (SEEInput.ToggleMenu())
                {
                    ModeMenu.ToggleMenu();
                }
                if (SEEInput.Undo())
                {
                    GlobalActionHistory.Undo();
                    if (!GlobalActionHistory.IsEmpty())
                    {
                        ActionStateType currentAction = GlobalActionHistory.Current();
                        SetPlayerMenu(currentAction.Name);
                        Indicator.ChangeActionState(currentAction);
                    }
                    else
                    {
                        // This case will be reached if there is no finished action in the undo history.
                        // Special case: The user is executing his first action after moving while running the application,
                        // but this action is not finished yet. Then, the user executes undo.
                        ModeMenu.ActiveEntry = ModeMenu.Entries.Single(x => x.Title == ActionStateType.Move.Name);
                        Indicator.ChangeActionState(ActionStateType.Move);
                    }
                }
                else if (SEEInput.Redo())
                {
                    GlobalActionHistory.Redo();
                    ActionStateType currentAction = GlobalActionHistory.Current();
                    SetPlayerMenu(currentAction.Name);
                    Indicator.ChangeActionState(currentAction);
                }
                GlobalActionHistory.Update();
            }
            catch (Exception e)
            {
                ShowNotification.Error("Action Error", e.Message);
#if UNITY_EDITOR
                Debug.LogError(e.StackTrace);
#endif
            }
        }

        /// <summary>
        /// Sets the currently selected menu entry in PlayerMenu to the action with given <paramref name="actionName"/>.
        /// </summary>
        /// <param name="actionName">name of the menu entry to be </param>
        private void SetPlayerMenu(string actionName)
        {
            if (PlayerSettings.LocalPlayer.TryGetComponentOrLog(out PlayerMenu playerMenu))
            {
                // We cannot use PlayerActionHistory.Current here
                playerMenu.ModeMenu.ActiveEntry
                    = playerMenu.ModeMenu.Entries.First
                         (x => x.Title.Equals(actionName));
            }
            foreach (ToggleMenuEntry toggleMenuEntry in playerMenu.ModeMenu.Entries)
            {
                // Hint (can be removed after review): we cannot use PlayerActionHistory.Current
                if (toggleMenuEntry.Title.Equals(actionName))
                {
                    playerMenu.ModeMenu.ActiveEntry = toggleMenuEntry;
                    break;
                }
            }
        }
    }
}

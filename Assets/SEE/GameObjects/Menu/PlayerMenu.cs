using System.Collections.Generic;
using System.Linq;
using SEE.Controls;
using SEE.Controls.Actions;
using SEE.Game.UI;
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
        /// Responsible for the permission to interact with the PlayerMenu. It should be true, if no interaction
        /// should be possible e.g. the user will insert an input to an input-field.
        /// </summary>
        public static bool InteractionIsForbidden = false;

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
            Assert.IsTrue(ActionStateType.AllTypes.Count == 10);

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
                UnityAction entryAction = () => PlayerActionHistory.Execute(type);
                UnityAction exitAction = null;

                //FIXME This is a bad hack and should be replaced with something proper for non-reversible actions.
                // This currently just attaches the ShowCodeAction to the menu and registers entry/exitActions which
                // will enable/disable the component. It should be replaced with something more generalizable.
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
                    PlayerActionHistory.Execute(type);
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
        /// This creates and returns the <see cref="ActionStateIndicator"/>, which displays the current mode.
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
            if (!InteractionIsForbidden)
            {
                // Select action state via numbers on the keyboard
                for (int i = 0; i < ModeMenu.Entries.Count; i++)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                    {
                        ModeMenu.SelectEntry(i);
                        break;
                    }
                }

                // space bar toggles menu            
                if (Input.GetKeyDown(KeyBindings.ToggleMenu))
                {
                    ModeMenu.ToggleMenu();
                }

                // trigger Undo or Redo if requested by keyboard shortcuts
#if UNITY_EDITOR == false
            // Ctrl keys are not available when running the game in the editor
            if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
#endif
                {
                    if (Input.GetKeyDown(KeyBindings.Undo))
                    {
                        PlayerActionHistory.Undo();
                        if (!PlayerActionHistory.IsEmpty())
                        {
                            ActionStateType currentAction = PlayerActionHistory.Current();
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
                    else if (Input.GetKeyDown(KeyBindings.Redo))
                    {
                        PlayerActionHistory.Redo();
                        ActionStateType currentAction = PlayerActionHistory.Current();
                        SetPlayerMenu(currentAction.Name);
                        Indicator.ChangeActionState(currentAction);
                    }
                }
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
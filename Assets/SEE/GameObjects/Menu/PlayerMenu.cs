using System.Collections.Generic;
using System.Linq;
using SEE.Controls;
using SEE.Controls.Actions;
using SEE.Game.UI.Menu;
using SEE.Game.UI.StateIndicator;
using SEE.Utils;
using UnityEngine;

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
            // Note: A ?? expression can't be used here, or Unity's overloaded null-check will be overridden.
            GameObject modeMenuGO = attachTo ? attachTo : new GameObject {name = "Mode Menu"};

            ActionStateType firstType = ActionStateTypes.FirstActionStateType();
            IEnumerable<ToggleMenuEntry> entries = MenuEntries(ActionStateTypes.AllRootTypes);

            // Initial state will be the first action state type
            GlobalActionHistory.Execute(firstType);

            SelectionMenu modeMenu = modeMenuGO.AddComponent<SelectionMenu>();
            modeMenu.Title = "Mode Selection";
            modeMenu.Description = "Please select the mode you want to activate.";
            modeMenu.AddEntries(entries);

            return modeMenu;

            #region Local Functions

            IEnumerable<ToggleMenuEntry> MenuEntries(Forrest<AbstractActionStateType> allTypes)
            {
                List<ToggleMenuEntry> result = new();
                Dictionary<ActionStateTypeGroup, NestedMenuEntry<MenuEntry>> toNestedMenuEntry = new();
                allTypes.PreorderTraverse(Visit);
                return result;

                bool Visit(AbstractActionStateType child, AbstractActionStateType parent)
                {
                    MenuEntry entry;

                    if (child is ActionStateType actionStateType)
                    {
                        ToggleMenuEntry toggleEntry = new (active: Equals(actionStateType, firstType),
                                                           entryAction: () => GlobalActionHistory.Execute(actionStateType),
                                                           exitAction: null,
                                                           title: actionStateType.Name,
                                                           description: actionStateType.Description,
                                                           entryColor: actionStateType.Color,
                                                           icon: Resources.Load<Sprite>(actionStateType.IconPath));
                        // Only ToggleMenuEntries are added to result.
                        result.Add(toggleEntry);
                        entry = toggleEntry;
                    }
                    else if (child is ActionStateTypeGroup actionStateTypeGroup)
                    {
                        NestedMenuEntry<MenuEntry> nestedMenuEntry = new (innerEntries: new List<MenuEntry>(),
                                                                          title: actionStateTypeGroup.Name,
                                                                          description: actionStateTypeGroup.Description,
                                                                          entryColor: actionStateTypeGroup.Color,
                                                                          enabled: Equals(actionStateTypeGroup, firstType),
                                                                          icon: Resources.Load<Sprite>(actionStateTypeGroup.IconPath));
                        toNestedMenuEntry[actionStateTypeGroup] = nestedMenuEntry;
                        entry = nestedMenuEntry;
                    }
                    else
                    {
                        throw new System.NotImplementedException($"{nameof(child)} not handled.");
                    }

                    if (parent != null)
                    {
                        if (parent is ActionStateTypeGroup parentGroup)
                        {
                            toNestedMenuEntry[parentGroup].InnerEntries.Add(entry);
                        }
                        else
                        {
                            throw new System.InvalidCastException($"parent is expected to be a {nameof(ActionStateTypeGroup)}.");
                        }
                    }
                    return true;
                }
            }

            #endregion
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
            GameObject actionStateGO = attachTo ? attachTo : new GameObject {name = "Action State Indicator"};
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
                Indicator.ChangeState(entry.Title, entry.EntryColor.WithAlpha(0.5f));
            }
        }

        /// <summary>
        /// The menu can be enabled/disabled by pressing the space bar.
        /// Additionally, the action state can be selected via number keys.
        /// </summary>
        private void Update()
        {
            if (SEEInput.ToggleMenu())
            {
                ModeMenu.ToggleMenu();
            }

            if (SEEInput.Undo())
            {
                GlobalActionHistory.Undo();

                if (GlobalActionHistory.IsEmpty())
                {
                    // We always want to have an action running.
                    // The default action will be the first action state type.
                    GlobalActionHistory.Execute(ActionStateTypes.FirstActionStateType());
                }
                ActionStateType currentAction = GlobalActionHistory.Current();
                SetPlayerMenu(currentAction.Name);
                Indicator.ChangeActionState(currentAction);
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

        /// <summary>
        /// Sets the currently selected menu entry in PlayerMenu to the action with given <paramref name="actionName"/>.
        /// </summary>
        /// <param name="actionName">name of the menu entry to be </param>
        private void SetPlayerMenu(string actionName)
        {
            if (SceneSettings.LocalPlayer.TryGetComponentOrLog(out PlayerMenu playerMenu))
            {
                // We cannot use PlayerActionHistory.Current here
                playerMenu.ModeMenu.ActiveEntry = playerMenu.ModeMenu.Entries.First(x => x.Title.Equals(actionName));
            }
            foreach (ToggleMenuEntry toggleMenuEntry in playerMenu.ModeMenu.Entries)
            {
                if (toggleMenuEntry.Title.Equals(actionName))
                {
                    playerMenu.ModeMenu.ActiveEntry = toggleMenuEntry;
                    break;
                }
            }
        }
    }
}
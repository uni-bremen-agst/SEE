using System.Collections.Generic;
using System.Linq;
using SEE.Controls;
using SEE.Controls.Actions;
using SEE.Controls.KeyActions;
using SEE.Game;
using SEE.UI.Menu;
using SEE.UI.StateIndicator;
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
        private SelectionMenu modeMenu;

        /// <summary>
        /// The UI object representing the indicator, which displays the current action state on the screen.
        /// </summary>
        private ActionStateIndicator indicator;

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
            IList<MenuEntry> entries = MenuEntries(ActionStateTypes.AllRootTypes);

            // Initial state will be the first action state type
            GlobalActionHistory.Execute(firstType);

            SelectionMenu modeMenu = modeMenuGO.AddComponent<SelectionMenu>();
            modeMenu.Title = "Mode Selection";
            modeMenu.Description = "Please select the mode you want to activate.";
            modeMenu.AddEntries(entries);
            // Assumption: The name of an action type is used as the title of the menu entry.
            modeMenu.ActiveEntry = entries.First(t => t.Title == firstType.Name);
            return modeMenu;

            #region Local Functions

            // Sets up the menu structure according to given forest structure and content.
            // Returns the list of all MenuEntry instances created for all roots of this forest.
            IList<MenuEntry> MenuEntries(Forest<AbstractActionStateType> allTypes)
            {
                List<MenuEntry> result = new();
                // A mapping of the action groups created onto their corresponding NestedMenuEntry.
                // It will be used to add nested actions (atomic or groups) as a child of their
                // containing NestedMenuEntry (added to the InnerEntries of it).
                Dictionary<ActionStateTypeGroup, NestedMenuEntry<MenuEntry>> toNestedMenuEntry = new();
                allTypes.PreorderTraverse(Visit);
                return result;

                // The callback being called during the pre-order traversal of the forest
                // where the traversal passes the currently visited node as parameter 'child'
                // and the child's parent in parameter 'parent'. The 'parent' will be null
                // if 'child' is a root.
                // If 'child' is an atomic ActionStateType, a corresponding
                // MenuEntry will be created.
                // If 'child' is an ActionStateTypeGroup, a corresponding NestedMenuEntry will
                // be created.
                // If 'parent' is null, thus, 'child' is a root, 'child' will be added to 'result'.
                bool Visit(AbstractActionStateType child, AbstractActionStateType parent)
                {
                    // The menu entry created for child (either a ToggleMenuEntry for an ActionStateType
                    // or a NestedMenuEntry for an ActionStateTypeGroup).
                    MenuEntry entry;

                    if (child is ActionStateType actionStateType)
                    {
                        MenuEntry menuEntry = new(SelectAction: () => GlobalActionHistory.Execute(actionStateType),
                                                  Title: actionStateType.Name,
                                                  Description: actionStateType.Description,
                                                  EntryColor: actionStateType.Color,
                                                  Icon: actionStateType.Icon);
                        entry = menuEntry;
                    }
                    else if (child is ActionStateTypeGroup actionStateTypeGroup)
                    {
                        NestedMenuEntry<MenuEntry> nestedMenuEntry = new(innerEntries: new List<MenuEntry>(),
                                                                          title: actionStateTypeGroup.Name,
                                                                          description: actionStateTypeGroup.Description,
                                                                          entryColor: actionStateTypeGroup.Color,
                                                                          icon: actionStateTypeGroup.Icon);
                        toNestedMenuEntry[actionStateTypeGroup] = nestedMenuEntry;
                        entry = nestedMenuEntry;
                    }
                    else
                    {
                        throw new System.NotImplementedException($"{nameof(child)} not handled.");
                    }

                    // If child is not a root (i.e., has a parent), we will add the entry to
                    // the InnerEntries of the NestedMenuEntry corresponding to the parent.
                    // We know that such a NestedMenuEntry must exist, because we are doing a
                    // preorder traversal.
                    if (parent != null)
                    {
                        if (parent is ActionStateTypeGroup parentGroup)
                        {
                            toNestedMenuEntry[parentGroup].InnerEntries.Add(entry);
                        }
                        else
                        {
                            throw new System.InvalidCastException($"Parent is expected to be an {nameof(ActionStateTypeGroup)}.");
                        }
                    }
                    else
                    {
                        result.Add(entry);
                    }
                    // Continue with the traversal.
                    return true;
                }
            }

            #endregion
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
            GameObject actionStateGO = attachTo ? attachTo : new GameObject {name = "Action State Indicator"};
            return actionStateGO.AddComponent<ActionStateIndicator>();
        }

        private void Start()
        {
            modeMenu = CreateModeMenu(gameObject);
            indicator = CreateActionStateIndicator(gameObject);
            // Whenever the state is changed, the action state indicator should reflect that
            modeMenu.OnEntrySelected += SetIndicatorStateToEntry;
            // Initialize action state indicator to current action state
            SetIndicatorStateToEntry(modeMenu.ActiveEntry);

            void SetIndicatorStateToEntry(MenuEntry entry)
            {
                indicator.ChangeState(entry.Title, entry.EntryColor.WithAlpha(0.5f));
            }
        }

        /// <summary>
        /// The menu can be enabled/disabled by pressing the space bar.
        /// Additionally, the action state can be selected via number keys.
        /// </summary>
        private void Update()
        {
            // Select action state via numbers on the keyboard
            for (int i = 0; i < modeMenu.Entries.Count; i++)
            {
                if (SEEInput.DigitKeyPressed(i))
                {
                    modeMenu.SelectEntry(modeMenu.Entries[i]);
                    break;
                }
            }

            // When the menu is shown, we still want the ToggleMenu key to work,
            // except if the search field is focussed. Hence, we cannot rely on
            // SEEInput here, but need to check the key directly.
            bool shouldReactToToggleMenu = (modeMenu.ShowMenu && !modeMenu.IsSearchFocused)
                || (!modeMenu.ShowMenu && SEEInput.KeyboardShortcutsEnabled);
            if (shouldReactToToggleMenu && KeyBindings.IsDown(KeyAction.ToggleMenu))
            {
                modeMenu.ToggleMenu();
            }

            if (SEEInput.Undo())
            {
                GlobalActionHistory.Undo();

                if (GlobalActionHistory.IsEmpty())
                {
                    // The first/default action is assumed to be at top level of the
                    // player menu hierarchy. If the last action is undone and we create
                    // a new first action to be pushed onto the action history, it could
                    // happen that we are currently in a submenu, in which case the
                    // first/default would not exist in this submenu level; hence we
                    // need to reset the menu to the top level.
                    modeMenu.ResetToBase();
                    // We always want to have an action running.
                    // The default action will be the first action state type.
                    GlobalActionHistory.Execute(ActionStateTypes.FirstActionStateType());
                }
                ActionStateType currentAction = GlobalActionHistory.Current();
                SetPlayerMenu(currentAction.Name);
                indicator.ChangeActionState(currentAction);
            }
            else if (SEEInput.Redo())
            {
                GlobalActionHistory.Redo();
                ActionStateType currentAction = GlobalActionHistory.Current();
                SetPlayerMenu(currentAction.Name);
                indicator.ChangeActionState(currentAction);
            }
            if (RadialSelection.IndicatorChange)
            {
                ActionStateType currentAction = GlobalActionHistory.Current();
                indicator.ChangeActionState(currentAction);
                RadialSelection.IndicatorChange = false;
            }
            GlobalActionHistory.Update();
        }

        /// <summary>
        /// Sets the currently selected menu entry in PlayerMenu to the action with given <paramref name="actionName"/>.
        /// </summary>
        /// <param name="actionName">name of the menu entry to be set</param>
        private static void SetPlayerMenu(string actionName)
        {
            if (LocalPlayer.TryGetPlayerMenu(out PlayerMenu playerMenu))
            {
                // We cannot use PlayerActionHistory.Current here
                playerMenu.modeMenu.ActiveEntry = playerMenu.modeMenu.Entries.First(x => x.Title.Equals(actionName));
            }
            foreach (MenuEntry toggleMenuEntry in playerMenu.modeMenu.Entries)
            {
                if (toggleMenuEntry.Title.Equals(actionName))
                {
                    playerMenu.modeMenu.ActiveEntry = toggleMenuEntry;
                    break;
                }
            }
        }

        /// <summary>
        /// Updates the menu depending on the currently selected action in <see cref="GlobalActionHistory"/>.
        /// It changes the current selected menu entry in PlayerMenu and
        /// it changes also the depending indicator.
        /// </summary>
        /// <param name="nestedMenuName">The name of the nested menu of the new active entry;
        /// null if the entry is in the root menu.</param>
        internal void UpdateActiveEntry(string nestedMenuName = null)
        {
            ActionStateType currentAction = GlobalActionHistory.Current();
            if (nestedMenuName != null && LocalPlayer.TryGetPlayerMenu(out PlayerMenu playerMenu))
            {
                playerMenu.modeMenu.ResetToBase();
                playerMenu.modeMenu.SelectEntry(playerMenu.modeMenu.Entries.First(x => x.Title.Equals(nestedMenuName)));
            }
            SetPlayerMenu(currentAction.Name);
            indicator.ChangeActionState(currentAction);

            foreach (MenuEntry entry in modeMenu.Entries)
            {
                if (entry.Title.Equals(currentAction.Name))
                {
                    modeMenu.SelectEntry(entry);
                }
            }
        }
    }
}
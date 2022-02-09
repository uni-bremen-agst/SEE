using System;
using System.Collections.Generic;
using System.Linq;
using SEE.Controls;
using SEE.Controls.Actions;
using SEE.Game.UI.Menu;
using SEE.Game.UI.Notification;
#if !UNITY_ANDROID
using SEE.Game.UI.StateIndicator;
#endif
using UnityEngine;
using UnityEngine.Assertions;

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
        /// The UI object representing the  mobile menu the user chooses the action state from.
        /// </summary>
        private SimpleMenu MobileMenu;

        /// <summary>
        /// The UI object representing the indicator, which displays the current action state on the screen.
        /// </summary>
#if !UNITY_ANDROID
        private ActionStateIndicator Indicator;
#endif
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
            Assert.IsTrue(ActionStateType.DesktopMenuTypes.Count == 11);

            // Note: A ?? expression can't be used here, or Unity's overloaded null-check will be overridden.
            GameObject modeMenuGO = attachTo ? attachTo : new GameObject { name = "Mode Menu" };

            // IMPORTANT NOTE: Because an ActionStateType value will be used as an index into
            // the following field of menu entries, the rank of an entry in this field of entry
            // must correspond to the ActionStateType value. If this is not the case, we will
            // run into an endless recursion.

            ActionStateType firstType = ActionStateType.DesktopMenuTypes.First();
            List<ToggleMenuEntry> entries = ActionStateType.DesktopMenuTypes.Select(ToModeMenuEntry).ToList();

            // Initial state will be the first action state type
            GlobalActionHistory.Execute(firstType);

            SelectionMenu modeMenu = modeMenuGO.AddComponent<SelectionMenu>();
            modeMenu.Title = "Mode Selection";
            modeMenu.Description = "Please select the mode you want to activate.";
            modeMenu.AddEntries(entries);

            return modeMenu;

#region Local Functions

            // Constructs a toggle menu entry for the mode menu from the given action state type.
            ToggleMenuEntry ToModeMenuEntry(ActionStateType type) =>
                new ToggleMenuEntry(active: Equals(type, firstType),
                                    entryAction: () => GlobalActionHistory.Execute(type), exitAction: null,
                                    title: type.Name, description: type.Description, entryColor: type.Color,
                                    icon: Resources.Load<Sprite>(type.IconPath));

#endregion
        }
        /// <summary>
        /// This creates and returns the mobile menu, with which you can select the active game mode.
        ///
        /// Available modes can be found in <see cref="MobileActionStateType"/>.
        /// </summary>
        /// <returns>the newly created menu component.</returns>
        private static SimpleMenu CreateMenu(GameObject attachTo = null)
        {

            // Note: A ?? expression can't be used here, or Unity's overloaded null-check will be overridden.
            GameObject modeMenuGO = attachTo ? attachTo : new GameObject { name = "Mode Menu" };

            // IMPORTANT NOTE: Because an ActionStateType value will be used as an index into
            // the following field of menu entries, the rank of an entry in this field of entry
            // must correspond to the ActionStateType value. If this is not the case, we will
            // run into an endless recursion.

            ActionStateType firstType = ActionStateType.MobileMenuTypes.First();
            List<MenuEntry> entries = ActionStateType.MobileMenuTypes.Select(ToMenuEntry).ToList();

            // Initial state will be the first action state type
            GlobalActionHistory.Execute(firstType);

            SimpleMenu menu = modeMenuGO.AddComponent<SimpleMenu>();
            menu.Title = "Mobile Menu";
            menu.Description = "Please select the mode you want to activate.";
            menu.AddEntries(entries);

            return menu;

            // Constructs a toggle menu entry for the mode menu from the given action state type.
            MenuEntry ToMenuEntry(ActionStateType type) =>
                new MenuEntry(
                    action: () => GlobalActionHistory.Execute(type), title: type.Name,
                    description: type.Description, entryColor: type.Color,
                    icon: Resources.Load<Sprite>(type.IconPath));

        }

        /// <summary>
        /// This creates and returns the <see cref="StateIndicator.ActionStateIndicator"/>, which displays the current mode.
        /// The indicator will either be attached to the given GameObject or to a new GameObject if
        /// <paramref name="attachTo"/> is null.
        /// </summary>
        /// <param name="attachTo">The GameObject the indicator shall be attached to.
        /// If <c>null</c>, a new one will be created.</param>
        /// <returns>The newly created ActionStateIndicator.</returns>
#if !UNITY_ANDROID
        private static ActionStateIndicator CreateActionStateIndicator(GameObject attachTo = null)
        {
            // Note: A ?? expression can't be used here, or Unity's overloaded null-check will be overridden.
            GameObject actionStateGO = attachTo ? attachTo : new GameObject { name = "Action State Indicator" };
            ActionStateIndicator indicator = actionStateGO.AddComponent<ActionStateIndicator>();
            return indicator;
        }
#endif

        private void Start()
        {
            if (PlayerSettings.GetInputType() == PlayerInputType.MobilePlayer)
            {
                CreateMenu(gameObject);

            }
            else
            {
                ModeMenu = CreateModeMenu(gameObject);
#if !UNITY_ANDROID
                Indicator = CreateActionStateIndicator(gameObject);

                // Whenever the state is changed, the action state indicator should reflect that
                ModeMenu.OnMenuEntrySelected.AddListener(SetIndicatorStateToEntry);
                // Initialize action state indicator to current action state
                SetIndicatorStateToEntry(ModeMenu.ActiveEntry);

                void SetIndicatorStateToEntry(ToggleMenuEntry entry)
                {
                    Indicator.ChangeActionState(ActionStateType.FromID(ModeMenu.Entries.IndexOf(entry)));
                }
#endif
            }
            
        }

        /// <summary>
        /// The menu can be enabled/disabled by pressing the space bar.
        /// Additionally, the action state can be selected via number keys.
        /// </summary>
        private void Update()
        {
            if (PlayerSettings.GetInputType() != PlayerInputType.MobilePlayer)
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

                        if (GlobalActionHistory.IsEmpty())
                        {
                            // We always want to have an action running.
                            // The default action will be the first action state type.
                            ActionStateType firstType = ActionStateType.AllTypes.First();
                            GlobalActionHistory.Execute(firstType);
                        }
                        ActionStateType currentAction = GlobalActionHistory.Current();
                        SetPlayerMenu(currentAction.Name);
#if !UNITY_ANDROID
                        Indicator.ChangeActionState(currentAction);
#endif
                    }
                    else if (SEEInput.Redo())
                    {
                        GlobalActionHistory.Redo();
                        ActionStateType currentAction = GlobalActionHistory.Current();
                        SetPlayerMenu(currentAction.Name);
#if !UNITY_ANDROID
                        Indicator.ChangeActionState(currentAction);
#endif
                    }
                    GlobalActionHistory.Update();
                }
                //TODO: This probably shouldn't catch *all* exceptions in this way.
                catch (Exception e)
                {
                    ShowNotification.Error("Action Error", e.Message);
#if UNITY_EDITOR
                throw;
#endif
                }
            }
            
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

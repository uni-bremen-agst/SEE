using System.Collections.Generic;
using System.Linq;
using SEE.Game.UI.Window;
using SEE.Game.UI.Menu;
using SEE.Game.UI.StateIndicator;
using SEE.GO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace SEE.Controls
{
    /// <summary>
    /// Manages the association from players to <see cref="WindowSpace"/>s.
    /// Will also display a menu which can be opened using TAB. The player whose code window shall be displayed
    /// can be selected from this menu.
    /// Note that only one instance of this class may be active in the scene. This instance can be retrieved
    /// using <see cref="ManagerInstance"/>.
    /// </summary>
    public class WindowSpaceManager : MonoBehaviour
    {
        /// <summary>
        /// String representing the local player.
        /// </summary>
        public const string LOCAL_PLAYER = "Local player";

        /// <summary>
        /// String representing no player, i.e. no windows being displayed.
        /// </summary>
        public const string NO_PLAYER = "None";

        /// <summary>
        /// The name of the player whose window is currently displayed.
        /// </summary>
        public string CurrentPlayer { get; private set; } = LOCAL_PLAYER;

        /// <summary>
        /// A dictionary mapping player names to their code spaces.
        /// </summary>
        private readonly Dictionary<string, WindowSpace> WindowSpaces = new();

        /// <summary>
        /// This event will be invoked whenever the active window for the <see cref="LOCAL_PLAYER"/> is changed.
        /// This includes changing the active window to nothing (i.e. closing all of them.)
        /// </summary>
        [FormerlySerializedAs("OnActiveCodeWindowChanged")]
        public UnityEvent OnActiveWindowChanged = new();

        /// <summary>
        /// The menu from which the user can select the player whose windows they want to see.
        /// </summary>
        private SelectionMenu WindowMenu;

        /// <summary>
        /// A <see cref="StateIndicator"/> which displays the IP address of the window we're currently viewing.
        /// </summary>
        private StateIndicator SpaceIndicator;

        /// <summary>
        /// Represents the space manager currently active in the scene.
        /// </summary>
        public static WindowSpaceManager ManagerInstance;

        /// <summary>
        /// Accesses the space for the given <paramref name="playerName"/>.
        /// If the given <paramref name="playerName"/> does not exist, <c>null</c> will be returned.
        /// </summary>
        /// <param name="playerName">The name of the player whose space should be returned.</param>
        public WindowSpace this[string playerName]
        {
            get => WindowSpaces.ContainsKey(playerName) ? WindowSpaces[playerName] : null;
            set => WindowSpaces[playerName] = value;
        }

        /// Updates the space of the player specified by <paramref name="playerName"/> using the values
        /// from <paramref name="valueObject"/>.
        /// </summary>
        /// <param name="playerName">The name of the player whose space shall be updated.</param>
        /// <param name="valueObject">The value object which represents the new space of the player.</param>
        public void UpdateSpaceFromValueObject(string playerName, WindowSpace.WindowSpaceValues valueObject)
        {
            if (!WindowSpaces.ContainsKey(playerName))
            {
                WindowSpaces[playerName] = WindowSpace.FromValueObject(valueObject, gameObject);
                WindowMenu.AddEntry(new MenuEntry(() => ActivateSpace(playerName),
                                                  DeactivateCurrentSpace, playerName,
                                                  $"Code window from player with IP address '{playerName}'.", Color.white));
                WindowSpaces[playerName].WindowSpaceName += $" ({playerName})";
                WindowSpaces[playerName].enabled = false;
                WindowSpaces[playerName].CanClose = false;  // User may only close their own windows
            }
            else
            {
                // We will try to do a partial update, otherwise we'd have to re-read each file on every update

                // Check for new entries
                List<BaseWindow> closedWindows = new(WindowSpaces[playerName].Windows);
                foreach (WindowValues windowValue in valueObject.Windows)
                {
                    if (WindowSpaces[playerName].Windows.All(x => windowValue.Title != x.Title))
                    {
                        // Window is new and has to be re-created
                        BaseWindow window = BaseWindow.FromValueObject<BaseWindow>(windowValue);
                        WindowSpaces[playerName].AddWindow(window);
                        // Only enable window if space is enabled as well
                        window.enabled = WindowSpaces[playerName].enabled;
                    }
                    else
                    {
                        // Layout may have changed
                        BaseWindow window = WindowSpaces[playerName].Windows.First(x => x.Title == windowValue.Title);
                        window.UpdateFromNetworkValueObject(windowValue);
                        // Window is still open, so it's not closed
                        closedWindows.RemoveAll(x => x.Title == windowValue.Title);
                    }
                }

                // Close windows which are no longer open
                closedWindows.ForEach(WindowSpaces[playerName].CloseWindow);

                // Set active window if it changed
                if (WindowSpaces[playerName].ActiveWindow.Title != valueObject.ActiveWindow.Title)
                {
                    WindowSpaces[playerName].ActiveWindow = WindowSpaces[playerName].Windows.First(x => x.Title == valueObject.ActiveWindow.Title);
                }
            }
        }

        private void Start()
        {
            if (FindObjectsOfType<WindowSpaceManager>().Length > 1)
            {
                Debug.LogError($"Warning: More than one  {nameof(WindowSpaceManager)} is present in the scene! "
                               + "This will lead to undefined behaviour when synchronizing "
                               + "windows across the network! No new indicator will be created.\n");
                foreach (WindowSpaceManager manager in FindObjectsOfType<WindowSpaceManager>())
                {
                    Debug.LogError($"{typeof(WindowSpaceManager)} at game object {manager.gameObject.FullName()}.\n");
                }
            }
            else
            {
                SpaceIndicator = gameObject.AddComponent<StateIndicator>();
                // Anchor to lower left
                SpaceIndicator.AnchorMin = Vector2.zero;
                SpaceIndicator.AnchorMax = Vector2.zero;
                SpaceIndicator.Pivot = Vector2.zero;
                ManagerInstance = this;
            }

            // Create local code space and associate it with current player
            WindowSpace space = WindowSpaces[LOCAL_PLAYER] = gameObject.AddOrGetComponent<WindowSpace>();
            space.OnActiveWindowChanged.AddListener(OnActiveWindowChanged.Invoke);

            ManagerInstance.SpaceIndicator.ChangeState(LOCAL_PLAYER, Color.black);

            SetUpWindowSelectionMenu();
        }

        private void Update()
        {
            if (SEEInput.ShowWindowMenu())
            {
                WindowMenu.ToggleMenu();
            }
        }

        /// <summary>
        /// Creates and sets up the window selection menu, from which the user can select a player whose
        /// window they want to see. Initially, this will have the entries "local player" and "none".
        /// </summary>
        private void SetUpWindowSelectionMenu()
        {
            //TODO: Icons
            WindowMenu = gameObject.AddComponent<SelectionMenu>();
            MenuEntry localEntry = new(selectAction: () => ActivateSpace(LOCAL_PLAYER),
                                       unselectAction: DeactivateCurrentSpace,
                                       title: LOCAL_PLAYER,
                                       description: "Windows for the local player (you).",
                                       entryColor: Color.black);
            MenuEntry noneEntry = new(() => CurrentPlayer = NO_PLAYER, () => { }, NO_PLAYER,
                                      "This option hides all windows.", Color.grey);
            WindowMenu.AddEntry(noneEntry);
            WindowMenu.AddEntry(localEntry);
            WindowMenu.SelectEntry(localEntry);
            foreach (KeyValuePair<string, WindowSpace> space in WindowSpaces.Where(space => space.Key != LOCAL_PLAYER))
            {
                WindowMenu.AddEntry(new MenuEntry(() => ActivateSpace(space.Key),
                                                  DeactivateCurrentSpace, space.Key,
                                                  $"Window from player with IP address '{space.Key}'.", Color.white));
            }
            WindowMenu.Title = "Window Selection";
            WindowMenu.Description = "Select the player whose windows you want to see.";
        }

        /// <summary>
        /// Calling this method will disable the <see cref="WindowSpace"/> for the <see cref="CurrentPlayer"/>.
        /// </summary>
        private void DeactivateCurrentSpace()
        {
            WindowSpaces[CurrentPlayer].enabled = false;
            ManagerInstance.SpaceIndicator.enabled = false;
        }

        /// <summary>
        /// Calling this method will enable the <see cref="WindowSpace"/> for the given <paramref name="playerName"/>
        /// and will set them as the <see cref="CurrentPlayer"/>.
        /// </summary>
        /// <param name="playerName">The player whose space to activate</param>
        private void ActivateSpace(string playerName)
        {
            WindowSpaces[playerName].enabled = true;
            CurrentPlayer = playerName;
            ManagerInstance.SpaceIndicator.enabled = true;
            ManagerInstance.SpaceIndicator.ChangeState(playerName, Color.black);
        }
    }
}

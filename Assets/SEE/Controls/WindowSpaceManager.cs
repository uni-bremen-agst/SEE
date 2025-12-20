using System.Collections.Generic;
using System.Linq;
using SEE.UI.Window;
using SEE.UI.Menu;
using SEE.UI.StateIndicator;
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
        public const string LocalPlayer = "Local player";

        /// <summary>
        /// String representing no player, i.e. no windows being displayed.
        /// </summary>
        public const string NoPlayer = "None";

        /// <summary>
        /// The name of the player whose window is currently displayed.
        /// </summary>
        public string CurrentPlayer { get; private set; } = LocalPlayer;

        /// <summary>
        /// A dictionary mapping player names to their code spaces.
        /// </summary>
        private readonly Dictionary<string, WindowSpace> windowSpaces = new();

        /// <summary>
        /// This event will be invoked whenever the active window for the <see cref="LocalPlayer"/> is changed.
        /// This includes changing the active window to nothing (i.e. closing all of them.)
        /// </summary>
        [FormerlySerializedAs("OnActiveCodeWindowChanged")]
        public UnityEvent OnActiveWindowChanged = new();

        /// <summary>
        /// The menu from which the user can select the player whose windows they want to see.
        /// </summary>
        private SelectionMenu windowMenu;

        /// <summary>
        /// A <see cref="StateIndicator"/> which displays the IP address of the window we're currently viewing.
        /// </summary>
        private StateIndicator spaceIndicator;

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
            get => windowSpaces.ContainsKey(playerName) ? windowSpaces[playerName] : null;
            set => windowSpaces[playerName] = value;
        }

        /// Updates the space of the player specified by <paramref name="playerName"/> using the values
        /// from <paramref name="valueObject"/>.
        /// </summary>
        /// <param name="playerName">The name of the player whose space shall be updated.</param>
        /// <param name="valueObject">The value object which represents the new space of the player.</param>
        public void UpdateSpaceFromValueObject(string playerName, WindowSpace.WindowSpaceValues valueObject)
        {
            if (!windowSpaces.ContainsKey(playerName))
            {
                windowSpaces[playerName] = WindowSpace.FromValueObject(valueObject, gameObject);
                windowMenu.AddEntry(new MenuEntry(() => ActivateSpace(playerName),
                                                  playerName, DeactivateCurrentSpace,
                                                  $"Code window from player with IP address '{playerName}'.", Color.white));
                windowSpaces[playerName].WindowSpaceName += $" ({playerName})";
                windowSpaces[playerName].enabled = false;
                windowSpaces[playerName].CanClose = false; // User may only close their own windows
            }
            else
            {
                // We will try to do a partial update, otherwise we'd have to re-read each file on every update

                // Check for new entries
                List<BaseWindow> closedWindows = new(windowSpaces[playerName].Windows);
                foreach (WindowValues windowValue in valueObject.Windows)
                {
                    if (windowSpaces[playerName].Windows.All(x => windowValue.Title != x.Title))
                    {
                        // Window is new and has to be re-created
                        BaseWindow window = BaseWindow.FromValueObject<BaseWindow>(windowValue);
                        windowSpaces[playerName].AddWindow(window);
                        // Only enable window if space is enabled as well
                        window.enabled = windowSpaces[playerName].enabled;
                    }
                    else
                    {
                        // Layout may have changed
                        BaseWindow window = windowSpaces[playerName].Windows.First(x => x.Title == windowValue.Title);
                        window.UpdateFromNetworkValueObject(windowValue);
                        // Window is still open, so it's not closed
                        closedWindows.RemoveAll(x => x.Title == windowValue.Title);
                    }
                }

                // Close windows which are no longer open
                closedWindows.ForEach(x => windowSpaces[playerName].CloseWindow(x));

                // Set active window if it changed
                if (windowSpaces[playerName].ActiveWindow.Title != valueObject.ActiveWindow.Title)
                {
                    windowSpaces[playerName].ActiveWindow = windowSpaces[playerName].Windows.First(x => x.Title == valueObject.ActiveWindow.Title);
                }
            }
        }

        private void Start()
        {
            if (FindObjectsOfType<WindowSpaceManager>().Length > 1)
            {
                Debug.LogError($"More than one {nameof(WindowSpaceManager)} is present in the scene! "
                               + "This will lead to undefined behaviour when synchronizing "
                               + "windows across the network! No new indicator will be created.\n");
                foreach (WindowSpaceManager manager in FindObjectsOfType<WindowSpaceManager>())
                {
                    Debug.LogError($"{typeof(WindowSpaceManager)} at game object {manager.gameObject.FullName()}.\n");
                }
            }
            else
            {
                spaceIndicator = gameObject.AddComponent<StateIndicator>();
                // Anchor to lower left
                spaceIndicator.AnchorMin = Vector2.zero;
                spaceIndicator.AnchorMax = Vector2.zero;
                spaceIndicator.Pivot = Vector2.zero;
                ManagerInstance = this;
            }

            // Create local code space and associate it with current player
            WindowSpace space = windowSpaces[LocalPlayer] = gameObject.AddOrGetComponent<WindowSpace>();
            space.OnActiveWindowChanged.AddListener(OnActiveWindowChanged.Invoke);

            ManagerInstance.spaceIndicator.ChangeState(LocalPlayer, Color.black);

            SetUpWindowSelectionMenu();
        }

        private void Update()
        {
            if (SEEInput.ShowWindowMenu())
            {
                windowMenu.ToggleMenu();
            }
        }

        /// <summary>
        /// Creates and sets up the window selection menu, from which the user can select a player whose
        /// window they want to see. Initially, this will have the entries "local player" and "none".
        /// </summary>
        private void SetUpWindowSelectionMenu()
        {
            windowMenu = gameObject.AddComponent<SelectionMenu>();
            MenuEntry localEntry = new(SelectAction: () => ActivateSpace(LocalPlayer),
                                       UnselectAction: DeactivateCurrentSpace,
                                       Title: LocalPlayer,
                                       Description: "Windows for the local player (you).",
                                       EntryColor: Color.black);
            MenuEntry noneEntry = new(() => CurrentPlayer = NoPlayer, NoPlayer,
                                      Description: "This option hides all windows.", EntryColor: Color.grey);
            windowMenu.AddEntry(noneEntry);
            windowMenu.AddEntry(localEntry);
            windowMenu.SelectEntry(localEntry);
            foreach (KeyValuePair<string, WindowSpace> space in windowSpaces.Where(space => space.Key != LocalPlayer))
            {
                windowMenu.AddEntry(new MenuEntry(() => ActivateSpace(space.Key),
                                                  space.Key, DeactivateCurrentSpace,
                                                  $"Window from player with IP address '{space.Key}'.", Color.white));
            }
            windowMenu.Title = "Window Selection";
            windowMenu.Description = "Select the player whose windows you want to see.";
        }

        /// <summary>
        /// Calling this method will disable the <see cref="WindowSpace"/> for the <see cref="CurrentPlayer"/>.
        /// </summary>
        private void DeactivateCurrentSpace()
        {
            windowSpaces[CurrentPlayer].enabled = false;
            ManagerInstance.spaceIndicator.enabled = false;
        }

        /// <summary>
        /// Calling this method will enable the <see cref="WindowSpace"/> for the given <paramref name="playerName"/>
        /// and will set them as the <see cref="CurrentPlayer"/>.
        /// </summary>
        /// <param name="playerName">The player whose space to activate.</param>
        private void ActivateSpace(string playerName)
        {
            windowSpaces[playerName].enabled = true;
            CurrentPlayer = playerName;
            ManagerInstance.spaceIndicator.enabled = true;
            ManagerInstance.spaceIndicator.ChangeState(playerName, Color.black);
        }
    }
}

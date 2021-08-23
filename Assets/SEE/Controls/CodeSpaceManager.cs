using System.Collections.Generic;
using System.Linq;
using SEE.Game.UI.CodeWindow;
using SEE.Game.UI.Menu;
using SEE.Game.UI.StateIndicator;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.Controls
{
    /// <summary>
    /// Manages the association from players to <see cref="CodeSpace"/>.
    /// Will also display a menu which can be opened using TAB. The player whose code window shall be displayed
    /// can be selected from this menu.
    /// Note that only one instance of this class may be active in the scene. This instance can be retrieved
    /// using <see cref="ManagerInstance"/>.
    /// </summary>
    public class CodeSpaceManager : MonoBehaviour
    {
        /// <summary>
        /// String representing the local player.
        /// </summary>
        public const string LOCAL_PLAYER = "Local player";

        /// <summary>
        /// String representing no player, i.e. no code windows being displayed.
        /// </summary>
        public const string NO_PLAYER = "None";

        /// <summary>
        /// The name of the player whose code window is currently displayed.
        /// </summary>
        public string CurrentPlayer { get; private set; } = LOCAL_PLAYER;

        /// <summary>
        /// A dictionary mapping player names to their code spaces.
        /// </summary>
        private readonly Dictionary<string, CodeSpace> CodeSpaces = new Dictionary<string, CodeSpace>();

        /// <summary>
        /// This event will be invoked whenever the active code window for the <see cref="LOCAL_PLAYER"/> is changed.
        /// This includes changing the active code window to nothing (i.e. closing all of them.)
        /// </summary>
        public UnityEvent OnActiveCodeWindowChanged = new UnityEvent();

        /// <summary>
        /// The menu from which the user can select the player whose code windows they want to see.
        /// </summary>
        private SelectionMenu CodeWindowMenu;

        /// <summary>
        /// A <see cref="StateIndicator"/> which displays the IP address of the window we're viewing currently. 
        /// </summary>
        private StateIndicator SpaceIndicator;

        /// <summary>
        /// Represents the code space manager currently active in the scene.
        /// </summary>
        public static CodeSpaceManager ManagerInstance;

        /// <summary>
        /// Accesses the code space for the given <paramref name="playerName"/>.
        /// If the given <paramref name="playerName"/> does not exist, <c>null</c> will be returned.
        /// </summary>
        /// <param name="playerName">The name of the player whose code space should be returned.</param>
        public CodeSpace this[string playerName]
        {
            get => CodeSpaces.ContainsKey(playerName) ? CodeSpaces[playerName] : null;
            set => CodeSpaces[playerName] = value;
        }

        /// <summary>
        /// Inserts a Char at the given index in the given CodeWindow
        /// </summary>
        /// <param name="playerName">The name of the player whose code space should be updated.</param>
        /// <param name="title">The Title of the code window that should be updated.</param>
        /// <param name="c">The Char that should be inserted.</param>
        /// <param name="index">The index at which the Char should be inserted.</param>
        public void InsertChar(string playerName, string title, char c, int index)
        {
            if (CodeSpaces.ContainsKey(playerName))
            {
                CodeWindow window = CodeSpaces[playerName].CodeWindows.First(x => x.Title == title);
                if(window != null)
                {
                    window.InsertChar(c, index);
                }
            }
        }

        /// <summary>
        /// Deletes a Char in a given CodeWindow at the given index
        /// </summary>
        /// <param name="playerName">The name of the player whose code space should be updated.</param>
        /// <param name="title">The Title of the code window that should be updated.</param>
        /// <param name="index">The index at which the Char should be deleted.</param>
        public void DeleteChar(string playerName, string title, int index)
        {
            if (CodeSpaces.ContainsKey(playerName))
            {
                CodeWindow window = CodeSpaces[playerName].CodeWindows.First(x => x.Title == title);
                if (window != null)
                {
                    window.DeletChar(index);
                }
            }
        }


        /// <summary>
        /// Updates the code space of the player specified by <paramref name="playerName"/> using the values
        /// from <paramref name="valueObject"/>.
        /// </summary>
        /// <param name="playerName">The name of the player whose code space should be updated.</param>
        /// <param name="valueObject">The value object which represents the new code space of the player.</param>
        /// <summary>
        /// Updates the code space of the player specified by <paramref name="playerName"/> using the values
        /// from <paramref name="valueObject"/>.
        /// </summary>
        /// <param name="playerName">The name of the player whose code space should be updated.</param>
        /// <param name="valueObject">The value object which represents the new code space of the player.</param>
        public void UpdateCodeSpaceFromValueObject(string playerName, CodeSpace.CodeSpaceValues valueObject)
        {
            if (!CodeSpaces.ContainsKey(playerName))
            {
                CodeSpaces[playerName] = CodeSpace.FromValueObject(valueObject, gameObject);
                CodeWindowMenu.AddEntry(new ToggleMenuEntry(false, () => ActivateSpace(playerName),
                                                  DeactivateCurrentSpace, playerName,
                                                  $"Code window from player with IP address '{playerName}'.", Color.white));
                CodeSpaces[playerName].CodeSpaceName += $" ({playerName})";
                CodeSpaces[playerName].enabled = false;
                CodeSpaces[playerName].CanClose = false;  // User may only close their own windows
            }
            else
            {
                // We will try to do a partial update, otherwise we'd have to re-read each file on every update

                // Check for new entries
                List<CodeWindow> closedWindows = new List<CodeWindow>(CodeSpaces[playerName].CodeWindows);
                foreach (CodeWindow.CodeWindowValues windowValue in valueObject.CodeWindows)
                {
                    if (CodeSpaces[playerName].CodeWindows.All(x => windowValue.Title != x.Title))
                    {
                        // Window is new and has to be re-created
                        CodeWindow window = CodeWindow.FromValueObject(windowValue);
                        CodeSpaces[playerName].AddCodeWindow(window);
                        // Only enable window if code space is enabled as well
                        window.enabled = CodeSpaces[playerName].enabled;
                    }
                    else
                    {
                        // Visible line may have changed
                        CodeWindow window = CodeSpaces[playerName].CodeWindows.First(x => x.Title == windowValue.Title);
                        window.VisibleLine = windowValue.VisibleLine;
                        //TODO: Text merge between windowValue.Text and window.Text

                        // Window is still open, so it's not closed
                        closedWindows.RemoveAll(x => x.Title == windowValue.Title);
                    }
                }

                // Close windows which are no longer open
                closedWindows.ForEach(CodeSpaces[playerName].CloseCodeWindow);

                // Set active window if it changed
                if (CodeSpaces[playerName].ActiveCodeWindow.Title != valueObject.ActiveCodeWindow.Title)
                {
                    CodeSpaces[playerName].ActiveCodeWindow = CodeSpaces[playerName].CodeWindows.First(x => x.Title == valueObject.ActiveCodeWindow.Title);
                }
            }
        }

        private void Start()
        {
            if (FindObjectsOfType<CodeSpaceManager>().Length > 1)
            {
                Debug.LogError("Warning: More than one CodeSpaceManager is present in the scene! "
                               + "This will lead to undefined behaviour when synchronizing "
                               + "code windows across the network! No new indicator will be created.\n");
            }
            else
            {
                SpaceIndicator = gameObject.AddComponent<StateIndicator>();
                SpaceIndicator.name = "Code Space Indicator";
                // Anchor to lower left
                SpaceIndicator.AnchorMin = Vector2.zero;
                SpaceIndicator.AnchorMax = Vector2.zero;
                SpaceIndicator.Pivot = Vector2.zero;
                ManagerInstance = this;
            }

            // Create local code space and associate it with current player
            if (!TryGetComponent(out CodeSpace space))
            {
                space = gameObject.AddComponent<CodeSpace>();
            }
            CodeSpaces[LOCAL_PLAYER] = space;
            space.OnActiveCodeWindowChanged.AddListener(OnActiveCodeWindowChanged.Invoke);

            ManagerInstance.SpaceIndicator.ChangeState(LOCAL_PLAYER, Color.black);

            SetUpWindowSelectionMenu();
        }

        private void Update()
        {
            if (SEEInput.ShowCodeWindowMenu())
            {
                CodeWindowMenu.ToggleMenu();
            }
        }

        /// <summary>
        /// Creates and sets up the code window selection menu, from which the user can select a player whose
        /// code window they want to see. Initially, this will have the entries "local player" and "none".
        /// </summary>
        private void SetUpWindowSelectionMenu()
        {
            //TODO: Icons
            CodeWindowMenu = gameObject.AddComponent<SelectionMenu>();
            ToggleMenuEntry localEntry = new ToggleMenuEntry(true, () => ActivateSpace(LOCAL_PLAYER),
                                                             DeactivateCurrentSpace, LOCAL_PLAYER,
                                                             "Code windows for the local player (you).", Color.black);
            ToggleMenuEntry noneEntry = new ToggleMenuEntry(false, () => CurrentPlayer = NO_PLAYER, () => { }, NO_PLAYER,
                                                            "This option hides all code windows.", Color.grey);
            CodeWindowMenu.AddEntry(noneEntry);
            CodeWindowMenu.AddEntry(localEntry);
            foreach (KeyValuePair<string, CodeSpace> space in CodeSpaces.Where(space => space.Key != LOCAL_PLAYER))
            {
                CodeWindowMenu.AddEntry(new ToggleMenuEntry(false, () => ActivateSpace(space.Key),
                                                  DeactivateCurrentSpace, space.Key,
                                                  $"Code window from player with IP address '{space.Key}'.", Color.white));
            }
            CodeWindowMenu.Title = "Code Window Selection";
            CodeWindowMenu.Description = "Select the player whose code windows you want to see.";
        }

        /// <summary>
        /// Calling this method will disable the <see cref="CodeSpace"/> for the <see cref="CurrentPlayer"/>.
        /// </summary>
        private void DeactivateCurrentSpace()
        {
            CodeSpaces[CurrentPlayer].enabled = false;
            ManagerInstance.SpaceIndicator.enabled = false;
        }

        /// <summary>
        /// Calling this method will enable the <see cref="CodeSpace"/> for the given <paramref name="playerName"/>
        /// and will set them as the <see cref="CurrentPlayer"/>.
        /// </summary>
        /// <param name="playerName">The player whose space to activate</param>
        private void ActivateSpace(string playerName)
        {
            CodeSpaces[playerName].enabled = true;
            CurrentPlayer = playerName;
            ManagerInstance.SpaceIndicator.enabled = true;
            ManagerInstance.SpaceIndicator.ChangeState(playerName, Color.black);
        }
    }
}

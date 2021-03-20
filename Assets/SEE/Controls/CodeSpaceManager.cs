using System.Collections.Generic;
using System.Linq;
using SEE.Game.UI;
using SEE.Game.UI.CodeWindow;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.Controls
{
    /// <summary>
    /// Manages the association from players to <see cref="CodeWindowSpace"/>.
    /// Will also display a menu which can be opened using TAB. The player whose code window shall be displayed
    /// can be selected from this menu.
    /// Note that only one instance of this class may be active in the scene. This instance can be retrieved
    /// using <see cref="ManagerInstance"/>.
    /// </summary>
    public class CodeSpaceManager: MonoBehaviour
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
        /// A dictionary mapping player names to their code window spaces.
        /// </summary>
        public readonly Dictionary<string, CodeWindowSpace> CodeSpaces = new Dictionary<string, CodeWindowSpace>();

        /// <summary>
        /// This event will be invoked whenever the active code window for the <see cref="LOCAL_PLAYER"/> is changed.
        /// </summary>
        public UnityEvent OnActiveCodeWindowChanged = new UnityEvent();

        /// <summary>
        /// The menu from which the user can select the player whose code windows they want to see.
        /// </summary>
        private SelectionMenu CodeWindowMenu;

        /// <summary>
        /// Represents the code space manager currently active in the scene.
        /// </summary>
        public static CodeSpaceManager ManagerInstance;

        /// <summary>
        /// Accesses the code window space for the given <paramref name="playerName"/>.
        /// If the given <paramref name="playerName"/> does not exist, <c>null</c> will be returned.
        /// </summary>
        /// <param name="playerName">The name of the player whose code window space should be returned.</param>
        public CodeWindowSpace this[string playerName]
        {
            get => CodeSpaces.ContainsKey(playerName) ? CodeSpaces[playerName] : null;
            set => CodeSpaces[playerName] = value;
        }

        public void UpdateCodeWindowSpaceFromValueObject(string playerName, CodeWindowSpace.CodeWindowSpaceValues valueObject)
        {
            if (!CodeSpaces.ContainsKey(playerName))
            {
                CodeSpaces[playerName] = CodeWindowSpace.FromValueObject(valueObject, gameObject);
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
                        CodeSpaces[playerName].AddCodeWindow(CodeWindow.FromValueObject(windowValue));
                    }
                    else
                    {
                        // Visible line may have changed
                        CodeSpaces[playerName].CodeWindows.First(x => x.Title == windowValue.Title).VisibleLine = windowValue.VisibleLine;
                        
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
            
            //TODO: Instead of tearing down and recreating the menu each time, change the code 
            // such that old entries are removed and new entries are added.
            // This way, performance will probably be significantly improved.
            SetUpWindowSelectionMenu();
        }

        private void Start()
        {
            if (FindObjectOfType<CodeSpaceManager>())
            {
                Debug.LogError("Warning: More than one CodeSpaceManager is present in the scene! "
                               + "This will lead to undefined behaviour when synchronizing "
                               + "code windows across the network!\n");
            }
            else
            {
                ManagerInstance = this;
            }
            
            // Create local code window space and associate it with current player
            if (!TryGetComponent(out CodeWindowSpace space))
            {
                space = gameObject.AddComponent<CodeWindowSpace>();
            }
            CodeSpaces[LOCAL_PLAYER] = space;
            space.OnActiveCodeWindowChanged.AddListener(OnActiveCodeWindowChanged.Invoke);
            
            SetUpWindowSelectionMenu();
        }

        private void Update()
        {
            // Show selection menu on TAB
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                CodeWindowMenu.ToggleMenu();
            }
        }

        /// <summary>
        /// Creates and sets up the code window selection menu, from which the user can select a player whose
        /// code window they want to see. Initially, this will have the entries "local player" and "none".
        /// </summary>
        /// <returns>The newly created <see cref="SelectionMenu"/></returns>
        private void SetUpWindowSelectionMenu()
        {
            //TODO: Icons
            if (CodeWindowMenu)
            {
                Destroy(CodeWindowMenu);
            }
            CodeWindowMenu = gameObject.AddComponent<SelectionMenu>();
            ToggleMenuEntry localEntry = new ToggleMenuEntry(true, () => ActivateSpace(LOCAL_PLAYER), 
                                                             DeactivateCurrentSpace, LOCAL_PLAYER,
                                                             "Code windows for the local player (you).", Color.black);
            ToggleMenuEntry noneEntry = new ToggleMenuEntry(false, () => CurrentPlayer = NO_PLAYER, () => { }, NO_PLAYER, 
                                                            "This option hides all code windows.", Color.grey);
            CodeWindowMenu.AddEntry(localEntry);
            foreach (KeyValuePair<string, CodeWindowSpace> space in CodeSpaces.Where(space => space.Key != LOCAL_PLAYER))
            {
                CodeWindowMenu.AddEntry(new ToggleMenuEntry(false, () => ActivateSpace(space.Key),
                                                  DeactivateCurrentSpace, space.Key, 
                                                  $"Code window from player with IP address '{space.Key}'.", Color.white));
            }
            CodeWindowMenu.AddEntry(noneEntry);
            CodeWindowMenu.Title = "Code Window Selection";
            CodeWindowMenu.Description = "Select the player whose code windows you want to see.";
            
            void ActivateSpace(string playerName)
            {
                CodeSpaces[playerName].enabled = true;
                CurrentPlayer = playerName;
            }

            void DeactivateCurrentSpace()
            {
                CodeSpaces[CurrentPlayer].enabled = false;
            }
        }
    }
}
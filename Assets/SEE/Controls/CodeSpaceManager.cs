using System.Collections.Generic;
using SEE.Game.UI;
using SEE.Game.UI.CodeWindow;
using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// Manages the association from players to <see cref="CodeWindowSpace"/>.
    /// Will also display a menu which can be opened using TAB. The player whose code window shall be displayed
    /// can be selected from this menu.
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
        private readonly Dictionary<string, CodeWindowSpace> CodeSpaces = new Dictionary<string, CodeWindowSpace>();

        /// <summary>
        /// The menu from which the user can select the player whose code windows they want to see.
        /// </summary>
        private SelectionMenu CodeWindowMenu;

        /// <summary>
        /// Accesses the code window space for the given <paramref name="playerName"/>.
        /// </summary>
        /// <param name="playerName">The name of the player whose code window space should be returned.</param>
        public CodeWindowSpace this[string playerName]
        {
            get => CodeSpaces[playerName];
            set => CodeSpaces[playerName] = value;
        }

        private void Start()
        {
            // Create local code window space and associate it with current player
            if (!TryGetComponent(out CodeWindowSpace space))
            {
                space = gameObject.AddComponent<CodeWindowSpace>();
            }
            CodeSpaces[LOCAL_PLAYER] = space;
            
            CodeWindowMenu = SetUpWindowSelectionMenu();
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
        private SelectionMenu SetUpWindowSelectionMenu()
        {
            //TODO: Icons
            SelectionMenu menu = gameObject.AddComponent<SelectionMenu>();
            ToggleMenuEntry localEntry = new ToggleMenuEntry(true, () => ActivateSpace(LOCAL_PLAYER), 
                                                             DeactivateCurrentSpace, LOCAL_PLAYER,
                                                             "Code windows for the local player (you).", Color.black);
            ToggleMenuEntry noneEntry = new ToggleMenuEntry(false, () => CurrentPlayer = NO_PLAYER, () => { }, NO_PLAYER, 
                                                            "This option hides all code windows.", Color.grey);
            menu.AddEntry(localEntry);
            menu.AddEntry(noneEntry);
            menu.Title = "Code Window Selection";
            menu.Description = "Select the player whose code windows you want to see.";
            return menu;
            
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
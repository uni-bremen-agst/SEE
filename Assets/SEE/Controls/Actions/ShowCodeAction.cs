using System.Collections.Generic;
using System.Linq;
using SEE.Game.UI;
using SEE.Game.UI.CodeWindow;
using SEE.GO;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to display the source code of the currently selected node using <see cref="CodeWindow"/>s.
    /// </summary>
    internal class ShowCodeAction : MonoBehaviour
    {
        /// <summary>
        /// Start() will register an anonymous delegate of type 
        /// <see cref="ActionState.OnStateChangedFn"/> on the event
        /// <see cref="ActionState.OnStateChanged"/> to be called upon every
        /// change of the action state, where the newly entered state will
        /// be passed as a parameter. The anonymous delegate will compare whether
        /// this state equals <see cref="ThisActionState"/> and if so, execute
        /// what needs to be done for this action here. If that parameter is
        /// different from <see cref="ThisActionState"/>, this action will
        /// put itself to sleep. 
        /// Thus, this action will be executed only if the new state is 
        /// <see cref="ThisActionState"/>.
        /// </summary>
        private readonly ActionStateType ThisActionState = ActionStateType.ShowCode;

        /// <summary>
        /// String representing the local player.
        /// </summary>
        private const string LOCAL_PLAYER = "Local player";

        /// <summary>
        /// String representing no player, i.e. no code windows being displayed.
        /// </summary>
        private const string NO_PLAYER = "None";
        
        /// <summary>
        /// The name of the player whose code window is currently displayed.
        /// </summary>
        private string CurrentPlayer = LOCAL_PLAYER;

        /// <summary>
        /// A dictionary mapping player names to their code window spaces.
        /// </summary>
        private readonly Dictionary<string, CodeWindowSpace> CodeSpaces = new Dictionary<string, CodeWindowSpace>();

        /// <summary>
        /// The menu from which the user can select the player whose code windows they want to see.
        /// </summary>
        private SelectionMenu CodeWindowMenu;

        /// <summary>
        /// The selected node.
        /// </summary>
        private NodeRef selectedNode;

        /// <summary>
        /// The currently selected node.
        /// This is a cached version of <see cref="selectedNode"/> and used to determine
        /// whether we need to change which code window is currently displayed.
        /// </summary>
        private NodeRef currentlySelectedNode;

        /// <summary>
        /// The selected node's filename.
        /// </summary>
        private string selectedPath;

        private void Start()
        {
            // An anonymous delegate is registered for the event <see cref="ActionState.OnStateChanged"/>.
            // This delegate will be called from <see cref="ActionState"/> upon every
            // state changed where the passed parameter is the newly entered state.
            ActionState.OnStateChanged += (ActionStateType newState) =>
            {
                // Is this our action state where we need to do something?
                if (Equals(newState, ThisActionState))
                {
                    // The MonoBehaviour is enabled and Update() will be called by Unity.
                    enabled = true;
                    InteractableObject.LocalAnySelectIn += LocalAnySelectIn;
                    InteractableObject.LocalAnySelectOut += LocalAnySelectOut;
                }
                else
                {
                    // The MonoBehaviour is disabled and Update() no longer be called by Unity.
                    enabled = false;
                    InteractableObject.LocalAnySelectIn -= LocalAnySelectIn;
                    InteractableObject.LocalAnySelectOut -= LocalAnySelectOut;
                }
            };
            enabled = ActionState.Is(ThisActionState);

            // Create local code window space and associate it with current player
            if (!TryGetComponent(out CodeWindowSpace space))
            {
                space = gameObject.AddComponent<CodeWindowSpace>();
            }
            CodeSpaces[LOCAL_PLAYER] = space;

            // TODO: We will also create a menu of players with open code windows
            CodeWindowMenu = SetUpWindowSelectionMenu();
        }

        /// <summary>
        /// Creates and sets up the code window selection menu, from which the user can select a player whose
        /// code window they want to see. Initially, this will have the entries "local player" and "none".
        /// </summary>
        /// <returns>The newly created <see cref="SelectionMenu"/></returns>
        private SelectionMenu SetUpWindowSelectionMenu()
        {
            //TODO: Icons
            //TODO: Actions
            SelectionMenu menu = gameObject.AddComponent<SelectionMenu>();
            ToggleMenuEntry localEntry = new ToggleMenuEntry(true, () => { }, () => { }, LOCAL_PLAYER,
                                                             "Code windows for the local player (you).", Color.black);
            ToggleMenuEntry noneEntry = new ToggleMenuEntry(true, () => { }, () => { }, NO_PLAYER, 
                                                            "This option hides all code windows.", Color.grey);
            menu.AddEntry(localEntry);
            menu.AddEntry(noneEntry);
            menu.Title = "Code Window Selection";
            menu.Description = "Select the player whose code windows you want to see.";
            return menu;
        }

        private void SetActivePlayer(string playerName)
        {
            // Hide existing code windows
            CurrentPlayer = playerName;
        }

        private void Update()
        {
            // This script should be disabled if the action state is not this action's type
            if (!ActionState.Is(ThisActionState))
            {
                // The MonoBehaviour is disabled and Update() no longer be called by Unity.
                enabled = false;
                InteractableObject.LocalAnySelectIn -= LocalAnySelectIn;
                InteractableObject.LocalAnySelectOut -= LocalAnySelectOut;
                return;
            }

            // Show selection menu on TAB
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                CodeWindowMenu.ToggleMenu();
            }

            // Only allow local player to open new code windows
            if (CurrentPlayer == LOCAL_PLAYER && !Equals(selectedNode?.Value, currentlySelectedNode?.Value))
            {
                currentlySelectedNode = selectedNode;
                // If nothing is selected, there's nothing more we need to do
                if (selectedNode == null)
                {
                    return;
                }

                // Create new code window for active selection, or use existing one
                if (!selectedNode.TryGetComponent(out CodeWindow codeWindow))
                {
                    codeWindow = selectedNode.gameObject.AddComponent<CodeWindow>();
                    // Pass file name of source code file to read from it
                    if (!selectedNode.Value.TryGetString("Source.File", out string selectedFile))
                    {
                        Debug.LogError("Source.Path was set, but Source.File was not. Can't show code window.\n");
                        return;
                    }

                    codeWindow.Title = selectedNode.Value.SourceName;
                    // If SourceName differs from Source.File (except file extension), display both
                    if (!codeWindow.Title.Replace(".", "").Equals(selectedFile.Split('.').Reverse().Skip(1)
                                                                              .Aggregate("", (acc, s) => s + acc)))
                    {
                        codeWindow.Title += $" ({selectedFile})";
                    }

                    codeWindow.EnterFromFile($"{selectedPath}{selectedFile}"); // selectedPath has trailing /
                }

                // Pass line number to automatically scroll to it, if it exists
                if (selectedNode.Value.TryGetInt("Source.Line", out int line))
                {
                    codeWindow.VisibleLine = line;
                }
                    
                // Add code window to our space of code windows
                CodeSpaces[LOCAL_PLAYER].AddCodeWindow(codeWindow);

                CodeSpaces[LOCAL_PLAYER].ActiveCodeWindow = codeWindow;
                //TODO: Set font size etc per SEECity settings (maybe, or maybe that's too much)
            }
        }

        private void LocalAnySelectIn(InteractableObject interactableObject)
        {
            if (!interactableObject.gameObject.TryGetComponent(out selectedNode) 
                || !selectedNode.Value.TryGetString("Source.Path", out selectedPath))
            {
                    selectedPath = null;
                    selectedNode = null;
            }
        }

        private void LocalAnySelectOut(InteractableObject interactableObject)
        {
            selectedPath = null;
            selectedNode = null;
        }
    }
}

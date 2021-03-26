using System.Linq;
using SEE.Game.UI.CodeWindow;
using SEE.GO;
using SEE.Net;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to display the source code of the currently selected node using <see cref="CodeWindow"/>s.
    /// </summary>
    internal class ShowCodeAction : MonoBehaviour
    {
        /// <summary>
        /// Manager object which takes care of the player selection menu and code space dictionary for us.
        /// </summary>
        private CodeSpaceManager spaceManager;

        /// <summary>
        /// Action responsible for synchronizing the code spaces across the network.
        /// </summary>
        private SyncCodeSpaceAction syncAction;

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

        private void OnDisable()
        {
            InteractableObject.LocalAnySelectIn -= LocalAnySelectIn;
            InteractableObject.LocalAnySelectOut -= LocalAnySelectOut;
        }

        private void OnEnable()
        {
            InteractableObject.LocalAnySelectIn += LocalAnySelectIn;
            InteractableObject.LocalAnySelectOut += LocalAnySelectOut;
        }

        private void Start()
        {
            if (!gameObject.TryGetComponent<CodeSpaceManager>(out spaceManager))
            {
                spaceManager = gameObject.AddComponent<CodeSpaceManager>();
            }

            syncAction = new SyncCodeSpaceAction(spaceManager[CodeSpaceManager.LOCAL_PLAYER]);
            spaceManager.OnActiveCodeWindowChanged.AddListener(() => syncAction.UpdateSpace(spaceManager[CodeSpaceManager.LOCAL_PLAYER]));
        }

        private void Update()
        {
            // Only allow local player to open new code windows
            if (spaceManager.CurrentPlayer == CodeSpaceManager.LOCAL_PLAYER 
                && !Equals(selectedNode?.Value, currentlySelectedNode?.Value))
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
                    // If SourceName differs from Source.File (except for its file extension), display both
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
                    
                // Add code window to our space of code window, if it isn't in there yet
                if (!spaceManager[CodeSpaceManager.LOCAL_PLAYER].CodeWindows.Contains(codeWindow))
                {
                    spaceManager[CodeSpaceManager.LOCAL_PLAYER].AddCodeWindow(codeWindow);
                    codeWindow.ScrollEvent.AddListener(() => syncAction.UpdateSpace(spaceManager[CodeSpaceManager.LOCAL_PLAYER]));
                }
                spaceManager[CodeSpaceManager.LOCAL_PLAYER].ActiveCodeWindow = codeWindow;
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

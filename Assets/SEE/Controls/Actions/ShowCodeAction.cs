using System.Collections.Generic;
using System.IO;
using System.Linq;
using SEE.Game;
using SEE.Game.City;
using SEE.Game.UI.CodeWindow;
using SEE.Game.UI.Notification;
using SEE.GO;
using SEE.Net;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to display the source code of the currently selected node using <see cref="CodeWindow"/>s.
    /// </summary>
    internal class ShowCodeAction : AbstractPlayerAction
    {
        /// <summary>
        /// Manager object which takes care of the player selection menu and code space dictionary for us.
        /// </summary>
        private CodeSpaceManager spaceManager;

        /// <summary>
        /// Action responsible for synchronizing the code spaces across the network.
        /// </summary>
        private SyncCodeSpaceAction syncAction;

        public override HashSet<string> GetChangedObjects()
        {
            // Changes to the code space are handled and synced by us separately, so we won't include them here.
            return new HashSet<string>();
        }

        public override ActionStateType GetActionStateType() => ActionStateType.ShowCode;

        /// <summary>
        /// Returns a new instance of <see cref="ShowCodeAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static ReversibleAction CreateReversibleAction() => new ShowCodeAction();

        public override ReversibleAction NewInstance() => CreateReversibleAction();

        public override void Start()
        {
            if(ICRDT.GetLocalID() == 0)
            {
                new NetCRDT().RequestID();
            }
            const string title = "Code Space Manager";
            GameObject gameObject = GameObject.Find(title) ?? new GameObject(title);
            if (!gameObject.TryGetComponent(out spaceManager))
            {
                spaceManager = gameObject.AddComponent<CodeSpaceManager>();
            }

            syncAction = new SyncCodeSpaceAction(spaceManager[CodeSpaceManager.LOCAL_PLAYER]);
            spaceManager.OnActiveCodeWindowChanged.AddListener(() => syncAction.UpdateSpace(spaceManager[CodeSpaceManager.LOCAL_PLAYER]));
        }

        public override bool Update()
        {
            // Only allow local player to open new code windows
            if (spaceManager.CurrentPlayer == CodeSpaceManager.LOCAL_PLAYER
                && Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(out RaycastHit hit, out GraphElementRef _) == HitGraphElement.Node)
            {
                NodeRef selectedNode = hit.collider.gameObject.GetComponent<NodeRef>();
                // If nothing is selected, there's nothing more we need to do
                if (selectedNode == null)
                {
                    return false;
                }

                // Create new code window for active selection, or use existing one
                if (!selectedNode.TryGetComponent(out CodeWindow codeWindow))
                {
                    codeWindow = selectedNode.gameObject.AddComponent<CodeWindow>();
                    // Pass file name of source code file to read from it
                    string selectedFile = selectedNode.Value.Filename();
                    if (selectedFile == null)
                    {
                        Debug.LogError("Source path was set, but source filename was not. Can't show code window.\n");
                        return false;
                    }

                    codeWindow.Title = selectedNode.Value.SourceName;
                    // If SourceName differs from Source.File (except for its file extension), display both
                    if (!codeWindow.Title.Replace(".", "").Equals(selectedFile.Split('.').Reverse().Skip(1)
                                                                              .Aggregate("", (acc, s) => s + acc)))
                    {
                        codeWindow.Title += $" ({selectedFile})";
                    }

                    codeWindow.EnterFromFile(selectedNode.Value.AbsolutePlatformPath());
                }

                // Pass line number to automatically scroll to it, if it exists
                int? line = selectedNode.Value.SourceLine();
                if (line.HasValue)
                {
                    codeWindow.VisibleLine = line.Value;
                }

                // Add solution path
                GameObject cityObject = SceneQueries.GetCodeCity(selectedNode.transform).gameObject;
                if (cityObject == null || !cityObject.TryGetComponent(out AbstractSEECity city))
                {
                    ShowNotification.Warn("No code city",
                      $"Selected node '{selectedNode.Value.SourceName}' is not contained in a code city.");
                    return false;
                }
                codeWindow.SolutionPath = city.SolutionPath.Path;

                // Add code window to our space of code window, if it isn't in there yet
                if (!spaceManager[CodeSpaceManager.LOCAL_PLAYER].CodeWindows.Contains(codeWindow))
                {
                    spaceManager[CodeSpaceManager.LOCAL_PLAYER].AddCodeWindow(codeWindow);
                    codeWindow.ScrollEvent.AddListener(() => syncAction.UpdateSpace(spaceManager[CodeSpaceManager.LOCAL_PLAYER]));
                }
                spaceManager[CodeSpaceManager.LOCAL_PLAYER].ActiveCodeWindow = codeWindow;
                // TODO: Set font size etc in settings (maybe, or maybe that's too much)
            }

            return false;
        }
    }
}

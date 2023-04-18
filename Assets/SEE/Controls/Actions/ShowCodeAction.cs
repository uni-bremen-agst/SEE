using System.Collections.Generic;
using System.IO;
using System.Linq;
using SEE.Game;
using SEE.Game.City;
using SEE.Game.UI.Window.CodeWindow;
using SEE.Game.UI.Notification;
using SEE.GO;
using SEE.Net.Actions;
using SEE.Utils;
using UnityEngine;
using SEE.DataModel.DG;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to display the source code of the currently selected node using <see cref="CodeWindow"/>s.
    /// </summary>
    internal class ShowCodeAction : AbstractPlayerAction
    {
        /// <summary>
        /// Manager object which takes care of the player selection menu and window space dictionary for us.
        /// </summary>
        private WindowSpaceManager spaceManager;

        /// <summary>
        /// Action responsible for synchronizing the window spaces across the network.
        /// </summary>
        private SyncWindowSpaceAction syncAction;

        public override HashSet<string> GetChangedObjects()
        {
            // Changes to the window space are handled and synced by us separately, so we won't include them here.
            return new HashSet<string>();
        }
        public override ActionStateType GetActionStateType() => ActionStateType.ShowCode;

        /// <summary>
        /// Returns a new instance of <see cref="ShowCodeAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static ReversibleAction CreateReversibleAction() => new ShowCodeAction();

        public override ReversibleAction NewInstance() => CreateReversibleAction();

        public override void Awake()
        {
            // In case we do not have an ID yet, we request one.
            if (ICRDT.GetLocalID() == 0)
            {
                new NetCRDT().RequestID();
            }
            spaceManager = WindowSpaceManager.ManagerInstance;
        }
        public override void Start()
        {
            syncAction = new SyncWindowSpaceAction();
            spaceManager.OnActiveWindowChanged.AddListener(() => syncAction.UpdateSpace(spaceManager[WindowSpaceManager.LOCAL_PLAYER]));
        }

        public override bool Update()
        {
            // Only allow local player to open new code windows
            if (spaceManager.CurrentPlayer == WindowSpaceManager.LOCAL_PLAYER
                && SEEInput.Select()
                && Raycasting.RaycastGraphElement(out RaycastHit hit, out GraphElementRef graphElementRef) != HitGraphElement.None)
            {
                // If nothing is selected, there's nothing more we need to do
                if (graphElementRef == null)
                {
                    return false;
                }
                GraphElement graphElement;
                if (graphElementRef is NodeRef nodeRef)
                {
                    graphElement = nodeRef.Value;
                }
                else if (graphElementRef is EdgeRef edgeRef)
                {
                    graphElement = edgeRef.Value;
                }
                else
                {
                    Debug.LogError("Neither node nor edge.\n");
                    return false;
                }

                // File name of source code file to read from it
                string selectedFile = graphElement.Filename();
                if (selectedFile == null)
                {
                    ShowNotification.Warn("No file", $"Selected {GetName(graphElement)} has no filename.");
                    return false;
                }
                string absolutePlatformPath = graphElement.AbsolutePlatformPath();
                if (!File.Exists(absolutePlatformPath))
                {
                    ShowNotification.Warn("File does not exist", $"Path {absolutePlatformPath} of selected {GetName(graphElement)} does not exist.");
                    return false;
                }
                if ((File.GetAttributes(absolutePlatformPath) & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    ShowNotification.Warn("Not a file", $"Path {absolutePlatformPath} of selected {GetName(graphElement)} is a directory.");
                    return false;
                }

                // Create new window for active selection, or use existing one
                if (!graphElementRef.TryGetComponent(out CodeWindow codeWindow))
                {
                    codeWindow = graphElementRef.gameObject.AddComponent<CodeWindow>();

                    codeWindow.Title = GetName(graphElement);
                    // If SourceName differs from Source.File (except for its file extension), display both
                    if (!codeWindow.Title.Replace(".", "").Equals(selectedFile.Split('.').Reverse().Skip(1)
                                                                              .Aggregate("", (acc, s) => s + acc)))
                    {
                        codeWindow.Title += $" ({selectedFile})";
                    }

                    codeWindow.EnterFromFile(absolutePlatformPath);
                }

                // Pass line number to automatically scroll to it, if it exists
                int? line = graphElement.SourceLine();
                if (line.HasValue)
                {
                    codeWindow.VisibleLine = line.Value;
                }
                else
                {
                    Debug.LogWarning($"Selected {GetName(graphElement)} has no source line.\n");
                }

                // Add solution path
                GameObject cityObject = SceneQueries.GetCodeCity(graphElementRef.transform).gameObject;
                if (cityObject == null || !cityObject.TryGetComponent(out AbstractSEECity city))
                {
                    ShowNotification.Warn("No code city",
                      $"Selected {GetName(graphElement)} is not contained in a code city.");
                    return false;
                }
                codeWindow.ShowIssues = city.ErosionSettings.ShowIssuesInCodeWindow;
                codeWindow.SolutionPath = city.SolutionPath.Path;

                // Add code window to our space of code window, if it isn't in there yet
                if (!spaceManager[WindowSpaceManager.LOCAL_PLAYER].Windows.Contains(codeWindow))
                {
                    spaceManager[WindowSpaceManager.LOCAL_PLAYER].AddWindow(codeWindow);
                    codeWindow.ScrollEvent.AddListener(() => syncAction.UpdateSpace(spaceManager[WindowSpaceManager.LOCAL_PLAYER]));
                }
                spaceManager[WindowSpaceManager.LOCAL_PLAYER].ActiveWindow = codeWindow;
                // TODO: Set font size etc in settings (maybe, or maybe that's too much)
            }

            return false;

            // Returns a human-readable representation of given graphElement.
            static string GetName(GraphElement graphElement)
            {
                return graphElement.ToShortString();
            }
        }
    }
}

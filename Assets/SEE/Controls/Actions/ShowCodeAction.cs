using System.Collections.Generic;
using System.IO;
using System.Linq;
using SEE.Game;
using SEE.Game.City;
using SEE.UI.Window.CodeWindow;
using SEE.UI.Notification;
using SEE.GO;
using SEE.Net.Actions;
using SEE.Utils;
using UnityEngine;
using SEE.DataModel.DG;
using System;
using SEE.Utils.History;

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

        public override ActionStateType GetActionStateType() => ActionStateTypes.ShowCode;

        /// <summary>
        /// Returns a new instance of <see cref="ShowCodeAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static IReversibleAction CreateReversibleAction() => new ShowCodeAction();

        public override IReversibleAction NewInstance() => CreateReversibleAction();

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
            spaceManager.OnActiveWindowChanged.AddListener(() => syncAction.UpdateSpace(spaceManager[WindowSpaceManager.LocalPlayer]));
        }

        public override bool Update()
        {
            // Only allow local player to open new code windows
            if (spaceManager.CurrentPlayer == WindowSpaceManager.LocalPlayer
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

                // Edges of type Clone will be handled differently. For these, we will be
                // showing a unified diff.
                CodeWindow codeWindow = graphElement is Edge edge && edge.Type == "Clone" ?
                                        ShowUnifiedDiff(edge, graphElementRef)
                                      : ShowCode(graphElement, graphElementRef);

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
                if (!spaceManager[WindowSpaceManager.LocalPlayer].Windows.Contains(codeWindow))
                {
                    spaceManager[WindowSpaceManager.LocalPlayer].AddWindow(codeWindow);
                    codeWindow.ScrollEvent.AddListener(() => syncAction.UpdateSpace(spaceManager[WindowSpaceManager.LocalPlayer]));
                }
                spaceManager[WindowSpaceManager.LocalPlayer].ActiveWindow = codeWindow;
                // TODO: Set font size etc in settings (maybe, or maybe that's too much)
            }

            return false;

            // Returns a new CodeWindow showing a unified diff for the given Clone edge.
            // We are assuming that the edge has type Clone.
            static CodeWindow ShowUnifiedDiff(Edge edge, GraphElementRef graphElementRef)
            {
                (string sourceFilename, string sourceAbsolutePlatformPath) = GetPath(edge.Source);
                (string targetFilename, string targetAbsolutePlatformPath) = GetPath(edge.Target);
                int sourceStartLine = GetAttribute(edge, "Clone.Source.Start.Line");
                int sourceEndLine = GetAttribute(edge, "Clone.Source.End.Line");
                int targetStartLine = GetAttribute(edge, "Clone.Target.Start.Line");
                int targetEndLine = GetAttribute(edge, "Clone.Target.End.Line");

                string[] diff = TextualDiff.Diff(sourceAbsolutePlatformPath, sourceStartLine, sourceEndLine,
                                                 targetAbsolutePlatformPath, targetStartLine, targetEndLine);
                CodeWindow codeWindow = GetOrCreateCodeWindow(edge, graphElementRef, sourceFilename);
                codeWindow.EnterFromText(diff, true);
                codeWindow.ScrolledVisibleLine = 1;
                return codeWindow;
            }

            // Returns the value of the edge's integer attribute.
            // If none exists, the user will be notified and an exception will be thrown.
            static int GetAttribute(Edge edge, string attribute)
            {
                if (edge.TryGetInt(attribute, out int value))
                {
                    return value;
                }
                else
                {
                    string message = $"Selected {GetName(edge)} has no attribute {attribute}.";
                    ShowNotification.Warn("No attribute", message);
                    throw new Exception(message);
                }
            }

            // Returns the filename and the absolute platform-specific path of
            // given graphElement.
            static (string filename, string absolutePlatformPath) GetPath(GraphElement graphElement)
            {
                string filename = graphElement.Filename();
                if (filename == null)
                {
                    string message = $"Selected {GetName(graphElement)} has no filename.";
                    ShowNotification.Warn("No file", message);
                    throw new Exception(message);
                }
                string absolutePlatformPath = graphElement.AbsolutePlatformPath();
                if (!File.Exists(absolutePlatformPath))
                {
                    string message = $"Path {absolutePlatformPath} of selected {GetName(graphElement)} does not exist.";
                    ShowNotification.Warn("File does not exist", message);
                    throw new Exception(message);
                }
                if ((File.GetAttributes(absolutePlatformPath) & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    string message = $"Path {absolutePlatformPath} of selected {GetName(graphElement)} is a directory.";
                    ShowNotification.Warn("Not a file", message);
                    throw new Exception(message);
                }
                return (filename, absolutePlatformPath);
            }

            // If the gameObject associated with graphElementRef has already a CodeWindow
            // attached to it, that CodeWindow will be returned. Otherwise a new CodeWindow
            // will be attached to the gameObject associated with graphElementRef and returned.
            // The title for the newly created CodeWindow will be GetName(graphElement).
            static CodeWindow GetOrCreateCodeWindow(GraphElement graphElement, GraphElementRef graphElementRef, string filename)
            {
                // Create new window for active selection, or use existing one
                if (!graphElementRef.TryGetComponent(out CodeWindow codeWindow))
                {
                    codeWindow = graphElementRef.gameObject.AddComponent<CodeWindow>();

                    codeWindow.Title = GetName(graphElement);
                    // If SourceName differs from Source.File (except for its file extension), display both
                    if (!codeWindow.Title.Replace(".", "").Equals(filename.Split('.').Reverse().Skip(1)
                                                                              .Aggregate("", (acc, s) => s + acc)))
                    {
                        codeWindow.Title += $" ({filename})";
                    }
                }
                return codeWindow;
            }

            // Returns a CodeWindow showing the code range of graphElement
            // retrieved from a file. The path of the file is retrieved from
            // the absolute path as specified by the graphElements source location
            // attributes.
            static CodeWindow ShowCode(GraphElement graphElement, GraphElementRef graphElementRef)
            {
                // File name of source code file to read from it
                (string filename, string absolutePlatformPath) = GetPath(graphElement);
                CodeWindow codeWindow = GetOrCreateCodeWindow(graphElement, graphElementRef, filename);
                codeWindow.EnterFromFile(absolutePlatformPath);

                // Pass line number to automatically scroll to it, if it exists
                int? line = graphElement.SourceLine();
                if (line.HasValue)
                {
                    codeWindow.ScrolledVisibleLine = line.Value;
                }
                else
                {
                    Debug.LogWarning($"Selected {GetName(graphElement)} has no source line.\n");
                }

                return codeWindow;
            }

            // Returns a human-readable representation of given graphElement.
            static string GetName(GraphElement graphElement)
            {
                return graphElement.ToShortString();
            }
        }
    }
}

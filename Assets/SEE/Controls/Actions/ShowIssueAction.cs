using System.Collections.Generic;
using System.IO;
using System.Linq;
using SEE.UI.Window;
using SEE.UI.Notification;
using SEE.GO;
using SEE.Net.Actions;
using SEE.Utils;
using UnityEngine;
using SEE.DataModel.DG;
using System;
using Cysharp.Threading.Tasks;
using SEE.UI.Window;
using SEE.Utils.History;
using SEE.Game.City;
using SEE.VCS;
using SEE.XR;

using GraphElementRef = SEE.GO.GraphElementRef;
using Range = SEE.DataModel.DG.Range;
using DG.Tweening.Plugins.Core.PathCore;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to display the source code of the currently selected node using <see cref="CodeWindow"/>s.
    /// </summary>
    internal class ShowIssueAction : AbstractPlayerAction
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

        public override ActionStateType GetActionStateType() => ActionStateTypes.ShowIssue;

        /// <summary>
        /// Returns a new instance of <see cref="ShowCodeAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static IReversibleAction CreateReversibleAction() => new ShowIssueAction();

        public override IReversibleAction NewInstance() => CreateReversibleAction();

        public override void Awake()
        {
            spaceManager = WindowSpaceManager.ManagerInstance;
        }

        public override void Start()
        {
            Awake();


            ShowNotification.Error("Show Notification Issue ShowIssueAction Start15.", "Notify", 10, true);
            //ShowNotification.Error("Show Notification Issue ShowIssueWindow.", "Notify", 10, true);
         
            syncAction = new SyncWindowSpaceAction();
            spaceManager.OnActiveWindowChanged.AddListener(UpdateSpace);
            spaceManager[WindowSpaceManager.LocalPlayer].OnWindowAdded.AddListener(w =>
            {
                if (w is IssueWindow codeWindow)
                {
                    codeWindow.Window.active = true;
                  // codeWindow.ScrollEvent.AddListener(UpdateSpace);
                }
            });
            return;

            void UpdateSpace()
            {
                syncAction.UpdateSpace(spaceManager[WindowSpaceManager.LocalPlayer]);
            }
        }

        /// <summary>
        /// If the gameObject associated with graphElementRef has already a CodeWindow
        /// attached to it, that CodeWindow will be returned. Otherwise a new CodeWindow
        /// will be attached to the gameObject associated with graphElementRef and returned.
        /// The title for the newly created CodeWindow will be GetName(graphElement).
        /// </summary>
        /// <param name="graphElementRef">The graph element to get the CodeWindow for</param>
        /// <param name="filename">The filename to use for the CodeWindow title</param>
        private static IssueWindow GetOrCreateCodeWindow(GraphElementRef graphElementRef, string filename)
        {
            // Create new window for active selection, or use existing one
            if (!graphElementRef.TryGetComponent(out IssueWindow codeWindow))
            {
                codeWindow = graphElementRef.gameObject.AddComponent<IssueWindow>();

                codeWindow.Title = "Issues";
                // If SourceName differs from Source.File (except for its file extension), display both
                //if (!codeWindow.Title.Replace(".", "").Equals(filename.Split('.').Reverse().Skip(1)
                //                                                      .Aggregate("", (acc, s) => s + acc)))
                //{
                //    codeWindow.Title += $" ({filename})";
                //}
            }
            return codeWindow;
        }

        /// <summary>
        /// Returns the <see cref="GraphElement.Filename"/> and the absolute platform-specific path of
        /// given <paramref name="graphElement"/>.
        /// </summary>
        /// <param name="graphElement">The graph element to get the filename and path for</param>
        /// <returns>filename and absolute path</returns>
        /// <exception cref="InvalidOperationException">
        /// If the given graphElement has no filename or the path does not exist.
        /// </exception>
        private static (string filename, string absolutePlatformPath) GetPath(GraphElement graphElement)
        {
            string filename = graphElement.Filename;
            if (filename == null)
            {
                string message = $"Selected {GetName(graphElement)} has no filename.";
                ShowNotification.Error("No filename", message, log: false);
                throw new InvalidOperationException(message);
            }
            string absolutePlatformPath = graphElement.AbsolutePlatformPath();
            if (!File.Exists(absolutePlatformPath))
            {
                string message = $"Path {absolutePlatformPath} of selected {GetName(graphElement)} does not exist.";
                ShowNotification.Error("Path does not exist", message, log: false);
                throw new InvalidOperationException(message);
            }
            if ((File.GetAttributes(absolutePlatformPath) & FileAttributes.Directory) == FileAttributes.Directory)
            {
                string message = $"Path {absolutePlatformPath} of selected {GetName(graphElement)} is a directory.";
                ShowNotification.Error("Path is a directory", message, log: false);
                throw new InvalidOperationException(message);
            }
            return (filename, absolutePlatformPath);
        }

        /// <summary>
        /// Returns a human-readable representation of given graphElement.
        /// </summary>
        /// <param name="graphElement">The graph element to get the name for</param>
        /// <returns>human-readable name</returns>
        private static string GetName(GraphElement graphElement)
        {
            return graphElement.ToShortString();
        }

        /// <summary>
        /// Returns a new CodeWindow showing a unified diff for the given Clone edge.
        /// We are assuming that the edge has type Clone.
        /// </summary>
        /// <param name="edgeRef">The edge to get the CodeWindow for</param>
        /// <returns>new CodeWindow showing a unified diff</returns>
        /// <exception cref="InvalidOperationException">If the given edge is not a proper Clone edge</exception>
        public static IssueWindow ShowUnifiedDiff(EdgeRef edgeRef)
        {
            Edge edge = edgeRef.Value;
            (string sourceFilename, string sourceAbsolutePlatformPath) = GetPath(edge.Source);
            (string _, string targetAbsolutePlatformPath) = GetPath(edge.Target);
            int sourceStartLine = GetAttribute(edge, "Clone.Source.Start.Line");
            int sourceEndLine = GetAttribute(edge, "Clone.Source.End.Line");
            int targetStartLine = GetAttribute(edge, "Clone.Target.Start.Line");
            int targetEndLine = GetAttribute(edge, "Clone.Target.End.Line");

            string[] diff = TextualDiff.Diff(sourceAbsolutePlatformPath, sourceStartLine, sourceEndLine,
                                             targetAbsolutePlatformPath, targetStartLine, targetEndLine);
            IssueWindow issueWindow = GetOrCreateCodeWindow(edgeRef, sourceFilename);
            //issueWindow.EnterFromText(diff, true);
            //issueWindow.ScrolledVisibleLine = 1;
            return issueWindow;

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
                    ShowNotification.Error("No attribute", message, log: false);
                    throw new InvalidOperationException(message);
                }
            }
        }

        /// <summary>
        /// Returns a new CodeWindow showing a diff for the given <paramref name="graphElementRef"/>
        /// in <paramref name="city"/>.
        /// </summary>
        /// <param name="graphElementRef">The graph element to get the CodeWindow for</param>
        /// <param name="city">the code city <paramref name="graphElementRef"/> is contained
        /// in; it is used to determine version control information needed to
        /// calculate the diff</param>
        /// <returns>new CodeWindow showing a diff</returns>
        public static IssueWindow ShowVCSDiff(GraphElementRef graphElementRef, CommitCity city)
        {
            GraphElement graphElement = graphElementRef.Elem;
            string sourceFilename = graphElement.Filename;
            if (sourceFilename == null)
            {
                string message = $"Selected {GetName(graphElement)} has no filename.";
                ShowNotification.Error("No filename", message, log: false);
                throw new InvalidOperationException(message);
            }

            IssueWindow issueWindow = GetOrCreateCodeWindow(graphElementRef, sourceFilename);

            IVersionControl vcs = VersionControlFactory.GetVersionControl(city.VersionControlSystem, city.VCSPath.Path);
            // The path of the file relative to the root of the repository where / is used as separator.
            string relativePath = graphElement.Path();
            Change change = vcs.GetFileChange(relativePath, city.OldRevision, city.NewRevision, out string oldRelativePath);

            switch (change)
            {
                case Change.Unmodified or Change.Added or Change.TypeChanged or Change.Copied or Change.Unknown:
                    // We can show the plain file in the newer revision.
                  //  issueWindow.EnterFromText(vcs.Show(relativePath, city.NewRevision).Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None));
                    break;
                case Change.Modified or Change.Deleted or Change.Renamed:
                    // If a file was renamed, it can still have differences.
                    // We need to show a difference.
                  //  codeWindow.EnterFromText(TextualDiff.Diff(vcs.Show(oldRelativePath, city.OldRevision),
                                                      //    vcs.Show(relativePath, city.NewRevision)), true);
                    break;
                default:
                    throw new Exception($"Unexpected change type {change} for {relativePath}");
            }

            //codeWindow.Title = change switch
            //{
            //    Change.Renamed => $"<color=\"red\"><s><noparse>{oldRelativePath}</noparse></s></color>"
            //        + $" -> <color=\"green\"><u><noparse>{sourceFilename}</noparse></u></color>",
            //    Change.Deleted => $"<color=\"red\"><s><noparse>{sourceFilename}</noparse></s></color>",
            //    _ => sourceFilename
            //};

            //issueWindow.ScrolledVisibleLine = 1;
            return issueWindow;
        }

        /// <summary>
        /// Returns a CodeWindow showing the code range of the graph element most closely matching
        /// the given <paramref name="path"/> and <paramref name="range"/> in the given <paramref name="graph"/>.
        /// Will return null and show an error message if no suitable graph element is found.
        /// </summary>
        /// <param name="graph">The graph to search in</param>
        /// <param name="path">The path to search for</param>
        /// <param name="range">The range to search for</param>
        /// <param name="ContentTextEntered">Action to be executed after the CodeWindow has been filled
        /// with its content</param>
        /// <returns>new CodeWindow showing the code range of the graph element most closely matching
        /// the given <paramref name="path"/> and <paramref name="range"/></returns>
        public static IssueWindow ShowCodeForPath(Graph graph, string path, Range range = null, Action<IssueWindow> ContentTextEntered = null)
        {
            // If we just have a path as input, we need to find a fitting graph element.
            GraphElementRef element = graph.FittingElements(path, range).WithGameObject()
                                           .Select(x => x.GameObject().MustGetComponent<GraphElementRef>())
                                           .FirstOrDefault();

            if (element == null)
            {
                ShowNotification.Error("No graph element found",
                                       $"No suitable graph element found for path {path}", log: false);
                return null;
            }

            return ShowIssues(element, ContentTextEntered);
        }

        /// <summary>
        /// Returns a CodeWindow showing the code range of the given graph element
        /// retrieved from a file. The path of the file is retrieved from
        /// the absolute path as specified by the graph element's source location
        /// attributes.
        /// </summary>
        /// <param name="graphElementRef">The graph element to get the CodeWindow for</param>
        /// <param name="ContentTextEntered">Action to be executed after the CodeWindow has been filled
        /// with its content</param>
        /// <returns>new CodeWindow showing the code range of the given graph element</returns>
        public static IssueWindow ShowIssues(GraphElementRef graphElementRef, Action<IssueWindow> ContentTextEntered = null)
        {
            ShowNotification.Error("IssueWindow ShowIssues(GraphElementRef","fs",10);
            GraphElement graphElement = graphElementRef.Elem;
            IssueWindow codeWindow = GetOrCreateCodeWindow(graphElementRef, graphElement.Filename);
           // EnterWindowContent().Forget();
           EnterWindowContent().ContinueWith(() => ContentTextEntered?.Invoke(codeWindow)).Forget();


            return codeWindow;
            async UniTask EnterWindowContent()
            {
                // We have to differentiate between a file-based and a VCS-based code city.
                if (graphElement.TryGetCommitID(out string commitID))
                {
                    if (!graphElement.TryGetRepositoryPath(out string repositoryPath))
                    {
                        string message = $"Selected {GetName(graphElement)} has no repository path.";
                        ShowNotification.Error("No repository path", message, log: false);
                        throw new InvalidOperationException(message);
                    }
                    IVersionControl vcs = VersionControlFactory.GetVersionControl(VCSKind.Git, repositoryPath);
                    string[] fileContent = vcs.Show(graphElement.ID, commitID).Split("\\n", StringSplitOptions.RemoveEmptyEntries);
                   // codeWindow.EnterFromText(fileContent);
                }
                //else if (!codeWindow.ContainsText)
                //{
                //    await codeWindow.EnterFromFileAsync(GetPath(graphElement).absolutePlatformPath);
                //}

                // Pass line number to automatically scroll to it, if it exists
                //if (graphElement.SourceLine is { } line)
                //{
                //    codeWindow.ScrolledVisibleLine = line;
                //}
            }
            //async UniTask EnterWindowContent()
            //{
            //   // We have to differentiate between a file - based and a VCS - based code city.
            //    if (graphElement.TryGetCommitID(out string commitID))
            //    {
            //        if (!graphElement.TryGetRepositoryPath(out string repositoryPath))
            //        {
            //            string message = $"Selected {GetName(graphElement)} has no repository path.";
            //            ShowNotification.Error("No repository path", message, log: false);
            //            throw new InvalidOperationException(message);
            //        }
            //        IVersionControl vcs = VersionControlFactory.GetVersionControl(VCSKind.Git, repositoryPath);
            //        string[] fileContent = vcs.Show(graphElement.ID, commitID).Split("\\n", StringSplitOptions.RemoveEmptyEntries);
            //        codeWindow.EnterFromText(fileContent);
            //    }
            //    else if (!codeWindow.ContainsText)
            //    {
            //        await codeWindow.EnterFromFileAsync(GetPath(graphElement).absolutePlatformPath);
            //    }

            //    // Pass line number to automatically scroll to it, if it exists
            //    if (graphElement.SourceLine is { } line)
            //    {
            //        codeWindow.ScrolledVisibleLine = line;
            //    }
            //}
        }

        public override bool Update()
        {

            Debug.Log($"SEEInput.Select(): {SEEInput.Select()}, XRSEEActions.Selected: {XRSEEActions.Selected}");
            // Only allow local player to open new code windows
            if (spaceManager.CurrentPlayer == WindowSpaceManager.LocalPlayer
               && (SEEInput.Select() || XRSEEActions.Selected)
              && Raycasting.RaycastGraphElement(out RaycastHit _, out GraphElementRef graphElementRef) != HitGraphElement.None)
            {
                ShowNotification.Error("Show Notification Issue ShowIssueWindow.", "Notify", 10, true);
                ShowIssueWindow();
                // If nothing is selected, there's nothing more we need to do
                if (graphElementRef == null)
                {
                    ShowNotification.Error("Show Notification Issue ShowIssueaction Update false.", "Notify", 10, true);
                    return false;
                }

            }

            return false;

            void ShowIssueWindow()
            {
                // Edges of type Clone will be handled differently. For these, we will be
                // showing a unified diff.
                IssueWindow issueWindow = ShowIssues(graphElementRef);// graphElementRef is EdgeRef { Value: { Type: "Clone" } } edgeRef
                  //  ? ShowIssues(graphElementRef)
                  //  : ShowIssues(graphElementRef);
                  Debug.Log($"IssueWindow instance: {issueWindow}");
Debug.Log($"Window GameObject activeSelf: {issueWindow.Window?.activeSelf}");
Debug.Log($"Window GameObject activeInHierarchy: {issueWindow.Window?.activeInHierarchy}");
Debug.Log($"IssueWindow transform position: {issueWindow.transform.position}");
                // Add code window to our space of code window, if it isn't in there yet
                WindowSpace manager = spaceManager[WindowSpaceManager.LocalPlayer];
                if (!manager.Windows.Contains(issueWindow))
                {
                    manager.AddWindow(issueWindow);
                }
                manager.ActiveWindow = issueWindow;
                // TODO (#669): Set font size etc in settings (maybe, or maybe that's too much)
            }
        }
    }
}

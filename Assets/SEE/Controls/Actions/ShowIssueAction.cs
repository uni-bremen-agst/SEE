using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.City;
using SEE.Net.Actions;
using SEE.UI.Notification;
using SEE.UI.Window;
using SEE.UI.Window.PropertyWindow;
using SEE.UI.Window.TreeWindow;
using SEE.Utils;
using SEE.Utils.History;
using SEE.XR;
using System;
using System.Collections.Generic;
//using UnityEditor.Experimental.GraphView;
using UnityEngine;
using GraphElementRef = SEE.GO.GraphElementRef;

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

        // public void ShowIssueAction()
        //{

        //}
        public override HashSet<string> GetChangedObjects()
        {
            // Changes to the window space are handled and synced by us separately, so we won't include them here.
            return new HashSet<string>();
        }

        public override ActionStateType GetActionStateType() => ActionStateTypes.ShowIssues;

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



            //Erstellen des IssueWindows und GetIssues
            //GameObject go = new GameObject("IssueWindow");
            //IssueWindow issueWindow = go.AddComponent<IssueWindow>();

            //GameObject[] cities = GameObject.FindGameObjectsWithTag(Tags.CodeCity);
            //if (cities.Length == 0)
            //{
            //    Debug.LogWarning("No code city found. Tree view will be empty.");
            //    return;
            //}

            //TreeWindow treeWindow = goa.AddComponent<TreeWindow>();
            //// We will create a tree view for each code city.
            //foreach (GameObject cityObject in cities)
            //{
            //    if (cityObject.TryGetComponent(out AbstractSEECity city))
            //    {
            //        if (city.LoadedGraph == null) //|| treeWindows.ContainsKey(city.name)
            //        {
            //            ShowNotification.Error($"Show Notification Issue TreeWindow: TreeWindow nooo", "Notify", 10, true);

            //            continue;
            //        }
            //        if (!cityObject.TryGetComponent(out TreeWindow window))
            //        {
            //            ShowNotification.Error($"Show Notification Issue TreeWindow: TreeWindow !!!!!!", "Notify", 10, true);

            //            window = cityObject.AddComponent<TreeWindow>();
            //            window.Graph = city.LoadedGraph;
            //        }
            //       // treeWindow.Add();
            //    }
            //}

            //IssueReceiverInterface.Settings settings = new IssueReceiverInterface.Settings { preUrl = "https://ecosystem.atlassian.net/rest/api/3/search?jql=", searchUrl = "project=CACHE" };
            //JiraIssueReceiver jiraReceiver = new JiraIssueReceiver();
            //jiraReceiver.getIssues(settings);

            //IssueReceiverInterface.Settings settings = new IssueReceiverInterface.Settings { preUrl = "https://api.github.com/repos/uni-bremen-agst/SEE/issues", searchUrl = "?filter=all" };
            //GitHubIssueReceiver gitHUbReceiver = new GitHubIssueReceiver();
            //gitHUbReceiver.getIssues(settings);

            //ShowNotification.Error($"Show Notification Issue Rows: {jiraReceiver.issuesJ.Count()}.", "Notify", 10, true);
            //issueWindow.Title = "Issues"; 

            syncAction = new SyncWindowSpaceAction();
        }

        /// <summary>
        /// If the gameObject associated with graphElementRef has already a CodeWindow
        /// attached to it, that CodeWindow will be returned. Otherwise a new CodeWindow
        /// will be attached to the gameObject associated with graphElementRef and returned.
        /// The title for the newly created CodeWindow will be GetName(graphElement).
        /// </summary>
        /// <param name="graphElementRef">The graph element to get the CodeWindow for</param>
        /// <param name="filename">The filename to use for the CodeWindow title</param>
        //private static TreeWindow GetOrCreateTreeViewWindow(GraphElementRef graphElementRef, string filename)
        //{


        //    GameObject[] cities = GameObject.FindGameObjectsWithTag(Tags.CodeCity);
        //    if (cities.Length == 0)
        //    {
        //        Debug.LogWarning("No code city found. Tree view will be empty.");

        //    }

        //    //   TreeWindow treeWindow = goa.AddComponent<TreeWindow>();
        //    // We will create a tree view for each code city.
        //    foreach (GameObject cityObject in cities)
        //    {
        //        if (cityObject.TryGetComponent(out AbstractSEECity city))
        //        {
        //            //if (city.LoadedGraph == null) //|| treeWindows.ContainsKey(city.name)
        //            //{
        //            //    ShowNotification.Error($"Show Notification Issue TreeWindow: TreeWindow nooo", "Notify", 10, true);

        //            //    continue;
        //            //}
        //            if (!cityObject.TryGetComponent(out TreeWindow window))
        //            {
        //                ShowNotification.Error($"Show Notification Issue TreeWindow: TreeWindow !!!!!!", "Notify", 10, true);
        //                Graph graph = new Graph();




        //                Node node = new Node { ID = "Test", SourceName = "Test", Type = "JsonClass" };
        //                Node nodeValue = new Node() { ID = "Value", SourceName = "Value", Type = "JsonAttribute" };
        //                Edge result = new Edge(node, nodeValue, "Attribute Zuweisung");
        //                node.AddOutgoing(result);



        //                //  node.AddOutgoing(result);
        //                //  graph.AddEdge(i1, a1);
        //                // node.AddChild(new Node { ID = "Value", SourceName = "Test", Type = "JsonNode"  });
        //                graph.AddNode(node);






        //                window = cityObject.AddComponent<TreeWindow>();
        //                window.Graph = graph; //city.LoadedGraph;
        //                return window;
        //            }
        //            //    treeWindow.Add();
        //        }
        //    }

        //    // Create new window for active selection, or use existing one
        //    if (!graphElementRef.TryGetComponent(out TreeWindow treeWindow))
        //    {
        //        treeWindow = graphElementRef.gameObject.AddComponent<TreeWindow>();

        //        treeWindow.Title = "Issues TreeViewss";
        //        // If SourceName differs from Source.File (except for its file extension), display both
        //        //if (!codeWindow.Title.Replace(".", "").Equals(filename.Split('.').Reverse().Skip(1)
        //        //                                                      .Aggregate("", (acc, s) => s + acc)))
        //        //{
        //        //    codeWindow.Title += $" ({filename})";
        //        //}
        //    }
        //    return treeWindow;
        //}


        /// <summary>
        /// If the gameObject associated with graphElementRef has already a CodeWindow
        /// attached to it, that CodeWindow will be returned. Otherwise a new CodeWindow
        /// will be attached to the gameObject associated with graphElementRef and returned.
        /// The title for the newly created CodeWindow will be GetName(graphElement).
        /// </summary>
        /// <param name="graphElementRef">The graph element to get the CodeWindow for</param>
        /// <param name="filename">The filename to use for the CodeWindow title</param>
        private static IssueWindow GetOrCreateIssueWindow(GraphElementRef graphElementRef, string filename)
        {
            // Create new window for active selection, or use existing one
            if (!graphElementRef.TryGetComponent(out IssueWindow issueWindow))
            {
                issueWindow = graphElementRef.gameObject.AddComponent<IssueWindow>();

                issueWindow.Title = "Issues";
                // If SourceName differs from Source.File (except for its file extension), display both
                //if (!codeWindow.Title.Replace(".", "").Equals(filename.Split('.').Reverse().Skip(1)
                //                                                      .Aggregate("", (acc, s) => s + acc)))
                //{
                //    codeWindow.Title += $" ({filename})";
                //}
            }
            return issueWindow;
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


        private static CreateIssueWindow GetOrCreateIssueWindow(GraphElementRef graphElementRef)
        {
            // Create new window for active selection, or use existing one
            if (!graphElementRef.TryGetComponent(out CreateIssueWindow issueWindow))
            {
                issueWindow = graphElementRef.gameObject.AddComponent<CreateIssueWindow>();
          
                issueWindow.Title = "Create Issue";
                // If SourceName differs from Source.File (except for its file extension), display both
                //if (!codeWindow.Title.Replace(".", "").Equals(filename.Split('.').Reverse().Skip(1)
                //                                                      .Aggregate("", (acc, s) => s + acc)))
                //{
                //    codeWindow.Title += $" ({filename})";
                //}
            }
            return issueWindow;
        }

        public static CreateIssueWindow ShowCreateIssueWindow(GraphElementRef graphElementRef, Dictionary<String,String> attributes, SEECity city)
        {
           ShowNotification.Error("CreateIssueWindowsssss ", "fs", 10);
            //GraphElement graphElement = graphElementRef.Elem;
            CreateIssueWindow createIssueWindow = GetOrCreateIssueWindow(graphElementRef);
            createIssueWindow.Init(city.IssueProvider.createIssue, attributes,"Create Issue");
    
            // //TreeWindow treeWindow = GetOrCreateTreeViewWindow(graphElementRef, "test");
            // //EnterWindowContent().Forget();
            // //EnterWindowContent().ContinueWith(() => ContentTextEntered?.Invoke(codeWindow)).Forget();
            //// EnterWindowContent().ContinueWith(() => ContentTextEntered?.Invoke(issueWindow));
            return createIssueWindow;

            async UniTask EnterWindowContent()
            {
                // We have to differentiate between a file-based and a VCS-based code city.
                //if (graphElement.TryGetCommitID(out string commitID))
                //{
                //    if (!graphElement.TryGetRepositoryPath(out string repositoryPath))
                //    {
                //        string message = $"Selected {GetName(graphElement)} has no repository path.";
                //        ShowNotification.Error("No repository path", message, log: false);
                //        throw new InvalidOperationException(message);
                //    }
                //    IVersionControl vcs = VersionControlFactory.GetVersionControl(VCSKind.Git, repositoryPath);
                //    string[] fileContent = vcs.Show(graphElement.ID, commitID).Split("\\n", StringSplitOptions.RemoveEmptyEntries);
                //    // codeWindow.EnterFromText(fileContent);
                //}
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

        }


        /// <summary>
        /// Returns a IssueWindow showing the of the Issues given graph element
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
            ShowNotification.Error("IssueWindow ShowIssues(GraphElementRef", "fs", 10);
            //GraphElement graphElement = graphElementRef.Elem;
            IssueWindow issueWindow = GetOrCreateIssueWindow(graphElementRef, "Issue win");

           

            // //TreeWindow treeWindow = GetOrCreateTreeViewWindow(graphElementRef, "test");
            // //EnterWindowContent().Forget();
            // //EnterWindowContent().ContinueWith(() => ContentTextEntered?.Invoke(codeWindow)).Forget();
            //// EnterWindowContent().ContinueWith(() => ContentTextEntered?.Invoke(issueWindow));
            return issueWindow;

            async UniTask EnterWindowContent()
            {
                // We have to differentiate between a file-based and a VCS-based code city.
                //if (graphElement.TryGetCommitID(out string commitID))
                //{
                //    if (!graphElement.TryGetRepositoryPath(out string repositoryPath))
                //    {
                //        string message = $"Selected {GetName(graphElement)} has no repository path.";
                //        ShowNotification.Error("No repository path", message, log: false);
                //        throw new InvalidOperationException(message);
                //    }
                //    IVersionControl vcs = VersionControlFactory.GetVersionControl(VCSKind.Git, repositoryPath);
                //    string[] fileContent = vcs.Show(graphElement.ID, commitID).Split("\\n", StringSplitOptions.RemoveEmptyEntries);
                //    // codeWindow.EnterFromText(fileContent);
                //}
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

        }

        public override bool Update()
        {
          
            //Debug.Log($"SEEInput.Select(): {SEEInput.Select()}, XRSEEActions.Selected: {XRSEEActions.Selected}");
            //// Only allow local player to open new code windows
            //if (spaceManager.CurrentPlayer == WindowSpaceManager.LocalPlayer
            //   && (SEEInput.Select() || XRSEEActions.Selected)
            //  && Raycasting.RaycastGraphElement(out RaycastHit _, out GraphElementRef graphElementRef) != HitGraphElement.None)
            //{
            //    //ShowNotification.Error("Show Notification Issue ShowIssueWindow.", "Notify", 10, true);
            //    //ShowIssueWindow();
            //    // If nothing is selected, there's nothing more we need to do
            //    if (graphElementRef == null)
            //    {
            //        ShowNotification.Error("Show Notification Issue ShowIssueaction Update false.", "Notify", 10, true);
            //        return false;
            //    }

            //}

            return false;

            void ShowIssueWindow()
            {
                // Edges of type Clone will be handled differently. For these, we will be
                // showing a unified diff.
                //IssueWindow issueWindow = ShowIssues(graphElementRef);// graphElementRef is EdgeRef { Value: { Type: "Clone" } } edgeRef
                //                                                      //  ? ShowIssues(graphElementRef)
                //                                                      //  : ShowIssues(graphElementRef);
                //Debug.Log($"IssueWindow instance: {issueWindow}");
                //Debug.Log($"Window GameObject activeSelf: {issueWindow.Window?.activeSelf}");
                //Debug.Log($"Window GameObject activeInHierarchy: {issueWindow.Window?.activeInHierarchy}");
                //Debug.Log($"IssueWindow transform position: {issueWindow.transform.position}");
                //// Add code window to our space of code window, if it isn't in there yet
                //WindowSpace manager = spaceManager[WindowSpaceManager.LocalPlayer];
                //if (!manager.Windows.Contains(issueWindow))
                //{
                //    manager.AddWindow(issueWindow);
                //}
                //manager.ActiveWindow = issueWindow;


                //TreeWindow treeWindow = GetOrCreateTreeViewWindow(graphElementRef, "test");
                //manager = spaceManager[WindowSpaceManager.LocalPlayer];
                //if (!manager.Windows.Contains(issueWindow))
                //{
                //    manager.AddWindow(treeWindow);
                //}
                //manager.ActiveWindow = treeWindow;
                // TODO (#669): Set font size etc in settings (maybe, or maybe that's too much)
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using SEE.Controls;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.DataModel.DG.GraphSearch;
using SEE.UI.Notification;
using SEE.Utils;
using UnityEngine;

namespace SEE.UI.Window.TreeWindow
{
    /// <summary>
    /// A window that displays a 2D tree view of a graph.
    ///
    /// The window contains a scrollable list of expandable items.
    /// Each item represents a node in the graph.
    /// In addition to its children, the expanded form of an item also shows its connected edges.
    /// </summary>
    public partial class TreeWindow : BaseWindow, IObserver<ChangeEvent>
    {
        /// <summary>
        /// Path to the tree window content prefab.
        /// </summary>
        private const string treeWindowPrefab = "Prefabs/UI/TreeView";

        /// <summary>
        /// Path to the tree window item prefab.
        /// </summary>
        private const string treeItemPrefab = "Prefabs/UI/TreeViewItem";

        /// <summary>
        /// The graph to be displayed.
        /// Must be set before starting the window.
        /// </summary>
        public Graph Graph;

        /// <summary>
        /// Whether the window is the main tree window, i.e., the one that contains the whole graph
        /// and is opened by default.
        /// For example, a tree window showing only a certain node's references would not be the main tree window.
        /// </summary>
        public bool MainWindow = true;

        private GraphSearch searcher;

        /// <summary>
        /// Transform of the object containing the items of the tree window.
        /// </summary>
        private RectTransform items;

        /// <summary>
        /// The context menu that is displayed when the user right-clicks on an item
        /// or uses the filter or sort buttons.
        /// </summary>
        private TreeWindowContextMenu contextMenu;

        /// <summary>
        /// The grouper that is used to group the elements in the tree window.
        /// </summary>
        private TreeWindowGrouper grouper;

        /// <summary>
        /// The subscription to the graph observable.
        /// </summary>
        private IDisposable subscription;

        protected override void Start()
        {
            searcher = new GraphSearch(Graph);
            grouper = new TreeWindowGrouper(searcher.Filter, Graph);
            subscription = Graph.Subscribe(this);
            base.Start();
        }

        protected override void OnDestroy()
        {
            subscription.Dispose();
            base.OnDestroy();
        }

        /// <summary>
        /// Returns the roots for the tree view.
        /// </summary>
        /// <param name="inGroup">The group to which the roots should belong.</param>
        /// <returns>The roots for the tree view.</returns>
        /// <remarks>
        /// The roots for the tree view may differ from the roots of the graph, such as when the graph is grouped.
        /// </remarks>
        private IList<Node> GetRoots(TreeWindowGroup inGroup = null)
        {
            return WithHiddenChildren(Graph.GetRoots(), inGroup).ToList();
        }

        /// <summary>
        /// Adds the roots of the graph to the tree view.
        /// It may take up to a frame to add and reorder all items, hence this method is asynchronous.
        /// </summary>
        private async UniTask AddRootsAsync()
        {
            if (grouper.IsActive)
            {
                // Instead of the roots, we should add the categories as the first level.
                foreach (TreeWindowGroup group in grouper.AllGroups)
                {
                    if (grouper.MembersInGroup(group) > 0)
                    {
                        AddGroup(group);
                    }
                }
            }
            else
            {
                IList<Node> roots = GetRoots();
                if (roots.Count == 0)
                {
                    ShowNotification.Warn("Empty graph", "Graph has no roots. TreeView will be empty.");
                    return;
                }

                foreach (Node root in roots)
                {
                    AddNode(root);
                }
                await UniTask.Yield();
                foreach (Node root in roots)
                {
                    OrderTree(root);
                }
            }
        }

        /// <summary>
        /// Clears the tree view of all items.
        /// </summary>
        private void ClearTree()
        {
            foreach (Transform child in items)
            {
                if (child != null)
                {
                    Destroyer.Destroy(child.gameObject);
                }
            }
        }

        public override void RebuildLayout()
        {
            // Nothing needs to be done.
        }

        protected override void InitializeFromValueObject(WindowValues valueObject)
        {
            // TODO: Should tree windows be sent over the network?
            throw new NotImplementedException();
        }

        public override void UpdateFromNetworkValueObject(WindowValues valueObject)
        {
            throw new NotImplementedException();
        }

        public override WindowValues ToValueObject()
        {
            throw new NotImplementedException();
        }

        public void OnCompleted()
        {
            // Graph has been destroyed.
            Destroyer.Destroy(this);
        }

        public void OnError(Exception error)
        {
            throw error;
        }

        public void OnNext(ChangeEvent value)
        {
            // Rebuild tree when graph changes.
            switch (value)
            {
                case EdgeChange:
                case EdgeEvent:
                case GraphElementTypeEvent:
                case HierarchyEvent:
                    Rebuild();
                    break;
                case NodeEvent nodeEvent:
                    if (nodeEvent.Node.IsRoot() && nodeEvent.Change == ChangeType.Removal)
                    {
                        WindowSpace winSpace = WindowSpaceManager.ManagerInstance[WindowSpaceManager.LocalPlayer];
                        winSpace.CloseWindow(this);
                    }
                    else
                    {
                        Rebuild();
                    }
                    break;
            }
        }
    }
}

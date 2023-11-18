using System;
using System.Collections.Generic;
using SEE.DataModel;
using SEE.DataModel.DG;
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

        // TODO: In the future, distinguish by node/edge type as well for the icons.
        /// <summary>
        /// The unicode character for a node.
        /// </summary>
        private const char nodeTypeUnicode = '\uf1b2';

        /// <summary>
        /// The unicode character for an edge.
        /// </summary>
        private const char edgeTypeUnicode = '\uf542';

        /// <summary>
        /// The unicode character for outgoing edges.
        /// </summary>
        private const char outgoingEdgeUnicode = '\uf2f5';

        /// <summary>
        /// The unicode character for incoming edges.
        /// </summary>
        private const char incomingEdgeUnicode = '\uf2f6';

        /// <summary>
        /// The graph to be displayed.
        /// Must be set before starting the window.
        /// </summary>
        public Graph Graph;

        /// <summary>
        /// The search helper used to search for elements in the graph.
        /// </summary>
        private GraphSearch searcher;

        /// <summary>
        /// The context menu that is displayed when the user right-clicks on an item.
        /// </summary>
        private PopupMenu.PopupMenu ContextMenu;

        /// <summary>
        /// Transform of the object containing the items of the tree window.
        /// </summary>
        private RectTransform items;

        protected override void Start()
        {
            searcher = new GraphSearch(Graph);
            ContextMenu = gameObject.AddComponent<PopupMenu.PopupMenu>();
            Graph.Subscribe(this);
            base.Start();
        }

        /// <summary>
        /// Adds the roots of the graph to the tree view.
        /// </summary>
        private void AddRoots()
        {
            // We will traverse the graph and add each node to the tree view.
            IList<Node> roots = Graph.GetRoots();
            foreach (Node root in roots)
            {
                AddNode(root);
            }

            if (roots.Count == 0)
            {
                ShowNotification.Warn("Empty graph", "Graph has no roots. TreeView will be empty.");
            }
        }

        /// <summary>
        /// Clears the tree view of all items.
        /// </summary>
        private void ClearTree()
        {
            foreach (Transform child in items)
            {
                Destroyer.Destroy(child.gameObject);
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
                case NodeEvent:
                    ClearTree();
                    AddRoots();
                    break;
            }
        }
    }
}

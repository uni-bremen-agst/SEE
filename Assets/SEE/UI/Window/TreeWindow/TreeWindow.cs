using System;
using SEE.DataModel.DG;

namespace SEE.UI.Window.TreeWindow
{
    /// <summary>
    /// A window that displays a 2D tree view of a graph.
    ///
    /// The window contains a scrollable list of expandable items.
    /// Each item represents a node in the graph.
    /// In addition to its children, the expanded form of an item also shows its connected edges.
    /// </summary>
    public partial class TreeWindow : BaseWindow
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

        protected override void Start()
        {
            searcher = new GraphSearch(Graph);
            base.Start();
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
    }
}

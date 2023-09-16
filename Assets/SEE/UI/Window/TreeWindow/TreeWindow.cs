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
        private const string TREE_WINDOW_PREFAB = "Prefabs/UI/TreeView";

        /// <summary>
        /// Path to the tree window item prefab.
        /// </summary>
        private const string TREE_ITEM_PREFAB = "Prefabs/UI/TreeViewItem";

        // TODO: In the future, distinguish by node/edge type as well for the icons.
        /// <summary>
        /// The unicode character for a node.
        /// </summary>
        private const char NODE_TYPE_UNICODE = '\uf1b2';

        /// <summary>
        /// The unicode character for an edge.
        /// </summary>
        private const char EDGE_TYPE_UNICODE = '\uf542';

        /// <summary>
        /// The unicode character for outgoing edges.
        /// </summary>
        private const char OUTGOING_EDGE_UNICODE = '\uf2f5';

        /// <summary>
        /// The unicode character for incoming edges.
        /// </summary>
        private const char INCOMING_EDGE_UNICODE = '\uf2f6';

        /// <summary>
        /// The graph to be displayed.
        /// Must be set before starting the window.
        /// </summary>
        public Graph graph;

        public override void RebuildLayout()
        {
            // Nothing needs to be done.
        }

        protected override void InitializeFromValueObject(WindowValues valueObject)
        {
            // TODO: Should tree windows be sent over the network?
            throw new System.NotImplementedException();
        }

        public override void UpdateFromNetworkValueObject(WindowValues valueObject)
        {
            throw new System.NotImplementedException();
        }

        public override WindowValues ToValueObject()
        {
            throw new System.NotImplementedException();
        }
    }
}

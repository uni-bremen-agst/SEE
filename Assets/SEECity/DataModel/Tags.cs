namespace SEE.DataModel
{
    /// <summary>
    /// The Unity tags for the graph entities represented by the GameObjects.
    /// </summary>
    public class Tags
    {
        public const string Graph = "Graph";
        public const string Node = "Node";
        public const string Edge = "Edge";
        public const string NodePrefab = "Node Prefab";
        public const string Text = "Text";

        /// <summary>
        /// All existing tags in one.
        /// </summary>
        public static readonly string[] All = new string[] { Graph, Node, Edge, NodePrefab, Text};
    }
}

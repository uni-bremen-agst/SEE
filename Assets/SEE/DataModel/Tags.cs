namespace SEE.DataModel
{
    /// <summary>
    /// The Unity tags for the graph entities represented by the GameObjects.
    /// </summary>
    public class Tags
    {
        public const string Graph = "Graph";
        public const string Node = "Node"; // for logical graph nodes (not their visual representation such as Blocks and Buildings)
        public const string Edge = "Edge";
        public const string NodePrefab = "Node Prefab";
        public const string Text = "Text";
        public const string Erosion = "Erosion";
        public const string Decoration = "Decoration"; // Planes, trees, etc.
        public const string Path = "Path"; // for camera paths
        public const string Runtime = "Runtime";
        public const string FunctionCall = "Function Call";

        /// <summary>
        /// All existing tags in one.
        /// </summary>
        public static readonly string[] All = new string[] { Graph, Node, Edge, NodePrefab, Text, Erosion, Decoration, Path, Runtime, FunctionCall };
    }
}

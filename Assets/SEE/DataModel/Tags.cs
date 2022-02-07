namespace SEE.DataModel
{
    /// <summary>
    /// The Unity tags for the graph entities represented by the GameObjects.
    /// </summary>
    public static class Tags
    {
        /// <summary>
        /// Pseudo tag whenever a value for no tag is needed.
        /// </summary>
        public const string None = "";

        /// <summary>
        /// For objects representing a code city hosting objects tagged by Node and Edge tags.
        /// </summary>
        public const string CodeCity = "Code City";

        /// <summary>
        /// For underlying Graphs (data model for global dependency graph read from GXL).
        /// </summary>
        public const string Graph = "Graph";

        /// <summary>
        /// For game objects representing graph nodes visually.
        /// </summary>
        public const string Node = "Node";

        /// <summary>
        /// For game objects representing graph edges visually.
        /// </summary>
        public const string Edge = "Edge";

        /// <summary>
        /// For game objects representing texts, e.g., node labels.
        /// </summary>
        public const string Text = "Text";

        /// <summary>
        /// For game objects representing software erosions.
        /// </summary>
        public const string Erosion = "Erosion";

        /// <summary>
        /// For game objects representing general decorations, such as planes, furniture, etc.
        /// </summary>
        public const string Decoration = "Decoration";

        /// <summary>
        /// For objects represented recorded camera paths.
        /// </summary>
        public const string Path = "Path";

        /// <summary>
        /// For objects representing runtime information. It is used by <see cref="SEEDynCity"/>.
        /// </summary>
        public const string Runtime = "Runtime";

        /// <summary>
        /// For objects representing dynamic function calls. It is used by for the animation of
        /// dynamic call graphs.
        /// </summary>
        public const string FunctionCall = "Function Call";

        /// <summary>
        /// For a plane where code cities can be put on and be moved around with culling.
        /// </summary>
        public const string CullingPlane = "CullingPlane";

        /// <summary>
        /// For a game object having a ChartManager as a component.
        /// </summary>
        public const string ChartManager = "ChartManager";

        /// <summary>
        /// For the main camera in the scene.
        /// </summary>
        public const string MainCamera = "MainCamera";

        /// <summary>
        /// All existing tags in one.
        /// </summary>
        public static readonly string[] All = { Graph, Node, Edge, Text, Erosion, Decoration,
              Path, Runtime, FunctionCall, CullingPlane, ChartManager, MainCamera};
    }
}

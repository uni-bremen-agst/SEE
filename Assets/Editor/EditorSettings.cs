
namespace SEEEditor
{
    /// <summary>
    /// Represents the parameters needed to create the visualization at design time.
    /// </summary>
    class EditorSettings : SEE.IGraphSettings
    {
        /// <summary>
        /// The tag of all nodes.
        /// </summary>
        public string nodeTag = "Node";

        /// <summary>
        /// The tag of all edges.
        /// </summary>
        public string edgeTag = "Edge";

        /// <summary>
        /// The relative path to the building preftab.
        /// </summary>
        public string nodePrefabPath = "Assets/Prefabs/House.prefab";

        /// <summary>
        /// The relative path to the connection preftab.
        /// </summary>
        public string edgePreftabPath = "Assets/Prefabs/Line.prefab";
    }
}

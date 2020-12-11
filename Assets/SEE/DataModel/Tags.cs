using UnityEngine;

namespace SEE.DataModel
{
    /// <summary>
    /// The Unity tags for the graph entities represented by the GameObjects.
    /// </summary>
    public class Tags
    {
        public const string None = ""; // pseudo tag whenever a value for no tag is needed

        public const string CodeCity = "Code City"; // for objects representing a code city hosting objects tagged by Node and Edge tags.

        // graph concepts
        public const string Graph = "Graph";
        public const string Node = "Node"; // for logical graph nodes (not their visual representation such as Blocks and Buildings)
        public const string Edge = "Edge";

        public const string NodePrefab = "Node Prefab";
        public const string Text = "Text";
        public const string Erosion = "Erosion";
        public const string Decoration = "Decoration"; // Planes, trees, etc.
        public const string Path = "Path"; // for camera paths

        // for visualization of dynamic execution
        public const string Runtime = "Runtime";
        public const string FunctionCall = "Function Call";
        public const string JLGVisualization = "JLG Visualization";

        // for culling
        public const string CullingPlane = "CullingPlane"; // for a plane where code cities can be put on and be moved around with culling

        // metric charts
        public const string ChartManager = "ChartManager";  // Tag of a game object having a ChartManager as a component.
        public const string ChartContainer = "ChartContainer";
        public const string Chart = "Chart"; // for a metric chart

        // for game objects representing a UI element
        public const string UI = "UI";
        
        // for the main camera in the scene
        public const string MainCamera = "MainCamera";

        /// <summary>
        /// All existing tags in one.
        /// </summary>
        public static readonly string[] All = new string[]
            { Graph, Node, Edge, NodePrefab, Text, Erosion, Decoration,
              Path, Runtime, FunctionCall, CullingPlane, ChartManager, ChartContainer, Chart,
              JLGVisualization, UI, MainCamera};

        /// <summary>
        /// Returns any descendant (transitive children of <paramref name="gameObject"/> including
        /// <paramref name="gameObject"/> itself) of given <paramref name="gameObject"/> that
        /// has the given <paramref name="tag"/>. If none could be found, null is returned. 
        /// </summary>
        /// <param name="gameObject">root of the game-object hierarchy to be searched in</param>
        /// <param name="tag">the tag the searched element must have</param>
        /// <returns>descendant tagged by <paramref name="tag"/> or null</returns>
        public static GameObject FindChildWithTag(GameObject gameObject, string tag)
        {
            if (gameObject == null)
            {
                return null;
            }

            if (gameObject.CompareTag(tag))
            {
                return gameObject;
            }

            foreach (Transform child in gameObject.transform)
            {
                GameObject result = FindChildWithTag(child.gameObject, tag);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }
    }
}

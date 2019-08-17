using UnityEngine;
using UnityEditor;

using SEE;

namespace SEEEditor
{
    /// <summary>
    /// Creates the visualization at design time and a runtime entity that represent
    /// the nodes and edges in the scene at runtime.
    /// </summary>
    class SceneGraphCreator
    {
        // The scene graph created by this CityEditor.
        private static SceneGraph sceneGraph = null;

        // The visualization parameters to be used for creating the visualization.
        public static EditorSettings settings = new EditorSettings();
        
        /// <summary>
        /// Returns the scene graph if it exists. Will return null if it does not exist.
        /// </summary>
        /// <returns>the scene graph or null</returns>
        private static SceneGraph GetSceneGraph()
        {
            if (sceneGraph == null)
            {
                sceneGraph = SEEEditor.SceneGraph.GetInstance();
            }
            return sceneGraph;
        }

        /// <summary>
        /// Deletes all nodes and edges in the scene.
        /// </summary>
        internal static void Delete()
        {
            sceneGraph = GetSceneGraph();
            sceneGraph.Delete();
        }

        /// <summary>
        /// Loads a graph from disk and creates the scene objects representing it.
        /// </summary>
        internal static void Load()
        {
            SEEEditor.SceneGraph sgraph = GetSceneGraph();
            if (sgraph != null)
            {
                Debug.Log("Loading graph from " + settings.graphPath + "\n");
                sgraph.Load(settings);
            }
            else
            {
                Debug.LogError("There is no scene graph.\n");
            }
        }

        /// <summary>
        /// Loads the graph from the given file and creates the GameObjects representing
        /// its nodes and edges. Sets the graphPath attribute.
        /// </summary>
        /// <param name="filename"></param>
        internal void LoadAndDraw(SEEEditor.EditorSettings settings)
        {
            SEEEditor.SceneGraph sgraph = GetSceneGraph();
            sgraph.Load(settings);
            {
                Performance p = Performance.Begin("drawing graph from file");
                sgraph.Draw();
                p.End();
            }
        }

    }
}


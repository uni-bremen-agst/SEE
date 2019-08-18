using System.Collections.Generic;
using UnityEngine;

using SEE.DataModel;

namespace SEE
{
    /// <summary>
    /// A container for all graphs shown in the scene.
    /// </summary>
    public class SceneGraphs
    {
        private static Dictionary<string, ISceneGraph> graphs = new Dictionary<string, ISceneGraph>();

        public static ISceneGraph Add(IGraphSettings settings)
        {
            ISceneGraph graph = null;
            if (string.IsNullOrEmpty(settings.graphPath))
            {
                Debug.LogError("No graph path given.\n");
            }
            else if (!graphs.ContainsKey(settings.graphPath))
            {
                graph = Load(settings);
                if (graph == null)
                {
                    Debug.LogError("graph " + settings.graphPath + " could not be loaded.");
                }
                else
                {
                    graphs.Add(settings.graphPath, graph);
                }
            }
            else
            {
                Debug.LogError("graph " + settings.graphPath + " is already loaded.");
            }
            return graph;
        }

        private static ISceneGraph Load(IGraphSettings settings)
        {
            HashSet<string> hierarchicalEdges = new HashSet<string>
                {
                    settings.hierarchicalEdgeType
                };
            GraphCreator graphCreator = new GraphCreator(settings.graphPath, hierarchicalEdges, new SEELogger());
            SEE.Performance p = SEE.Performance.Begin("loading graph data");
            graphCreator.Load();
            ISceneGraph graph = graphCreator.GetGraph();
            p.End();
            Debug.Log("Number of nodes loaded: " + graph.NodeCount + "\n");
            Debug.Log("Number of edges loaded: " + graph.EdgeCount + "\n");
            return graph;
        }

        private static void Delete(ISceneGraph graph)
        {
            graph.Destroy();
        }

        public static void Delete(string path)
        {
            if (graphs.TryGetValue(path, out ISceneGraph graph))
            {
                // first remove graph from the dictionary
                graphs.Remove(path);
                // only now delete the graph
                Delete(graph);
            }
        }

        public static void DeleteAll()
        {
            foreach (ISceneGraph graph in graphs.Values)
            {
                Delete(graph);
            }
            graphs.Clear();
        }
    }
}

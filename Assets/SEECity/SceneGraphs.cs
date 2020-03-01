using System.Collections.Generic;
using UnityEngine;

using SEE.DataModel;

namespace SEE
{
    /// <summary>
    /// A container for all graphs shown in the scene. Exists only in editor mode,
    /// not during the game.
    /// </summary>
    public class SceneGraphs
    {
        private static Dictionary<string, Graph> graphs = new Dictionary<string, Graph>();

        public static Graph Add(SEECity settings)
        {
            Graph graph = null;
            if (string.IsNullOrEmpty(settings.GXLPath()))
            {
                Debug.LogError("No graph path given.\n");
            }
            else 
            {
                graph = Load(settings);
                if (ReferenceEquals(graph, null))
                {
                    Debug.LogErrorFormat("graph {0} could not be loaded.\n", settings.GXLPath());
                }
                else
                {
                    if (graphs.ContainsKey(settings.GXLPath()))
                    {
                        Debug.LogWarningFormat("graph {0} is already loaded and will be overridden.\n", settings.GXLPath());
                    }
                    graphs[settings.GXLPath()] = graph;
                }
            }
            return graph;
        }

        private static Graph Load(SEECity settings)
        {
            // GraphCreator graphCreator = new GraphCreator(settings.GXLPath(), settings.HierarchicalEdges, new SEELogger());
            GraphReader graphCreator = new GraphReader(settings.GXLPath(), settings.HierarchicalEdges, new SEELogger());
            if (string.IsNullOrEmpty(settings.GXLPath()))
            {
                Debug.LogError("Empty graph path.\n");
                return null;
            }
            else
            {
                SEE.Performance p = SEE.Performance.Begin("loading graph data from " + settings.GXLPath());
                graphCreator.Load();
                Graph graph = graphCreator.GetGraph();
                graph.CalculateLevels();
                p.End();
                Debug.Log("Number of nodes loaded: " + graph.NodeCount + "\n");
                Debug.Log("Number of edges loaded: " + graph.EdgeCount + "\n");
                return graph;
            }
        }

        private static void Delete(Graph graph)
        {
            graph.Destroy();
        }

        public static void Delete(string path)
        {
            if (graphs.TryGetValue(path, out Graph graph))
            {
                // first remove graph from the dictionary
                graphs.Remove(path);
                // only now delete the graph
                Delete(graph);
            }
        }

        public static void DeleteAll()
        {
            foreach (Graph graph in graphs.Values)
            {
                Delete(graph);
            }
            graphs.Clear();
        }
    }
}

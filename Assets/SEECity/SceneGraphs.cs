using System.Collections.Generic;
using UnityEngine;

using SEE.DataModel;
using static SEE.GraphSettings;
using System.Linq;

namespace SEE
{
    /// <summary>
    /// A container for all graphs shown in the scene. Exists only in editor mode,
    /// not during the game.
    /// </summary>
    public class SceneGraphs
    {
        private static Dictionary<string, Graph> graphs = new Dictionary<string, Graph>();

        public static Graph Add(GraphSettings settings)
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

        private static Graph Load(GraphSettings settings)
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

                // die "veralteten" dirs, vor dem neuen laden
                Dictionary<string, bool> dirs = settings.CoseGraphSettings.ListDirToggle;
                // die neuen dirs 
                Dictionary<string, bool> dirsLocal = new Dictionary<string, bool>();
                List<Node> dirsNodes = graph.GetRoots();

                Dictionary<string, NodeLayouts> dirsLayout = new Dictionary<string, NodeLayouts>();

                foreach (Node node in graph.Nodes())
                {
                    if (!node.IsLeaf())
                    {
                        dirsLocal.Add(node.LinkName, false);
                        dirsLayout.Add(node.LinkName, settings.NodeLayout);
                    }
                }

                // falls der key nicht in den alten dictonary ist
                dirsLocal = dirsLocal.Where(i => !dirs.ContainsKey(i.Key)).ToDictionary(i => i.Key, i => i.Value);

                settings.CoseGraphSettings.show = new Dictionary<Node, bool>();
                if (dirsLocal.Count != 0)
                {
                    settings.CoseGraphSettings.DirNodeLayout = dirsLayout;
                    settings.CoseGraphSettings.ListDirToggle = dirsLocal;
                    // get roots
                    settings.CoseGraphSettings.dirs = dirsNodes;
                }

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

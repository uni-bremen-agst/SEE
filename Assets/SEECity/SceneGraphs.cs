using System.Collections.Generic;
using UnityEngine;

using SEE.DataModel;
using System.IO;
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
        
        /// <summary>
        /// A list of names for all loaded animated graphs. The name equals the path of the gxl file.
        /// </summary>
        private static List<string> animatedGraphNames = new List<string>();

        /// <summary>
        /// The index of the last returned animated graph by getNextAnimatedGraph or getPreviousAnimatedGraph
        /// </summary>
        private static int openAnimatedGraphIndex = 0;

        public static Graph Add(GraphSettings settings)
        {
            Graph graph = null;
            if (string.IsNullOrEmpty(settings.GXLPath()))
            {
                Debug.LogError("No graph path given.\n");
            }
            else if (!graphs.ContainsKey(settings.GXLPath()))
            {
                graph = Load(settings);
                if (graph == null)
                {
                    Debug.LogError("graph " + settings.GXLPath() + " could not be loaded.");
                }
                else
                {
                    graphs.Add(settings.GXLPath(), graph);
                }
            }
            else
            {
                Debug.LogError("graph " + settings.GXLPath() + " is already loaded.");
            }
            return graph;
        }

        /// <summary>
        /// Loads all graphs located in the directory defined by GraphSettings.AnimatedPath()
        /// </summary>
        /// <param name="settings"></param>
        /// <returns>The first graph loaded</returns>
        public static Graph AddAnimated(GraphSettings settings)
        {
            if(animatedGraphNames.Count != 0)
            {
                Debug.LogError("Animated Graph is already loaded, please clear scene.");

                return null;
            }
            SEE.Performance p = SEE.Performance.Begin("loading animated graph data from " + settings.AnimatedPath());
            var settingsOldGxlPath = settings.gxlPath;
            var newGraphName = Directory
                .GetFiles(settings.AnimatedPath(), "*.gxl", SearchOption.TopDirectoryOnly)
                .NumericalSort();
                //.OrderBy(n => n);
            // TODO flo: sort by name and number
            animatedGraphNames.AddRange(newGraphName);
            foreach (string gxlPath in animatedGraphNames)
            {
                settings.gxlPath = gxlPath.Replace(settings.pathPrefix, "");
                Add(settings);
            }
            settings.gxlPath = settingsOldGxlPath;
            p.End();
            Debug.Log("Number of graphs loaded: " + animatedGraphNames.Count + "\n");
            Debug.Log("First graph loaded: " + animatedGraphNames.First() + "\n");
            Debug.Log("Last graph loaded: " + animatedGraphNames.Last() + "\n");


            return graphs[animatedGraphNames[0]];
        }

        /// <summary>
        /// Gets the next Graph in list, loaded from the GraphSettings.animatedPath() with AddAnimated.
        /// If there is no next Graph, it will return the last. If there is no loaded Graph it returns null.
        /// </summary>
        /// <returns>A Graph</returns>
        public static Graph getNextAnimatedGraph()
        {
            if(animatedGraphNames.Count == 0)
            {
                Debug.LogError("There are no loaded animated graphs.");
                return null;
            }
            openAnimatedGraphIndex++;
            if (openAnimatedGraphIndex >= animatedGraphNames.Count)
            {
                Debug.LogError("The last animated graph is already open.");
                openAnimatedGraphIndex = animatedGraphNames.Count - 1;
            }
            Debug.Log("Got animated graph " + animatedGraphNames[openAnimatedGraphIndex]);
            return graphs[animatedGraphNames[openAnimatedGraphIndex]];
        }

        /// <summary>
        /// Gets the previous graph in list, loaded from the GraphSettings.animatedPath() with AddAnimated.
        /// If there is no previous Graph, it will return the last. If there is no loaded Graph it returns null.
        /// </summary>
        /// <returns>A Graph</returns>
        public static Graph getPreviousAnimatedGraph()
        {
            if (animatedGraphNames.Count == 0)
            {
                Debug.LogError("There are no loaded animated graphs.");
                return null;
            }
            openAnimatedGraphIndex--;
            if (openAnimatedGraphIndex < 0 || openAnimatedGraphIndex >= animatedGraphNames.Count)
            {
                Debug.LogError("The first animated graph is already open.");
                openAnimatedGraphIndex = 0;
            }
            Debug.Log("Got animated graph " + animatedGraphNames[openAnimatedGraphIndex]);
            return graphs[animatedGraphNames[openAnimatedGraphIndex]];
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
                p.End();
                Debug.Log("Number of nodes loaded: " + graph.NodeCount + "\n");
                Debug.Log("Number of edges loaded: " + graph.EdgeCount + "\n");
                return graph;
            }
        }

        private static void Delete(Graph graph)
        {
            graph.Destroy();
            openAnimatedGraphIndex = 0;
        }

        public static void Delete(string path)
        {
            if (graphs.TryGetValue(path, out Graph graph))
            {
                // first remove the graph from animated graphs name list
                animatedGraphNames.Remove(path);
                // then remove graph from the dictionary
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
            animatedGraphNames.Clear();
            openAnimatedGraphIndex = 0;
        }
    }
}

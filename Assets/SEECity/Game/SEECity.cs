using SEE.DataModel;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using SEE.Tools;

namespace SEE.Game
{
    /// <summary>
    /// Manages settings of the graph data showing a single version of a software
    /// system needed at runtime.
    /// </summary>
    public class SEECity : AbstractSEECity
    {
        /// <summary>
        /// The graph that is visualized in the scene and whose visualization settings are 
        /// managed here.
        /// </summary>
        protected Graph graph = null;

        /// <summary>
        /// The graph underlying this SEE city. May be null.
        /// the element is currently not in a graph.
        /// </summary>
        public Graph ItsGraph
        {
            get => graph;
            set => graph = value;
        }

        /// Clone graph with one directory and two files contained therein.
        //public string gxlPath = "..\\Data\\GXL\\two_files.gxl";
        //public string csvPath = "..\\Data\\GXL\\two_files.csv";

        /// Clone graph with one directory and three files contained therein.
        //public string gxlPath = "..\\Data\\GXL\\three_files.gxl";
        //public string csvPath = "..\\Data\\GXL\\three_files.csv";

        /// Very tiny clone graph with single root, one child as a leaf and 
        /// two more children with two children each to experiment with.
        //public string gxlPath = "..\\Data\\GXL\\micro_clones.gxl";
        //public string csvPath = "..\\Data\\GXL\\micro_clones.csv";

        /// Tiny clone graph with single root to experiment with.
        //public string gxlPath = "..\\Data\\GXL\\minimal_clones.gxl";
        //public string csvPath = "..\\Data\\GXL\\minimal_clones.csv";

        /// Tiny clone graph with single roots to check edge bundling.
        //public string gxlPath = "..\\Data\\GXL\\controlPoints.gxl";
        //public string csvPath = "..\\Data\\GXL\\controlPoints.csv";

        // Smaller clone graph with single root (Linux directory "fs").
        //public string gxlPath = "..\\Data\\GXL\\linux-clones\\fs.gxl";
        //public string csvPath = "..\\Data\\GXL\\linux-clones\\fs.csv";

        // Smaller clone graph with single root (Linux directory "net").

        /// <summary>
        /// The relative path for the GXL file containing the graph data.
        /// </summary>
        public string gxlPath = "..\\Data\\GXL\\linux-clones\\net.gxl";
        /// <summary>
        /// The relative path for the CSV file containing the node metrics.
        /// </summary>
        public string csvPath = "..\\Data\\GXL\\linux-clones\\net.csv";

        // Larger clone graph with single root (Linux directory "drivers"): 16.920 nodes, 10583 edges.
        //public string gxlPath = "..\\Data\\GXL\\linux-clones\\drivers.gxl";
        //public string csvPath = "..\\Data\\GXL\\linux-clones\\drivers.csv";

        // Medium size include graph with single root (OpenSSL).
        //public string gxlPath = "..\\Data\\GXL\\OpenSSL\\openssl-include.gxl";
        //public string csvPath = "..\\Data\\GXL\\OpenSSL\\openssl-include.csv";

        /// <summary>
        /// Returns the concatenation of pathPrefix and gxlPath. That is the complete
        /// absolute path to the GXL file containing the graph data.
        /// </summary>
        /// <returns>concatenation of pathPrefix and gxlPath</returns>
        public string GXLPath()
        {
            return PathPrefix + gxlPath;
        }

        /// <summary>
        /// Returns the concatenation of pathPrefix and csvPath. That is the complete
        /// absolute path to the CSV file containing the additional metric values.
        /// </summary>
        /// <returns>concatenation of pathPrefix and csvPath</returns>
        public string CSVPath()
        {
            return PathPrefix + csvPath;
        }

        /// <summary>
        /// Loads the metrics from CSVPath() and aggregates and adds them to the graph.
        /// Precondition: graph must have been loaded before.
        /// </summary>
        private void LoadMetrics()
        {
            int numberOfErrors = MetricImporter.Load(ItsGraph, CSVPath());
            if (numberOfErrors > 0)
            {
                Debug.LogErrorFormat("CSV file {0} has {1} many errors.\n", CSVPath(), numberOfErrors);
            }
            {
                MetricAggregator.AggregateSum(ItsGraph, AllLeafIssues().ToArray<string>());
                // Note: We do not want to compute the derived metric editorSettings.InnerDonutMetric
                // when we have a single root node in the graph. This metric will be used to define the color
                // of inner circles of Donut charts. Because the color is a linear interpolation of the whole
                // metric value range, the inner circle would always have the maximal value (it is the total
                // sum over all) and hence the maximal color gradient. The color of the other nodes would be
                // hardly distinguishable. 
                // FIXME: We need a better solution. This is a kind of hack.
                MetricAggregator.DeriveSum(ItsGraph, AllInnerNodeIssues().ToArray<string>(), InnerDonutMetric, true);
            }
        }

        /// <summary>
        /// Loads the graph data from the GXL file with GXLPath() and the metrics
        /// from the CSV file with CSVPath().
        /// </summary>
        public virtual void LoadData()
        {
            if (string.IsNullOrEmpty(GXLPath()))
            {
                Debug.LogError("Empty graph path.\n");
            }
            else
            {
                graph = LoadGraph(GXLPath());
                LoadMetrics();
                DrawGraph();
            }
        }

        /// <summary>
        /// Draws the graph.
        /// Precondition: The graph and its metrics have been loaded.
        /// </summary>
        protected void DrawGraph()
        {
            if (ReferenceEquals(ItsGraph, null))
            {
                Debug.LogError("No graph loaded.\n");
            }
            else
            {
                GraphRenderer renderer = new GraphRenderer(this);
                // We assume here that this SEECity instance was added to a game object as
                // a component. The inherited attribute gameObject identifies this game object.
                renderer.Draw(ItsGraph, gameObject);
                // If CScape buildings are used, the scale of the world is larger and, hence, the camera needs to move faster.
                // We may have cities with blocks and cities with CScape buildings in the same scene.
                // We cannot simply alternate the speed each time when a graph is loaded.
                // Cameras.AdjustCameraSpeed(renderer.Unit());
            }
        }

        /// <summary>
        /// Destroys the underlying graph and all game objects visualizing information about it.
        /// </summary>
        public void DeleteGraph()
        {
            // Delete all children.
            // Note: foreach (GameObject child in transform)... would not work;
            // we really need to collect all children first and only then can destroy each.
            foreach (GameObject child in AllChildren())
            {
                Destroyer.DestroyGameObject(child);
            }
            // Delete the underlying graph.
            if (graph != null)
            {
                graph.Destroy();
            }
            graph = null;
        }

        /// <summary>
        /// Returns all immediate children of the game object this SEECity is attached to.
        /// </summary>
        /// <returns>immediate children of the game object this SEECity is attached to</returns>
        private List<GameObject> AllChildren()
        {
            List<GameObject> result = new List<GameObject>();
            foreach (Transform child in transform)
            {
                result.Add(child.gameObject);
            }
            return result;
        }
    }
}

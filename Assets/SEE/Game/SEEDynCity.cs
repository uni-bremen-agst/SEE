using SEE.DataModel;
using SEE.DataModel.Runtime.IO;
using SEE.Utils;
using System.IO;
using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    /// Configuration of a code city for the visualization of dynamic data in
    /// traced at the level of method calls.
    /// </summary>
    public class SEEDynCity : SEECity
    {
        /// IMPORTANT NOTE: If you add any attribute that should be persisted in a
        /// configuration file, make sure you save and restore it in 
        /// <see cref="SEEDynCity.Save(ConfigWriter)"/> and 
        /// <see cref="SEEDynCity.Restore(Dictionary{string, object})"/>, 
        /// respectively. You should also extend the test cases in TestConfigIO.

        /// <summary>
        /// The path to the DYN file containing the trace data.
        /// </summary>
        /// <returns>path of DYN file</returns>
        public DataPath DYNPath = new DataPath();

        /// <summary>
        /// Loads the graph data from the GXL file with GXLPath() and the metrics
        /// from the CSV file with CSVPath(). Afterwards, DrawGraph() can be used
        /// to actually render the graph data.
        /// </summary>
        public override void LoadData()
        {
            base.LoadData();
            LoadDYN();
        }

        /// <summary>
        /// Loads the dynamic data from the DYN file with DYNPath() and adds them to the graph.
        /// Precondition: graph must have been loaded before.
        /// </summary>
        private void LoadDYN()
        {
            string filename = DYNPath.Path;
            if (string.IsNullOrEmpty(filename))
            {
                Debug.LogError("Empty path for dynamic trace file.\n");
            }
            else if (!File.Exists(filename))
            {
                Debug.LogErrorFormat("Dynamic trace file {0} does not exist.\n", filename);
            }
            else
            {
                CallTreeReader callTreeReader = new CallTreeReader(filename, new SEELogger());
                callTreeReader.Load();

                GameObject runtimeGO = new GameObject();
                runtimeGO.transform.parent = transform;
                runtimeGO.name = Tags.Runtime;
                runtimeGO.tag = Tags.Runtime;
                runtimeGO.AddComponent<Runtime.Runtime>().callTree = callTreeReader.CallTree;
            }
        }
    }
}
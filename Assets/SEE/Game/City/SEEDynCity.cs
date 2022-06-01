using System.Collections.Generic;
using System.IO;
using SEE.DataModel;
using SEE.DataModel.Runtime.IO;
using SEE.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SEE.Game.City
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
        /// <see cref="SEEDynCity.Restore(Dictionary{string,object})"/>,
        /// respectively. You should also extend the test cases in TestConfigIO.

        /// <summary>
        /// The path to the DYN file containing the trace data.
        /// </summary>
        /// <returns>path of DYN file</returns>
        [SerializeField, ShowInInspector, Tooltip("Path of DYN file"), FoldoutGroup(DataFoldoutGroup)]
        public FilePath DYNPath = new FilePath();

        /// <summary>
        /// Loads the graph data from the GXL file with GXLPath() and the metrics
        /// from the CSV file with CSVPath(). Afterwards, DrawGraph() can be used
        /// to actually render the graph data.
        /// </summary>
        [Button(ButtonSizes.Small)]
        [HorizontalGroup(DataButtonsGroup)]
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

                // There might already be a child with a Runtime component. If that is the
                // case, we will re-use it. Otherwise we will create a new child with a
                // fresh Runtime component.
                Runtime.Runtime runTime = gameObject.GetComponentInChildren<Runtime.Runtime>();
                if (runTime == null)
                {
                    GameObject runtimeGO = new GameObject
                    {
                        transform = { parent = transform },
                        name = Tags.Runtime,
                        tag = Tags.Runtime
                    };
                    runTime = runtimeGO.AddComponent<Runtime.Runtime>();
                }
                runTime.callTree = callTreeReader.CallTree;
            }
        }

        /// <summary>
        /// In addition to the behavior of Reset() in the superclass, all children
        /// tagged by Tags.Runtime will be destroyed.
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            foreach (Transform child in transform)
            {
                if (child.CompareTag(Tags.Runtime))
                {
                    Destroyer.DestroyGameObject(child.gameObject);
                }
            }
        }

        //----------------------------------------------------------------------------
        // Input/output of configuration attributes
        //----------------------------------------------------------------------------

        // The labels for the configuration attributes in the configuration file.
        private const string DYNPathLabel = "DYNPath";

        /// <summary>
        /// <see cref="City.AbstractSEECity.Save(ConfigWriter)"/>
        /// </summary>
        protected override void Save(ConfigWriter writer)
        {
            base.Save(writer);
            DYNPath.Save(writer, DYNPathLabel);
        }

        /// <summary>
        /// <see cref="City.AbstractSEECity.Restore(Dictionary{string, object})"/>.
        /// </summary>
        protected override void Restore(Dictionary<string, object> attributes)
        {
            base.Restore(attributes);
            DYNPath.Restore(attributes, DYNPathLabel);
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.DataModel.DG.IO;
using SEE.GO;
using SEE.Utils;
using SEE.Tools.ReflexionAnalysis;
using static SEE.Tools.ReflexionAnalysis.ReflexionGraphTools;
using UnityEngine;
using Sirenix.OdinInspector;

namespace SEE.Game.City
{
    /// <summary>
    /// A code city that supports architectural mappings from
    /// implementation nodes onto architecture nodes.
    /// NOTE: It is assumed the implementation and architecture graphs are not edited!
    /// TODO: We should allow changes, but trigger the respective incremental reflexion analysis methods.
    /// </summary>
    public class SEEReflexionCity : SEECity
    {
        /// <summary>
        /// The path to the GXL file containing the architecture graph data.
        /// </summary>
        [SerializeField, ShowInInspector, Tooltip("Path of GXL file for the architecture"), FoldoutGroup(DataFoldoutGroup)]
        public FilePath GxlArchitecturePath = new FilePath();

        /// <summary>
        /// The path to the GXL file containing the mapping graph data.
        /// </summary>
        [SerializeField, ShowInInspector, Tooltip("Path of GXL file for the mapping from the implementation onto the architecture"), FoldoutGroup(DataFoldoutGroup)]
        public FilePath GxlMappingPath = new FilePath();

        /// <summary>
        /// The path to the CSV file containing the architecture metric data.
        /// </summary>
        [SerializeField, ShowInInspector, Tooltip("Path of CSV file for the metrics of the architecture"), FoldoutGroup(DataFoldoutGroup)]
        public FilePath CsvArchitecturePath = new FilePath();

        /// <summary>
        /// Name of this code city.
        /// </summary>
        public string CityName = "Reflexion Analysis";

        /// <summary>
        /// Reflexion analysis graph. Note that this simply casts <see cref="LoadedGraph"/>,
        /// to make it easier to call reflexion-specific methods.
        /// May be <c>null</c> if the graph has not yet been loaded.
        /// </summary>
        public ReflexionGraph ReflexionGraph => LoadedGraph as ReflexionGraph;

        /// <summary>
        /// Root node of the implementation subgraph.
        /// </summary>
        public Node ImplementationRoot => ReflexionGraph.ArchitectureRoot;

        /// <summary>
        /// Root node of the architecture subgraph.
        /// </summary>
        public Node ArchitectureRoot => ReflexionGraph.ArchitectureRoot;

        /// <summary>
        /// The <see cref="ReflexionVisualization"/> responsible for handling reflexion analysis changes.
        /// </summary>
        private ReflexionVisualization Visualization;

        /// <summary>
        /// First, if a graph was already loaded, everything will be reset by calling <see cref="Reset"/>.
        /// Second, the graph data from the three GXL files are loaded. The loaded graph is available
        /// in <see cref="LoadedGraph"/> afterwards.
        ///
        /// This method loads only the data, but does not actually render the graph.
        /// </summary>
        [Button(ButtonSizes.Small)]
        [ButtonGroup(DataButtonsGroup)]
        [PropertyOrder(DataButtonsGroupOrderLoad)]
        public override void LoadData()
        {
            if (string.IsNullOrEmpty(GxlArchitecturePath.Path))
            {
                Debug.LogError("Architecture graph path is empty.\n");
            }
            else if (string.IsNullOrEmpty(GXLPath.RootPath))
            {
                Debug.LogError("Implementation graph path is empty.\n");
            }
            else
            {
                if (LoadedGraph != null)
                {
                    Reset();
                }

                LoadAllGraphs().Forget(); // needs to be async call due to metric retrieval
            }

            #region Local Functions

            async UniTaskVoid LoadAllGraphs()
            {
                Graph ArchitectureGraph = LoadGraph(GxlArchitecturePath.Path, "");
                Graph ImplementationGraph = LoadGraph(GXLPath.Path, "");
                Graph MappingGraph;
                if (string.IsNullOrEmpty(GxlMappingPath.Path))
                {
                    Debug.LogWarning("Mapping graph path is empty. Will create new mapping from scratch.\n");
                    /// The mapping graph may contain nodes and edges from the implementation. Possibly, their
                    /// <see cref="GraphElement.AbsolutePlatformPath()"/> will be retrieved. That is why we
                    /// will set the base path to <see cref="ProjectPath.Path"/>.
                    MappingGraph = new Graph(SourceCodeDirectory.Path);
                }
                else
                {
                    MappingGraph = LoadGraph(GxlMappingPath.Path, "");
                }

                // We collect the tasks here so we can wait on them both at the same time instead of sequentially
                IList<UniTask> tasks = new List<UniTask>();
                if (!string.IsNullOrEmpty(CsvArchitecturePath.Path))
                {
                    tasks.Add(LoadGraphMetrics(ArchitectureGraph, CsvArchitecturePath.Path, ErosionSettings));
                }

                if (!string.IsNullOrEmpty(CsvArchitecturePath.Path))
                {
                    tasks.Add(LoadGraphMetrics(ArchitectureGraph, CsvArchitecturePath.Path, ErosionSettings));
                }

                await UniTask.WhenAll(tasks);

                ReflexionGraph reflexionGraph = new ReflexionGraph(ImplementationGraph, ArchitectureGraph, MappingGraph, CityName);
                LoadedGraph = reflexionGraph;
                Debug.Log($"Loaded graph {LoadedGraph.Name}.\n");
                Visualization = gameObject.AddOrGetComponent<ReflexionVisualization>();
                Visualization.StartFromScratch(reflexionGraph);
                Debug.Log("Initialized Reflexion Analysis.\n");
            }

            #endregion
        }

        /// <summary>
        /// Draws the graph.
        /// Precondition: The graph and its metrics have been loaded.
        /// </summary>
        [Button(ButtonSizes.Small, Name = "Draw Data")]
        [ButtonGroup(DataButtonsGroup)]
        [PropertyOrder(DataButtonsGroupOrderDraw)]
        public override void DrawGraph()
        {
            base.DrawGraph();
        }

        /// <summary>
        /// Saves implementation, architecture, and mapping graphs as GXL.
        /// </summary>
        [Button(ButtonSizes.Small)]
        [ButtonGroup(DataButtonsGroup)]
        [PropertyOrder(DataButtonsGroupOrderSave)]
        public override void SaveData()
        {
            IList<string> NoPathGraphs = new[]
            {
                GxlArchitecturePath.Path, GXLPath.Path, GxlMappingPath.Path
            }.Where(string.IsNullOrEmpty).ToList();
            if (NoPathGraphs.Count > 0)
            {
                Debug.LogError($"Couldn't find any graph at path{(NoPathGraphs.Count > 1 ? "s" : "")} " +
                               string.Join(", ", NoPathGraphs) + ".\n");
            }
            else
            {
                string hierarchicalType = HierarchicalEdges.First();
                (Graph implementation, Graph architecture, Graph mapping) = ReflexionGraph.Disassemble();
                GraphWriter.Save(GxlArchitecturePath.Path, architecture, hierarchicalType);
                Debug.Log($"Architecture graph saved at {GxlArchitecturePath.Path}.\n");
                GraphWriter.Save(GXLPath.Path, implementation, hierarchicalType);
                Debug.Log($"Implementation graph saved at {GXLPath.Path}.\n");
                GraphWriter.Save(GxlMappingPath.Path, mapping, hierarchicalType);
                Debug.Log($"Mapping graph saved at {GxlMappingPath.Path}.\n");
            }
        }

        #region Configuration file input/output

        /// <summary>
        /// Label of attribute <see cref="GxlArchitecturePath"/> in the configuration file.
        /// </summary>
        private const string GxlArchitectureLabel = "ArchitectureGXL";

        /// <summary>
        /// Label of attribute <see cref="GxlMappingPath"/> in the configuration file.
        /// </summary>
        private const string GxlMappingLabel = "MappingGXL";

        /// <summary>
        /// Label of attribute <see cref="CsvArchitecturePath"/> in the configuration file.
        /// </summary>
        private const string CsvArchitectureLabel = "ArchitectureCSV";

        /// <summary>
        /// Label of attribute <see cref="CityName"/> in the configuration file.
        /// </summary>
        private const string CityNameLabel = "CityName";

        protected override void Save(ConfigWriter writer)
        {
            base.Save(writer);
            GxlArchitecturePath.Save(writer, GxlArchitectureLabel);
            GxlMappingPath.Save(writer, GxlMappingLabel);
            CsvArchitecturePath.Save(writer, CsvArchitectureLabel);
            writer.Save(CityName, CityNameLabel);
        }

        protected override void Restore(Dictionary<string, object> attributes)
        {
            base.Restore(attributes);
            GxlArchitecturePath.Restore(attributes, GxlArchitectureLabel);
            GxlMappingPath.Restore(attributes, GxlMappingLabel);
            CsvArchitecturePath.Restore(attributes, CsvArchitectureLabel);
            ConfigIO.Restore(attributes, CityNameLabel, ref CityName);
        }

        #endregion
    }
}
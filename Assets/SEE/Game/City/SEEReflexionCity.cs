using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.UI.RuntimeConfigMenu;
using SEE.GO;
using SEE.Tools.ReflexionAnalysis;
using UnityEngine;
using Sirenix.OdinInspector;
using Assets.SEE.Tools.ReflexionAnalysis;
using SEE.GraphProviders;
using System.Collections.Generic;
using Sirenix.Serialization;

namespace SEE.Game.City
{
    /// <summary>
    /// A code city that supports architectural mappings from
    /// implementation nodes onto architecture nodes.
    /// NOTE: It is assumed the implementation and architecture graphs are not edited!
    /// </summary>
    public class SEEReflexionCity : SEECity
    {
        /// <summary>
        /// Reflexion analysis graph. Note that this simply casts <see cref="LoadedGraph"/>,
        /// to make it easier to call reflexion-specific methods.
        /// May be <c>null</c> if the graph has not yet been loaded.
        /// </summary>
        public ReflexionGraph ReflexionGraph => VisualizedSubGraph as ReflexionGraph;

        /// <summary>
        /// The <see cref="ReflexionVisualization"/> responsible for handling reflexion analysis changes.
        /// </summary>
        private ReflexionVisualization visualization;

        private string oracleProviderPathLabel = "OracleGraph";

        /// <summary>
        /// A provider of the data shown as code city.
        /// </summary>
        [OdinSerialize, ShowInInspector,
            Tooltip("A graph provider yielding the oracle mapping for a corresponding reflexion graph"),
            TabGroup(DataFoldoutGroup), RuntimeTab(DataFoldoutGroup),
            HideReferenceObjectPicker]
        private PipelineGraphProvider OracleMappingProvider { get; set; }

        /// <summary>
        /// First, if a graph was already loaded, everything will be reset by calling <see cref="Reset"/>.
        /// Second, the graph data from the three GXL files are loaded. The loaded graph is available
        /// in <see cref="LoadedGraph"/> afterwards.
        ///
        /// This method loads only the data, but does not actually render the graph.
        /// </summary>
        [Button("Load Data", ButtonSizes.Small)]
        [ButtonGroup(DataButtonsGroup), RuntimeButton(DataButtonsGroup, "Load Data")]
        [PropertyOrder(DataButtonsGroupOrderLoad)]
        public override async UniTask LoadDataAsync()
        {
            if (LoadedGraph != null)
            {
                Reset();
            }
            LoadedGraph = await DataProvider.ProvideAsync(new Graph(""), this);
            Graph oracleMapping = null;
            if(OracleMappingProvider != null)
            {
                oracleMapping = await OracleMappingProvider.ProvideAsync(new Graph(""), this);
            }
            AddCandidateRecommendation(LoadedGraph as ReflexionGraph, oracleMapping);
            visualization = gameObject.AddOrGetComponent<ReflexionVisualization>();
            visualization.StartFromScratch(VisualizedSubGraph as ReflexionGraph, this);
        }

        private void AddCandidateRecommendation(ReflexionGraph loadedGraph, Graph oracleMapping)
        {
            CandidateRecommendationVisualization candidateRecommendationViz = gameObject.AddOrGetComponent<CandidateRecommendationVisualization>();
            if (candidateRecommendationViz != null)
            {
                LoadedGraph.Subscribe(candidateRecommendationViz);
                Debug.Log("Registered CandidateRecommendation.");
                candidateRecommendationViz.UpdateConfiguration(loadedGraph, oracleMapping);
            }
        }

        protected override void InitializeAfterDrawn()
        {
            base.InitializeAfterDrawn();

            // We also need to have the ReflexionVisualization apply the correct edge
            // visualization, but we have to wait until all edges have become meshes.
            if (gameObject.TryGetComponentOrLog(out EdgeMeshScheduler scheduler))
            {
                scheduler.OnInitialEdgesDone += visualization.InitializeEdges;
            }
        }
        protected override void Restore(Dictionary<string, object> attributes)
        {
            base.Restore(attributes);
            if (attributes.ContainsKey(oracleProviderPathLabel))
            {
                OracleMappingProvider = GraphProvider.Restore(attributes, oracleProviderPathLabel) as PipelineGraphProvider; 
            }
        }
    }
}

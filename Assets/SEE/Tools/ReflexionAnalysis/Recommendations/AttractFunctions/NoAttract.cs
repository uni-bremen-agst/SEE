using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Tools.ReflexionAnalysis;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    /// <summary>
    /// Implementation of the NoAttract class, which is a specialized <see cref="AttractFunction"/>
    /// that provides no attraction effect between nodes. This class effectively disables any 
    /// attraction calculation by returning fixed, neutral values in its methods.
    /// 
    /// The NoAttract class is used in situations where recommendation calculation needs to be disabled 
    /// without removing the underlying calculation component. This class can be used as a workaround 
    /// to turn off recommendation calculation.
    /// 
    /// </summary>
    public class NoAttract : AttractFunction
    {
        /// <summary>
        /// Constructor for initializing a NoAttract instance.
        /// </summary>
        /// <param name="reflexionGraph">The given reflexion graph.</param>
        /// <param name="candidateRecommendation">The <see cref="Recommendations"/> object
        /// that uses this attract function.</param>
        /// <param name="config">Configuration object for the attraction function.</param>
        public NoAttract(ReflexionGraph reflexionGraph,
                         Recommendations candidateRecommendation,
                         AttractFunctionConfig config)
            : base(reflexionGraph, candidateRecommendation, config)
        {
        }

        /// <summary>
        /// Dumps the training data of this attract function.
        /// As this function performs no calculations, it returns an empty string.
        /// </summary>
        /// <returns>An empty string.</returns>
        public override string DumpTrainingData()
        {
            return string.Empty;
        }

        /// <summary>
        /// Checks if the attract function contains any training data.
        /// Since this function doesn't track or use training data, it always returns true.
        /// </summary>
        /// <returns>True, indicating no training data is present.</returns>
        public override bool EmptyTrainingData()
        {
            return true;
        }

        /// <summary>
        /// Calculates the attraction value between a given node and a cluster.
        /// For this function, this always returns 0.0 to represent no attraction.
        /// </summary>
        /// <param name="node">The candidate node.</param>
        /// <param name="cluster">The cluster node.</param>
        /// <returns>0.0, indicating no attraction.</returns>
        public override double GetAttractionValue(Node node, Node cluster)
        {
            return 0.0;
        }

        /// <summary>
        /// Handles changes in candidate mappings within a cluster.
        /// Since this function has no effect on candidate mappings, it performs no actions.
        /// </summary>
        /// <param name="cluster">The cluster node where the change occurred.</param>
        /// <param name="changedNode">The candidate node that was added or removed.</param>
        /// <param name="changeType">Type of change (Addition or Removal).</param>
        public override void HandleChangedCandidate(Node cluster, Node changedNode, ChangeType changeType)
        {
            return;
        }

        /// <summary>
        /// Handles changes in edge states within the reflexion graph.
        /// Since this function does not interact with edges, it performs no actions.
        /// </summary>
        /// <param name="edgeChange">The edge change event to process.</param>
        public override void HandleChangedState(EdgeChange edgeChange)
        {
            return;
        }
    }
}
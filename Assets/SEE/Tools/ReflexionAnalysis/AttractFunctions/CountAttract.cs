using Crosstales.RTVoice.Util;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Tools.ReflexionAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    public class CountAttract : AttractFunction
    {
        // TODO: Implementation of delta
        private CountAttractConfig config;

        private Dictionary<string, double> overallValues;

        private Dictionary<string, int> mappingCount;

        /// <summary>
        /// 
        /// </summary>
        public float Phi { get => config.Phi; set => config.Phi = value; }

        public CountAttract(ReflexionGraph graph, CountAttractConfig config) : base(graph, config)
        {
            this.config = config;
            overallValues = new Dictionary<string, double>();
            mappingCount = new Dictionary<string, int>();
        }

        public override double GetAttractionValue(Node candidateNode, Node cluster)
        {
            if (!candidateNode.Type.Equals(this.CandidateType)) return 0;
            if (overallValues.TryGetValue(candidateNode.ID, out double overall))
            {
                double toOthers = GetToOthersValue(candidateNode, cluster);

                // UnityEngine.Debug.Log($"CountAttract({candidateNode.ID},{cluster.ID}) = {overall}(overall) - {toOthers}(toOthers) = {overall - toOthers}");
                return overall - toOthers;
            } 
            else
            {
                // TODO: dirty? does no overall value imply always 0?
                // UnityEngine.Debug.LogWarning($"Couldn't find overall value for the candidate {candidateNode.ID}");
                return 0;
            };
        }

        public double GetToOthersValue(Node candidate, Node cluster)
        {
            List<Edge> implementationEdges = candidate.GetImplementationEdges();
            double toOthers = 0;

            foreach (Edge edge in implementationEdges)
            {
                Node candidateNeighbor = edge.Source.Equals(candidate) ? edge.Target : edge.Source;
                Node neighborCluster = reflexionGraph.MapsTo(candidateNeighbor);
  
                if (neighborCluster == null || neighborCluster.ID.Equals(cluster.ID)) continue;

                double weight = GetEdgeWeight(edge);

                State edgeState = this.edgeStatesCache.GetFromCache(cluster.ID, candidate.ID, candidateNeighbor.ID, edge.ID);
                
                // UnityEngine.Debug.Log($"stateCache: candidate id = {candidate.ID} cluster id = {cluster.ID} edge id = {edge.ID} State={edgeState}");
                if (edgeState == State.Allowed || edgeState == State.ImplicitlyAllowed)
                {
                    // UnityEngine.Debug.Log($"State is allowed. Phi value will be applied.");
                    weight *= Phi;
                }

                toOthers += weight;
            }

            // UnityEngine.Debug.Log($"ToOthers({candidateNode.ID},{cluster.ID}) = {toOthers}");
            return toOthers;
        }

        public override void HandleChangedNodes(Node cluster, List<Node> nodesChangedInMapping, ChangeType changeType)
        {
            foreach (Node nodeChangedInMapping in nodesChangedInMapping)
            {
                List<Edge> implementationEdges = nodeChangedInMapping.GetImplementationEdges();
                foreach (Edge edge in implementationEdges)
                {
                    Node neighborOfAffectedNode = edge.Source.Equals(nodeChangedInMapping) ? edge.Target : edge.Source;
                    UpdateOverallTable(neighborOfAffectedNode, edge, changeType);
                    
                    // TODO: Is there a way to also update a datastructure for the ToOthers value efficiently?
                    UpdateMappingCountTable(neighborOfAffectedNode, changeType);
                }
            }
        }

        public void UpdateOverallTable(Node NeighborOfMappedNode, Edge edge, ChangeType changeType)
        {
            if (!overallValues.ContainsKey(NeighborOfMappedNode.ID)) overallValues.Add(NeighborOfMappedNode.ID, 0);
            double edgeWeight = GetEdgeWeight(edge);
            if (changeType == ChangeType.Removal) edgeWeight *= -1;
            overallValues[NeighborOfMappedNode.ID] += edgeWeight;
        }

        public void UpdateMappingCountTable(Node NeighborOfMappedNode, ChangeType changeType)
        {
            if (!mappingCount.ContainsKey(NeighborOfMappedNode.ID)) mappingCount.Add(NeighborOfMappedNode.ID, 0);
            int count = 1;
            if (changeType == ChangeType.Removal) count *= -1;
            mappingCount[NeighborOfMappedNode.ID] += count;
        }

        public override string DumpTrainingData()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"overall values:{Environment.NewLine}");
            foreach (string nodeID in overallValues.Keys)
            {
                sb.Append(nodeID.PadRight(10));
                sb.Append($" :{overallValues[nodeID]}{Environment.NewLine}");
            }
            return sb.ToString();
        }

        public override bool EmptyTrainingData()
        {
            foreach(string key in this.overallValues.Keys)
            {
                if (this.overallValues[key] > 0) return false;
            }
            return true;
        }

        public override void Reset()
        {
            this.overallValues.Clear();
            this.edgeStatesCache.ClearCache();
        }
    }
}

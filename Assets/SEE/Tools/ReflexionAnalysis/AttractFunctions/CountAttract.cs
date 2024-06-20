using MoreLinq;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Tools.ReflexionAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    /// <summary>
    /// 
    /// </summary>
    public class CountAttract : AttractFunction
    {
        /// <summary>
        /// 
        /// </summary>
        private Dictionary<string, double> localOverallValues;

        /// <summary>
        /// 
        /// </summary>
        public float Phi { get;  private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="config"></param>
        public CountAttract(ReflexionGraph graph, 
                            CandidateRecommendation candidateRecommendation, 
                            CountAttractConfig config) : base(graph, candidateRecommendation, config)
        {
            localOverallValues = new Dictionary<string, double>();
            this.Phi = config.Phi;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="candidate"></param>
        /// <param name="cluster"></param>
        /// <returns></returns>
        public override double GetAttractionValue(Node candidate, Node cluster)
        {
            if (!candidate.Type.Equals(this.CandidateType))
            {
                return 0;
            }

            double attraction = 0;
            candidate.PostOrderDescendants().ForEach(d => attraction += GetOverallLocal(d) - GetToOthersLocal(candidate, descendant: d, cluster));

            return attraction;
        }

        private double GetOverallLocal(Node node)
        {
            return localOverallValues.TryGetValue(node.ID, out double overall) == true ? overall : 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="candidate"></param>
        /// <param name="cluster"></param>
        /// <returns></returns>
        private double GetToOthersLocal(Node candidate, Node descendant, Node cluster)
        {
            List<Edge> implementationEdges = descendant.GetImplementationEdges();
            double toOthers = 0;

            foreach (Edge edge in implementationEdges)
            {
                Node descendantNeighbor = edge.Source.ID.Equals(descendant.ID) ? edge.Target : edge.Source;
                Node neighborCluster = reflexionGraph.MapsTo(descendantNeighbor);

                if (neighborCluster == null || neighborCluster.ID.Equals(cluster.ID))
                {
                    continue;
                }

                double weight = GetEdgeWeight(edge);

                State edgeState = this.edgeStateCache.GetFromCache(cluster.ID, candidate.ID, descendantNeighbor.ID, edge.ID);

                if (edgeState == State.Allowed || edgeState == State.ImplicitlyAllowed)
                {
                    weight *= Phi;
                }

                toOthers += weight;
            }

            return toOthers;
        }

        public override void HandleChangedCandidate(Node cluster, Node changedNode, ChangeType changeType)
        {
            if (!this.HandlingRequired(changedNode.ID, changeType, updateHandling: true))
            {
                return;
            }

            // TODO: is the cluster still there? regarding removal of architecture node
            this.AddClusterToUpdate(cluster.ID);

            List<Edge> implementationEdges = changedNode.GetImplementationEdges();

            foreach (Edge edge in implementationEdges)
            {
                Node neighborOfAffectedNode = edge.Source.ID.Equals(changedNode.ID) ? edge.Target : edge.Source;

                UpdateOverallTable(neighborOfAffectedNode, edge, changeType);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="NeighborOfMappedNode"></param>
        /// <param name="edge"></param>
        /// <param name="changeType"></param>
        private void UpdateOverallTable(Node NeighborOfMappedNode, Edge edge, ChangeType changeType)
        {
            if (!localOverallValues.ContainsKey(NeighborOfMappedNode.ID))
            {
                localOverallValues.Add(NeighborOfMappedNode.ID, 0);
            }

            double edgeWeight = GetEdgeWeight(edge);

            if (changeType == ChangeType.Removal)
            {
                edgeWeight *= -1;
            }

            localOverallValues[NeighborOfMappedNode.ID] += edgeWeight;

            Node neighborCluster = reflexionGraph.MapsTo(NeighborOfMappedNode);

            if (neighborCluster != null)
            {
                this.AddClusterToUpdate(neighborCluster.ID);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string DumpTrainingData()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"overall values:{Environment.NewLine}");
            foreach (string nodeID in localOverallValues.Keys)
            {
                sb.Append(nodeID.PadRight(10));
                sb.Append($" :{localOverallValues[nodeID]}{Environment.NewLine}");
            }
            return sb.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override bool EmptyTrainingData()
        {
            foreach(string key in this.localOverallValues.Keys)
            {
                if (this.localOverallValues[key] != 0)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Reset()
        {
            this.localOverallValues.Clear();
            this.edgeStateCache.ClearCache();
        }

        public override void HandleAddCluster(Node cluster)
        {
            base.HandleAddCluster(cluster); 
        }

        public override void HandleRemovedCluster(Node cluster)
        {
            base.HandleRemovedCluster(cluster);
        }

        public override void HandleAddArchEdge(Edge archEdge)
        {
            base.HandleAddArchEdge(archEdge);
            this.AddClusterToUpdate(archEdge.Source.ID);
            this.AddClusterToUpdate(archEdge.Target.ID);
        }

        public override void HandleRemovedArchEdge(Edge archEdge)
        {
            base.HandleRemovedArchEdge(archEdge);
            
            if(reflexionGraph.ContainsNode(archEdge.Source))
            {
                this.AddClusterToUpdate(archEdge.Source.ID);
            }

            if (reflexionGraph.ContainsNode(archEdge.Target))
            {
                this.AddClusterToUpdate(archEdge.Target.ID);
            }
        }

        public override void HandleChangedState(EdgeChange edgeChange)
        {
            //No handling necessary
        }
    }
}

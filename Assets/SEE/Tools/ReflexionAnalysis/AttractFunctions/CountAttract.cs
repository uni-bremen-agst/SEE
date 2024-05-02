using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Tools.ReflexionAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    /// <summary>
    /// 
    /// </summary>
    public class CountAttract : AttractFunction
    {
        // TODO: Implementation of delta

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<string, double> overallValues;

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<string, int> mappingCount;

        /// <summary>
        /// 
        /// </summary>
        public float Phi { get;  private set; }

        /// <summary>
        /// 
        /// </summary>
        public float Delta { get;  private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="config"></param>
        public CountAttract(ReflexionGraph graph, 
                            CandidateRecommendation candidateRecommendation, 
                            CountAttractConfig config) : base(graph, candidateRecommendation, config)
        {
            overallValues = new Dictionary<string, double>();
            mappingCount = new Dictionary<string, int>();
            this.Phi = config.Phi;
            this.Delta = config.Delta;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="candidateNode"></param>
        /// <param name="cluster"></param>
        /// <returns></returns>
        public override double GetAttractionValue(Node candidateNode, Node cluster)
        {
            if (!candidateNode.Type.Equals(this.CandidateType))
            {
                return 0;
            }

            if (overallValues.TryGetValue(candidateNode.ID, out double overall))
            {
                double toOthers = GetToOthersValue(candidateNode, cluster);
                return overall - toOthers;
            } 
            else
            {
                return 0;
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="candidate"></param>
        /// <param name="cluster"></param>
        /// <returns></returns>
        private double GetToOthersValue(Node candidate, Node cluster)
        {
            List<Edge> implementationEdges = candidate.GetImplementationEdges();
            double toOthers = 0;

            foreach (Edge edge in implementationEdges)
            {
                Node candidateNeighbor = edge.Source.Equals(candidate) ? edge.Target : edge.Source;
                Node neighborCluster = reflexionGraph.MapsTo(candidateNeighbor);

                if (neighborCluster == null || neighborCluster.ID.Equals(cluster.ID))
                {
                    continue;
                }

                double weight = GetEdgeWeight(edge);

                State edgeState = this.edgeStateCache.GetFromCache(cluster.ID, candidate.ID, candidateNeighbor.ID, edge.ID);
                
                if (edgeState == State.Allowed || edgeState == State.ImplicitlyAllowed)
                {
                    weight *= Phi;
                }

                toOthers += weight;
            }

            return toOthers;
        }

        public override void HandleChangedCandidate(Node cluster, Node nodeChangedInMapping, ChangeType changeType)
        {
            if (!this.HandlingRequired(nodeChangedInMapping.ID, changeType, updateHandling: true))
            {
                return;
            }

            // TODO: is the cluster still there? regarding removal of architecture node
            this.AddClusterToUpdate(cluster.ID);

            List<Edge> implementationEdges = nodeChangedInMapping.GetImplementationEdges();
            foreach (Edge edge in implementationEdges)
            {
                Node neighborOfAffectedNode = edge.Source.ID.Equals(nodeChangedInMapping.ID) ? edge.Target : edge.Source;

                UpdateOverallTable(neighborOfAffectedNode, edge, changeType);
                    
                // TODO: Is there a way to also update a datastructure for the ToOthers value efficiently?
                
                UpdateMappingCountTable(neighborOfAffectedNode, changeType);
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
            if (!overallValues.ContainsKey(NeighborOfMappedNode.ID))
            {
                overallValues.Add(NeighborOfMappedNode.ID, 0);
            }

            double edgeWeight = GetEdgeWeight(edge);

            if (changeType == ChangeType.Removal)
            {
                edgeWeight *= -1;
            }

            overallValues[NeighborOfMappedNode.ID] += edgeWeight;

            Node neighborCluster = reflexionGraph.MapsTo(NeighborOfMappedNode);

            if (neighborCluster != null)
            {
                this.AddClusterToUpdate(neighborCluster.ID);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="NeighborOfMappedNode"></param>
        /// <param name="changeType"></param>
        public void UpdateMappingCountTable(Node NeighborOfMappedNode, ChangeType changeType)
        {
            if (!mappingCount.ContainsKey(NeighborOfMappedNode.ID))
            {
                mappingCount.Add(NeighborOfMappedNode.ID, 0);
            }
            int count = 1;
            if (changeType == ChangeType.Removal)
            {
                count *= -1;
            }
            mappingCount[NeighborOfMappedNode.ID] += count;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override bool EmptyTrainingData()
        {
            foreach(string key in this.overallValues.Keys)
            {
                if (this.overallValues[key] != 0)
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
            this.overallValues.Clear();
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

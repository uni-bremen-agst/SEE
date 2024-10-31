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
    /// This class implements the <see cref="AttractFunction"/> CountAttract.
    /// It calculates attraction values for candidates based on the counting 
    /// of mapped neighbors. The more neighbors are mapped to a cluster 
    /// the higher a candidate is attracted to this cluster. Neighbors 
    /// mapped to different clusters are considered within the attract value, 
    /// by applying parameter <see cref="Phi"/>, if the edges are allowed by the reflexion graph.
    /// 
    /// TODO: cite christl et. al.
    /// 
    /// </summary>
    public class CountAttract : AttractFunction
    {
        /// <summary>
        /// This dictionary save for a node id the number of mapped neighbors.
        /// Only the node and not its subtree is considered within the value.
        /// The node ids does not have to be candidates.
        /// </summary>
        private Dictionary<string, double> localOverallValues;

        /// <summary>
        /// The factor Phi is a scaling factor used to scale down the ToOthers(c,a) value 
        /// for a given node c and cluster a if the neighbors of c are mapped to clusters a' != a,
        /// but the neighbor relations are allowed by the reflexion graph.
        /// </summary>
        public float Phi { get;  private set; }

        /// <summary>
        /// This constructor initializes a new instance of <see cref="CountAttract"/>.
        /// </summary>
        /// <param name="graph">Reflexion graph this attraction function is reading on.</param>
        /// <param name="candidateRecommendation">CandidateRecommendation object which uses and is used by the created attract function.</param>
        /// <param name="config">Configuration objects containing parameters to configure this attraction function</param>
        public CountAttract(ReflexionGraph graph, 
                            Recommendations candidateRecommendation, 
                            CountAttractConfig config) : base(graph, candidateRecommendation, config)
        {
            localOverallValues = new Dictionary<string, double>();
            this.Phi = config.Phi;
        }

        /// <summary>
        /// This method calculates the attract value for a given candidate node and a given cluster node.
        /// The method sums up all mapped neighbors of the node and its subtree and substracts all of these neighbors
        /// which are not mapped to the given cluster node. The substraction is scaled down by the value <see cref="Phi"/>
        /// if the relations to different clusters are allowed by the reflexion graph. 
        /// </summary>
        /// <param name="candidate">given candidate node</param>
        /// <param name="cluster">given cluster node</param>
        /// <returns>The attraction between the given nodes.</returns>
        public override double GetAttractionValue(Node candidate, Node cluster)
        {
            if (!candidate.Type.Equals(this.CandidateType))
            {
                return 0;
            }

            double attraction = 0;
            candidate.PostOrderDescendants().ForEach(d => attraction += GetOverallLocal(d) - GetToOthersLocal(descendant: d, cluster));

            return attraction;
        }

        /// <summary>
        /// Returns the local overall value (number of mapped neighbors) for a given node. 
        /// This method does not consider the subtree of the node.
        /// </summary>
        /// <param name="node">given node</param>
        /// <returns>local overall value of the node</returns>
        private double GetOverallLocal(Node node)
        {
            return localOverallValues.TryGetValue(node.ID, out double overall) == true ? overall : 0;
        }

        /// <summary>
        /// This method calculates the local ToOthers() value for given candidate and its descendant 
        /// and a given cluster. This method sums up all relations to mapped neighbor of the descendants 
        /// that are not mapped to the cluster. Every relation to different clusters which is allowed 
        /// by the reflexion graph is scaled down by the value <see cref="Phi"/>.
        /// </summary>
        /// <param name="candidate">Candidate which is considered to be mapped. 
        /// This parameter is used to retrieve information from the edge state cache</param>
        /// <param name="descendant">Descendant within the subtree of the candidate. Can be the candidate as well.</param>
        /// <param name="cluster">Given cluster</param>
        /// <returns>Local ToOthers value</returns>
        private double GetToOthersLocal(Node descendant, Node cluster)
        {
            List<Edge> implementationEdges = descendant.GetImplementationEdges();
            double toOthers = 0;

            foreach (Edge edge in implementationEdges)
            {
                bool isDescendantSource = edge.Source.ID.Equals(descendant.ID);
                Node descendantNeighbor = isDescendantSource ? edge.Target : edge.Source;
                Node neighborCluster = reflexionGraph.MapsTo(descendantNeighbor);

                if (neighborCluster == null || neighborCluster.ID.Equals(cluster.ID))
                {
                    continue;
                }

                double weight = GetEdgeWeight(edge);

                State edgeState = this.edgeStateCache.GetFromCache(cluster, descendant, edge);

                if (edgeState == State.Allowed || edgeState == State.ImplicitlyAllowed)
                {
                    weight *= Phi;
                }

                toOthers += weight;
            }

            return toOthers;
        }

        /// <summary>
        /// This method is called if a node was add or removed from a given cluster. 
        /// This call updates the overall table for the neighbors of the mapped or unmapped 
        /// node. Furthermore it does add the id of the given cluster as a cluster to update.
        /// </summary>
        /// <param name="cluster">Cluster node from which the changedNode was add or removed.</param>
        /// <param name="changedNode">Candidate node which was add or removed from the cluster</param>
        /// <param name="changeType">given change type</param>
        public override void HandleChangedCandidate(Node cluster, Node changedNode, ChangeType changeType)
        {
            if (!this.HandlingRequired(changedNode.ID, changeType, updateHandling: true))
            {
                return;
            }

            this.AddClusterToUpdate(cluster.ID);
            this.AddClusterToUpdate(cluster.Incomings.Where(e => e.IsInArchitecture()).Select(e => e.Source.ID));
            this.AddClusterToUpdate(cluster.Outgoings.Where(e => e.IsInArchitecture()).Select(e => e.Target.ID));

            if(changeType == ChangeType.Removal)
            {
                this.AddCandidateToUpdate(changedNode.ID);
            }

            List<Edge> implementationEdges = changedNode.GetImplementationEdges();

            foreach (Edge edge in implementationEdges)
            {
                Node neighborOfChangedNode = edge.Source.ID.Equals(changedNode.ID) ? edge.Target : edge.Source;
                UpdateOverallTable(neighborOfChangedNode, edge, changeType);
            }
        }

        /// <summary>
        /// Updates the overall table for a given node.
        /// Depending on the given change type this method adds or removes the 
        /// edge weight of the given edge from the overall value of the given node.
        /// </summary>
        /// <param name="NeighborOfChangedNode">given node which neighbor was mapped or unmapped.</param>
        /// <param name="edge">edge connected the given node and the neighbor</param>
        /// <param name="changeType">given change type</param>
        private void UpdateOverallTable(Node NeighborOfChangedNode, Edge edge, ChangeType changeType)
        {
            if (!localOverallValues.ContainsKey(NeighborOfChangedNode.ID))
            {
                localOverallValues.Add(NeighborOfChangedNode.ID, 0);
            }

            double edgeWeight = GetEdgeWeight(edge);

            if (changeType == ChangeType.Removal)
            {
                edgeWeight *= -1;
            }

            localOverallValues[NeighborOfChangedNode.ID] += edgeWeight;

            Node neighborCluster = reflexionGraph.MapsTo(NeighborOfChangedNode);

            if (neighborCluster != null && this.CandidateRecommendation.IsCandidate(NeighborOfChangedNode))
            {
                this.AddCandidateToUpdate(NeighborOfChangedNode.ID);
            }
        }

        /// <summary>
        /// This method returns the current overall table as a formatted string 
        /// representing the data the attract function is currently holding. 
        /// 
        /// Can be used for debug and logging purposes.
        /// 
        /// </summary>
        /// <returns>a formatted string representing the overall table</returns>
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
        /// Returns if the overall table is currently empty or 
        /// does only contain 0 values.
        /// </summary>
        /// <returns>true of the overall is empty or contains only 0 values.</returns>
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
        /// Resets the edge state cache and the overall table
        /// </summary>
        public override void Reset()
        {
            this.localOverallValues.Clear();
            this.edgeStateCache.ClearCache();
        }

        /// <summary>
        /// Handles a cluster node which was add. 
        /// Forwards the call to the base class.
        /// </summary>
        /// <param name="cluster">given cluster node</param>
        public override void HandleAddCluster(Node cluster)
        {
            base.HandleAddCluster(cluster);
            this.AddAllCandidatesToUpdate();
        }

        /// <summary>
        /// Handles a cluster node which was removed. 
        /// Forwards the call to the base class.
        /// </summary>
        /// <param name="cluster">given cluster node</param>
        public override void HandleRemovedCluster(Node cluster)
        {
            base.HandleRemovedCluster(cluster);
        }

        /// <summary>
        /// Handles an architecture edge which was add to the reflexion graph.
        /// Adds the source and target of the edge as clusters to update.
        /// </summary>
        /// <param name="archEdge">given architecture edge</param>
        public override void HandleAddArchEdge(Edge archEdge)
        {
            base.HandleAddArchEdge(archEdge);
            this.AddClusterToUpdate(archEdge.Source.ID);
            this.AddClusterToUpdate(archEdge.Target.ID);
            this.AddAllCandidatesToUpdate();
        }

        /// <summary>
        /// Handles an architecture edge which was removed to the reflexion graph.
        /// Adds the source and target of the edge as clusters to update, if they still exist.
        /// </summary>
        /// <param name="archEdge">given architecture edge</param>
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
            this.AddAllCandidatesToUpdate();
        }

        /// <summary>
        /// This method does not need to be handled.
        /// </summary>
        /// <param name="edgeChange"></param>
        public override void HandleChangedState(EdgeChange edgeChange)
        {
            //No handling necessary
        }
    }
}

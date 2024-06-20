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
    public class ADCAttract : LanguageAttract
    {
        private Dictionary<string, Document> wordsPerDependency = new Dictionary<string, Document>();

        private Dictionary<string, Edge> specifiedByDependency = new Dictionary<string, Edge>();

        private Document.DocumentMergingType MergingType { get; }

        public ADCAttract(ReflexionGraph reflexionGraph, 
                          CandidateRecommendation candidateRecommendation, 
                          ADCAttractConfig config) : base(reflexionGraph, candidateRecommendation, config)
        {   
            this.MergingType = config.MergingType;
        }

        public override string DumpTrainingData()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"Words per abstract dependency:{Environment.NewLine}");
            foreach (string edgeID in wordsPerDependency.Keys)
            {
                Edge edge = reflexionGraph.GetEdge(edgeID);
                if (edge != null)
                {
                    sb.Append($"{edge.Source.ID} -{edge.Type}-> {edge.Target.ID}"); 
                }
                else
                {
                    sb.Append(edgeID);
                }
                sb.Append($" :{Environment.NewLine}{wordsPerDependency[edgeID]}{Environment.NewLine}");
            }
            return sb.ToString();
        }
        public override double GetAttractionValue(Node candidate, Node cluster)
        {
            if (!candidate.Type.Equals(this.CandidateType))
            {
                return 0;
            }

            double attraction = 0;
            candidate.PostOrderDescendants().ForEach(d => attraction += GetAttractionValueLocal(candidate, descendant:d, cluster));

            return attraction;
        }

        public double GetAttractionValueLocal(Node candidate, Node descendant, Node cluster)
        {
            List<Edge> implementationEdges = descendant.GetImplementationEdges();

            double attraction = 0;

            foreach (Edge edge in implementationEdges)
            {
                bool isDescendantSource = edge.Source.Equals(descendant);
                Node descendantNeighbor = isDescendantSource ? edge.Target : edge.Source;

                // Get the state of the current implementation edge if the candidate would be mapped to the cluster 
                State edgeState = this.edgeStateCache.GetFromCache(clusterId: cluster.ID,
                                                                    candidateId: candidate.ID,
                                                                    candidateNeighborId: descendantNeighbor.ID,
                                                                    edgeId: edge.ID);

                if ((edgeState == State.Allowed || edgeState == State.ImplicitlyAllowed))
                {
                    Node neighborCluster = reflexionGraph.MapsTo(descendantNeighbor);
                    Node clusterSource = isDescendantSource ? cluster : neighborCluster;
                    Node clusterTarget = isDescendantSource ? neighborCluster : cluster;

                    Edge architectureEdge = this.AllowedBy(clusterSource, clusterTarget, edgeState, edge.Type);

                    if(architectureEdge == null) 
                    {
                        throw new Exception($"No specifying architecture dependency was found for the edge {edge.ID} in edgeState {edgeState}.");
                    }

                    if (this.wordsPerDependency.ContainsKey(architectureEdge.ID))
                    {
                        Document architectureEdgeDoc = this.wordsPerDependency[architectureEdge.ID];
                        Document mergedDocument = this.GetMergedTerms(edge.Source, edge.Target, MergingType);
                        double similarity = Document.OverlapCoefficient(mergedDocument, architectureEdgeDoc);
                        attraction += similarity;
                    }
                }
            }
            return attraction;
        }

        public override void HandleChangedCandidate(Node cluster, Node nodeChangedInMapping, ChangeType changeType)
        {
            if(!HandlingRequired(nodeChangedInMapping.ID, changeType, updateHandling: true))
            {
                return;
            } 

            this.AddClusterToUpdate(cluster.ID);
        }

        /// <summary>
        /// This method updates the documents of an architecture edge corresponding to a propagated implementation edge.
        /// If 
        ///  1. the changed node was add to the cluster and 
        ///  2. if the neighbor of the changed node is already mapped and
        ///  3. the implementation edge is in the state allowed or implicitly allowed
        /// the documents of the given implementation edge will be add to its propagated architecture edge
        /// and the id of the implementation edge will be saved within a look up set.
        /// 
        /// TODO: describe selfloops within architecture
        /// 
        /// </summary>
        /// <param name="implEdge">incoming or outgoing implementation edge associated with the changed node</param>
        public void AddDocumentsToAllowingDependency(Edge implEdge)
        {
            // UnityEngine.Debug.Log($"Try to add Documents of edge {implEdge.Source.ID} --> {implEdge.Target.ID} (State: {implEdge.State()}, Graph: {implEdge.ItsGraph.Name})");
            State edgeState = implEdge.State();
            if ((edgeState == State.Allowed || edgeState == State.ImplicitlyAllowed) 
                 && !this.specifiedByDependency.ContainsKey(implEdge.ID))
            {
                Node mapsToSource = this.reflexionGraph.MapsTo(implEdge.Source);
                Node mapsToTarget = this.reflexionGraph.MapsTo(implEdge.Target);

                this.AddClusterToUpdate(mapsToSource.ID);
                this.AddClusterToUpdate(mapsToTarget.ID);

                Edge architectureEdge = AllowedBy(implEdge, edgeState, implEdge.Type);

                if(architectureEdge == null)
                {
                    throw new Exception($"No matching architecture edge was found for {mapsToSource.ID} -{implEdge.Type}-> {mapsToTarget.ID}." +
                                          $" Expected by implementation Edge {implEdge.ToShortString()} in edgeState {edgeState}");
                }

                this.specifiedByDependency[implEdge.ID] = architectureEdge;

                Document mergedDocument = this.GetMergedTerms(implEdge.Source, implEdge.Target, MergingType);

                if (!wordsPerDependency.ContainsKey(architectureEdge.ID))
                {
                    wordsPerDependency.Add(architectureEdge.ID, mergedDocument.Clone());
                }
                else
                {
                    wordsPerDependency[architectureEdge.ID].AddWords(mergedDocument);
                }
            } 
        }

        private Edge AllowedBy(Edge edge, State expectedState, string type)
        {
            Node sourceCluster = reflexionGraph.MapsTo(edge.Source);
            Node targetCluster = reflexionGraph.MapsTo(edge.Target);
            return AllowedBy(sourceCluster, targetCluster, expectedState, type);
        }

        // TODO: wording of function name
        private Edge AllowedBy(Node sourceCluster, Node targetCluster, State expectedState, string type)
        {
            Edge architectureEdge = null;
            if (expectedState == State.Allowed && !sourceCluster.ID.Equals(targetCluster.ID))
            {
                // TODO: Use type hierarchy in the future
                List<Edge> architectureEdges = sourceCluster.FromTo(targetCluster, null).Where(e => ReflexionGraph.IsSpecified(e)
                                                                                 || e.Source.ID.Equals(e.Target.ID)).ToList(); ;
                architectureEdge = architectureEdges.SingleOrDefault();
                return architectureEdge;
            }
            else if (expectedState == State.ImplicitlyAllowed)
            {
                // special case for implicitly allowed edges: 
                // The architecture dependencies which are allowing implementation edges within the same 
                // cluster are not specified until there are already two connected nodes mapped to an 
                // architecture node. So if only one node 'a' is add to a Cluster A, the calculation for (A,b) for a second node 'b'
                // with the dependecy b->a could not compare b->a with A->A even A->A is assumed per definition. A->A will only be 
                // created after b was already add. We create a corresponding architecure edge ourself, so we do not have 
                // to depend on the lifecycle of artificial self loop architecture edges created by the reflexion analysis.
                architectureEdge = new Edge(sourceCluster, targetCluster, type);
            }
            else
            {
                throw new Exception("State must be allowed or implicitly allowed to find allowing architecture dependency.");
            }

            return architectureEdge;
        }

        /// <summary>
        /// This method removes the documents of an architecture edge corresponding to a propagated implementation edge.
        /// 
        /// TODO: describe selfloops within architecture
        /// 
        /// If 
        ///  1. the changed node was removed from the cluster 
        ///  2. and the neighbor is currently mapped
        ///  3. and the implementation edge was in the allowed or implicitly allowed state
        ///  This will be case the if the implementation edge is contained in the look up set.(TODO: PROBLEM)
        ///  
        /// the documents of the implementation edge will be removed from the propagated architecture edge
        /// and the id of the implementation edge will be removed from the look up set. 
        /// 
        /// </summary>
        /// <param name="implEdge">incoming or outgoing implementation edge associated with the changed node</param>
        public void DeleteDocumentsFromSpecifyingDependency(Edge implEdge)
        {
            if(this.specifiedByDependency.ContainsKey(implEdge.ID))
            {
                Edge architectureEdge = this.specifiedByDependency[implEdge.ID];

                this.specifiedByDependency.Remove(implEdge.ID);

                Document mergedDocument = this.GetMergedTerms(implEdge.Source, implEdge.Target, MergingType);

                if (wordsPerDependency.ContainsKey(architectureEdge.ID))
                {
                    wordsPerDependency[architectureEdge.ID].RemoveWords(mergedDocument);
                }

                if (architectureEdge != null)
                {
                    this.AddClusterToUpdate(architectureEdge.Source.ID);
                    this.AddClusterToUpdate(architectureEdge.Target.ID);
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"Architecture edge {architectureEdge.ID} is no longer contained within the graph. Attraction values may not be updated completely.");
                }
            }
        }
        public override bool EmptyTrainingData()
        {
            foreach (string id in wordsPerDependency.Keys)
            {
                if (wordsPerDependency[id].WordCount > 0)
                {
                    return false;
                }
            }
            return true;
        }

        public override void Reset()
        {
            this.edgeStateCache.ClearCache();
            this.ClearDocumentCache();
            this.wordsPerDependency.Clear();
            this.specifiedByDependency.Clear();
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
        }

        public override void HandleRemovedArchEdge(Edge archEdge)
        {
            base.HandleRemovedArchEdge(archEdge);

            if (reflexionGraph.ContainsNode(archEdge.Source))
            {
                this.AddClusterToUpdate(archEdge.Source.ID);
            }

            if (reflexionGraph.ContainsNode(archEdge.Target))
            {
                this.AddClusterToUpdate(archEdge.Target.ID);
            }

            IList<string> keysToDelete = new List<string>();

            foreach (string implEdgeId in this.specifiedByDependency.Keys)
            {
                if (this.specifiedByDependency[implEdgeId].ID.Equals(archEdge.ID))
                {
                    keysToDelete.Add(implEdgeId);
                }
            }

            foreach (string key in keysToDelete)
            {
                this.specifiedByDependency.Remove(key);
            }
        }

        public override void HandleChangedState(EdgeChange edgeChange)
        {
            if ((edgeChange.NewState == State.Allowed || edgeChange.NewState == State.ImplicitlyAllowed)
                && edgeChange.OldState != State.Allowed 
                && edgeChange.OldState != State.ImplicitlyAllowed
                && edgeChange.Edge.IsInImplementation())
            {
                AddDocumentsToAllowingDependency(edgeChange.Edge);
            }

            if ((edgeChange.OldState == State.Allowed || edgeChange.OldState == State.ImplicitlyAllowed)
                && edgeChange.NewState != State.Allowed 
                && edgeChange.NewState != State.ImplicitlyAllowed
                && edgeChange.Edge.IsInImplementation())
            {
                DeleteDocumentsFromSpecifyingDependency(edgeChange.Edge);
            }

            return;
        }
    }
}
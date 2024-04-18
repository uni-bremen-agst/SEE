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

        private new ADCAttractConfig config;

        private Dictionary<string, Edge> specifiedByDependency = new Dictionary<string, Edge>();

        public ADCAttract(ReflexionGraph reflexionGraph, 
                          CandidateRecommendation candidateRecommendation, 
                          ADCAttractConfig config) : base(reflexionGraph, candidateRecommendation, config)
        {   
            // TODO: Copy values from config?
            this.config = config;
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
            List<Edge> implementationEdges = candidate.GetImplementationEdges();

            double attraction = 0;

            foreach (Edge edge in implementationEdges)
            {
                bool isCandidateSource = edge.Source.Equals(candidate);
                Node candidateNeighbor = isCandidateSource ? edge.Target : edge.Source;

                // Get the state of the current implementation edge if the candidate would be mapped to the cluster 
                State edgeState = this.edgeStateCache.GetFromCache(clusterId: cluster.ID,
                                                                    candidateId: candidate.ID,
                                                                    candidateNeighborId: candidateNeighbor.ID,
                                                                    edgeId: edge.ID);

                if (edgeState == State.Allowed || edgeState == State.ImplicitlyAllowed)
                {
                    Node neighborCluster = reflexionGraph.MapsTo(candidateNeighbor);
                    Node clusterSource = isCandidateSource ? cluster : neighborCluster;
                    Node clusterTarget = isCandidateSource ? neighborCluster : cluster;

                    Edge architectureEdge = this.GetSpecifyingArchitectureDepedency(clusterSource, clusterTarget, edge.Type);

                    if (architectureEdge != null && this.wordsPerDependency.ContainsKey(architectureEdge.ID))
                    {
                        Document architectureEdgeDoc = this.wordsPerDependency[architectureEdge.ID];
                        Document mergedDocument = this.GetMergedTerms(edge.Source, edge.Target, config.MergingType);
                        double similarity = Document.DotProduct(mergedDocument, architectureEdgeDoc);
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

            IEnumerable<Edge> edges = nodeChangedInMapping.GetImplementationEdges();

            if (changeType == ChangeType.Addition)
            {
                foreach (Edge edge in edges)
                {
                    AddDocumentsOfPropagatedEdge(edge);
                }
            } 
            else if(changeType == ChangeType.Removal)
            {
                foreach(Edge edge in edges)
                {
                    DeleteDocumentsOfPropagatedEdge(edge);
                }
            }
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
        public void AddDocumentsOfPropagatedEdge(Edge implEdge)
        {
            // UnityEngine.Debug.Log($"Try to add Documents of edge {implEdge.Source.ID} --> {implEdge.Target.ID} (State: {implEdge.State()}, Graph: {implEdge.ItsGraph.Name})");
            State state = implEdge.State();
            if ((state == State.Allowed || state == State.ImplicitlyAllowed) 
                 && !this.specifiedByDependency.ContainsKey(implEdge.ID))
            {
                Node mapsToSource = this.reflexionGraph.MapsTo(implEdge.Source);
                Node mapsToTarget = this.reflexionGraph.MapsTo(implEdge.Target);

                this.AddClusterToUpdate(mapsToSource.ID);
                this.AddClusterToUpdate(mapsToTarget.ID);

                Edge architectureEdge = GetSpecifyingArchitectureDepedency(mapsToSource, mapsToTarget, implEdge.Type);

                if(architectureEdge == null)
                {
                    throw new Exception($"No matching architecture edge was found for {mapsToSource.ID} -{implEdge.Type}-> {mapsToTarget.ID}." +
                                          $" Expected by implementation Edge {implEdge.ToShortString()} in state {state}");
                }

                this.specifiedByDependency[implEdge.ID] = architectureEdge;

                Document mergedDocument = this.GetMergedTerms(implEdge.Source, implEdge.Target, config.MergingType);

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

        private Edge GetSpecifyingArchitectureDepedency(Node source, Node target, string type) 
        {
            // TODO: Is this correct?
            List<Edge> edges = source.FromTo(target, null);
            // Edge architectureEdge = source.FromTo(target, null).SingleOrDefault(edge => ReflexionGraph.IsSpecified(edge));
            
            // TODO: Use type hierarchy in the future
            List<Edge> architectureEdges = source.FromTo(target, "Source_Dependency");

            //if (architectureEdges.Count > 1)
            //{
            //    UnityEngine.Debug.Log("Multiple matching architecture edges found.");
            //    foreach (Edge edge in architectureEdges)
            //    {
            //        UnityEngine.Debug.Log($"Retrieved architecture dependency {edge.ToShortString()} for {source.ID} -{type}-> {target.ID}");
            //    } 
            //}

            Edge architectureEdge = architectureEdges.FirstOrDefault();

            if (architectureEdge == null)
            {
                return null;
                // TODO: Throw Exception here
                throw new Exception($"No matching Architecture edge could be found. {source.ID} -{type}-> {target.ID}");
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
        public void DeleteDocumentsOfPropagatedEdge(Edge implEdge)
        {
            if(this.specifiedByDependency.ContainsKey(implEdge.ID))
            {
                Edge architectureEdge = this.specifiedByDependency[implEdge.ID];

                this.specifiedByDependency.Remove(implEdge.ID);

                Document mergedDocument = this.GetMergedTerms(implEdge.Source, implEdge.Target, config.MergingType);

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
    }
}
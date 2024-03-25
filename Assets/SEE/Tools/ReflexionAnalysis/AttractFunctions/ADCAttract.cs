using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Tools.ReflexionAnalysis;
using Sirenix.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    public class ADCAttract : LanguageAttract
    {
        private Dictionary<string, Document> wordsPerDependency = new Dictionary<string, Document>();

        // TODO: is this datastructure still necessary?
        private HashSet<string> propagatedEdges = new HashSet<string>();

        private new ADCAttractConfig config;

        private HashSet<string> handledAsIncoming = new HashSet<string>();

        private HashSet<string> handledAsOutgoing = new HashSet<string>();

        public ADCAttract(ReflexionGraph reflexionGraph, ADCAttractConfig config) : base(reflexionGraph, config)
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

            bool isCandidateSource;

            foreach (Edge edge in implementationEdges)
            {
                isCandidateSource = edge.Source.Equals(candidate);
                Node candidateNeighbor = isCandidateSource ? edge.Target : edge.Source;
                Node neighborCluster = reflexionGraph.MapsTo(candidateNeighbor);

                if(neighborCluster == null)
                {
                    continue;
                }

                State edgeState = this.edgeStatesCache.GetFromCache(clusterId: cluster.ID,
                                                                    candidateId: candidate.ID,
                                                                    candidateNeighborId: candidateNeighbor.ID,
                                                                    edgeId: edge.ID);

                if (edgeState == State.Allowed || edgeState == State.ImplicitlyAllowed)
                {
                    Node clusterSource = isCandidateSource ? cluster : neighborCluster;
                    Node clusterTarget = isCandidateSource ? neighborCluster : cluster;

                    string id = this.GetMatchingArchitectureDepedency(clusterSource, clusterTarget, edge.Type);

                    if (id != null)
                    {
                        if (!this.wordsPerDependency.ContainsKey(id))
                        {
                            //UnityEngine.Debug.Log($"No Document found for propagated Dependency: " + Environment.NewLine +
                            //    $"id:{id} propagated by " + Environment.NewLine +
                            //    $"{edge.Source.ID} -{edge.Type}-> {edge.Target.ID} " + Environment.NewLine +
                            //    $"implementation edge is in State {edge.State()}");

                            continue;
                        }

                        Document architectureEdgeDoc = this.wordsPerDependency[id];
                        Document mergedDocument = this.GetMergedTerms(edge.Source, edge.Target, config.MergingType);
                        double similarity = Document.DotProduct(mergedDocument, architectureEdgeDoc);
                        attraction += similarity;/*Math.Abs(similarity);*/
                    }
                }
            }
            return attraction;
        }

        private void HandleAddedEdges(IEnumerable<Edge> edges, HashSet<string> handledEdges)
        {
            foreach (Edge edge in edges)
            {
                handledEdges.Add(edge.ID);
                AddDocumentsOfPropagatedEdge(edge);
            }
        }

        public override void HandleChangedNodes(Node cluster, List<Node> nodesChangedInMapping, ChangeType changeType)
        {   
            foreach (Node nodeChangedInMapping in nodesChangedInMapping)
            {
                IEnumerable<Edge> edgesIncoming = nodeChangedInMapping.Incomings.Where(e => e.IsInImplementation() 
                                                                                    && !handledAsIncoming.Contains(e.ID));
                IEnumerable<Edge> edgesOutgoing = nodeChangedInMapping.Outgoings.Where(e => e.IsInImplementation()
                                                                                    && !handledAsOutgoing.Contains(e.ID));

                if (changeType == ChangeType.Addition)
                {
                    HandleAddedEdges(edgesIncoming, handledAsIncoming);
                    HandleAddedEdges(edgesOutgoing, handledAsOutgoing);
                } 
                else
                {
                    edgesIncoming.ForEach(e => DeleteDocumentsOfPropagatedEdge(nodeChangedInMapping, cluster, e, handledAsIncoming));
                    edgesOutgoing.ForEach(e => DeleteDocumentsOfPropagatedEdge(nodeChangedInMapping, cluster, e, handledAsOutgoing));
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
            if (state == State.Allowed || state == State.ImplicitlyAllowed)
            {
                Node mapsToSource = this.reflexionGraph.MapsTo(implEdge.Source);
                Node mapsToTarget = this.reflexionGraph.MapsTo(implEdge.Target);

                string id = GetMatchingArchitectureDepedency(mapsToSource, mapsToTarget, implEdge.Type);

                this.propagatedEdges.Add(implEdge.ID);

                Document mergedDocument = this.GetMergedTerms(implEdge.Source, implEdge.Target, config.MergingType);

                if (!wordsPerDependency.ContainsKey(id))
                {
                    wordsPerDependency.Add(id, mergedDocument.Clone());
                }
                else
                {
                    wordsPerDependency[id].AddWords(mergedDocument);
                }
            } 
        }

        private string GetMatchingArchitectureDepedency(Node source, Node target, string type) 
        {
            string architectureId;

            if (!source.ID.Equals(target.ID))
            {
                // TODO: Is this correct?
                // TODO: Use type hierarchy in the future
                List<Edge> edges = source.FromTo(target, null);
                Edge architectureEdge = source.FromTo(target, null).SingleOrDefault(edge => ReflexionGraph.IsSpecified(edge));

                if(architectureEdge == null)
                {
                    return null;
                    throw new Exception($"No matching Architecture edge could be found. {source.ID} -{type}-> {target.ID}");
                }

                architectureId = architectureEdge.ID;
            }
            else
            {
                // TODO: describe self loops in architecture of implicitly allowed depedencies
                architectureId = source.ID;
            }
            return architectureId;
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
        /// <param name="changedNode">Node add or removed from the cluster</param>
        /// <param name="oldCluster">cluster from which the Node was removed</param>
        /// <param name="implEdge">incoming or outgoing implementation edge associated with the changed node</param>
        public void DeleteDocumentsOfPropagatedEdge(Node changedNode, 
                                                    Node oldCluster, 
                                                    Edge implEdge, 
                                                    HashSet<string> handledEdges)
        {
            if(!handledEdges.Contains(implEdge.ID))
            {
                return;
            } 
            else
            {
                handledEdges.Remove(implEdge.ID); 
            }

            Node mapsToSource;
            Node mapsToTarget;

            if (changedNode == implEdge.Source)
            {
                mapsToSource = oldCluster;
                mapsToTarget = this.reflexionGraph.MapsTo(implEdge.Target);
            } 
            else
            {
                mapsToSource = this.reflexionGraph.MapsTo(implEdge.Source);
                mapsToTarget = oldCluster;
            }

            if(propagatedEdges.Contains(implEdge.ID) && mapsToSource != null && mapsToTarget != null)
            {
                string id = GetMatchingArchitectureDepedency(mapsToSource, mapsToTarget, implEdge.Type);

                this.propagatedEdges.Remove(implEdge.ID);

                Document mergedDocument = this.GetMergedTerms(implEdge.Source, implEdge.Target, config.MergingType);

                if (wordsPerDependency.ContainsKey(id))
                {
                    wordsPerDependency[id].RemoveWords(mergedDocument);
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
            this.edgeStatesCache.ClearCache();
            this.ClearDocumentCache();
            this.wordsPerDependency.Clear();
            this.propagatedEdges.Clear();
            this.handledAsIncoming.Clear();
            this.handledAsOutgoing.Clear();
        }
    }
}
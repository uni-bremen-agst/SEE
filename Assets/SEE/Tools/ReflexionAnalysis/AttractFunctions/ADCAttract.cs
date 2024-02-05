using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Tools.ReflexionAnalysis;
using SEE.UI.Window.CodeWindow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    public class ADCAttract : LanguageAttract
    {
        private Dictionary<string, Document> wordsPerDependency = new Dictionary<string, Document>();

        private HashSet<string> propagatedEdges = new HashSet<string>();

        private new ADCAttractConfig config;

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
                sb.Append($" :{wordsPerDependency[edgeID]}{Environment.NewLine}");
            }
            return sb.ToString();
        }

        public override double GetAttractionValue(Node candidate, Node cluster)
        {
            this.reflexionGraph.SuppressNotifications = true;
            this.reflexionGraph.AddToMapping(candidate, cluster);
            List<Edge> implementationEdges = candidate.GetImplementationEdges();

            double attraction = 0;

            bool isCandidateSource;

            foreach (Edge edge in implementationEdges)
            {
                isCandidateSource = edge.Source.Equals(candidate);
                Node candidateNeighbor = isCandidateSource ? edge.Target : edge.Source;
                Node neighborCluster = reflexionGraph.MapsTo(candidateNeighbor);

                if (neighborCluster != null &&
                   edge.State() != State.Divergent)
                {
                    Node clusterSource = isCandidateSource ? cluster : neighborCluster;
                    Node clusterTarget = isCandidateSource ? neighborCluster : cluster;

                    string id = this.GetPropagatedDependencyID(clusterSource, clusterTarget, edge.Type);

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

                        Document documentSource = new Document();
                        Document documentTarget = new Document();
                        this.AddStandardTerms(edge.Source, documentSource);
                        this.AddStandardTerms(edge.Target, documentTarget);
                        Document mergedDocument = documentSource.MergeDocuments(documentTarget, config.MergingType);
                        attraction += Math.Abs(architectureEdgeDoc.CosineSimilarity(mergedDocument));
                    }
                }
                else
                {
                    // Implementation edge has to be in undefined state
                }
            }

            this.reflexionGraph.RemoveFromMapping(candidate, cluster);
            this.reflexionGraph.SuppressNotifications = false;
            return attraction;
        }

        public override void HandleChangedNodes(Node cluster, List<Node> nodesChangedInMapping, ChangeType changeType)
        {
            string nodes = string.Empty;
            nodesChangedInMapping.ForEach(node => { nodes += node.ID + ","; });
            UnityEngine.Debug.Log($"Handle changed nodes {nodes} for {cluster.ID} in ADCAttract");
            
            foreach (Node nodeChangedInMapping in nodesChangedInMapping)
            {
                // TODO: duplicate implementationEdges problem
                List<Edge> implementationEdges = nodeChangedInMapping.GetImplementationEdges();

                foreach (Edge edge in implementationEdges)
                {
                    if (changeType == ChangeType.Addition)
                    {
                        AddDocumentsOfPropagatedEdge(edge); 
                    } 
                    else
                    {
                        DeleteDocumentsOfPropagatedEdge(nodeChangedInMapping, cluster, edge);
                    }
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
            if (implEdge.State() == State.Allowed || implEdge.State() == State.ImplicitlyAllowed)
            {
                Node mapsToSource = this.reflexionGraph.MapsTo(implEdge.Source);
                Node mapsToTarget = this.reflexionGraph.MapsTo(implEdge.Target);

                string id = GetPropagatedDependencyID(mapsToSource, mapsToTarget, implEdge.Type);

                this.propagatedEdges.Add(implEdge.ID);

                Document mergedDocument = GetDocumentOfImplEdge(implEdge, config.MergingType);

                if (!wordsPerDependency.ContainsKey(id))
                {
                    wordsPerDependency.Add(id, mergedDocument);
                }
                else
                {
                    wordsPerDependency[id].AddWords(mergedDocument);
                }
            } 
            else
            {
                // UnityEngine.Debug.Log($"Edge {implEdge.Source.ID} --> {implEdge.Target.ID} is not in an allowed State.(State: {implEdge.State()})");
            }
        }

        private string GetPropagatedDependencyID(Node source, Node target, string type) 
        {
            string architectureId;

            if (!source.ID.Equals(target.ID))
            {
                //Edge architectureEdge = ReflexionGraph.GetPropagatedDependency(source,
                //                                                            target,
                //                                                            type);
                // TODO: Is this correct?
                Edge architectureEdge = source.FromTo(target, type).SingleOrDefault(edge => ReflexionGraph.IsSpecified(edge));
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
        public void DeleteDocumentsOfPropagatedEdge(Node changedNode, Node oldCluster, Edge implEdge)
        {
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
                string id = GetPropagatedDependencyID(mapsToSource, mapsToTarget, implEdge.Type);

                this.propagatedEdges.Remove(implEdge.ID);

                Document mergedDocument = GetDocumentOfImplEdge(implEdge, config.MergingType);

                if (wordsPerDependency.ContainsKey(id))
                {
                    wordsPerDependency[id].RemoveWords(mergedDocument);
                }
            }
        }

        private Document GetDocumentOfImplEdge(Edge edge, Document.DocumentMergingType mergingType)
        {
            Document documentSource = new Document();
            Document documentTarget = new Document();
            this.AddStandardTerms(edge.Source, documentSource);
            this.AddStandardTerms(edge.Target, documentTarget);
            return documentSource.MergeDocuments(documentTarget, mergingType);
        }
    }
}
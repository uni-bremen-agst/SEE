using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Tools.ReflexionAnalysis;
using SEE.UI.Window.CodeWindow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    public class ADCAttract : LanguageProcessingAttractFunction
    {
        Dictionary<string, Document> wordsPerDependency = new Dictionary<string, Document>();

        public ADCAttract(ReflexionGraph reflexionGraph,
                            string targetType,
                            TokenLanguage tokenLanguage) : base(reflexionGraph, targetType, tokenLanguage)
        {
        }

        public override string DumpTrainingData()
        {
            throw new NotImplementedException();
        }

        public override double GetAttractionValue(Node node, Node cluster)
        {
            return 0.0;
        }

        public override void HandleMappedEntities(Node cluster, List<Node> nodesChangedInMapping, ChangeType changeType)
        {
            foreach (Node nodeChangedInMapping in nodesChangedInMapping)
            {
                List<Edge> implementationEdges = nodeChangedInMapping.GetImplementationEdges();

                foreach (Edge edge in implementationEdges)
                {
                    if (edge.State() == State.Allowed)
                    {
                        // Node neighborOfAffectedNode = edge.Source.Equals(nodeChangedInMapping) ? edge.Target : edge.Source;
                        Document relatedTerms = GetRelatedTerms(edge);
                        Edge architectureEdge = FindCorrespondingEdgeInArchitecture(edge);
                        if (!wordsPerDependency.ContainsKey(architectureEdge.ID)) 
                        {
                            wordsPerDependency.Add(architectureEdge.ID, relatedTerms);
                        } 
                        else
                        {
                            wordsPerDependency[architectureEdge.ID].AddWords(relatedTerms);
                        } 
                    }
                }
            }
        }

        private Edge FindCorrespondingEdgeInArchitecture(Edge edge)
        {
            if (edge.State() != State.Allowed) return null;
            Node mapsToSource = this.reflexionGraph.MapsTo(edge.Source);
            Node mapsToTarget = this.reflexionGraph.MapsTo(edge.Target);
            // TODO: How to get the correct architecture edge
            throw new NotImplementedException();
        }

        private Document GetRelatedTerms(Edge edge)
        {
            // TODO: Is this way of analysing sufficient
            Document document = new Document();
            this.AddStandardTerms(edge.Source, document);
            this.AddStandardTerms(edge.Target, document);
            return document;
        }
    }
}
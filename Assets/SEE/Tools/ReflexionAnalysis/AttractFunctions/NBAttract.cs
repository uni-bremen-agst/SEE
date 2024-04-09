using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Tools.ReflexionAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    public class NBAttract : LanguageAttract
    {
        private ITextClassifier naiveBayes;

        private new NBAttractConfig config;

        public NBAttract(ReflexionGraph reflexionGraph, NBAttractConfig config) : base(reflexionGraph, config)
        {
            this.config = config;
            this.naiveBayes = new NaiveBayesIncremental();   
        }

        public override string DumpTrainingData()
        {
            if (naiveBayes == null) return string.Empty;
            StringBuilder sb = new StringBuilder();
            string indent = "\t";
            foreach (string clazz in naiveBayes)
            {
                sb.Append(clazz);
                sb.Append(" {");
                sb.Append(Environment.NewLine);

                Dictionary<string, int> wordFrequencies = naiveBayes.GetTrainingsData(clazz);
                foreach (string word in wordFrequencies.Keys)
                {
                    sb.Append($"{indent}{word}".PadRight(10));
                    sb.Append($": {wordFrequencies[word]}{Environment.NewLine}");
                }

                sb.Append("}");
                sb.Append(Environment.NewLine);
            }
            return sb.ToString();
        }

        public override bool EmptyTrainingData()
        {
            // TODO: 
            return true;
        }

        public override double GetAttractionValue(Node candidate, Node cluster)
        {
            Document doc = new Document();

            if (this.config.UseStandardTerms)
            {
                this.AddStandardTerms(candidate, doc);
                this.AddWordsOfAscendants(candidate, doc);
            }

            Dictionary<string, IDocument> docCdaTerms = new Dictionary<string, IDocument>();

            if (this.config.UseCDA)
            {
                this.reflexionGraph.AddToMappingSilent(cluster, candidate);
                this.CreateCdaTerms(cluster, candidate, docCdaTerms);    
                this.reflexionGraph.RemoveFromMappingSilent(cluster, candidate);
                doc.AddWords(docCdaTerms[cluster.ID]);
            }

            // TODO : check and formulate cda problems
            // add cda terms to naiveBayes classifier
            //foreach (string clusterID in docCdaTerms.Keys)
            //{

            //}

            double attraction = naiveBayes.ProbabilityForClass(cluster.ID, doc);

            // remove cda terms from naiveBayes classifier
            //foreach (string clusterID in docCdaTerms.Keys)
            //{

            //}

            return attraction;
        }

        public override void HandleChangedNodes(Node cluster, List<Node> nodesChangedInMapping, ChangeType changeType)
        {
            foreach(Node nodeChangedInMapping in nodesChangedInMapping)
            {
                if (!nodeChangedInMapping.Type.Equals(this.CandidateType))
                {
                    continue;
                }

                Document docStandardTerms = new Document();

                if (this.config.UseStandardTerms)
                {
                    this.AddStandardTerms(nodeChangedInMapping, docStandardTerms);
                }

                Dictionary<string, IDocument> docCdaTerms = new Dictionary<string, IDocument>();

                if (this.config.UseCDA)
                {
                    this.CreateCdaTerms(cluster, nodeChangedInMapping, docCdaTerms);
                }

                if(changeType == ChangeType.Addition)
                {
                    naiveBayes.AddDocument(cluster.ID, docStandardTerms);

                    foreach (string clusterID in docCdaTerms.Keys)
                    {
                        naiveBayes.AddDocument(clusterID, docCdaTerms[clusterID]);
                    }
                }
                else if(changeType == ChangeType.Removal) 
                {
                    naiveBayes.DeleteDocument(cluster.ID, docStandardTerms);

                    foreach (string clusterID in docCdaTerms.Keys)
                    {
                        naiveBayes.DeleteDocument(clusterID, docCdaTerms[clusterID]);
                    }
                }
            }
        }

        public override void Reset()
        {
            this.naiveBayes.Reset();
            this.edgeStatesCache.ClearCache();
            this.ClearDocumentCache();
        }
    }
}

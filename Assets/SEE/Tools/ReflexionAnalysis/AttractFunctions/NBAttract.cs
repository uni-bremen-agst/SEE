using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Tools.ReflexionAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
using SEE.UI.Window.CodeWindow;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    public class NBAttract : LanguageProcessingAttractFunction
    {
        NaiveBayes naiveBayes;

        bool useCda;

        bool useStandardTerms;

        public NBAttract(ReflexionGraph reflexionGraph, 
                        string candidateType,
                        bool useStandardTerms,
                        TokenLanguage targetLanguage,
                        bool useCda
                        ) : base(reflexionGraph, candidateType, targetLanguage)
        {
            this.naiveBayes = new NaiveBayes();   
            this.useCda = useCda;
            this.useStandardTerms = useStandardTerms;     
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

        public override double GetAttractionValue(Node candidate, Node cluster)
        {
            Document docStandardTerms = new Document();

            if (this.useStandardTerms)
            {
                this.AddStandardTerms(candidate, docStandardTerms);
                this.AddWordsOfAscendants(candidate, docStandardTerms);
            }

            Dictionary<string, Document> docCdaTerms = new Dictionary<string, Document>();

            if (this.useCda)
            {
                this.reflexionGraph.AddToMappingSilent(cluster, candidate);
                this.CreateCdaTerms(cluster, candidate, docCdaTerms);    
                this.reflexionGraph.RemoveFromMappingSilent(cluster, candidate);
            }

            // TODO : check and formulate cda problems
            // add cda terms to naiveBayes classifier
            //foreach (string clusterID in docCdaTerms.Keys)
            //{

            //}

            Document document = new Document(docStandardTerms);
            document.AddWords(docCdaTerms[cluster.ID]);
            double attraction = naiveBayes.ProbabilityForClass(cluster.ID, docStandardTerms);

            // remove cda terms from naiveBayes classifier
            //foreach (string clusterID in docCdaTerms.Keys)
            //{

            //}

            return attraction;
        }

        public override void HandleMappedEntities(Node cluster, List<Node> nodesChangedInMapping, ChangeType changeType)
        {
            foreach(Node nodeChangedInMapping in nodesChangedInMapping)
            {
                if (!nodeChangedInMapping.Type.Equals(candidateType)) continue;

                Document docStandardTerms = new Document();
                if(useStandardTerms) this.AddStandardTerms(nodeChangedInMapping, docStandardTerms);

                Dictionary<string, Document> docCdaTerms = new Dictionary<string, Document>();
                if (useCda) this.CreateCdaTerms(cluster, nodeChangedInMapping, docCdaTerms);

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
    }
}

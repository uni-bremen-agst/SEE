using ClusteringMethods;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Tools.ReflexionAnalysis;
using System;
using System.Collections.Generic;
using Accord.MachineLearning.Text.Stemmers;
using System.Text;
using SEE.Game.UI.Window.CodeWindow;
using UnityEngine;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    public class NBAttract : AttractFunction
    {
        NaiveBayes naiveBayes;

        bool useCda;

        bool useStandardTerms;

        public NBAttract(ReflexionGraph reflexionGraph, 
                        string targetType,
                        bool useStandardTerms,
                        bool useCda) : base(reflexionGraph, targetType)
        {
            this.naiveBayes = new NaiveBayes();   
            this.useCda = useCda;
            this.useStandardTerms = useStandardTerms;
        }

        public override double GetAttractionValue(Node candidate, Node cluster)
        {
            Document docStandardTerms = new Document();

            if (this.useStandardTerms)
            {
                this.AddStandardTerms(candidate, docStandardTerms);
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

        public override void HandleMappedEntities(Node cluster, List<Node> mappedEntities, ChangeType changeType)
        {
            foreach(Node mappedEntity in mappedEntities)
            {
                if (!mappedEntity.Type.Equals(targetType)) continue;

                Document docStandardTerms = new Document();
                if(useStandardTerms) this.AddStandardTerms(mappedEntity, docStandardTerms);

                Dictionary<string, Document> docCdaTerms = new Dictionary<string, Document>();
                if (useCda) this.CreateCdaTerms(cluster, mappedEntity, docCdaTerms);

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

        private void CreateCdaTerms(Node cluster, Node mappedEntity, Dictionary<string, Document> documents)
        {
            Debug.Log($"Try to create CDA Terms for {mappedEntity.ID} and cluster {cluster.ID}...");

            List<Edge> edges = mappedEntity.GetImplementationEdges();

            documents.Add(cluster.ID, new Document());

            foreach (Edge edge in edges)
            {
                bool mappedEntityIsSource = edge.Source == mappedEntity;

                Node neighbor = mappedEntityIsSource ? edge.Target : edge.Source;

                Node neighborCluster = this.reflexionGraph.MapsTo(neighbor);

                if (neighborCluster == null) continue;

                if (neighborCluster != null)
                {
                    // create cda term
                    string term = mappedEntityIsSource ? $"{cluster.ID} -{edge.Type}- {neighborCluster.ID}"
                                                       : $"{neighborCluster.ID} -{edge.Type}- {cluster.ID}";

                    // add for current changed cluster
                    documents[cluster.ID].AddWord(term);

                    if(!documents.TryGetValue(neighborCluster.ID, out Document neighborDocument)) 
                    {
                        neighborDocument = new Document();
                        documents[neighborCluster.ID] = neighborDocument;
                    } 
                    neighborDocument.AddWord(term);                 
                }
            }
        }

        private Document AddStandardTerms(Node node, Document document)
        {
            string sourceCodeRegion = NodeRegionReader.ReadRegion(node);
            UnityEngine.Debug.Log("source code region: " + sourceCodeRegion);

            // TODO: How to determine the right language?
            IList<SEEToken> tokens = SEEToken.FromString(sourceCodeRegion, TokenLanguage.Java);

            // TODO: Add words regarding the ascendant hierarchy(like class, package, filename etc.)
            // node.Ascendants
            List<string> words = GetWordsOfAscendants(node);

            UnityEngine.Debug.Log($"words of ascendants: {string.Join(',', words)}");

            // TODO: What exactly are String literals?
            foreach (SEEToken token in tokens)
            {
                if(token.TokenType == SEEToken.Type.Comment || 
                   token.TokenType == SEEToken.Type.Identifier ||
                   token.TokenType == SEEToken.Type.StringLiteral)
                {
                    words.Add(token.Text);
                }
            }

            UnityEngine.Debug.Log($"raw words: {string.Join(',',words)}");

            // TODO: White splitting only necessary for comments? 
            // Maybe treat comments separately in a more complex way(depends on language)
            words = this.SplitWhiteSpaces(words);
            words = this.SplitCasing(words);

            UnityEngine.Debug.Log($"splitted words: {string.Join(',', words)}");

            // TODO: Stemming of differences languages
            words = this.StemWords(words);

            UnityEngine.Debug.Log($"stemmed words: {string.Join(',', words)}");

            document.AddWords(words);
            UnityEngine.Debug.Log(document.ToString());
            return document;
        }

        /// <summary>
        /// 
        /// </summary>
        public List<string> GetWordsOfAscendants(Node node)
        {
            List<string> wordsOfAscendants = new List<string>();
            // TODO: use name of class AND name of file within the descendants?
            foreach(Node ascendant in node.Ascendants())
            {
                // TODO: Use HashSet for types?
                string key = "Source.Name";
                if(ascendant.Type.Equals("Class"))
                {
                    if (node.StringAttributes.ContainsKey(key)) wordsOfAscendants.Add(node.GetString(key));
                } 
                else if(ascendant.Type.Equals("Package"))
                {
                    if (node.StringAttributes.ContainsKey(key)) wordsOfAscendants.Add(node.GetString(key));
                } 
                else if(ascendant.Type.Equals("File"))
                {
                    key = "Source.File";
                    if (node.StringAttributes.ContainsKey(key)) wordsOfAscendants.Add(node.GetString(key));
                }
            }
            return wordsOfAscendants;
        }

        public List<string> StemWords(List<string> words)
        {
            List<string> stemmedWords = new List<string>();
            EnglishStemmer stemmer = new EnglishStemmer();

            for (int i = 0; i < words.Count; i++)
            {
                stemmedWords.Add(stemmer.Stem(words[i]));
            }
            return stemmedWords;
        }

        public List<string> SplitWhiteSpaces(IEnumerable<string> words)
        {
            List<string> splittedWords = new List<string>();
            foreach (string word in words)
            {
                splittedWords.AddRange(word.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            }
            return splittedWords;
        }

        public List<string> SplitCasing(List<string> words)
        {
            List<string> splittedWords= new List<string>();

            for(int i = 0; i < words.Count; ++i)
            {
                string word = words[i];
                if(word.Contains('_'))
                {
                    splittedWords.AddRange(this.Split(word, SplitCamelCase, true));
                } 
                else if(word.Contains('-'))
                {
                    splittedWords.AddRange(this.Split(word, SplitSnakeCase, false));
                } 
                else
                {
                    splittedWords.AddRange(this.Split(word, SplitKebabCase, false));
                }               
            }

            return splittedWords;
        }

        private List<string> Split(string word, Func<char[],int, bool> splitFunction, bool keepCharAtSplit)
        {
            List<string> words = new List<string>();
            char[] chars = word.ToCharArray();
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < chars.Length; i++)
            {
                if (splitFunction(chars, i))
                {               
                    if(keepCharAtSplit) builder.Append(chars[i]);
                    words.Add(builder.ToString());
                    builder.Clear();
                } 
                else
                {
                    builder.Append(chars[i]);
                }
            }

            words.Add(builder.ToString()); 

            return words;
        }

        private bool SplitCamelCase(char[] chars, int i) 
        {
            if (!char.IsUpper(chars[i]))
            {
                if (i + 1 >= chars.Length) return false;
                if (char.IsUpper(chars[i + 1])) return true;
            }
            return false;
        }

        private bool SplitKebabCase(char[] chars, int i)
        {
            return chars[i] == '-';
        }

        private bool SplitSnakeCase(char[] chars, int i)
        {
            return chars[i] == '_';
        }
    }
}

using Accord.MachineLearning.Text.Stemmers;
using Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Tools.ReflexionAnalysis;
using SEE.UI.Window.CodeWindow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    public abstract class LanguageProcessingAttractFunction : AttractFunction
    {
        private Dictionary<string, Document> cachedStandardTerms = new Dictionary<string, Document>();

        public TokenLanguage TargetLanguage { get; set; }

        protected LanguageProcessingAttractFunction(ReflexionGraph reflexionGraph, 
                                                    string targetType, 
                                                    TokenLanguage targetLanguage) : base(reflexionGraph, targetType)
        {
            TargetLanguage = targetLanguage;
        }

        protected void CreateCdaTerms(Node cluster, Node nodeChangedInMapping, Dictionary<string, Document> documents)
        {
            // Debug.Log($"Try to create CDA Terms for {nodeChangedInMapping.ID} and cluster {cluster.ID}...");

            List<Edge> edges = nodeChangedInMapping.GetImplementationEdges();

            documents.Add(cluster.ID, new Document());

            foreach (Edge edge in edges)
            {
                bool mappedEntityIsSource = edge.Source == nodeChangedInMapping;

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

                    if (!documents.TryGetValue(neighborCluster.ID, out Document neighborDocument))
                    {
                        neighborDocument = new Document();
                        documents[neighborCluster.ID] = neighborDocument;
                    }
                    neighborDocument.AddWord(term);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected void AddWordsOfAscendants(Node node, Document document)
        {
            // TODO: use name of class AND name of file within the descendants?
            foreach (Node ascendant in node.Ascendants())
            {
                // TODO: Use HashSet for types?
                string key = "Source.Name";
                if (ascendant.Type.Equals("Class"))
                {
                    if (node.StringAttributes.ContainsKey(key)) document.AddWord(node.GetString(key));
                }
                else if (ascendant.Type.Equals("Package"))
                {
                    if (node.StringAttributes.ContainsKey(key)) document.AddWord(node.GetString(key));
                }
                else if (ascendant.Type.Equals("File"))
                {
                    key = "Source.File";
                    if (node.StringAttributes.ContainsKey(key)) document.AddWord(node.GetString(key));
                }
            }
            return;
        }

        protected void ClearTermCache()
        {
            this.cachedStandardTerms.Clear();
        }

        protected void AddStandardTerms(Node node, Document document)
        {
            if(cachedStandardTerms.ContainsKey(node.ID))
            {
                document.AddWords(cachedStandardTerms[node.ID]);
                return;
            }
            
            string codeRegion = NodeRegionReader.ReadRegion(node);

            IList<SEEToken> tokens = SEEToken.FromString(codeRegion, TargetLanguage);

            List<string> words = new List<string>();

            // TODO: What exactly are String literals?
            foreach (SEEToken token in tokens)
            {
                if (token.TokenType == SEEToken.Type.Comment ||
                   token.TokenType == SEEToken.Type.Identifier )
                   //|| token.TokenType == SEEToken.Type.StringLiteral)
                {
                    words.Add(token.Text);
                }
            }

            // TODO: White splitting only necessary for comments? 
            // Maybe treat comments separately in a more complex way(depends on language)
            words = this.SplitWhiteSpaces(words);
            words = this.SplitCasing(words);

            // TODO: Stemming of differences languages
            words = this.StemWords(words);

            words = words.Where(x => x.Length > 3).Select(x => x.ToLower()).ToList();

            document.AddWords(words);
            Document cachedDocument = new Document();
            cachedDocument.AddWords(words);
            cachedStandardTerms.Add(node.ID, cachedDocument);
        }

        private List<string> Split(string word, Func<char[], int, bool> splitFunction, bool keepCharAtSplit)
        {
            List<string> words = new List<string>();
            char[] chars = word.ToCharArray();
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < chars.Length; i++)
            {
                if (splitFunction(chars, i))
                {
                    if (keepCharAtSplit) builder.Append(chars[i]);
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
            List<string> splittedWords = new List<string>();

            for (int i = 0; i < words.Count; ++i)
            {
                string word = words[i];
                if (word.Contains('_'))
                {
                    splittedWords.AddRange(this.Split(word, SplitSnakeCase, true));
                }
                else if (word.Contains('-'))
                {
                    splittedWords.AddRange(this.Split(word, SplitKebabCase, false));
                }
                else
                {
                    splittedWords.AddRange(this.Split(word, SplitCamelCase, false));
                }
            }

            return splittedWords;
        }
    }
}

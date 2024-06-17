using Accord.MachineLearning.Text.Stemmers;
using SEE.DataModel.DG;
using SEE.Scanner;
using SEE.Tools.ReflexionAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions.Document;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    public abstract class LanguageAttract : AttractFunction
    {
        private Dictionary<string, Document> cachedDocuments = new Dictionary<string, Document>();

        public TokenLanguage TargetLanguage { get => config.TargetLanguage; set => config.TargetLanguage = value; }

        private LanguageAttractConfig config;

        private INodeReader nodeReader;

        // TODO: What to do about keywords???
        HashSet<string> Keywords = new HashSet<string>
        {
            "abstract", "assert", "boolean", "break", "byte", "case", "catch", "char", "class", "const",
            "continue", "default", "do", "double", "else", "enum", "extends", "final", "finally", /*"float",*/
            "for", "goto", "if", "implements", "import", "instanceof", /*"int",*/ "interface", /*"long",*/ "native",
            "new", "package", "private", "protected", "public", "return", /*"short",*/ "static", "strictfp",
            "super", "switch", "synchronized", "this", "throw", "throws", "transient", "try", "void",
            "volatile", "while"
        };

        protected LanguageAttract(ReflexionGraph reflexionGraph,
                                  CandidateRecommendation candidateRecommendation,
                                  LanguageAttractConfig config) : base(reflexionGraph, candidateRecommendation, config)
        {
            this.config = config;
            nodeReader = new NodeReader();
        }

        protected void CreateCdaTerms(Node cluster, Node nodeChangedInMapping, Dictionary<string, IDocument> documents)
        {
            // Debug.Log($"Try to create CDA Terms for {nodeChangedInMapping.ID} and cluster {cluster.ID}...");

            List<Edge> edges = nodeChangedInMapping.GetImplementationEdges();

            documents.Add(cluster.ID, new Document());

            foreach (Edge edge in edges)
            {
                bool mappedEntityIsSource = edge.Source == nodeChangedInMapping;

                Node neighbor = mappedEntityIsSource ? edge.Target : edge.Source;

                Node neighborCluster = this.reflexionGraph.MapsTo(neighbor);

                if (neighborCluster == null)
                {
                    continue;
                }

                if (neighborCluster != null)
                {
                    // create cda term
                    string term = mappedEntityIsSource ? $"{cluster.ID} -{edge.Type}- {neighborCluster.ID}"
                                                       : $"{neighborCluster.ID} -{edge.Type}- {cluster.ID}";

                    // add for current changed cluster
                    documents[cluster.ID].AddWord(term);

                    if (!documents.TryGetValue(neighborCluster.ID, out IDocument neighborDocument))
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
        protected void AddWordsOfAscendants(Node node, IDocument document)
        {
            // TODO: use name of class AND name of file within the descendants?
            foreach (Node ascendant in node.Ascendants())
            {
                // TODO: Use HashSet for types?
                string key = "Source.Name";
                if (ascendant.Type.Equals("Class"))
                {
                    if (ascendant.StringAttributes.ContainsKey(key))
                    {
                        document.AddWord(ascendant.GetString(key));
                    }
                }
                else if (ascendant.Type.Equals("Package"))
                {
                    if (ascendant.StringAttributes.ContainsKey(key))
                    {
                        document.AddWord(ascendant.GetString(key));
                    }
                }
                else if (ascendant.Type.Equals("File"))
                {
                    key = "Source.File";
                    if (ascendant.StringAttributes.ContainsKey(key))
                    {
                        document.AddWord(ascendant.GetString(key));
                    }
                }
            }
            return;
        }
        public void ClearDocumentCache()
        {
            this.cachedDocuments.Clear();
        }

        protected Document GetMergedTerms(Node node1, Node node2, DocumentMergingType mergingType)
        {
            string mergedDocId = node1.ID + mergingType.ToString() + node2.ID;

            if (!cachedDocuments.ContainsKey(mergedDocId))
            {
                Document document1 = this.GetStandardTerms(node1);
                Document document2 = this.GetStandardTerms(node2);
                Document mergedDocument = Document.MergeDocuments(document1, document2, mergingType);
                cachedDocuments[mergedDocId] = mergedDocument;
            }
            return cachedDocuments[mergedDocId];
        }

        protected Document GetStandardTerms(Node node)
        {
            if (cachedDocuments.ContainsKey(node.ID))
            {
                return cachedDocuments[node.ID].Clone();     
            } 
            else
            {
                Document doc = new Document();
                this.AddStandardTerms(node, doc);
                return doc;
            }
        }

        protected void AddStandardTerms(Node node, Document document)
        {
            if(cachedDocuments.ContainsKey(node.ID))
            {
                document.AddWords(cachedDocuments[node.ID]);
                return;
            }
            
            string codeRegion = nodeReader.ReadRegion(node);

            IList<SEEToken> tokens = SEEToken.FromString(codeRegion, TargetLanguage);

            List<string> words = new List<string>();

            // TODO: What exactly are String literals?
            foreach (SEEToken token in tokens)
            {
                if ((token.TokenType == SEEToken.Type.Comment ||
                   token.TokenType == SEEToken.Type.Identifier ||
                   token.TokenType == SEEToken.Type.StringLiteral) 
                   && !Keywords.Contains(token.Text))
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

            Document cachedDocument = new Document();
            cachedDocument.AddWords(words);
            document.AddWords(cachedDocument);
            cachedDocuments.Add(node.ID, cachedDocument);
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

        public void SetNodeReader(INodeReader nodeReader)
        {
            this.nodeReader = nodeReader;
        }
    }
}

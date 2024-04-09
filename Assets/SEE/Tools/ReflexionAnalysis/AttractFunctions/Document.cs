using Accord.Math.Distances;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    public class Document : IDocument, IEnumerable<string>
    {
        //private static int GlobalWords = 0;

        //private static Dictionary<string, int> GlobalWordIndices = new Dictionary<string, int>();

        int square = 0;

        private int SquareSum { get => square;}

        public enum DocumentMergingType
        {
            Union,
            Intersection
        }

        private Dictionary<string, int> wordFrequencies;

        public int WordCount { get => wordFrequencies.Keys.Count; }

        public IEnumerable<string> GetContainedWords()
        {
            return wordFrequencies.Keys;
        }

        private HashSet<string> GetContainedWordsAsHashSet()
        {
            return new HashSet<string>(wordFrequencies.Keys);
        }

        public Document Clone()
        {
            return new Document(new Dictionary<string, int>(this.wordFrequencies));
        }

        public Document(Dictionary<string, int> words)
        {
            this.wordFrequencies = words;
        }

        public Document()
        {
            this.wordFrequencies = new Dictionary<string, int>();
        }

        public Dictionary<string,int> GetWordFrequencies()
        {
            return new Dictionary<string, int>(this.wordFrequencies);
        }

        public void AddWords(Document document)
        {
            IEnumerable<string> containedWords = document != this ?
                                                   document.GetContainedWords()
                                                 : document.GetContainedWords().ToList();

            foreach (string word in containedWords)
            {
                int count = document.GetFrequency(word);
                this.AddWord(word, count);
            }
        }

        public void RemoveWords(Document document)
        {
            IEnumerable<string> containedWords = document != this ? 
                                                 document.GetContainedWords() 
                                                 : document.GetContainedWords().ToList();

            foreach (string word in containedWords)
            {
                int count = document.GetFrequency(word);
                this.RemoveWord(word, count);
            }
        }

        public void AddWords(IEnumerable<string> words)
        {
            foreach (string word in words)
            {
                this.AddWord(word);
            }
        }

        public void RemoveWords(IEnumerable<string> words)
        {
            foreach (string word in words)
            {
                this.RemoveWord(word);
            }
        }

        public void AddWord(string word)
        {
            AddWord(word, 1);
        }

        public void AddWord(string word, int count)
        {
            if (!wordFrequencies.ContainsKey(word)) 
            {
                //EnsureGlobalWordIndex(word); 
                wordFrequencies.Add(word, 0); 
            }

            int oldVal = wordFrequencies[word];
            wordFrequencies[word] += count;
            UpdateSquareSum(oldVal, wordFrequencies[word]);
        }

        //private static void EnsureGlobalWordIndex(string word)
        //{
            //if (GlobalWordIndices.ContainsKey(word)) return;
            //GlobalWordIndices.Add(word, GlobalWords);
            //GlobalWords++;
        //}

        public void RemoveWord(string word) 
        {
            RemoveWord(word, 1);
        }

        public void RemoveWord(string word, int count)
        {
            if (!wordFrequencies.ContainsKey(word))
            {
                return;
            }

            int oldVal = wordFrequencies[word];
            wordFrequencies[word]-= count;
            UpdateSquareSum(oldVal, wordFrequencies[word]);

            if (wordFrequencies[word] < 0)
            {
                throw new Exception($"Cannot remove word {word} {count} times. " +
                                                               $"Word count would be negative.");
            }

            if (wordFrequencies[word] == 0)
            {
                wordFrequencies.Remove(word);
            }
        }

        public int GetFrequency(string word)
        {
            if(wordFrequencies.ContainsKey(word)) return wordFrequencies[word];
            return 0;
        }

        public int GetTotalWordFrequencies()
        {
            int totalNumber = 0;
            foreach(int frequency in wordFrequencies.Values)
            {
                totalNumber += frequency;
            }
            return totalNumber;
        }

        public IEnumerator<string> GetEnumerator()
        {
            // TODO: iterate all words once or as often as they are contained in the document
            foreach (string word in wordFrequencies.Keys)
            {
                for (int i = 0; i < wordFrequencies[word]; i++)
                {
                    yield return word;
                }
            }
        }

        public override string ToString()
        {
            string doc = "Document {" + Environment.NewLine;
            foreach (string word in wordFrequencies.Keys)
            {
                doc+= $"{word}[{wordFrequencies[word]}]" + Environment.NewLine;
            }
            doc += "}";
            return doc;
        }

        public static Document MergeDocuments(Document doc1, Document doc2, DocumentMergingType type = DocumentMergingType.Union)
        {
            Document mergedDocument = new Document();
            IEnumerable<string> wordsThis = doc1.GetContainedWords();
            IEnumerable<string> wordsOther = doc2.GetContainedWords();

            IEnumerable<string> wordsMerged;

            if(type == DocumentMergingType.Intersection)
            {
                wordsMerged = wordsThis.Intersect(wordsOther);
            }
            else if(type == DocumentMergingType.Union)
            {
                wordsMerged = wordsThis.Union(wordsOther);
            } 
            else
            {
                throw new ArgumentException("Unknown document merging type:" + type);
            }

            foreach (string word in wordsMerged)
            {
                mergedDocument.AddWord(word, doc2.GetFrequency(word) + doc1.GetFrequency(word));
            }

            return mergedDocument;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public static double CosineSimilarityByFrequency(Document doc1, Document doc2)
        {
            HashSet<string> doc1Set = doc1.GetContainedWordsAsHashSet();
            HashSet<string> doc2Set = doc2.GetContainedWordsAsHashSet();
            doc1Set.UnionWith(doc2Set);

            double[] doc1Array = new double[doc1Set.Count];
            double[] doc2Array = new double[doc1Set.Count];

            int i = 0;
            foreach(string word in doc1Set)
            {
                doc1Array[i] = doc1.GetFrequency(word);
                doc2Array[i] = doc2.GetFrequency(word);
                i++;
            }

            Cosine cos = new Cosine();
            double result = cos.Similarity(doc1Array, doc2Array);
            return result;
        }

        public static double DotProduct(Document doc1, Document doc2)
        {
            Document smallerDoc = doc1.WordCount <= doc2.WordCount ? doc1 : doc2;
            Document biggerDoc = doc1.WordCount <= doc2.WordCount ? doc2 : doc1;

            IEnumerable<string> words = smallerDoc.GetContainedWords();
            double val = 0.0;
            foreach (var word in words)
            {
                int val1 = biggerDoc.GetFrequency(word) > 0 ? 1 : 0;
                int val2 = smallerDoc.GetFrequency(word) > 0 ? 1 : 0;
                //int val1 = biggerDoc.GetFrequency(word);
                //int val2 = smallerDoc.GetFrequency(word);
                val += val1 * val2;
            }
            return val;
        }

        public static double CosineSimilarity(Document doc1, Document doc2)
        {
            Document smallerDoc = doc1.WordCount <= doc2.WordCount ? doc1 : doc2;
            Document biggerDoc = doc1.WordCount <= doc2.WordCount ? doc2 : doc1;

            IEnumerable<string> words = smallerDoc.GetContainedWords();
            double dotProduct = 0.0;
            foreach (var word in words)
            {
                int val1 = biggerDoc.GetFrequency(word);
                int val2 = smallerDoc.GetFrequency(word);
                dotProduct += val1 * val2;
            }

            double result = dotProduct / (Math.Sqrt(doc1.SquareSum) * Math.Sqrt(doc2.SquareSum));
            return result;        
        }

        public static double CosineSimilarity2(Document doc1, Document doc2)
        {
            Document smallerDoc = doc1.WordCount <= doc2.WordCount ? doc1 : doc2;
            Document biggerDoc = doc1.WordCount <= doc2.WordCount ? doc2 : doc1;

            IEnumerable<string> words = smallerDoc.GetContainedWords();
            double dotProduct = 0.0;
            foreach (var word in words)
            {
                int val1 = biggerDoc.GetFrequency(word) > 0 ? 1 : 0;
                int val2 = smallerDoc.GetFrequency(word) > 0 ? 1 : 0;
                dotProduct += val1 * val2;
            }

            double result = dotProduct / (Math.Sqrt(doc1.SquareSum) * Math.Sqrt(doc2.SquareSum));
            return result;
        }

        public static double EuclideanDistance(Document doc1, Document doc2)
        {
            Document smallerDoc = doc1.WordCount <= doc2.WordCount ? doc1 : doc2;
            Document biggerDoc = doc1.WordCount <= doc2.WordCount ? doc2 : doc1;

            IEnumerable<string> words = biggerDoc.GetContainedWords();
            double sum = 0.0;
            foreach (var word in words)
            {
                int val1 = doc1.GetFrequency(word);
                int val2 = doc2.GetFrequency(word);
                int diff = (val1 - val2);
                sum += diff * diff;
            }

            double result = Math.Sqrt(sum);
            return result;
        }

        public static double EuclideanSimilarity(Document doc1, Document doc2)
        {
            double euclideanDistance = Document.EuclideanDistance(doc1, doc2);
            double result = 1 / (1 + euclideanDistance);
            return result;
        }

        //public double[] ToFrequencyArray()
        //{
        //    double[] documentArray = new double[GlobalWords];
        //    IEnumerable<string> containedWords = this.GetContainedWords();
        //    foreach (string containedWord in containedWords)
        //    {
        //        documentArray[GlobalWordIndices[containedWord]] = this.GetFrequency(containedWord);
        //    }
        //    return documentArray;
        //}

        //public int[] ToOccurenceArray()
        //{
        //    int[] documentArray = new int[GlobalWords];
        //    IEnumerable<string> containedWords = this.GetContainedWords();
        //    foreach (string containedWord in containedWords)
        //    {
        //        documentArray[GlobalWordIndices[containedWord]] = this.GetFrequency(containedWord) > 0 ? 1 : 0;
        //    }
        //    return documentArray;
        //}

        IDocument IDocument.Clone()
        {
            return this.Clone();
        }

        private void UpdateSquareSum(int oldVal, int newVal)
        {
            square -= oldVal * oldVal;
            square += newVal * newVal;
        }
    }
}

using Sirenix.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    public class Document : IEnumerable<string>
    {
        public enum DocumentMergingType
        {
            Union,
            Intersection
        }

        private Dictionary<string, int> wordFrequencies;

        public int NumberWords { get => wordFrequencies.Keys.Count; }

        public Document(IEnumerable<string> words) : this()
        {
            this.AddWords(words);
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

        public void AddWords(IEnumerable<string> words)
        {
            foreach (string word in words) this.AddWord(word);
        }

        public void RemoveWords(IEnumerable<string> words)
        {
            foreach (string word in words) this.RemoveWord(word);
        }

        public void AddWord(string word)
        {
            if (!wordFrequencies.ContainsKey(word)) wordFrequencies.Add(word, 0);
            wordFrequencies[word]++;
        }

        public void AddWord(string word, int count)
        {
            if (!wordFrequencies.ContainsKey(word)) wordFrequencies.Add(word, 0);
            wordFrequencies[word] += count;
        }

        public void RemoveWord(string word) 
        {
            if (!wordFrequencies.ContainsKey(word)) return;
            if (wordFrequencies[word] <= 0) throw new Exception($"Cannot remove word {word}. Count word would be negative."); 
            wordFrequencies[word]--;
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

        public IEnumerable<string> GetContainedWords()
        {
            return wordFrequencies.Keys;
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

        public Document MergeDocuments(Document otherDocument, DocumentMergingType type = DocumentMergingType.Union)
        {
            Document mergedDocument = new Document();
            IEnumerable<string> wordsThis = this.GetContainedWords();
            IEnumerable<string> wordsOther = otherDocument.GetContainedWords();

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
                mergedDocument.AddWord(word, otherDocument.GetFrequency(word) + this.GetFrequency(word));
            }

            return mergedDocument;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public double CosineSimilarity(Document otherDocument)
        {
            if (otherDocument == null)
            {
                throw new ArgumentNullException(nameof(otherDocument));
            }

            IEnumerable<string> allWords = wordFrequencies.Keys.Union(otherDocument.GetContainedWords());

            var vectorA = allWords.Select(word => wordFrequencies.TryGetValue(word, out var freqA) ? freqA : 0).ToArray();
            var vectorB = allWords.Select(word => otherDocument.wordFrequencies.TryGetValue(word, out var freqB) ? freqB : 0).ToArray();

            var dotProduct = DotProduct(vectorA, vectorB);
            var magnitudeA = Magnitude(vectorA);
            var magnitudeB = Magnitude(vectorB);

            if (magnitudeA > 0 && magnitudeB > 0)
            {
                return dotProduct / (magnitudeA * magnitudeB);
            }
            else
            {
                return 0.0;
            }
        }

        private double DotProduct(int[] vectorA, int[] vectorB)
        {
            return vectorA.Zip(vectorB, (a, b) => a * b).Sum();
        }

        private double Magnitude(int[] vector)
        {
            return Math.Sqrt(vector.Select(x => (double)(x * x)).Sum());
        }
    }
}

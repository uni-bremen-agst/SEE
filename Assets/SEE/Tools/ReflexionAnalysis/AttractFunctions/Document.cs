using System;
using System.Collections;
using System.Collections.Generic;

namespace ClusteringMethods
{
    internal class Document : IEnumerable<string>
    {
        private Dictionary<string, int> wordFrequencies;

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

        public void AddWords(IEnumerable<string> words)
        {
            foreach (string word in words) this.AddWord(word);
        }

        public void AddWord(string word)
        {
            if (!wordFrequencies.ContainsKey(word)) wordFrequencies.Add(word, 0);
            wordFrequencies[word]++;
        }

        public int GetFrequency(string word)
        {
            return wordFrequencies[word];
        }

        public int GetNumberWords()
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

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}

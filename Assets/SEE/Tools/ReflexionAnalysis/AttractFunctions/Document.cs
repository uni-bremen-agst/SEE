using System;
using System.Collections;
using System.Collections.Generic;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    public class Document : IEnumerable<string>
    {
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

        public void RemoveWord(string word) 
        {
            if (!wordFrequencies.ContainsKey(word)) return;
            if (wordFrequencies[word] > 0) wordFrequencies[word]--;
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

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}

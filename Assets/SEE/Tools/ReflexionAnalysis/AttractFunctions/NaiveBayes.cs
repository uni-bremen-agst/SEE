using System;
using System.Collections;
using System.Collections.Generic;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    public class NaiveBayes : IEnumerable<string>
    {
        private Dictionary<string, ClassInformation> trainingData;

        private int alpha;

        private int DocumentCountGlobal { get; set; }

        public int NumberClasses
        {
            get { return this.trainingData.Keys.Count; }
        }

        public int Alpha { get { return alpha; } }

        public NaiveBayes(int alpha = 1)
        {
            trainingData = new Dictionary<string, ClassInformation>();
            this.alpha = alpha;
        }

        private void EnsureClass(string clazz)
        {
            if (clazz == null) throw new Exception("Invalid class given.");

            if (!trainingData.ContainsKey(clazz))
            {
                trainingData.Add(clazz, new ClassInformation(this));
            }
        }

        public void AddDocument(string clazz, Document document) 
        {
            this.EnsureClass(clazz);

            trainingData[clazz].Add(document);
            DocumentCountGlobal++;
        }

        internal void DeleteDocument(string clazz, Document document)
        {
            if (clazz == null) throw new Exception("Invalid class given.");

            if(!trainingData.ContainsKey(clazz))
            {
                throw new Exception("Given class is unknown.");
            }

            trainingData[clazz].Remove(document);
            DocumentCountGlobal--;
        }

        public string ClassifyDocument(Document document)
        {
            double highestProbability = 0;
            string highestClass = null;
            
            foreach(string clazz in this.trainingData.Keys)
            {
                double currentProbability = ProbabilityForClass(clazz, document);
                if(currentProbability > highestProbability)
                {
                    highestProbability = currentProbability;
                    highestClass = clazz;
                }
            }

            return highestClass;
        }

        public double ProbabilityForClass(string clazz, Document doc)
        {
            this.EnsureClass(clazz);
            double prob = trainingData[clazz].GetPriorProbability();
            foreach (string word in doc)
            {
                prob *= trainingData[clazz].GetWordProbability(word);
            }
            return prob;
        }

        public Dictionary<string, int> GetTrainingsData(string clazz)
        {
            if (!this.trainingData.ContainsKey(clazz)) return new Dictionary<string, int>();
            return this.trainingData[clazz].WordFrequencies;
        }

        public IEnumerator<string> GetEnumerator()
        {
            return trainingData.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        internal class ClassInformation
        {
            private Dictionary<string, int> wordFrequencies;

            public Dictionary<string, int> WordFrequencies
            {
                get { return new Dictionary<string, int>(wordFrequencies); }
            }

            private int wordCount;

            private static Dictionary<string, int> wordFrequenciesGlobal = new Dictionary<string, int>();

            private int documentCount;

            private NaiveBayes classifier;

            public ClassInformation(NaiveBayes classifier)
            {
                this.wordFrequencies = new Dictionary<string, int>();
                this.classifier = classifier;
            }

            public int DocumentCount { get => documentCount; set => documentCount = value; }

            public double GetWordProbability(string word)
            {
                int wordFrequency = classifier.Alpha;
                int wordCountWithAlpha = wordCount + wordFrequenciesGlobal.Keys.Count * classifier.Alpha;
                
                if (wordFrequencies.ContainsKey(word))
                {
                    wordFrequency += wordFrequencies[word];
                } 
                
                if(!wordFrequenciesGlobal.ContainsKey(word))
                {
                    wordCountWithAlpha++;
                }

                double wordProbability = (double)wordFrequency / (double)wordCountWithAlpha;
                return wordProbability;
            }

            public void Add(Document document)
            {
                foreach (string word in document)
                {
                    if (!wordFrequencies.ContainsKey(word)) wordFrequencies.Add(word, 0);
                    if (!wordFrequenciesGlobal.ContainsKey(word)) wordFrequenciesGlobal.Add(word,0);
                    wordFrequencies[word]++;
                    wordFrequenciesGlobal[word]++;
                    wordCount++;
                }
                DocumentCount++;
            }

            public void Remove(Document document)
            {
                foreach (string word in document)
                {
                    if (wordFrequencies.ContainsKey(word))
                    {
                        wordFrequencies[word]--;
                        wordFrequenciesGlobal[word]--;
                        if (wordFrequenciesGlobal[word] == 0) wordFrequenciesGlobal.Remove(word);
                        wordCount--;
                    }
                }
                DocumentCount--;
            }

            public double GetPriorProbability()
            {
                return (double)this.documentCount / (double)classifier.DocumentCountGlobal;
            }
        }
    }
}

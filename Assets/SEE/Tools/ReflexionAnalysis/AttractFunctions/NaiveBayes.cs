using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusteringMethods
{
    internal class NaiveBayes
    {
        Dictionary<string, ClassInformation> trainingData;

        int alpha;

        int documentCountGlobal;

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

            //// TODO: Really necessary?
            //foreach(ClassInformation classInfo in trainingData.Values)
            //{
            //    //classInfo.AddWords(document);
            //}

            trainingData[clazz].Add(document);
            documentCountGlobal++;
        }

        internal void DeleteDocument(string clazz, Document document)
        {
            if (clazz == null) throw new Exception("Invalid class given.");

            if(!trainingData.ContainsKey(clazz))
            {
                throw new Exception("Given class is unknown.");
            }

            trainingData[clazz].Remove(document);
            documentCountGlobal--;
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

        private class ClassInformation
        {
            private Dictionary<string, int> wordFrequencies;
            
            private int wordCount;

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
                if (!wordFrequencies.ContainsKey(word)) return (double)classifier.Alpha / (double)wordCount;
                double wordProbability = (double) wordFrequencies[word] / (double)wordCount;
                return wordProbability;
            }

            public void Add(Document document)
            {
                foreach(string word in document)
                {
                    if(!wordFrequencies.ContainsKey(word)) wordFrequencies.Add(word, this.classifier.Alpha);
                    wordFrequencies[word] += document.GetFrequency(word);
                    wordCount++;
                }
                DocumentCount++;
            }

            public void Remove(Document document)
            {
                foreach(string word in document)
                {
                    if(wordFrequencies.ContainsKey(word))
                    {
                        wordFrequencies[word] -= document.GetFrequency(word);
                        wordCount--;
                    }
                }
                DocumentCount--;
            }

            public double GetPriorProbability()
            {
                return (double)this.documentCount / (double)classifier.documentCountGlobal;
            }
        }
    }
}

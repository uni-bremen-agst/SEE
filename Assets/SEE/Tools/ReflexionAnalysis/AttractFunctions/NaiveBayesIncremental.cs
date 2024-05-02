using System;
using System.Collections;
using System.Collections.Generic;
using XInputDotNetPure;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    public class NaiveBayesIncremental : ITextClassifier
    {
        public static int UNDERFLOW_OFFSET = 10000;

        private Dictionary<string, ClassInformation> trainingData;

        private double alpha;

        private int DocumentCountGlobal { get; set; }

        public int NumberClasses
        {
            get { return this.trainingData.Keys.Count; }
        }

        public IEnumerable<string> Classes
        {
            get { return this.trainingData.Keys; }
        }

        public double Alpha { get { return alpha; } }

        public NaiveBayesIncremental(double alpha = 1)
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

        public void AddDocument(string clazz, IDocument document) 
        {
            this.EnsureClass(clazz);
            trainingData[clazz].Add(document);
            DocumentCountGlobal++;
        }

        public void DeleteDocument(string clazz, IDocument document)
        {
            if (clazz == null)
            {
                throw new Exception("Invalid class given.");
            }

            if(!trainingData.ContainsKey(clazz))
            {
                throw new Exception("Given class is unknown.");
            }

            trainingData[clazz].Remove(document);
            DocumentCountGlobal--;
        }

        public string ClassifyDocument(IDocument document)
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

        public double ProbabilityForClass(string clazz, IDocument doc)
        {
            this.EnsureClass(clazz);

            double prob = Math.Log(trainingData[clazz].GetPriorProbability());

            int unknownWords = 0;

            foreach(string word in doc.GetContainedWords())
            {
                if(!ClassInformation.wordFrequenciesGlobal.ContainsKey(word))
                {
                    unknownWords++;
                }
            }

            foreach (string word in doc.GetContainedWords())
            {
                int wordFrequency = doc.GetFrequency(word);
                double wordProbability = trainingData[clazz].GetWordProbability(word, unknownWords);
                prob += Math.Log(wordProbability) * wordFrequency;
            }
            return prob + UNDERFLOW_OFFSET;
        }
        public static double ConvertFromLogarithmicScale(double value)
        {
            value -= UNDERFLOW_OFFSET;
            value = Math.Pow(Math.E, value);
            return value;
        }

        public Dictionary<string, int> GetTrainingsData(string clazz)
        {
            if (!this.trainingData.ContainsKey(clazz))
            {
                return new Dictionary<string, int>();
            }
            return this.trainingData[clazz].WordFrequencies;
        }

        public void Reset()
        {
            trainingData.Clear();
            DocumentCountGlobal = 0;
        }

        public bool IsEmpty()
        {
            foreach (string clazz in trainingData.Keys)
            {
                if (trainingData[clazz].DocumentCount != 0)
                {
                    return false;
                }

                Dictionary<string, int> wordFrequencies = trainingData[clazz].WordFrequencies;
                
                foreach (string word in wordFrequencies.Keys)
                {
                    if (wordFrequencies[word] != 0)
                    {
                        return false;
                    }
                }
            }

            return DocumentCountGlobal == 0;
        }

        public void DeleteClass(string clazz)
        {
            if (!this.trainingData.ContainsKey(clazz))
            {
                throw new Exception($"Given class {clazz} is unknown.");
            }

            ClassInformation classToRemove = this.trainingData[clazz];

            DocumentCountGlobal -= classToRemove.DocumentCount;

            Dictionary<string, int> wordFrequencies = classToRemove.WordFrequencies;
            foreach (string word in wordFrequencies.Keys)
            {
                ClassInformation.wordFrequenciesGlobal[word] -= wordFrequencies[word];

                if (ClassInformation.wordFrequenciesGlobal[word] == 0)
                {
                    ClassInformation.wordFrequenciesGlobal.Remove(word);
                }

                if (ClassInformation.wordFrequenciesGlobal[word] < 0)
                {
                    throw new Exception($"Global Frequency for word {word} in naive bayes classifier cannot be negative.");
                }
            }

            trainingData.Remove(clazz);
        }

        internal class ClassInformation
        {
            private Dictionary<string, int> wordFrequencies;

            public Dictionary<string, int> WordFrequencies
            {
                get { return new Dictionary<string, int>(wordFrequencies); }
            }

            private int totalWordsInClass;

            public static Dictionary<string, int> wordFrequenciesGlobal = new Dictionary<string, int>();

            private int documentCount;

            private NaiveBayesIncremental classifier;

            public ClassInformation(NaiveBayesIncremental classifier)
            {
                this.wordFrequencies = new Dictionary<string, int>();
                this.classifier = classifier;
            }

            public int DocumentCount { get => documentCount; set => documentCount = value; }

            public double GetWordProbability(string word, int unknownWordsCount)
            {
                double wordFrequency = classifier.Alpha;
      
                if (wordFrequencies.ContainsKey(word))
                {
                    wordFrequency += wordFrequencies[word];
                }

                double totalWordsAlphaSmoothed = totalWordsInClass + (wordFrequenciesGlobal.Keys.Count + unknownWordsCount) * classifier.Alpha;
                double wordProbability = wordFrequency / totalWordsAlphaSmoothed;
                return wordProbability;
            }

            public void Add(IDocument document)
            {
                foreach (string word in document.GetContainedWords())
                {
                    if (!wordFrequencies.ContainsKey(word))
                    {
                        wordFrequencies.Add(word, 0);
                    }
                    if (!wordFrequenciesGlobal.ContainsKey(word))
                    {
                        wordFrequenciesGlobal.Add(word, 0);
                    }
                    wordFrequencies[word]+= document.GetFrequency(word);
                    wordFrequenciesGlobal[word]+= document.GetFrequency(word);
                    totalWordsInClass+= document.GetFrequency(word);
                }
                DocumentCount++;
            }

            public void Remove(IDocument document)
            {
                foreach (string word in document.GetContainedWords())
                {
                    if (wordFrequencies.ContainsKey(word))
                    {
                        wordFrequencies[word]-= document.GetFrequency(word);
                        wordFrequenciesGlobal[word]-= document.GetFrequency(word);
                        if (wordFrequenciesGlobal[word] == 0)
                        {
                            wordFrequenciesGlobal.Remove(word);
                        }
                        totalWordsInClass-= document.GetFrequency(word);
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

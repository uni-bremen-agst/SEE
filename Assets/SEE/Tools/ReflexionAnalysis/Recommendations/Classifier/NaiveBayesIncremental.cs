using System;
using System.Collections.Generic;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    /// <summary>
    /// This class is used a Multinominal Naive Bayes classifier. IDocument 
    /// objects can be incrementally add to or removed from classes as training data
    /// to classify IDocument objects. 
    /// 
    /// The used prior probability is the number of observed documents per class. 
    /// 
    /// </summary>
    public class NaiveBayesIncremental : ITextClassifier
    {
        /// <summary>
        /// Offset used to shift probability values that were transformed to 
        /// the logarithmic scale.
        /// </summary>
        public readonly static int UNDERFLOW_OFFSET = 10000;

        /// <summary>
        /// Dictionary holding <see cref="ClassInformation"/> objects per class name. 
        /// </summary>
        private Dictionary<string, ClassInformation> trainingData;

        /// <summary>
        /// Value used for alpha smoothing.
        /// </summary>
        private double alpha;

        /// <summary>
        /// Value used for alpha smoothing.
        /// </summary>
        public double Alpha { get { return alpha; } }

        /// <summary>
        /// Number of globally add Documents considering all classes.
        /// </summary>
        private int DocumentCountGlobal { get; set; }

        /// <summary>
        /// Number of currently known classes.
        /// </summary>
        public int NumberClasses
        {
            get { return this.trainingData.Keys.Count; }
        }

        /// <summary>
        /// Names of currently known classes.
        /// </summary>
        public IEnumerable<string> Classes
        {
            get { return this.trainingData.Keys; }
        }

        /// <summary>
        /// This constructor initializes a new instance of <see cref="NaiveBayesIncremental"/>.
        /// </summary>
        /// <param name="alpha">Given value for alpha smoothing.</param>
        public NaiveBayesIncremental(double alpha = 1)
        {
            trainingData = new Dictionary<string, ClassInformation>();
            this.alpha = alpha;
        }

        /// <summary>
        /// Ensures a given class name if known. 
        /// If it was unknown it is known after this call.
        /// </summary>
        /// <param name="clazz">Given class name</param>
        /// <exception cref="NullReferenceException">Throws if the given class name is null.</exception>
        public void EnsureClass(string clazz)
        {
            if (clazz == null)
            {
                throw new NullReferenceException("Given class name cannot be null.");
            }

            if (!trainingData.ContainsKey(clazz))
            {
                trainingData.Add(clazz, new ClassInformation(this));
            }
        }

        /// <summary>
        /// Adds a <see cref="IDocument"/> object to a given class. 
        /// 
        /// Adds the class if the class was unknown.
        /// 
        /// </summary>
        /// <param name="clazz">Given class</param>
        /// <param name="document">Given document object</param>
        public void AddDocument(string clazz, IDocument document) 
        {
            this.EnsureClass(clazz);
            trainingData[clazz].Add(document);
            DocumentCountGlobal++;
        }

        /// <summary>
        /// Removes a gíven <see cref="IDocument"/> object from a given class. 
        /// 
        /// Removes the class if no documents are left. //TODO: remove empty classes?
        /// 
        /// </summary>
        /// <param name="clazz">Given class</param>
        /// <param name="document">Given document object</param>
        /// <exception cref="NullReferenceException">Throws if the given class name is null.</exception>
        /// <exception cref="ArgumentException">Throws if the given class name is unknown.</exception>
        public void RemoveDocument(string clazz, IDocument document)
        {
            if (clazz == null)
            {
                throw new NullReferenceException("Invalid class given.");
            }

            if(!trainingData.ContainsKey(clazz))
            {
                throw new ArgumentException("Given class is unknown.");
            }

            trainingData[clazz].Remove(document);
            DocumentCountGlobal--;
        }

        /// <summary>
        /// Returns the most probable class given a <see cref="IDocument"/> object 
        /// to classify.
        /// </summary>
        /// <param name="document">Given document to classify.</param>
        /// <returns>The most probable class.</returns>
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

        /// <summary>
        /// Returns the probability a given <see cref="IDocument"/> object belongs 
        /// to a given class.
        /// 
        /// The probability is calculated by multiplying the prior probability 
        /// a document belongs to a clazz with the probability that each word 
        /// was observed within the class.
        /// 
        /// The returned probability is transformed to the logarithmic scale
        /// and shifted using the <see cref="UNDERFLOW_OFFSET"/>.
        /// 
        /// Unknown words will be handled by applying alpha smoothing given
        /// the value <see cref="Alpha"/>
        /// 
        /// </summary>
        /// <param name="clazz">Given class</param>
        /// <param name="doc">Given document object</param>
        /// <returns>The probability the given object belongs to the given class.</returns>
        public double ProbabilityForClass(string clazz, IDocument doc)
        {
            this.EnsureClass(clazz);

            double prob = Math.Log(trainingData[clazz].GetPriorProbability());

            int unknownWords = 0;

            foreach (string word in doc.GetContainedWords())
            {
                if (!ClassInformation.wordFrequenciesGlobal.ContainsKey(word))
                {
                    unknownWords++;
                }
            }

            // Counts all currently known and unknown words and applies the alpha value
            double alphaSmoothing = (ClassInformation.wordFrequenciesGlobal.Keys.Count + unknownWords) * Alpha;

            foreach (string word in doc.GetContainedWords())
            {
                int wordFrequency = doc.GetFrequency(word);
                double wordProbability = trainingData[clazz].GetWordProbability(word, alphaSmoothing);
                prob += Math.Log(wordProbability) * wordFrequency;
            }
            return prob + UNDERFLOW_OFFSET;
        }

        /// <summary>
        /// Converts a given probability on the shifted logarithmic scale
        /// back to a standard probability value betwen 0 and 1.
        /// </summary>
        /// <param name="value">Given probability value on the shifted logarithmic scale</param>
        /// <returns>Standard probability value betwen 0 and 1</returns>
        public static double ConvertFromLogarithmicScale(double value)
        {
            value -= UNDERFLOW_OFFSET;
            value = Math.Pow(Math.E, value);
            return value;
        }

        /// <summary>
        /// Returns all word frequencies of a given class as a dictionary.
        /// </summary>
        /// <param name="clazz">Given class</param>
        /// <returns>word frequencies for a given class</returns>
        public Dictionary<string, int> GetTrainingsData(string clazz)
        {
            if (!this.trainingData.ContainsKey(clazz))
            {
                return new Dictionary<string, int>();
            }
            return this.trainingData[clazz].WordFrequencies;
        }

        /// <summary>
        /// Resets the classifier. 
        /// All classes and training data will be forgotten.
        /// </summary>
        public void Reset()
        {
            trainingData.Clear();         
            DocumentCountGlobal = 0;
        }

        /// <summary>
        /// Returns if no documents and terms are contained for all classes
        /// or if no class is known.
        /// </summary>
        /// <returns>Returns if this classifier contains training data</returns>
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

        /// <summary>
        /// Removes a given clazz from this classifier. 
        /// 
        /// All terms and documents contained by this class will 
        /// be forgotten.
        /// 
        /// </summary>
        /// <param name="clazz">given clazz</param>
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

        /// <summary>
        /// Private class to represent the information of a known class.
        /// Holds the word frequencies and document count for a class.
        /// </summary>
        internal class ClassInformation
        {
            /// <summary>
            /// Frequency of each word known to this class
            /// </summary>
            private Dictionary<string, int> wordFrequencies;

            /// <summary>
            /// Frequency of each word known to this class
            /// </summary>
            public Dictionary<string, int> WordFrequencies
            {
                get { return new Dictionary<string, int>(wordFrequencies); }
            }

            /// <summary>
            /// Total number of words contained by a class.
            /// </summary>
            private int totalWordsInClass;

            /// <summary>
            /// Frequency of each word considering all classes.
            /// </summary>
            public static Dictionary<string, int> wordFrequenciesGlobal = new Dictionary<string, int>();

            /// <summary>
            /// Number of Documents add to this class.
            /// </summary>
            private int documentCount;

            /// <summary>
            /// Number of Documents add to this class.
            /// </summary>
            public int DocumentCount { get => documentCount; set => documentCount = value; }

            /// <summary>
            /// Outer classifier object used for retrieving the <see cref="Alpha"/> value and the <see cref="DocumentCountGlobal"/>.
            /// </summary>
            private NaiveBayesIncremental classifier;

            /// <summary>
            /// This constructor initializes a new instance of <see cref="ClassInformation"/>.
            /// </summary>
            /// <param name="classifier">Classifier object which initialized this object.</param>
            public ClassInformation(NaiveBayesIncremental classifier)
            {
                this.wordFrequencies = new Dictionary<string, int>();
                this.classifier = classifier;
            }

            /// <summary>
            /// Calculates the probability a certain word belongs to this class, 
            /// based on currently observed words.
            /// </summary>
            /// <param name="word">given word</param>
            /// <param name="alphaSmoothing">Additional words used when the total number of words of this class is considered. 
            /// Used to compensate globally known words missing in this class and unseen words given the document to classify.</param>
            /// <returns>alpha smoothed probability this word belongs to this class.</returns>
            public double GetWordProbability(string word, double alphaSmoothing)
            {
                double wordFrequencyInClass = classifier.Alpha;

                if (wordFrequencies.ContainsKey(word))
                {
                    wordFrequencyInClass += wordFrequencies[word];
                }

                // Total words in class plus alpha smoothing for all seen and unknown words
                double knownWordsAlphaSmoothed = totalWordsInClass + alphaSmoothing;
                
                double wordProbability = wordFrequencyInClass / knownWordsAlphaSmoothed;
                return wordProbability;
            }

            /// <summary>
            /// Adds the words of <see cref="IDocument"/> object to this class. 
            /// 
            /// The datastructures <see cref="wordFrequencies"/>, <see cref="DocumentCount"/>
            /// <see cref="wordFrequenciesGlobal"/> are updated.
            /// 
            /// The identity of the given <see cref="IDocument"/> object is not remembered.
            /// 
            /// </summary>
            /// <param name="document">Given document object</param>
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

            /// <summary>
            /// Removes the words of <see cref="IDocument"/> object from this class. 
            /// 
            /// The datastructures <see cref="wordFrequencies"/>, <see cref="DocumentCount"/>
            /// <see cref="wordFrequenciesGlobal"/> are updated.
            /// 
            /// The identity of the given <see cref="IDocument"/> object is not remembered.
            /// 
            /// </summary>
            /// <param name="document">Given document object</param>
            public void Remove(IDocument document)
            {
                foreach (string word in document.GetContainedWords())
                {
                    if (wordFrequencies.ContainsKey(word))
                    {
                        wordFrequencies[word]-= document.GetFrequency(word);
                        wordFrequenciesGlobal[word]-= document.GetFrequency(word);
                        if (wordFrequencies[word] == 0)
                        {
                            wordFrequencies.Remove(word);
                        }
                        if (wordFrequenciesGlobal[word] == 0)
                        {
                            wordFrequenciesGlobal.Remove(word);
                        }
                        totalWordsInClass-= document.GetFrequency(word);
                    }
                }
                DocumentCount--;
            }

            /// <summary>
            /// Calculates the prior probability a given document 
            /// belonging to this class based on the documents
            /// which were add to this class and and add globally.
            /// </summary>
            /// <returns>The prior probability a document belonging to this class</returns>
            public double GetPriorProbability()
            {
                return (double)this.documentCount / (double)classifier.DocumentCountGlobal;
            }
        }
    }
}

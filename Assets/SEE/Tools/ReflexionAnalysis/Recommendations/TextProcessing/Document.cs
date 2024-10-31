using System;
using System.Collections.Generic;
using System.Linq;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    /// <summary>
    /// This class represents a document object containing word frequencies.
    /// </summary>
    public class Document : IDocument
    {
        /// <summary>
        /// Merging type used when merging two documents. Documents can 
        /// be merged using the intersection or union.
        /// </summary>
        public enum DocumentMergingType
        {
            Union,
            Intersection
        }

        public bool UseAsSet = false;

        /// <summary>
        /// Current word frequencies
        /// </summary>
        private Dictionary<string, int> wordFrequencies;

        /// <summary>
        /// Returns the all words contained within the document .
        /// </summary>
        /// <returns>IEnumerable object containing all words of this document.</returns>
        public IEnumerable<string> GetContainedWords()
        {
            return wordFrequencies.Keys;
        }

        /// <summary>
        /// Returns the number of different words contained within the document .
        /// </summary>
        /// <returns>number of words.</returns>
        public int WordCount { get => wordFrequencies.Keys.Count; }

        /// <summary>
        /// Method to clone this document.
        /// </summary>
        /// <returns>A clone of this object.</returns>
        public Document Clone()
        {
            Document clone = new Document(new Dictionary<string, int>(this.wordFrequencies));
            clone.UseAsSet = this.UseAsSet;
            return clone;
        }

        IDocument IDocument.Clone()
        {
            return this.Clone();
        }

        /// <summary>
        /// This constructor initializes a new instance of <see cref="Document"/>.
        /// based on given word frequencies.
        /// </summary>
        /// <param name="words">Given word frequencies contained by a dictionary.</param>
        public Document(Dictionary<string, int> words)
        {
            this.wordFrequencies = words;
        }

        /// <summary>
        /// This constructor initializes a new empty instance of <see cref="Document"/>.
        /// </summary>
        public Document()
        {
            this.wordFrequencies = new Dictionary<string, int>();
        }

        /// <summary>
        /// Returns a dictionary mapping all words containing in this document to 
        /// frequency, representing the occurence of a word.
        /// </summary>
        /// <returns>Dictionary containing the frequencies of all words</returns>
        public Dictionary<string,int> GetWordFrequencies()
        {
            return new Dictionary<string, int>(this.wordFrequencies);
        }

        /// <summary>
        /// Adds all words of a given <see cref="IDocument"/> object to this document
        /// </summary>
        /// <param name="document">Given <see cref="IDocument"/> object</param>
        public void AddWords(IDocument document)
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

        /// <summary>
        /// Removes all words of a given <see cref="IDocument"/> object from this document
        /// </summary>
        /// <param name="document">Given <see cref="IDocument"/> object</param>
        public void RemoveWords(IDocument document)
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

        /// <summary>
        /// Adds all words contained in a given enumerable object to this document.
        /// </summary>
        /// <param name="document">Given enumerable object</param>
        public void AddWords(IEnumerable<string> words)
        {
            foreach (string word in words)
            {
                this.AddWord(word);
            }
        }

        /// <summary>
        /// Removes all words contained of a given enumerable object from this document.
        /// </summary>
        /// <param name="document">Given enumerable object</param>
        public void RemoveWords(IEnumerable<string> words)
        {
            foreach (string word in words)
            {
                this.RemoveWord(word);
            }
        }

        /// <summary>
        /// Adds a word to this document.
        /// </summary>
        /// <param name="word">Given word.</param>
        public void AddWord(string word)
        {
            AddWord(word, 1);
        }

        /// <summary>
        /// Adds a word to this document multiple times. 
        /// </summary>
        /// <param name="word">Given word</param>
        /// <param name="count">Times the word will be add</param>
        public void AddWord(string word, int count)
        {
            if (!wordFrequencies.ContainsKey(word)) 
            {
                wordFrequencies.Add(word, 0); 
            }

            int oldVal = wordFrequencies[word];
            wordFrequencies[word] += count;
        }

        /// <summary>
        /// Removes a word from this document.
        /// </summary>
        /// <param name="word">Given word.</param>
        public void RemoveWord(string word) 
        {
            RemoveWord(word, 1);
        }

        /// <summary>
        /// Removes a word from this document multiple times.
        /// </summary>
        /// <param name="word">Given word</param>
        /// <param name="count">Times the word will be removed</param>
        /// <exception cref="Exception">Throws if the new frequency of a word falls below zero.</exception>
        public void RemoveWord(string word, int count)
        {
            if (!wordFrequencies.ContainsKey(word))
            {
                return;
            }

            int oldVal = wordFrequencies[word];
            wordFrequencies[word]-= count;

            if (wordFrequencies[word] < 0)
            {
                throw new Exception($"Cannot remove word {word} {count} times.(wordFrequency[{word}]={wordFrequencies[word]})" +
                                                               $"Word count would be negative.");
            }

            if (wordFrequencies[word] == 0)
            {
                wordFrequencies.Remove(word);
            }
        }

        /// <summary>
        /// Returns the frequency of a given word.
        /// </summary>
        /// <param name="word">given word.</param>
        /// <returns>The frequency of this word contained in the document</returns>
        public int GetFrequency(string word)
        {
            if (wordFrequencies.ContainsKey(word))
            {
                if (UseAsSet)
                {
                    return 1;
                } 
                else
                {
                    return wordFrequencies[word]; 
                }
            }
            return 0;
        }

        /// <summary>
        /// Returns a formatted string describing the word freuquencies contained in this object.
        /// </summary>
        /// <returns>A formatted string describing this document.</returns>
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

        /// <summary>
        /// Merges two given documents into a new instance of a document object given a merging
        /// type.
        /// 
        /// If the merging type given is <see cref="DocumentMergingType.Union"/> the union of 
        /// the words of <paramref name="doc1"/> and <paramref name="doc2"/> is used.
        /// 
        /// If the merging type given is <see cref="DocumentMergingType.Intersection"/> the intersection of 
        /// the words of <paramref name="doc1"/> and <paramref name="doc2"/> is used.
        /// 
        /// </summary>
        /// <param name="doc1">First given document.</param>
        /// <param name="doc2">Secong given document.</param>
        /// <param name="type">Given merging type</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Throws if the merging type is unknown.</exception>
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
                doc2.wordFrequencies.TryGetValue(word, out int freq2);
                doc1.wordFrequencies.TryGetValue(word, out int freq1);
                mergedDocument.AddWord(word, freq1 + freq2);
            }

            return mergedDocument;
        }

        /// <summary>
        /// This method counts the number of common words between two given document 
        /// objects.
        /// </summary>
        /// <param name="doc1">First given document.</param>
        /// <param name="doc2">Second given document.</param>
        /// <returns>Returns the number of common words</returns>
        public static int CommonWords(Document doc1, Document doc2)
        {
            Document smallerDoc = doc1.WordCount <= doc2.WordCount ? doc1 : doc2;
            Document biggerDoc = doc1.WordCount <= doc2.WordCount ? doc2 : doc1;

            IEnumerable<string> words = smallerDoc.GetContainedWords();
            int val = 0;
            foreach (var word in words)
            {          
                val += biggerDoc.GetFrequency(word) > 0 ? 1 : 0;
            }
            return val;
        }

        /// <summary>
        /// This methods calculates the overlap coefficient of two given documents.
        /// 
        /// Overlap(X,Y) = |(X Intersect Y)| / Min(|X|, |Y|)
        /// 
        /// </summary>
        /// <param name="doc1">First given document.</param>
        /// <param name="doc2">Second given document.</param>
        /// <returns>Returns the number of common words</returns>
        public static double OverlapCoefficient(Document doc1, Document doc2)
        {
            if (doc1.WordCount == 0 || doc2.WordCount == 0)
            {
                return 0;
            }
            return (double)CommonWords(doc1, doc2) / Math.Min(doc1.WordCount, doc2.WordCount);
        }

        // TODO: Keep other distance functions?

        /// <summary>
        /// This Method calculates the SorensenDiceSimilarity between two given document objects.
        /// </summary>
        /// <param name="doc1">First given document.</param>
        /// <param name="doc2">Second given document.</param>
        /// <returns>Returns the sorensen dice similarity</returns>
        //public static double SorensenDiceSimilarity(Document doc1, Document doc2)
        //{
        //    return 2 * CommonWords(doc1, doc2) / doc1.WordCount + doc2.WordCount;
        //}

        /// <summary>
        /// This Method calculates the JaccardSimilarity between two given document objects.
        /// </summary>
        /// <param name="doc1">First given document.</param>
        /// <param name="doc2">Second given document.</param>
        /// <returns>Returns the jaccard similarity</returns>
        //public static double JaccardSimilarity(Document doc1, Document doc2)
        //{
        //    Document smallerDoc = doc1.WordCount <= doc2.WordCount ? doc1 : doc2;
        //    Document biggerDoc = doc1.WordCount <= doc2.WordCount ? doc2 : doc1;

        //    IEnumerable<string> words = smallerDoc.GetContainedWords();

        //    int intersectionCount = 0;

        //    foreach (var word in words)
        //    {
        //        int val1 = biggerDoc.GetFrequency(word) > 0 ? 1 : 0;
        //        intersectionCount += val1;
        //    }

        //    int unionCount = smallerDoc.GetContainedWords().Count() + biggerDoc.GetContainedWords().Count() - intersectionCount;

        //    return (double)intersectionCount / (double)unionCount;
        //}

        /// <summary>
        /// This Method calculates the CosineSimilarity between two given document objects.
        /// </summary>
        /// <param name="doc1">First given document.</param>
        /// <param name="doc2">Second given document.</param>
        /// <returns>Returns the cosine similarity</returns>
        //public static double CosineSimilarity(Document doc1, Document doc2)
        //{
        //    Document smallerDoc = doc1.WordCount <= doc2.WordCount ? doc1 : doc2;
        //    Document biggerDoc = doc1.WordCount <= doc2.WordCount ? doc2 : doc1;

        //    IEnumerable<string> words = smallerDoc.GetContainedWords();
        //    double dotProduct = 0.0;
        //    foreach (var word in words)
        //    {
        //        int val1 = biggerDoc.GetFrequency(word);
        //        int val2 = smallerDoc.GetFrequency(word);
        //        dotProduct += val1 * val2;
        //    }

        //    double result = dotProduct / (Math.Sqrt(doc1.SquareSum) * Math.Sqrt(doc2.SquareSum));
        //    return result;        
        //}

        /// <summary>
        /// This Method calculates the CosineSimilarity between two given document objects.
        /// This method only takes the occurence of a word into account and not its frequency.
        /// </summary>
        /// <param name="doc1">First given document.</param>
        /// <param name="doc2">Second given document.</param>
        /// <returns>Returns the cosine similarity</returns>
        //public static double CosineSimilarityByOccurence(Document doc1, Document doc2)
        //{
        //    Document smallerDoc = doc1.WordCount <= doc2.WordCount ? doc1 : doc2;
        //    Document biggerDoc = doc1.WordCount <= doc2.WordCount ? doc2 : doc1;

        //    IEnumerable<string> words = smallerDoc.GetContainedWords();
        //    double dotProduct = 0.0;
        //    foreach (var word in words)
        //    {
        //        int val1 = biggerDoc.GetFrequency(word) > 0 ? 1 : 0;
        //        int val2 = smallerDoc.GetFrequency(word) > 0 ? 1 : 0;
        //        dotProduct += val1 * val2;
        //    }

        //    double result = dotProduct / (Math.Sqrt(doc1.SquareSum) * Math.Sqrt(doc2.SquareSum));
        //    return result;
        //}

        /// <summary>
        /// This Method calculates the euclidean distance between two given document objects.
        /// </summary>
        /// <param name="doc1">First given document.</param>
        /// <param name="doc2">Second given document.</param>
        /// <returns>Returns the euclidean distance</returns>
        //public static double EuclideanDistance(Document doc1, Document doc2)
        //{
        //    Document smallerDoc = doc1.WordCount <= doc2.WordCount ? doc1 : doc2;
        //    Document biggerDoc = doc1.WordCount <= doc2.WordCount ? doc2 : doc1;

        //    IEnumerable<string> words = biggerDoc.GetContainedWords();
        //    double sum = 0.0;
        //    foreach (var word in words)
        //    {
        //        int val1 = doc1.GetFrequency(word);
        //        int val2 = doc2.GetFrequency(word);
        //        int diff = (val1 - val2);
        //        sum += diff * diff;
        //    }

        //    double result = Math.Sqrt(sum);
        //    return result;
        //}

        /// <summary>
        /// This Method calculates the euclidean similarity between two given document objects.
        /// </summary>
        /// <param name="doc1">First given document.</param>
        /// <param name="doc2">Second given document.</param>
        /// <returns>Returns the euclidean similarity</returns>
        //public static double EuclideanSimilarity(Document doc1, Document doc2)
        //{
        //    double euclideanDistance = Document.EuclideanDistance(doc1, doc2);
        //    double result = 1 / (1 + euclideanDistance);
        //    return result;
        //}

        /// <summary>
        /// Updates the square sum of this document incremental.
        /// </summary>
        //private void UpdateSquareSum(int oldVal, int newVal)
        //{
        //    square -= oldVal * oldVal;
        //    square += newVal * newVal;
        //}
    }
}

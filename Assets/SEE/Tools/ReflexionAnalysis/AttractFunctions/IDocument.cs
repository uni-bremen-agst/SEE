using System.Collections.Generic;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    /// <summary>
    /// This interface is used to implement a document class which contains the frequency of add terms. 
    /// </summary>
    public interface IDocument
    {
        /// <summary>
        /// Returns the all words contained within the document .
        /// </summary>
        /// <returns>IEnumerable object containing all words of this document.</returns>
        public IEnumerable<string> GetContainedWords();

        /// <summary>
        /// Returns the number of different words contained within the document .
        /// </summary>
        /// <returns>number of words.</returns>
        public int WordCount { get; }

        /// <summary>
        /// Returns a dictionary mapping all words containing in this document to 
        /// frequency, representing the occurence of a word.
        /// </summary>
        /// <returns>Dictionary containing the frequencies of all words</returns>
        public Dictionary<string, int> GetWordFrequencies();
        
        /// <summary>
        /// Method to clone this document.
        /// </summary>
        /// <returns>A clone of this object.</returns>
        public IDocument Clone();

        /// <summary>
        /// Adds all words of a given <see cref="IDocument"/> object to this document
        /// </summary>
        /// <param name="document">Given <see cref="IDocument"/> object</param>
        public void AddWords(IDocument document);

        /// <summary>
        /// Removes all words of a given <see cref="IDocument"/> object from this document
        /// </summary>
        /// <param name="document">Given <see cref="IDocument"/> object</param>
        public void RemoveWords(IDocument document);

        /// <summary>
        /// Adds all words contained in a given enumerable object to this document.
        /// </summary>
        /// <param name="document">Given enumerable object</param>
        public void AddWords(IEnumerable<string> words);

        /// <summary>
        /// Removes all words contained of a given enumerable object from this document.
        /// </summary>
        /// <param name="document">Given enumerable object</param>
        public void RemoveWords(IEnumerable<string> words);

        /// <summary>
        /// Adds a word to this document.
        /// </summary>
        /// <param name="word">Given word.</param>
        public void AddWord(string word);

        /// <summary>
        /// Adds a word to this document multiple times. 
        /// </summary>
        /// <param name="word">Given word</param>
        /// <param name="count">Times the word will be add</param>
        public void AddWord(string word, int count);

        /// <summary>
        /// Removes a word from this document.
        /// </summary>
        /// <param name="word">Given word.</param>
        public void RemoveWord(string word);

        /// <summary>
        /// Removes a word from this document multiple times.
        /// </summary>
        /// <param name="word">Given word</param>
        /// <param name="count">Times the word will be removed</param>
        public void RemoveWord(string word, int count);

        /// <summary>
        /// Returns the frequency of a given word.
        /// </summary>
        /// <param name="word">given word.</param>
        /// <returns>The frequency of this word contained in the document</returns>
        public int GetFrequency(string word);

        /// <summary>
        /// Returns a formatted string describing the word freuquencies contained in this object.
        /// </summary>
        /// <returns>A formatted string describing this document.</returns>
        public string ToString();
    }
}

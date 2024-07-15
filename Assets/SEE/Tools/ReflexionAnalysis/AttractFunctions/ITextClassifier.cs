using System.Collections.Generic;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    /// <summary>
    /// Interface used to implement incremental classifier to classify given <see cref="IDocument"/> objects.
    /// </summary>
    public interface ITextClassifier
    {
        /// <summary>
        /// Returns all classes known the classifier.
        /// </summary>
        public IEnumerable<string> Classes { get; }

        /// <summary>
        /// Adds a <see cref="IDocument"/> object to a given class. 
        /// 
        /// Adds the class if the class was unknown.
        /// 
        /// </summary>
        /// <param name="clazz">Given class</param>
        /// <param name="document">Given document object</param>
        void AddDocument(string clazz, IDocument document);

        /// <summary>
        /// Removes a gíven <see cref="IDocument"/> object from a given class. 
        /// 
        /// Removes the class if no documents are left.
        /// 
        /// </summary>
        /// <param name="clazz">Given class</param>
        /// <param name="document">Given document object</param>
        void RemoveDocument(string clazz, IDocument document);

        /// <summary>
        /// Removes a given clazz from this classifier. 
        /// 
        /// All terms and documents contained by this class will 
        /// be forgotten.
        /// 
        /// </summary>
        /// <param name="clazz">given clazz</param>
        void DeleteClass(string clazz);

        /// <summary>
        /// Returns if no documents and terms are contained for all classes
        /// or if no class is known.
        /// </summary>
        /// <returns>Returns if this classifier contains training data</returns>
        bool IsEmpty();

        /// <summary>
        /// Resets the classifier. 
        /// All classes and training data will be forgotten.
        /// </summary>
        void Reset();

        /// <summary>
        /// Returns the most probable class given a <see cref="IDocument"/> object 
        /// to classify.
        /// </summary>
        /// <param name="document">Given document to classify.</param>
        /// <returns>The most probable class.</returns>
        string ClassifyDocument(IDocument document);

        /// <summary>
        /// Returns the probability a given <see cref="IDocument"/> object belongs 
        /// to a given class.
        /// </summary>
        /// <param name="clazz">Given class</param>
        /// <param name="doc">Given document object</param>
        /// <returns>The probability the given object belongs to the given class.</returns>
        double ProbabilityForClass(string clazz, IDocument doc);

        /// <summary>
        /// Returns all word frequencies of a given class as a dictionary.
        /// </summary>
        /// <param name="clazz">Given class</param>
        /// <returns>word frequencies for a given class</returns>
        Dictionary<string, int> GetTrainingsData(string clazz);
    }
}

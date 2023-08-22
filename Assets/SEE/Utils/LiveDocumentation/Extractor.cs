using System.Collections.Generic;
using JetBrains.Annotations;
using SEE.Game.UI.LiveDocumentation;
using SEE.Game.UI.LiveDocumentation.Buffer;

namespace SEE.Utils.LiveDocumentation
{
    /// <summary>
    /// Represents an general extractor for the LiveDocumentation.
    ///
    /// This interface is used to create new documentation parser for different languages.
    /// </summary>
    public interface IExtractor
    {
        /// <summary>
        /// This method should extract the comments of an specific class in a specific source code file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="className"></param>
        /// <returns>A new <see cref="LiveDocumentationBuffer"/> containing the documentation </returns>
        [NotNull]
        public LiveDocumentationBuffer ExtractClassComments(string className);

        /// <summary>
        /// This method should extract all methods and their signatures
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="className"></param>
        /// <exception cref="ClassNotFoundException">Is thrown, when the class cant be found in the file</exception>
        /// <returns>A list of <see cref="LiveDocumentationClassMemberBuffer"/> for each of the class members/methods</returns>
        [NotNull]
        public List<LiveDocumentationClassMemberBuffer> ExtractMethods(string className);


        /// <summary>
        /// This method should extract all import statements of the source code file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>A <see cref="List{T}"/> of the names of all imported namespaces</returns>
        [NotNull]
        public List<string> ExtractImportedNamespaces();
    }
}

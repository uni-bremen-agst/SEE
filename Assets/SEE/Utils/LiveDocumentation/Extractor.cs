using System.Collections.Generic;
using SEE.Game.UI.LiveDocumantation;

namespace SEE.Utils.LiveDocumentation
{

    /// <summary>
    /// Represents an general extractor for the LiveDocumentation.
    ///
    /// This interface is used to create new documentation parser for different languages.
    /// </summary>
    public interface Extractor
    {
        /// <summary>
        /// This method should extract the comments of an specific class in a specific source code file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="className"></param>
        /// <returns>A new <see cref="LiveDocumentationBuffer"/> containing the documentation </returns>
        LiveDocumentationBuffer ExtractClassComments(string fileName, string className);

        /// <summary>
        /// This method should extract all methods and their signatures
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="className"></param>
        /// <returns></returns>
        List<LiveDocumentationBuffer> ExtractMethods(string fileName, string className);

        /// <summary>
        /// This method should extract all import statements of the source code file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        List<string> ExtractImportedNamespaces(string fileName);
    }
}
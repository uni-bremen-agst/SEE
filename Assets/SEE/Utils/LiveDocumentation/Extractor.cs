using System.Collections.Generic;
using SEE.Game.UI.LiveDocumantation;

namespace SEE.Utils.LiveDocumentation
{
   
    
    /// <summary>
    /// Represents 
    /// </summary>
    public interface Extractor
    {
        /// <summary>
        /// This method should extract the comments of an speciffic class in a specific source code file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="className"></param>
        /// <returns>A new <see cref="LiveDocumentationBuffer"/> containing the documentation </returns>
        LiveDocumentationBuffer ExtractComments(string fileName, string className);

        List<LiveDocumentationBuffer> ExtractMethods(string fileName, string className);
    }
}
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using SEE.Game.UI.LiveDocumentation.Buffer;
using SEE.Game.UI.Notification;

namespace SEE.Utils.LiveDocumentation
{
    /// <summary>
    /// This class is used as an abstraction for documentation parsers for diffrent file types.
    ///
    /// To add a new supported file type extend a class from <see cref="IExtractor"/> and append the file extension check in the constructor.
    /// </summary>
    public class FileParser
    {
        private IExtractor extractor;

        /// <summary>
        /// Constructs a new instance of a parser, which parses the file at the given file: <paramref name="fileName"/>.
        /// </summary>
        /// <param name="fileName">The name of the file which should be passed</param>
        /// <exception cref="FileNotFoundException">Is thrown, when the file doesn't exist</exception>
        public FileParser(string fileName)
        {
            // Check file extension and load the corresponding parser.
            if (fileName.EndsWith(".cs"))
            {
                extractor = new CSharpExtractor(fileName);
            }
            else
            {
                ShowNotification.Error("Unknown Filetype",
                    "The file extension is not supported by the LiveDocumentation in SEE");
            }
        }

        /// <summary>
        /// Parses the documentation of a specific class
        /// </summary>
        /// <param name="className">The name of the class</param>
        /// <exception cref="ClassNotFoundException">Will be thrown, when the class cant be found</exception>
        /// <returns>The parsed documentation of the class (<paramref name="className"/>) with its links (if present)</returns>
        [CanBeNull]
        public LiveDocumentationBuffer ParseClassDoc(string className)
        {
            return extractor.ExtractClassComments(className);
        }

        /// <summary>
        /// Parses the methods of a class
        /// </summary>
        /// <param name="className">The name of the class</param>
        /// <returns>A list of <see cref="LiveDocumentationClassMemberBuffer"/> with the documentation for each class member/method of the class (<paramref name="className"/>)</returns>
        [NotNull]
        public List<LiveDocumentationClassMemberBuffer> ParseClassMethods(string className)
        {
            return extractor.ExtractMethods(className);
        }

        /// <summary>
        /// Parses the names of all imported namespaces/packages in the file.
        /// </summary>
        /// <returns>A List<T> of the names of all imported namespace</returns>
        [NotNull]
        public List<string> ParseNamespaceImports()
        {
            return extractor.ExtractImportedNamespaces();
        }
    }
}

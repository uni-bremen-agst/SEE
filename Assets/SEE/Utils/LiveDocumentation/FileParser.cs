using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using SEE.Game.UI.LiveDocumantation;
using SEE.Game.UI.Notification;

namespace SEE.Utils.LiveDocumentation
{
    public class FileParser
    {
        private Extractor _extractor;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <exception cref="FileNotFoundException">Is thrown, when the file doesn't exist</exception>
        public FileParser(string fileName)
        {
            if (fileName.EndsWith(".cs"))
            {
                _extractor = new CSharpExtractor(fileName);
            }
            else
            {
                ShowNotification.Error("Unknown Filetype",
                    "The file extension is not supported by the LiveDocumentation in SEE");
            }
        }

        [CanBeNull]
        private Extractor GetExtractorForFile(string fileName)
        {
            if (fileName.EndsWith(".cs"))
            {
                return new CSharpExtractor(fileName);
            }

            return null;
        }

        private void ShowUnknownFileTypeErrorMessage()
        {
            ShowNotification.Error("Unknown Filetype",
                "The file extension is not supported by the LiveDocumentation in SEE");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="className"></param>
        /// <exception cref="ClassNotFoundException">Will be thrown, when the class cant be found</exception>
        /// <returns></returns>
        [CanBeNull]
        public LiveDocumentationBuffer ParseClassDoc(string className)
        {
            return _extractor.ExtractClassComments(className);
        }

        [NotNull]
        public List<LiveDocumentationClassMemberBuffer> ParseClassMethods(string className)
        {
            return _extractor.ExtractMethods(className);
        }

        [NotNull]
        public List<string> ParseNamespaceImports()
        {
            return _extractor.ExtractImportedNamespaces();
        }
    }
}
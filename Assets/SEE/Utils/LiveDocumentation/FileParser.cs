using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SEE.Game.UI.LiveDocumantation;
using SEE.Game.UI.Notification;

namespace SEE.Utils.LiveDocumentation
{
    public class FileParser
    {
        private Extractor _extractor;

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

        [CanBeNull]
        public LiveDocumentationBuffer ParseClassDoc(string fileName, string className)
        {
            return _extractor.ExtractClassComments(fileName, className);
        }

        [CanBeNull]
        public List<LiveDocumentationClassMemberBuffer> ParseClassMethods(string fileName, string className)
        {
            return _extractor.ExtractMethods(fileName, className);
        }

        public List<string> ParseNamespaceImports(string fileName)
        {
            return _extractor.ExtractImportedNamespaces(fileName);
        }
    }
}
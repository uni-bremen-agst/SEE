using System.Collections.Generic;
using JetBrains.Annotations;
using SEE.Game.UI.LiveDocumantation;
using SEE.Game.UI.Notification;

namespace SEE.Utils.LiveDocumentation
{
    public class FileParser
    {
        [CanBeNull]
        private static Extractor GetExtractorForFile(string fileName)
        {
            if (fileName.EndsWith(".cs"))
            {
                return new CSharpExtractor();
            }

            return null;
        }

        [CanBeNull]
        public static LiveDocumentationBuffer ParseClassDoc(string fileName, string className)
        {
            Extractor extractor = GetExtractorForFile(fileName);
            if (extractor == null)
            {
                ShowNotification.Error("Unknown Filetype",
                    "The file extension is not supported by the LiveDocumentation in SEE");
                return null;
            }

            return extractor.ExtractComments(fileName, className);
        }
        
        [CanBeNull]
        public static List<LiveDocumentationBuffer> ParseClassMethods(string fileName, string className)
        {
            Extractor extractor = GetExtractorForFile(fileName);
            if (extractor == null)
            {
                ShowNotification.Error("Unknown Filetype",
                    "The file extension is not supported by the LiveDocumentation in SEE");
                return null;
            }

            return extractor.ExtractMethods(fileName, className);
        }
    }
}
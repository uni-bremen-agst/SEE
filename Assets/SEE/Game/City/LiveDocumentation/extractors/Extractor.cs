using System.IO;
using JetBrains.Annotations;
using SEE.Game.City.LiveDocumentation.results;

namespace SEE.Game.City.LiveDocumentation
{
    public abstract class Extractor
    {
        private FileStream file;

        internal void OpenFile(string path)
        {
            file = File.Open(path, FileMode.Open);
        }

        [CanBeNull] public abstract IExtractionResult PerformExtraction(string path);
    }
}
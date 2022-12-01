using SEE.Game.City.LiveDocumentation.results;

namespace SEE.Game.City.LiveDocumentation
{
    /// <summary>
    /// This extractor extracts comments from files.
    /// </summary>
    public class CommentExtractor: Extractor
    {
        public override IExtractionResult PerformExtraction(string path)
        {
            ///TODO
            return new CommentExtractionResult("");
        }
    }
}
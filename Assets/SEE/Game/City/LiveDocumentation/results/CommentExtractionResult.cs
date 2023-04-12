
namespace SEE.Game.City.LiveDocumentation.results
{
    public class CommentExtractionResult: IExtractionResult
    {
        /// <summary>
        /// The comments that have been extracted
        /// </summary>
        private readonly string comments;

        public CommentExtractionResult(string comments)
        {
            this.comments = comments;
        }

        public object GetExtractionResult()
        {
            return comments;
        }
    }
}
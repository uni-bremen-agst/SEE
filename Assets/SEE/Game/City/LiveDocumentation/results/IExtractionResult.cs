namespace SEE.Game.City.LiveDocumentation.results
{
    /// <summary>
    /// The interface is used to unify the results of extractions. They can be shown within the Live Documentation UI.
    /// </summary>
    public interface IExtractionResult
    {
    object GetExtractionResult();
    }
}
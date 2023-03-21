namespace SEE.Game.UI.LiveDocumantation
{
    /// <summary>
    /// ILiveDocumentationBufferItem represents an item in the <see cref="LiveDocumentationBuffer"/>.
    ///
    /// This should wrap around 
    /// </summary>
    public interface ILiveDocumentationBufferItem
    {
        public string GetPrintableText();
    }
}
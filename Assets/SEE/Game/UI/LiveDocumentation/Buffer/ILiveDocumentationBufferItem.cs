namespace SEE.Game.UI.LiveDocumentation.Buffer
{
    /// <summary>
    /// ILiveDocumentationBufferItem represents an item in the <see cref="LiveDocumentationBuffer"/>.
    ///
    /// This can be a text (<see cref="LiveDocumentationBufferText"/>) or a link (<see cref="LiveDocumentationLink"/>)
    /// </summary>
    public interface ILiveDocumentationBufferItem
    {
        /// <summary>
        /// This method should calculate a printable text in the RTF-Format, so that it can be displayed in the TextMeshPro element.
        /// </summary>
        /// <returns>The buffer item in a printable RTF-Format</returns>
        public string GetPrintableText();
    }
}

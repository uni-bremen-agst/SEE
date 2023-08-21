namespace SEE.Game.UI.LiveDocumentation.Buffer
{
    /// <summary>
    /// Represents some text which is part of the documentation displayed in the LiveDocumentation window.
    /// </summary>
    public class LiveDocumentationBufferText : ILiveDocumentationBufferItem
    {
        /// <summary>
        /// The actual text which is should be displayed.
        ///
        /// This field is read only since altering after initialization isn't needed right now.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Returns the text which should be displayed
        /// </summary>
        /// <returns>Returns just <see cref="Text"/> </returns>
        public string GetPrintableText()
        {
            return Text;
        }

        /// <summary>
        /// Constructs a new instance of a <see cref="LiveDocumentationBufferText"/>
        /// </summary>
        /// <param name="text">The text which should be displayed in the LiveDocumentation window</param>
        public LiveDocumentationBufferText(string text)
        {
            Text = text;
        }
    }
}

namespace SEE.Game.UI.LiveDocumentation.Buffer
{
    /// <summary>
    /// 
    /// </summary>
    public class LiveDocumentationBufferText : ILiveDocumentationBufferItem
    {
        public string Text { get; set; }

        public string GetPrintableText()
        {
            return Text;
        }

        public LiveDocumentationBufferText()
        {
        }

        public LiveDocumentationBufferText(string text)
        {
            Text = text;
        }
    }
}
namespace SEE.Game.UI.LiveDocumantation
{
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
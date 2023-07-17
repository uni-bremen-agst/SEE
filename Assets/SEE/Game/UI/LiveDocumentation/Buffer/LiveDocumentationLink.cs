namespace SEE.Game.UI.LiveDocumentation.Buffer
{
    /// <summary>
    /// Representing a link to another class in the LiveDocumentation
    /// </summary>
    public class LiveDocumentationLink : ILiveDocumentationBufferItem
    {
        /// <summary>
        /// The path to the Class in the Project
        /// </summary>
        public string TargetPath { get; set; }

        /// <summary>
        /// The name of the link, which should be displayed in the LiveDocumentation window.
        ///
        /// Usually it is the shortened class Name.   
        /// </summary>
        public string LinkName { get; set; }
        
        /// <summary>
        /// Constructs a new instance of a <see cref="LiveDocumentationLink"/>
        /// </summary>
        /// <param name="targetPath">The path the link should point to</param>
        /// <param name="linkName">The name of the link which should be displayed</param>
        public LiveDocumentationLink(string targetPath, string linkName)
        {
            this.TargetPath = targetPath;
            this.LinkName = linkName;
        }

        /// <summary>
        /// Creates the link to the specific class in the RTF Format. 
        /// </summary>
        /// <returns></returns>
        public string GetPrintableText()
        {
            return $"<link=\"{this.TargetPath}\"> <color=#0000FF>{this.LinkName}</color></link>";
        }
    }
}
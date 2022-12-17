using System.Collections.Generic;
using DynamicPanels;

namespace SEE.Game.UI.LiveDocumentation
{
    /// <summary>
    /// LiveDocumentationSpace
    /// </summary>
    public class LiveDocumentationSpace : PlatformDependentComponent
    {
        
        public DynamicPanelsCanvas PanelsCanvas { get; set; }

        private List<LiveDocumentationWindow> _openedWindows;

        public static LiveDocumentationSpace Instance => GetInstance();

        private static LiveDocumentationSpace _ClassInstance;
        
        private static LiveDocumentationSpace GetInstance()
        {
            if (_ClassInstance == null)
                _ClassInstance = new LiveDocumentationSpace();
            return _ClassInstance;
        }

        /// <summary>
        /// Private to to comply with singleton pattern 
        /// </summary>
        private LiveDocumentationSpace()
        {
        }
    }
}
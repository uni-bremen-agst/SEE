namespace SEE.Game.UI.LiveDocumentation
{
    /// <summary>
    /// This class represents the live documentation windows. <p/>
    /// 
    /// In this window the documentation for a specific code module is shown.
    /// Also the function signatures are shown.
    ///
    /// In all of these any link to a another module (eg. '@link' in JavaDoc) produces a clickable link in the window. <p/>
    ///
    /// This class is meant to be applied in the <see cref="SEE.Controls.Actions.LiveDocumentationAction"/>
    /// And then the attributes of these class are set, so they can then be applied to the Unity Object (the UI Canvas).
    ///
    /// So something like that:
    /// <code>
    /// // I'm in LiveDocumentationAction
    /// docWin = selectedNode.gameObject.AddComponent &lt;LiveDocumentationWindow&gt;();
    /// docWin.ClassName = ...
    /// ...
    /// // And so on
    /// </code> 
    /// 
    /// This class is split in multiple files (like the CodeWindow).
    /// Currently the following files are part of this partial class:
    /// <ul>
    ///     <li>LiveDocumentationWindow.cs</li>
    ///     <li>LiveDocumentationWindowDesktop.cs</li>
    /// </ul>
    /// </summary>
    public partial class LiveDocumentationWindow : PlatformDependentComponent
    {
        /// <summary>
        /// Path to the UI prefab 
        /// </summary>
        private const string PREFAB_PATH = "Prefabs/UI/LiveDocumentation/LiveDocumentation";

        public struct LiveDocumentationWindowValue
        {
            public string TypeName { get; private set; }
        }
    }
}
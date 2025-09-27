using SEE.GameObjects;

namespace SEE.UI.Window.PropertyWindow
{
    /// <summary>
    /// A property window showing the properties of an author.
    /// </summary>
    internal class AuthorPropertyWindow : PropertyWindow
    {
        /// <summary>
        /// The author whose properties are shown in this window.
        /// </summary>
        public AuthorSphere author;

        /// <inheritdoc/>
        protected override void CreateItems()
        {

        }
    }
}

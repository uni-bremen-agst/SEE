using TMPro;

namespace SEE.Game.UI.ConfigMenu
{
    /// <summary>
    /// Controls the active page (mostly its headline).
    /// </summary>
    public class PageController : DynamicUIBehaviour
    {
        /// <summary>
        /// The headline text of this page.
        /// </summary>
        public string headlineText;

        private TextMeshProUGUI _headline;

        public void Start()
        {
            MustGetComponentInChild("Heading", out _headline);
            _headline.text = headlineText;
        }
    }
    
}


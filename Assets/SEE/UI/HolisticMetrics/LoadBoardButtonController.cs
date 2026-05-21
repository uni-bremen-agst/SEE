using UnityEngine;

namespace SEE.UI.HolisticMetrics
{
    /// <summary>
    /// Controller for a little button that appears in the player's UI. This is used to start the dialog for saving
    /// boards to disk and the dialog for loading boards from disk.
    /// </summary>
    public class LoadBoardButtonController : MonoBehaviour
    {
        /// <summary>
        /// Whether this class has a click in store that wasn't yet fetched.
        /// </summary>
        private bool gotClick;

        /// <summary>
        /// When the player clicks the button, we will mark <see cref="gotClick"/> as true.
        /// </summary>
        public void OnClick()
        {
            gotClick = true;
        }

        /// <summary>
        /// Fetches a click.
        /// </summary>
        /// <returns>The value of <see cref="gotClick"/>.</returns>
        internal bool IsClicked()
        {
            if (gotClick)
            {
                gotClick = false;
                return true;
            }

            return false;
        }
    }
}

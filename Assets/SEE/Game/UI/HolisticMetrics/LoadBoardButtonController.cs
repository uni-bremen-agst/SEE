using UnityEngine;

namespace SEE.Game.UI.HolisticMetrics
{
    /// <summary>
    /// A little button that opens the dialog for loading / saving boards (although the name implies it might only be
    /// used for the board loading process, it is actually used for both).
    /// </summary>
    public class LoadBoardButtonController : MonoBehaviour
    {
        /// <summary>
        /// Whether this class has a click in store that wasn't yet fetched.
        /// </summary>
        private bool gotClick;
        
        /// <summary>
        /// This gets called when the button registers a click.
        /// </summary>
        public void OnClick()
        {
            gotClick = true;
        }

        /// <summary>
        /// Fetches a click.
        /// </summary>
        /// <returns>The value of <see cref="gotClick"/></returns>
        internal bool GetClick()
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

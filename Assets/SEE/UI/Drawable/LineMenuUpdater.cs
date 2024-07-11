using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.UI.Menu.Drawable;
using System.Collections;
using UnityEngine;

namespace SEE.UI.Drawable
{
    /// <summary>
    /// This class resolves a display issue with the <see cref="HorizontalSelector"/>.
    /// During the initial execution, default values are otherwise displayed
    /// because the selectors update too late.
    /// </summary>
    public class LineMenuUpdater : MonoBehaviour
    {

        /// The update is only performed during the initial use of the menu.
        void Start()
        {
            StartCoroutine(Updater());
        }

        /// <summary>
        /// Performs the update.
        /// </summary>
        /// <returns>waits 1 ms.</returns>
        public IEnumerator Updater()
        {
            yield return new WaitForSeconds(0.1f);
            LineMenu.RefreshHorizontalSelectors();
        }
    }
}
using Michsky.UI.ModernUIPack;
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
        private void Start()
        {
            StartCoroutine(Updater());
        }

        /// <summary>
        /// Performs the update.
        /// </summary>
        /// <returns>Waits 100 ms.</returns>
        private IEnumerator Updater()
        {
            yield return new WaitForSeconds(0.1f);
            LineMenu.RefreshHorizontalSelectors();
        }
    }
}

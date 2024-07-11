using System.Collections;
using UnityEngine;

namespace SEE.Game.Drawable.ValueHolders
{
    /// <summary>
    /// Holder for the associated surface page for the drawable type objects.
    /// </summary>
    public class AssociatedPageHolder : MonoBehaviour
    {
        /// <summary>
        /// The associted page.
        /// </summary>
        private int associatedPage;

        /// <summary>
        /// Associted page property.
        /// </summary>
        public int AssociatedPage
        {
            get { return associatedPage; }
            set { associatedPage = value;}
        }
    }
}
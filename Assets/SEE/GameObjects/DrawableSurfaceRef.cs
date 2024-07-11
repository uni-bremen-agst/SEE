using SEE.DataModel.Drawable;
using System.Collections;
using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// A reference to a drawable surface that can be attached to a game object as a component.
    /// </summary>
    public class DrawableSurfaceRef : MonoBehaviour
    {
        /// <summary>
        /// The drawable surface it referenced.
        /// </summary>
        public DrawableSurface surface;

        /// <summary>
        /// The property for the drawable surface.
        /// </summary>
        public DrawableSurface Value
        {
            get => surface;
            set => surface = value;
        }
    }
}
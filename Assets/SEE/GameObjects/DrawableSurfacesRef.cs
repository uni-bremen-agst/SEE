using Assets.SEE.UI.Window.DrawableManagerWindow;
using SEE.Game;
using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// Class that provides a list of all drawable surfaces in the scene. 
    /// This is needed for the <see cref="DrawableManagerWindow"> to detect and record newly added or removed surfaces.
    /// </summary>
    public class DrawableSurfacesRef : MonoBehaviour
    {
        /// <summary>
        /// The list of all surfaces in the scene.
        /// </summary>
        private readonly DrawableSurfaces surfacesInScene;

        /// <summary>
        /// Getter property for the list.
        /// </summary>
        public DrawableSurfaces SurfacesInScene { get { return surfacesInScene; } }

        /// <summary>
        /// Constructor.
        /// </summary>
        public DrawableSurfacesRef()
        {
            surfacesInScene = new DrawableSurfaces();
        }
    }
}
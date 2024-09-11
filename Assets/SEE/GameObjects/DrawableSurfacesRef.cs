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
        /// Getter property for the list.
        /// </summary>
        public DrawableSurfaces SurfacesInScene { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public DrawableSurfacesRef()
        {
            SurfacesInScene = new DrawableSurfaces();
        }
    }
}

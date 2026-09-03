using UnityEngine;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// A reference to a drawable surface that can be attached to a game object as a component.
    /// </summary>
    /// <remarks>This component is attached to a player via DesktopPlayer.prefab./></remarks>
    public class DrawableSurfaceRef : MonoBehaviour
    {
        /// <summary>
        /// The drawable surface it referenced.
        /// </summary>
        public DrawableSurface Surface;
    }
}

using SEE.Game;
using SEE.Game.Drawable;
using UnityEngine;

namespace Assets.SEE.Game.Drawable
{
    /// <summary>
    /// This component must be assigned to a drawable surface.
    /// It is used to manage the list of surfaces in the scene.
    /// </summary>
    /// <remarks>This component is meant to be attached to a drawable surface.</remarks>
    public class DrawableSurfaceController : MonoBehaviour
    {
        /// <summary>
        /// With this method, the surface is added to the list as soon as it is created.
        /// </summary>
        void Awake()
        {
            if (!ValueHolder.DrawableSurfaces.Contains(gameObject)
                && gameObject.CompareTag(Tags.Drawable)) 
            {
                ValueHolder.DrawableSurfaces.Add(gameObject);
            }        
        }

        /// <summary>
        /// Removes the object from the list when it is destroyed.
        /// </summary>
        void OnDestroy()
        {
            ValueHolder.DrawableSurfaces.Remove(gameObject);
        }
    }
}
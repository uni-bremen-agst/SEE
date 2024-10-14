using SEE.DataModel.Drawable;
using SEE.GO;
using UnityEngine;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// This component must be assigned to a drawable surface.
    /// It is used to manage the list of surfaces in the scene.
    /// </summary>
    /// <remarks>This component is meant to be attached to a drawable surface.</remarks>
    public class DrawableSurfaceController : MonoBehaviour
    {
        /// <summary>
        /// Status indicating whether the instantiation of the <see cref="LocalPlayer.Instance">
        /// needs to be awaited.
        /// </summary>
        private bool mustWaitForPlayerInstantiate = false;

        /// <summary>
        /// With this method, the surface is added to the list as soon as it is created.
        /// </summary>
        private void Awake()
        {
            if (!ValueHolder.DrawableSurfaces.Contains(gameObject)
                && gameObject.CompareTag(Tags.Drawable))
            {
                ValueHolder.DrawableSurfaces.Add(gameObject);

                DrawableSurfaceRef reference = gameObject.AddComponent<DrawableSurfaceRef>();
                reference.Surface = new DrawableSurface(gameObject);
                if (LocalPlayer.Instance != null
                    && LocalPlayer.TryGetDrawableSurfaces(out DrawableSurfaces surfaces))
                {
                    surfaces.Add(reference.Surface);
                }
                else
                {
                    mustWaitForPlayerInstantiate = true;
                }
            }
        }

        /// <summary>
        /// At the start of the visualization, the <see cref="LocalPlayer.Instance"/> is not yet set,
        /// so it is necessary to wait until it is set to avoid an error.
        /// </summary>
        private void Update()
        {
            if (mustWaitForPlayerInstantiate
                && LocalPlayer.Instance != null
                && LocalPlayer.TryGetDrawableSurfaces(out DrawableSurfaces surfaces))
            {
                DrawableSurfaceRef reference = gameObject.GetComponent<DrawableSurfaceRef>();
                surfaces.Add(reference.Surface);
                mustWaitForPlayerInstantiate = false;
            }
        }

        /// <summary>
        /// Removes the object from the list when it is destroyed.
        /// </summary>
        void OnDestroy()
        {
            ValueHolder.DrawableSurfaces.Remove(gameObject);
            if (LocalPlayer.Instance != null
                && LocalPlayer.TryGetDrawableSurfaces(out DrawableSurfaces surfaces))
            {
                surfaces.Remove(gameObject.GetComponent<DrawableSurfaceRef>().Surface);
            }
        }
    }
}
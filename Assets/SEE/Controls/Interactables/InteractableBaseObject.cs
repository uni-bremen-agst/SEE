using SEE.Game;
using SEE.Game.SceneManipulation;
using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// Super class of the behaviours of game objects the player interacts with.
    /// </summary>
    public abstract class InteractableObjectBase : MonoBehaviour
    {
        /// <summary>
        /// The layer the object is on if it is interactable.
        /// </summary>
        public abstract int InteractableLayer { get; }

        /// <summary>
        /// The layer the object is on if it is non-interactable, i.e., inactivated.
        /// </summary>
        public abstract int NonInteractableLayer { get; }

        /// <summary>
        /// Custom color for the laser pointer if it is aimed at the object and <see cref="IsInteractable(Vector3?)"/> returns <c>true</c>.
        /// </summary>
        public abstract Color? HitColor { get; }

        /// <summary>
        /// Whether the object is partially interactable, i.e., if it is not entirely within its portal.
        /// <para>
        /// This usually means:
        /// <list type="bullet">
        /// <item>
        /// A ray is cast using
        /// <see cref="SEE.Utils.Raycasting.RaycastInteractableAuxiliaryObject(out RaycastHit, out InteractableAuxiliaryObject, bool, float)"/>
        /// with <c>requireInteractable = false</c>, and
        /// </item><item>
        /// the hitpoint is handed to <see cref="IsInteractable(Vector3?)"/> to make sure the given interaction point is within the portal.
        /// </item>
        /// </list>
        /// </para>
        /// </summary>
        public bool PartiallyInteractable = false;

        /// <summary>
        /// Whether the object is currently interactable (at the given hit point).
        /// <para>
        /// If the object has a portal:
        /// <list type="bullet">
        /// <item>
        /// If a hit point is given, it is checked against the portal bounds.
        /// </item><item>
        /// Else, the object boundaries are checked against the portal bounds.
        /// </item></list>
        /// If either given point or the object's bounds are outside the portal bounds, the object is not interactable.
        /// </para>
        /// </summary>
        /// <param name="point">The hit point on the object.</param>
        public bool IsInteractable(Vector3? point = null)
        {
            if (Portal.GetPortal(gameObject, out Vector2 leftFront, out Vector2 rightBack))
            {
                Bounds2D portalBounds = Bounds2D.FromPortal(leftFront, rightBack);

                // Check interaction point
                if (point != null)
                {
                    if (!portalBounds.Contains(point.Value))
                    {
                        return false;
                    }
                }
                // Check if the object is (partially) within the portal bounds.
                else
                {
                    Bounds2D bounds = new Bounds2D(gameObject);
                    if (!portalBounds.Contains(bounds))
                    {
                        return false;
                    }
                }
            }
            // Potential further checks go here
            return true;
        }

        /// <summary>
        /// Updates the layer of the <see cref="GameObject"/> to match the current interactable state.
        /// <para>
        /// Switches between layser <see cref="InteractableLayer"/> and <see cref="NonInteractableLayer"/>
        /// based on the result of <see cref="IsInteractable"/>.
        /// </para>
        /// </summary>
        public void UpdateLayer()
        {
            if (IsInteractable())
            {
                if (gameObject.layer == NonInteractableLayer || gameObject.layer == Layers.Default)
                {
                    gameObject.layer = InteractableLayer;
                }
            }
            else
            {
                if (gameObject.layer == InteractableLayer || gameObject.layer == Layers.Default)
                {
                    gameObject.layer = NonInteractableLayer;
                }
            }
        }
    }
}

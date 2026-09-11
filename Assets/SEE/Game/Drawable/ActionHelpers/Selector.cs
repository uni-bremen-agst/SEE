using SEE.Controls;
using SEE.Game.Drawable.Configurations;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.Drawable.ActionHelpers
{
    /// <summary>
    /// This class provides functions for selecting <see cref="DrawableType"/> objects.
    /// </summary>
    public static class Selector
    {
        /// <summary>
        /// Performs the selection. For this, a drawable type object must be clicked
        /// with the left mouse button. If child selection is enabled, nested child
        /// objects can be resolved to their owning drawable type object.
        /// </summary>
        /// <param name="selectedObj">The selected object.</param>
        /// <param name="oldSelectedObj">The object selected during the previous action.</param>
        /// <param name="mouseWasReleased">
        /// Whether the mouse button was released after the previous selection.
        /// </param>
        /// <param name="hasDrawable">
        /// Whether the selected object must belong to a drawable surface.
        /// </param>
        /// <param name="isDrawableType">
        /// Whether the selected object must resolve to a drawable type object.
        /// </param>
        /// <param name="collisionDetection">
        /// Whether collision detection should be enabled for the selected object.
        /// </param>
        /// <param name="setOldObject">
        /// Whether the selected object should be stored as the previously selected object.
        /// </param>
        /// <param name="allowSelectViaChild">
        /// Whether the owning object may be selected through one of its child objects.
        /// </param>
        /// <returns>True if an object was successfully selected; otherwise, false.</returns>
        public static bool SelectObject(
            ref GameObject selectedObj,
            ref GameObject oldSelectedObj,
            ref bool mouseWasReleased,
            bool hasDrawable,
            bool isDrawableType,
            bool collisionDetection = false,
            bool setOldObject = true,
            bool allowSelectViaChild = true)
        {
            if (!SEEInput.LeftMouseInteraction()
                || selectedObj != null
                || !Raycasting.RaycastAnything(out RaycastHit raycastHit))
            {
                return false;
            }

            GameObject hitObject = raycastHit.collider.gameObject;
            GameObject selectableObject = ResolveSelectableObject(
                hitObject,
                isDrawableType,
                allowSelectViaChild);

            if (selectableObject == null)
            {
                return false;
            }

            if (oldSelectedObj != null
                && oldSelectedObj == selectableObject
                && !mouseWasReleased)
            {
                return false;
            }

            if (hasDrawable && !GameFinder.HasDrawableSurface(hitObject))
            {
                return false;
            }

            if (isDrawableType
                && !Tags.DrawableTypes.Contains(selectableObject.tag))
            {
                return false;
            }

            selectedObj = selectableObject;

            if (setOldObject)
            {
                oldSelectedObj = selectedObj;
            }

            selectedObj.AddOrGetComponent<BlinkEffect>();
            mouseWasReleased = false;

            /// The rigidbody and the collision controller are needed to detect
            /// a collision with a border.
            if (collisionDetection)
            {
                CollisionDetectionManager.Enable(selectedObj);
            }

            return true;
        }

        /// <summary>
        /// Resolves the object that should be selected for the given raycast hit.
        /// If a drawable type is required, nested children are resolved recursively
        /// to their owning drawable type object.
        /// </summary>
        /// <param name="hitObject">The object hit by the raycast.</param>
        /// <param name="isDrawableType">
        /// Whether the result must resolve to a drawable type object.
        /// </param>
        /// <param name="allowSelectViaChild">
        /// Whether selection through child objects is allowed.
        /// </param>
        /// <returns>
        /// The object that should be selected, or null if no selectable drawable type
        /// can be resolved.
        /// </returns>
        internal static GameObject ResolveSelectableObject(
            GameObject hitObject,
            bool isDrawableType,
            bool allowSelectViaChild)
        {
            if (hitObject == null)
            {
                return null;
            }

            if (!allowSelectViaChild
                || Tags.DrawableTypes.Contains(hitObject.tag))
            {
                return hitObject;
            }

            if (isDrawableType)
            {
                return GameFinder.GetDrawableTypObject(hitObject);
            }

            return hitObject.transform.parent != null
                ? hitObject.transform.parent.gameObject
                : hitObject;
        }

        /// <summary>
        /// Selects an object if it is placed on a drawable surface.
        /// </summary>
        /// <param name="raycastHit">The detected <see cref="RaycastHit"/>.</param>
        /// <returns>Status indicating whether the selection was successful or not.</returns>
        public static bool SelectQueryHasDrawableSurface(out RaycastHit raycastHit)
        {
            if (SEEInput.LeftMouseInteraction()
                && Raycasting.RaycastAnything(out RaycastHit hit)
                && GameFinder.HasDrawableSurface(hit.collider.gameObject))
            {
                raycastHit = hit;
                return true;
            }
            raycastHit = new RaycastHit();
            return false;
        }

        /// <summary>
        /// Selects an object if it is placed on a drawable surface.
        /// </summary>
        /// <param name="raycastHit">The detected <see cref="RaycastHit"/>.</param>
        /// <param name="leftMouseButton">Status indicating whether the left or right mouse button should be used.</param>
        /// <param name="onlyLeftDown">True if only the down click should be registered. Not holding them.</param>
        /// <returns>Status indicating whether the selection was successful or not.</returns>
        public static bool SelectQueryHasOrIsDrawableSurface(out RaycastHit raycastHit, bool leftMouseButton = true, bool onlyLeftDown = false)
        {
            if ((SEEInput.LeftMouseInteraction() && leftMouseButton && !onlyLeftDown
                 || SEEInput.LeftMouseDown() && leftMouseButton && onlyLeftDown
                 || SEEInput.RightMouseInteraction() && !leftMouseButton)
                && Raycasting.RaycastAnything(out RaycastHit hit)
                && GameFinder.IsOrHasDrawableSurface(hit.collider.gameObject))
            {
                raycastHit = hit;
                return true;
            }
            raycastHit = new RaycastHit();
            return false;
        }

        /// <summary>
        /// Selects an object if it has a tag of a <see cref="DrawableType">.
        /// </summary>
        /// <param name="raycastHit">The detected <see cref="RaycastHit"/>.</param>
        /// <returns>Status indicating whether the selection was successful or not.</returns>
        public static bool SelectQueryIsDrawableType(out RaycastHit raycastHit)
        {
            if (SEEInput.LeftMouseInteraction()
                && Raycasting.RaycastAnything(out RaycastHit hit)
                && Tags.DrawableTypes.Contains(hit.collider.gameObject.tag))
            {
                raycastHit = hit;
                return true;
            }
            raycastHit = new RaycastHit();
            return false;
        }

        /// <summary>
        /// Selects an object if it is placed on a drawable surface without a mouse click.
        /// </summary>
        /// <param name="raycastHit">The detected <see cref="RaycastHit"/>.</param>
        /// <returns>Status indicating whether the selection was successful or not.</returns>
        public static bool SelectQueryHasOrIsSurfaceWithoutMouse(out RaycastHit raycastHit)
        {
            if (Raycasting.RaycastAnything(out RaycastHit hit)
                && GameFinder.IsOrHasDrawableSurface(hit.collider.gameObject))
            {
                raycastHit = hit;
                return true;
            }
            raycastHit = new RaycastHit();
            return false;
        }
    }
}

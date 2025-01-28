using SEE.Controls;
using SEE.Controls.Actions;
using SEE.Game.Drawable.Configurations;
using SEE.GO;
using SEE.UI.Drawable;
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
        /// Performs the selection. For this, a Drawable type object must be clicked with the left mouse button.
        /// </summary>
        /// <param name="selectedObj">The object for the selected object.</param>
        /// <param name="oldSelectedObj">The object of the previous selected object.</param>
        /// <param name="mouseWasReleased">The state, if the mouse was released.</param>
        /// <param name="Surface">The drawable surface of the executed drawable action.</param>
        /// <param name="hasDrawable">Status indicating whether the hasDrawable check should be performed.</param>
        /// <param name="isDrawableType">Status indicating whether the hasDrawable check should be performed.</param>
        /// <param name="collisionDetection">Check whether the tag of the selected object should be a <see cref="DrawableType"> tag.</param>
        /// <param name="action">The action to be taken if data needs to be reset after an action change.</param>
        /// <param name="setOldObject">Status indicating whether the previously selected object should be set.</param>
        /// <paramref name="allowSelectViaChild"/>Whether the object can selected via a child object.<paramref name="allowSelectViaChild"/>
        /// <returns>Status indicating whether the selection was successful or not.</returns>
        public static bool SelectObject(ref GameObject selectedObj, ref GameObject oldSelectedObj, ref bool mouseWasReleased, GameObject Surface,
            bool hasDrawable, bool isDrawableType, bool collisionDetection = false, ActionStateType action = null, bool setOldObject = true,
            bool allowSelectViaChild = true)
        {
            if (SEEInput.LeftMouseInteraction()
                && selectedObj == null
                && Raycasting.RaycastAnything(out RaycastHit raycastHit)
                && (oldSelectedObj == null
                    || oldSelectedObj != raycastHit.collider.gameObject && oldSelectedObj != raycastHit.collider.transform.parent.gameObject
                    || oldSelectedObj == raycastHit.collider.gameObject && mouseWasReleased
                    || oldSelectedObj == raycastHit.collider.transform.parent.gameObject && mouseWasReleased)
                && (!hasDrawable
                    || hasDrawable && GameFinder.HasDrawableSurface(raycastHit.collider.gameObject))
                && (!isDrawableType
                    || isDrawableType && Tags.DrawableTypes.Contains(raycastHit.collider.gameObject.tag)
                    || isDrawableType && allowSelectViaChild && raycastHit.collider.transform.parent != null &&
                        Tags.DrawableTypes.Contains(raycastHit.collider.transform.parent.gameObject.tag)))
            {
                selectedObj = raycastHit.collider.gameObject;
                if (allowSelectViaChild && !Tags.DrawableTypes.Contains(selectedObj.tag))
                {
                    selectedObj = selectedObj.transform.parent.gameObject;
                }
                if (setOldObject)
                {
                    oldSelectedObj = selectedObj;
                }
                selectedObj.AddOrGetComponent<BlinkEffect>();
                mouseWasReleased = false;

                /// If the action should reset after changing the actions.
                if (action != null)
                {
                    Surface.AddOrGetComponent<ValueResetter>().SetAllowedState(action);
                }

                /// The rigidbody and the collision controller are needed to detect a collision with a border.
                if (collisionDetection)
                {
                    CollisionDetectionManager.Enable(selectedObj);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Selects an object if it is placed on a drawable surface.
        /// </summary>
        /// <param name="raycastHit">The detected <see cref="RaycastHit">.</param>
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
        /// <param name="raycastHit">The detected <see cref="RaycastHit">.</param>
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
        /// <param name="raycastHit">The detected <see cref="RaycastHit">.</param>
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
        /// <param name="raycastHit">The detected <see cref="RaycastHit">.</param>
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

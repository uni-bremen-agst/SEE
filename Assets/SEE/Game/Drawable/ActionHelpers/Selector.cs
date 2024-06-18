using SEE.Controls.Actions;
using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.GO;
using SEE.UI.Drawable;
using SEE.Utils;
using UnityEngine;

namespace Assets.SEE.Game.Drawable.ActionHelpers
{
    /// <summary>
    /// This class provides a function for selecting <see cref="DrawableType"/> objects.
    /// </summary>
    public static class Selector
    {
        /// <summary>
        /// Performs the selection. For this, a Drawable type object must be clicked with the left mouse button.
        /// </summary>
        /// <param name="selectedObj">The object for the selected object.</param>
        /// <param name="oldSelectedObj">The object of the previous selected object.</param>
        /// <param name="mouseWasReleased">The state, if the mouse was released.</param>
        /// <param name="Canvas">The canvas of the executed drawable action.</param>
        /// <param name="hasDrawable">Status indicating whether the hasDrawable check should be performed.</param>
        /// <param name="isDrawableType">Status indicating whether the hasDrawable check should be performed.</param>
        /// <param name="collisionDetection">Check whether the tag of the selected object should be a <see cref="DrawableType"> tag.</param>
        /// <param name="action">The action to be taken if data needs to be reset after an action change.</param>
        /// <param name="setOldObject">Status indicating whether the previously selected object should be set.</param>
        /// <returns>Status indicating whether the selection was successful or not.</returns>
        public static bool SelectObject(ref GameObject selectedObj, ref GameObject oldSelectedObj, ref bool mouseWasReleased, GameObject Canvas, 
            bool hasDrawable, bool isDrawableType, bool collisionDetection = false, ActionStateType action = null, bool setOldObject = true)
        {
            if (Queries.LeftMouseInteraction()
                && selectedObj == null
                && Raycasting.RaycastAnything(out RaycastHit raycastHit)
                && (oldSelectedObj == null 
                    || oldSelectedObj != raycastHit.collider.gameObject
                    || (oldSelectedObj == raycastHit.collider.gameObject && mouseWasReleased))
                && (!hasDrawable 
                    || (hasDrawable && GameFinder.HasDrawable(raycastHit.collider.gameObject)))
                && (!isDrawableType
                    || (isDrawableType && Tags.DrawableTypes.Contains(raycastHit.collider.gameObject.tag))))
            {
                selectedObj = raycastHit.collider.gameObject;
                if (setOldObject)
                {
                    oldSelectedObj = selectedObj;
                }
                selectedObj.AddOrGetComponent<BlinkEffect>();
                mouseWasReleased = false;

                /// If the action should reset after changing the actions.
                if (action != null)
                {
                    Canvas.AddOrGetComponent<ValueResetter>().SetAllowedState(action);
                }

                /// The rigidbody and the collision controller are needed to detect a collision with a border.
                if (collisionDetection)
                {
                    CollisionDetectionEnabler.Enable(selectedObj);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Selects an object if it is placed on a drawable.
        /// </summary>
        /// <param name="hitObject">The selected object</param>
        /// <returns>Status indicating whether the selection was successful or not.</returns>
        public static bool SelectQueryHasDrawable(out GameObject hitObject)
        {
            if (Queries.LeftMouseInteraction()
                && Raycasting.RaycastAnything(out RaycastHit raycastHit)
                && GameFinder.HasDrawable(raycastHit.collider.gameObject)) 
            {
                hitObject = raycastHit.collider.gameObject;
                return true;
            }
            hitObject = null;
            return false;
        }

        /// <summary>
        /// Selects an object if it has a tag of a <see cref="DrawableType">.
        /// </summary>
        /// <param name="hitObject">The selected object</param>
        /// <returns>Status indicating whether the selection was successful or not.</returns>
        public static bool SelectQueryIsDrawableType(out GameObject hitObject)
        {
            if (Queries.LeftMouseInteraction()
                && Raycasting.RaycastAnything(out RaycastHit raycastHit)
                && Tags.DrawableTypes.Contains(raycastHit.collider.gameObject.tag))
            {
                hitObject = raycastHit.collider.gameObject;
                return true;
            }
            hitObject = null;
            return false;
        }
    }
}
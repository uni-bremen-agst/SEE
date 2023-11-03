using Assets.SEE.Game.Drawable;
using SEE.Net.Actions.Drawable;
using SEE.Game;
using System;
using System.Collections;
using UnityEngine;
using System.Linq;
using SEE.Game.Drawable;

namespace Assets.SEE.Game.UI.Drawable
{
    /// <summary>
    /// The border trigger controller ensures that the 
    /// drawable type objects stay within the Drawables 
    /// and moves them in the respective direction when necessary.
    /// </summary>
    public class BorderTriggerController : MonoBehaviour
    {
        /// <summary>
        /// Method that will be executed when a collision stay.
        /// It moves the collision object back in the drawable area.
        /// </summary>
        /// <param name="other">The object that causes the collision.</param>
        private void OnTriggerStay(Collider other)
        {
            if (Tags.DrawableTypes.Contains(other.gameObject.tag) && 
                GameFinder.GetHighestParent(gameObject).Equals(GameFinder.GetHighestParent(other.gameObject)))
            {
                GameObject drawable = GameFinder.FindDrawable(other.gameObject);
                string drawableParentName = GameFinder.GetDrawableParentName(drawable);
                Transform transform = other.gameObject.transform;
                Vector3 eulerAngles = transform.localEulerAngles;
                transform.localEulerAngles = Vector3.zero;
                float moveValue = 0.01f;
                Vector3 newPosition = transform.position;
                switch (tag)
                {
                    case Tags.Top:
                        newPosition -= transform.up * moveValue;
                        break;
                    case Tags.Bottom:
                        newPosition += transform.up * moveValue;
                        break;
                    case Tags.Left:
                        newPosition += transform.right * moveValue;
                        break;
                    case Tags.Right:
                        newPosition -= transform.right * moveValue;
                        break;
                    default:
                        break;
                }
                transform.localEulerAngles = eulerAngles;
                GameMoveRotator.MoveObject(other.gameObject, newPosition);
                new MoveNetAction(drawable.name, drawableParentName, other.name, newPosition).Execute();
            }
        }
    }
}
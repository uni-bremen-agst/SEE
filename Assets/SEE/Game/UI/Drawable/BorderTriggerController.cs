using Assets.SEE.Game.Drawable;
using SEE.Game;
using SEE.Game.Drawable;
using SEE.Net.Actions.Drawable;
using UnityEngine;

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
        /// It moves the collision object back into the drawable area.
        /// </summary>
        /// <param name="other">The object that causes the collision.</param>
        private void OnTriggerStay(Collider other)
        {
            if (Tags.DrawableTypes.Contains(other.gameObject.tag) && 
                GameFinder.GetHighestParent(gameObject).Equals(GameFinder.GetHighestParent(other.gameObject)))
            {
                GameObject drawable = GameFinder.GetDrawable(other.gameObject);
                string drawableParentName = GameFinder.GetDrawableParentName(drawable);

                if (other.gameObject.CompareTag(Tags.MindMapNode))
                {
                    Rigidbody[] bodys = GameFinder.GetAttachedObjectsObject(other.gameObject).GetComponentsInChildren<Rigidbody>();
                    MMNodeValueHolder valueHolder = other.gameObject.GetComponent<MMNodeValueHolder>();
                    foreach(Rigidbody body in bodys)
                    {
                        if (body.gameObject == other.gameObject || 
                            valueHolder.GetAllChildren().ContainsKey(body.gameObject) || 
                            valueHolder.GetAllParentAncestors().Contains(body.gameObject))
                        {
                            MoveBack(body.gameObject, drawable, drawableParentName);
                        }
                    }
                } else
                {
                    MoveBack(other.gameObject, drawable, drawableParentName);
                }
            }
        }

        /// <summary>
        /// This method moves the object back.
        /// </summary>
        /// <param name="objToMove">The object to move</param>
        /// <param name="drawable">The drawable of the object.</param>
        /// <param name="drawableParentName">The parent name of the drawable.</param>
        private void MoveBack(GameObject objToMove, GameObject drawable, string drawableParentName)
        {
            Transform transform = objToMove.transform;
            Vector3 eulerAngles = transform.localEulerAngles;
            transform.localEulerAngles = Vector3.zero;
            float moveValue = 0.01f;
            Vector3 newPosition = transform.localPosition;
            switch (tag)
            {
                case Tags.Top:
                    newPosition -= Vector3.up * moveValue;
                    break;
                case Tags.Bottom:
                    newPosition += Vector3.up * moveValue;
                    break;
                case Tags.Left:
                    newPosition += Vector3.right * moveValue;
                    break;
                case Tags.Right:
                    newPosition -= Vector3.right * moveValue;
                    break;
                default:
                    break;
            }
            transform.localEulerAngles = eulerAngles;
            GameMoveRotator.SetPosition(objToMove, newPosition, false);
            new MoveNetAction(drawable.name, drawableParentName, objToMove.name, newPosition, false).Execute();
        }
    }
}
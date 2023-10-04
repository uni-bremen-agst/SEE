using Assets.SEE.Game.Drawable;
using Assets.SEE.Net.Actions.Drawable;
using SEE.Game;
using System;
using System.Collections;
using UnityEngine;

namespace Assets.SEE.Game.UI.Drawable
{
    public class BorderTriggerController : MonoBehaviour
    {
        /*
        [SerializeField]
        private bool trigger = false;

        private void OnTriggerEnter(Collider other)
        {
            //FIXEME ADD OTHER TAGS.
            if (other.gameObject.CompareTag(Tags.Line))
            {
             //   trigger = true;  // makes problems with the reset.
            }
        }*/

        private void OnTriggerStay(Collider other)
        {
            //FIXEME ADD OTHER TAGS.
            if (other.gameObject.CompareTag(Tags.Line))
            {
                GameObject drawable = GameDrawableFinder.FindDrawable(other.gameObject);
                string drawableParentName = GameDrawableFinder.GetDrawableParentName(drawable);
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
        /*
        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.CompareTag(Tags.Line))
            {
                trigger = false;
            }
        }

        public bool Trigger()
        {
            return trigger;
        }*/
    }
}
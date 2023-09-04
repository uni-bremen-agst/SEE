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
        private bool trigger = false;

        private void OnTriggerEnter(Collider other)
        {
            trigger = true;
        }

        private void OnTriggerStay(Collider other)
        {
            //FIXEME ADD OTHER TAGS.
            if (other.gameObject.CompareTag(Tags.Line))
            {
                GameObject drawable = GameDrawableFinder.FindDrawableParent(other.gameObject);
                string drawableParentName = GameDrawableFinder.GetDrawableParentName(drawable);
                Vector3 newPosition = other.gameObject.transform.position;
                switch (tag)
                {
                    case Tags.Top:
                        newPosition -= new Vector3(0, 0.01f, 0);
                        break;
                    case Tags.Bottom:
                        newPosition += new Vector3(0, 0.01f, 0);
                        break;
                    case Tags.Left:
                        newPosition += new Vector3(0.01f, 0, 0);
                        break;
                    case Tags.Right:
                        newPosition -= new Vector3(0.01f, 0, 0);
                        break;
                    default:
                        break;
                }
                GameMoveRotator.MoveObject(other.gameObject, newPosition);
                new MoveNetAction(drawable.name, drawableParentName, other.name, newPosition).Execute();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            trigger = false;
        }

        public bool Trigger()
        {
            return trigger;
        }
    }
}
using RTG;
using SEE.Game;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.SEE.Game.Drawable
{
    public static class GameMoveRotator
    {
        public static void MoveObject(GameObject obj, Vector3 position)
        {
            obj.transform.position = position;
        }

        public static void RotateObject(GameObject obj, Vector3 firstPoint, Vector3 moveDirection, float degree, Vector3 oldPosition)
        {
            DrawableHelper.Direction drawableDirection = DrawableHelper.checkDirection(GameDrawableFinder.GetHighestParent(obj));

            if ((drawableDirection == DrawableHelper.Direction.Front || drawableDirection == DrawableHelper.Direction.Back) &&
                (moveDirection == Vector3.forward || moveDirection == Vector3.back))
            {
                obj.transform.RotateAround(firstPoint, moveDirection, degree);
                obj.transform.position = new Vector3(obj.transform.position.x, obj.transform.position.y, oldPosition.z);
            }

            if ((drawableDirection == DrawableHelper.Direction.Left || drawableDirection == DrawableHelper.Direction.Right) &&
                (moveDirection == Vector3.right || moveDirection == Vector3.left))
            {
                obj.transform.RotateAround(firstPoint, moveDirection, degree);
                obj.transform.position = new Vector3(oldPosition.x, obj.transform.position.y, obj.transform.position.z);
            }

            if ((drawableDirection == DrawableHelper.Direction.Below || drawableDirection == DrawableHelper.Direction.Above) &&
                (moveDirection == Vector3.right || moveDirection == Vector3.left))
            {
                if (obj.CompareTag(Tags.Line))
                {
                    Debug.Log(DateTime.Now + " - " + obj.name);
                    if (obj.name.Contains("Holder"))
                    {
                        Debug.Log(DateTime.Now + " - enthält holder: " + obj.name);
                        obj.transform.Rotate(moveDirection, degree);
                    }
                    else
                    {
                        Debug.Log(DateTime.Now + " - muss kind sein: " + obj.name);
                        Transform parentTransform = obj.transform.parent;
                        parentTransform.Rotate(moveDirection, degree);
                    }
                    // To refresh the line mesh collider
                    obj.GetComponent<MeshCollider>().enabled = false;
                    obj.GetComponent<MeshCollider>().enabled = true;
                }
                else
                {
                    //FIXME: TESTEN OB DAS MIT ANDEREN OBJEKTEN WIRKLICH KLAPPT!
                    obj.transform.RotateAround(firstPoint, moveDirection, degree);
                }
            }
        }
        /*
        public static void RotateObject(GameObject obj, Quaternion oldRotation, Vector3 oldPosition)
        {
            obj.transform.rotation = oldRotation;
            obj.transform.position = oldPosition;
        }*/
    }
}
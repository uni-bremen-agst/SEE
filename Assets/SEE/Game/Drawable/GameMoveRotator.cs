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

        public static void RotateObject(GameObject obj, Vector3 moveDirection, float degree)
        {
            Transform transform;
            if (obj.CompareTag(Tags.Line))
            {
                transform = obj.transform.parent;
            } else
            {
                transform = obj.transform;
            }
            transform.Rotate(moveDirection, degree, Space.Self);
            obj.GetComponent<MeshCollider>().enabled = false;
            obj.GetComponent<MeshCollider>().enabled = true;
        }

        public static void SetRotate(GameObject obj, float localEulerAngleZ)
        {
            Transform transform = obj.CompareTag(Tags.Line) ? obj.transform.parent : obj.transform;
            transform.localEulerAngles = new Vector3(0, 0, localEulerAngleZ);
        }
    }
}
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

        public static void RotateObject(GameObject obj, Vector3 firstPoint, Vector3 direction, float degree)
        {
            Debug.Log("Rotate: " + obj + firstPoint + direction + degree);
            obj.transform.RotateAround(firstPoint, direction, degree);
        }
        /*
        public static void RotateObject(GameObject obj, Quaternion oldRotation)
        {
            obj.transform.rotation = oldRotation;
        }*/
    }
}
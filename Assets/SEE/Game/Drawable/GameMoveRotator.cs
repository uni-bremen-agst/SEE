using RTG;
using SEE.Game;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using UnityEngine.UIElements;

namespace Assets.SEE.Game.Drawable
{
    public static class GameMoveRotator
    {
        public static Vector3 MoveObject(GameObject obj, Vector3 hitPoint, Vector3 firstPoint, Vector3 oldPosition)
        {
            Vector3 offset = new Vector3(hitPoint.x - firstPoint.x, hitPoint.y - firstPoint.y, hitPoint.z - firstPoint.z);
            Vector3 position;
            ///This is needed to ensure that the correct axes are being moved. A rotation changes the axis position.
            Vector3 eulerAngles = obj.transform.localEulerAngles;
            obj.transform.localEulerAngles = Vector3.zero;

            position = oldPosition + multiply(obj.transform.right, offset) + multiply(obj.transform.up, offset);
            /*
            if (obj.CompareTag(Tags.Line))
            {
                
                Vector3 eulerAngles = obj.transform.parent.localEulerAngles;
                obj.transform.parent.localEulerAngles = Vector3.zero;
                position = oldPosition + multiply(obj.transform.right, offset) + multiply(obj.transform.up, offset);
                obj.transform.parent.localEulerAngles = eulerAngles;
            }
            else
            {
                position = oldPosition + multiply(obj.transform.right, offset) + multiply(obj.transform.up, offset);
            }*/
            obj.transform.position = position;
            obj.transform.localEulerAngles = eulerAngles;
            return position;
        }

        private static Vector3 multiply(Vector3 a, Vector3 b)
        {
            if (a.x <= -0.7f || a.y <= -0.7f || a.z <= -0.7f)
            {
                a = -a;
            }
            return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
        }

        public static Vector3 MoveObjectByKeyboard(GameObject obj, KeyCode key, bool speedUp)
        {
            Vector3 newPosition = obj.transform.position;
            Vector3 eulerAngles = obj.transform.localEulerAngles;
            obj.transform.localEulerAngles = Vector3.zero;
            float multiplyValue = 0.001f;
            if (speedUp)
            {
                multiplyValue = 0.01f;
            }
            switch (key)
            {
                case KeyCode.LeftArrow:
                    newPosition -= obj.transform.right * multiplyValue;
                    break;
                case KeyCode.RightArrow:
                    newPosition += obj.transform.right * multiplyValue;
                    break;
                case KeyCode.UpArrow:
                    newPosition += obj.transform.up * multiplyValue;
                    break;
                case KeyCode.DownArrow:
                    newPosition -= obj.transform.up * multiplyValue;
                    break;
            }
            obj.transform.position = newPosition;
            obj.transform.localEulerAngles = eulerAngles;
            return newPosition;
        }

        public static void MoveObject(GameObject obj, Vector3 position)
        {
            obj.transform.position = position;
        }

        public static void MovePoint(GameObject line, List<int> indexes, Vector3 point)
        {
            LineRenderer renderer = line.GetComponent<LineRenderer>();
            foreach (int i in indexes)
            {
                float z = renderer.GetPosition(i).z;
                Vector3 newPoint = new Vector3(point.x, point.y, z);
                renderer.SetPosition(i, newPoint);
            }
            GameDrawer.RefreshCollider(line);
        }

        public static Vector3 RotateObject(GameObject obj, Vector3 moveDirection, float degree)
        {
            Transform transform = obj.transform;//Transform transform;
            /*
            if (obj.CompareTag(Tags.Line))
            {
                transform = obj.transform.parent;
                obj.transform.SetParent(null);
                transform.position = obj.transform.position;
                obj.transform.SetParent(transform);
            }
            else
            {
                transform = obj.transform;
            }*/
            transform.Rotate(moveDirection, degree, Space.Self);
            obj.GetComponent<Collider>().enabled = false;
            obj.GetComponent<Collider>().enabled = true;
            return obj.transform.localEulerAngles;
        }

        public static void RotateObject(GameObject obj, Vector3 moveDirection, float degree, Vector3 point)
        {
            Transform transform;
            if (obj.CompareTag(Tags.Line))
            {
                transform = obj.transform.parent;
                obj.transform.SetParent(null);
                transform.position = obj.transform.position;
                obj.transform.SetParent(transform);
            }
            else
            {
                transform = obj.transform;
            }
            transform.RotateAround(point, moveDirection, degree);
            obj.GetComponent<Collider>().enabled = false;
            obj.GetComponent<Collider>().enabled = true;
        }
        /*
        public static void SetRotate(GameObject obj, float localEulerAngleZ, Vector3 holderPosition, Vector3 objPosition)
        {
            Transform transform = obj.CompareTag(Tags.Line) ? obj.transform.parent : obj.transform;
            transform.localEulerAngles = new Vector3(0, 0, localEulerAngleZ);
            if (obj.CompareTag(Tags.Line))
            {
                transform.localPosition = holderPosition;
                obj.transform.localPosition = objPosition;
            }
        }*/
        public static void SetRotate(GameObject obj, float localEulerAngleZ)
        {
            Transform transform = obj.transform;
            transform.localEulerAngles = new Vector3(0, transform.localEulerAngles.y, localEulerAngleZ);
            /*if (obj.CompareTag(Tags.Line))
            {
                transform.localPosition = holderPosition;
                obj.transform.localPosition = objPosition;
            }*/
        }

        public static void SetRotateY(GameObject obj, float localEulerAngleY)
        {
            Transform transform = obj.transform;
            transform.localEulerAngles = new Vector3(0, localEulerAngleY, transform.localEulerAngles.z);
        }
    }
}
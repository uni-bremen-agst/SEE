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
            if (obj.CompareTag(Tags.Line))
            {
                ///This is needed to ensure that the correct axes are being moved. A rotation changes the axis position.
                Vector3 eulerAngles = obj.transform.parent.localEulerAngles;
                obj.transform.parent.localEulerAngles = Vector3.zero;
                position = oldPosition + multiply(obj.transform.right, offset) + multiply(obj.transform.up, offset);
                obj.transform.parent.localEulerAngles = eulerAngles;
            }
            else
            {
                position = oldPosition + multiply(obj.transform.right, offset) + multiply(obj.transform.up, offset);
            }
            obj.transform.position = position;
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

        public static void RotateObject(GameObject obj, Vector3 moveDirection, float degree)
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
            transform.Rotate(moveDirection, degree, Space.Self);
            obj.GetComponent<MeshCollider>().enabled = false;
            obj.GetComponent<MeshCollider>().enabled = true;
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
            obj.GetComponent<MeshCollider>().enabled = false;
            obj.GetComponent<MeshCollider>().enabled = true;
        }

        public static void SetRotate(GameObject obj, float localEulerAngleZ, Vector3 holderPosition, Vector3 objPosition)
        {
            Transform transform = obj.CompareTag(Tags.Line) ? obj.transform.parent : obj.transform;
            transform.localEulerAngles = new Vector3(0, 0, localEulerAngleZ);
            if (obj.CompareTag(Tags.Line))
            {
                transform.localPosition = holderPosition;
                obj.transform.localPosition = objPosition;
            }
        }
    }
}
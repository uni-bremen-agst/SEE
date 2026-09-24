using SEE.Game.Drawable.MindMap;
using SEE.Game.Drawable.ValueHolders;
using SEE.GO;
using UnityEngine;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// This class provides methods for moving and rotating objects.
    /// </summary>
    public static class GameMoveRotator
    {
        /// <summary>
        /// Move an object (using its pivot point) by mouse.
        /// For moving it is necessary that the rotation of the object is zero.
        /// Because if they are not zero, the axes are rotated.
        /// And that would lead to an incorrect movement.
        /// </summary>
        /// <param name="obj">The object that should be moved.</param>
        /// <param name="hitPoint">The mouse hit point.</param>
        /// <returns>The new position of the moved object.</returns>
        public static Vector3 MoveObjectByMouse(GameObject obj, Vector3 hitPoint, bool includeChildren)
        {
            /// For mind map nodes.
            /// If child nodes are to be included, the child objects in the hierarchy are added to the parent object.
            GameMindMapTransform.PrepareForTransform(obj, includeChildren);

            Vector3 oldPos = obj.transform.localPosition;

            /// This is needed to ensure that the correct axes are being moved. A rotation changes the axis position.
            Vector3 localEulerAngles = obj.transform.localEulerAngles;
            obj.transform.localEulerAngles = Vector3.zero;

            /// Transforms the hit point to local space.
            Vector3 convertedHitPoint = obj.GetRootParent().transform.InverseTransformPoint(hitPoint);

            /// Ensure that the converted hit point preserves the distance to the drawable.
            convertedHitPoint -= obj.GetComponent<OrderInLayerValueHolder>().OrderInLayer
                * ValueHolder.DistanceToDrawable.z * obj.transform.forward;

            /// Build the new object position.
            Vector3 position = new(convertedHitPoint.x, convertedHitPoint.y, oldPos.z);

            /// Sets the new object position.
            obj.transform.localPosition = position;

            /// Restore the old euler angles.
            obj.transform.localEulerAngles = localEulerAngles;

            /// For mind map nodes.
            /// If child nodes were to be included, they are now encapsulated by the parent object.
            GameMindMapTransform.FinishTransform(obj, includeChildren);
            return position;
        }

        /// <summary>
        /// Move an object by keyboard or by the move menu.
        /// For moving it is necessary that the rotation of the object is zero.
        /// Because if they are not zero, the axes are rotated.
        /// And that would lead to incorrect movement.
        /// </summary>
        /// <param name="obj">The object that should be moved.</param>
        /// <param name="direction">The direction for the movement.</param>
        /// <param name="speedUp">If true the speed is 0.01f. otherwise it's 0.001.</param>
        /// <returns>The new position of the object.</returns>
        public static Vector3 MoveObjectByKeyboard(GameObject obj, ValueHolder.MoveDirection direction, bool speedUp, bool includeChildren)
        {
            /// For mind map nodes.
            /// If child nodes are to be included, the child objects in the hierarchy are added to the parent object.
            GameMindMapTransform.PrepareForTransform(obj, includeChildren);

            Vector3 newPosition = obj.transform.localPosition;

            /// This is needed to ensure that the correct axes are being moved. A rotation changes the axis position.
            Vector3 localEulerAngles = obj.transform.localEulerAngles;
            obj.transform.localEulerAngles = Vector3.zero;

            /// The moving speed.
            float multiplyValue = ValueHolder.Move;
            if (speedUp)
            {
                multiplyValue = ValueHolder.MoveFast;
            }

            /// Moves the object in the desired direction with the chosen speed.
            switch (direction)
            {
                case ValueHolder.MoveDirection.Left:
                    newPosition -= Vector3.right * multiplyValue;
                    break;
                case ValueHolder.MoveDirection.Right:
                    newPosition += Vector3.right * multiplyValue;
                    break;
                case ValueHolder.MoveDirection.Up:
                    newPosition += Vector3.up * multiplyValue;
                    break;
                case ValueHolder.MoveDirection.Down:
                    newPosition -= Vector3.up * multiplyValue;
                    break;
            }

            /// Sets the new position to the object.
            obj.transform.localPosition = newPosition;
            /// Restores the old euler angles.
            obj.transform.localEulerAngles = localEulerAngles;

            /// For mind map nodes.
            /// If child nodes were to be included, they are now encapsulated by the parent object.
            GameMindMapTransform.FinishTransform(obj, includeChildren);
            return newPosition;
        }

        /// <summary>
        /// Sets the given position to the object.
        /// It will be needed for undo/redo.
        /// </summary>
        /// <param name="obj">The object that should be moved.</param>
        /// <param name="position">The new position for the object.</param>
        public static void SetPosition(GameObject obj, Vector3 position, bool includeChildren)
        {
            /// For mind map nodes.
            /// If child nodes are to be included, the child objects in the hierarchy are added to the parent object.
            GameMindMapTransform.PrepareForTransform(obj, includeChildren, false, true);

            /// Sets the position.
            obj.transform.localPosition = position;

            /// For mind map nodes.
            /// If child nodes were to be included, they are now encapsulated by the parent object.
            GameMindMapTransform.FinishTransform(obj, includeChildren);
        }

        /// <summary>
        /// Rotates an object at its pivot point.
        /// It is necessary to refresh the object's collider, as it does not update itself.
        /// </summary>
        /// <param name="obj">The object which should be rotated.</param>
        /// <param name="rotateDirection">The direction in which should be rotated.</param>
        /// <param name="degree">The new degree.</param>
        /// <returns>The new local euler angles of the object.</returns>
        public static Vector3 RotateObject(GameObject obj, Vector3 rotateDirection, float degree, bool includeChildren)
        {
            /// For mind map nodes.
            /// If child nodes are to be included, the child objects in the hierarchy are added to the parent object.
            GameMindMapTransform.PrepareForTransform(obj, includeChildren);

            Transform transform = obj.transform;
            /// Roates the object based on the degree to rotate.
            transform.Rotate(rotateDirection, degree, Space.Self);

            /// Refreshes the collider of the object.
            obj.GetComponent<Collider>().enabled = false;
            obj.GetComponent<Collider>().enabled = true;

            /// For mind map nodes.
            /// If child nodes were to be included, they are now encapsulated by the parent object.
            GameMindMapTransform.FinishTransform(obj, includeChildren);
            return obj.transform.localEulerAngles;
        }

        /// <summary>
        /// Sets the given z euler angle to the object.
        /// It rotates around the z axis.
        /// Will be needed for undo/redo.
        /// </summary>
        /// <param name="obj">The object which should be rotated.</param>
        /// <param name="localEulerAngleZ">The new z degree.</param>
        public static void SetRotate(GameObject obj, float localEulerAngleZ, bool includeChildren)
        {
            /// For mind map nodes.
            /// If child nodes are to be included, the child objects in the hierarchy are added to the parent object.
            GameMindMapTransform.PrepareForTransform(obj, includeChildren, true, true);

            Transform transform = obj.transform;

            /// Sets the new rotation.
            transform.localEulerAngles = new Vector3(0, transform.localEulerAngles.y, localEulerAngleZ);

            /// For mind map nodes.
            /// If child nodes were to be included, they are now encapsulated by the parent object.
            GameMindMapTransform.FinishTransform(obj, includeChildren);
        }

        /// <summary>
        /// Sets the given y euler angle to the object.
        /// It rotates the y axis.
        /// Will needed for mirror an image.
        /// </summary>
        /// <param name="obj">The image object which should be mirrored.</param>
        /// <param name="localEulerAngleY">The new degree for the y axis.</param>
        public static void SetRotateY(GameObject obj, float localEulerAngleY)
        {
            Transform transform = obj.transform;
            transform.localEulerAngles = new Vector3(transform.localEulerAngles.x,
                localEulerAngleY, transform.localEulerAngles.z);
        }
    }
}
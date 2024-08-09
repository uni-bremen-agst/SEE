using SEE.Game.Drawable.ValueHolders;
using SEE.Net.Actions.Drawable;
using SEE.UI.Drawable;
using SEE.Utils;
using System.Collections.Generic;
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
        /// <returns>The new position of the moved object</returns>
        public static Vector3 MoveObjectByMouse(GameObject obj, Vector3 hitPoint, bool includeChildren)
        {
            /// For mind map nodes.
            /// If child nodes are to be included, the child objects in the hierarchy are added to the parent object.
            CheckPrepareNodeChilds(obj, includeChildren);

            Vector3 oldPos = obj.transform.localPosition;

            /// This is needed to ensure that the correct axes are being moved. A rotation changes the axis position.
            Vector3 localEulerAngles = obj.transform.localEulerAngles;
            obj.transform.localEulerAngles = Vector3.zero;

            /// Transforms the hit point to local space.
            Vector3 convertedHitPoint = GameFinder.GetHighestParent(obj).transform.InverseTransformPoint(hitPoint);

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
            IsPostProcessNodeNeeded(obj, includeChildren);
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
        /// <param name="speedUp">if true the speed is 0.01f. otherwise it's 0.001.</param>
        /// <returns>The new position of the object.</returns>
        public static Vector3 MoveObjectByKeyboard(GameObject obj, ValueHolder.MoveDirection direction, bool speedUp, bool includeChildren)
        {
            /// For mind map nodes.
            /// If child nodes are to be included, the child objects in the hierarchy are added to the parent object.
            CheckPrepareNodeChilds(obj, includeChildren);

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
            IsPostProcessNodeNeeded(obj, includeChildren);
            return newPosition;
        }

        /// <summary>
        /// Sets the given position to the object.
        /// It will be needed for undo/redo.
        /// </summary>
        /// <param name="obj">The object that should be moved</param>
        /// <param name="position">The new position for the object.</param>
        public static void SetPosition(GameObject obj, Vector3 position, bool includeChildren)
        {
            /// For mind map nodes.
            /// If child nodes are to be included, the child objects in the hierarchy are added to the parent object.
            CheckPrepareNodeChilds(obj, includeChildren, false, true);

            /// Sets the position.
            obj.transform.localPosition = position;

            /// For mind map nodes.
            /// If child nodes were to be included, they are now encapsulated by the parent object.
            IsPostProcessNodeNeeded(obj, includeChildren);
        }

        /// <summary>
        /// Moves a point of a line.
        /// It only works for the drawable type line.
        /// </summary>
        /// <param name="line">The line which holds the to moved point</param>
        /// <param name="Indices">The indices of the points which should be moved (all indices have the same position).</param>
        /// <param name="point">The new point position</param>
        public static void MovePoint(GameObject line, List<int> Indices, Vector3 point)
        {
            LineRenderer renderer = line.GetComponent<LineRenderer>();
            foreach (int i in Indices)
            {
                /// Calculates the new position for the point.
                Vector3 newPoint = new(point.x, point.y, renderer.GetPosition(i).z);
                renderer.SetPosition(i, newPoint);
            }
            GameDrawer.RefreshCollider(line);
        }

        /// <summary>
        /// Rotates an object at its pivot point.
        /// It is necessary to refresh the object's collider, as it does not update itself.
        /// </summary>
        /// <param name="obj">The object which should be rotated.</param>
        /// <param name="rotateDirection">The direction in which should be rotated.</param>
        /// <param name="degree">The new degree.</param>
        /// <returns>The new local euler angles of the object</returns>
        public static Vector3 RotateObject(GameObject obj, Vector3 rotateDirection, float degree, bool includeChildren)
        {
            /// For mind map nodes.
            /// If child nodes are to be included, the child objects in the hierarchy are added to the parent object.
            CheckPrepareNodeChilds(obj, includeChildren);

            Transform transform = obj.transform;
            /// Roates the object based on the degree to rotate.
            transform.Rotate(rotateDirection, degree, Space.Self);

            /// Refreshes the collider of the object.
            obj.GetComponent<Collider>().enabled = false;
            obj.GetComponent<Collider>().enabled = true;

            /// For mind map nodes.
            /// If child nodes were to be included, they are now encapsulated by the parent object.
            IsPostProcessNodeNeeded(obj, includeChildren);
            return obj.transform.localEulerAngles;
        }

        /// <summary>
        /// Sets the given z euler angle to the object.
        /// It rotates around the z axis.
        /// Will be needed for undo/redo.
        /// </summary>
        /// <param name="obj">The object which should be rotated.</param>
        /// <param name="localEulerAngleZ">The new z degree</param>
        public static void SetRotate(GameObject obj, float localEulerAngleZ, bool includeChildren)
        {
            /// For mind map nodes.
            /// If child nodes are to be included, the child objects in the hierarchy are added to the parent object.
            CheckPrepareNodeChilds(obj, includeChildren, true, true);

            Transform transform = obj.transform;

            /// Sets the new rotation.
            transform.localEulerAngles = new Vector3(0, transform.localEulerAngles.y, localEulerAngleZ);

            /// For mind map nodes.
            /// If child nodes were to be included, they are now encapsulated by the parent object.
            IsPostProcessNodeNeeded(obj, includeChildren);
        }

        /// <summary>
        /// Sets the given y euler angle to the object.
        /// It rotates the y axis.
        /// Will needed for mirror an image.
        /// </summary>
        /// <param name="obj">The image object which should be mirrored.</param>
        /// <param name="localEulerAngleY">The new degree for the y axis</param>
        public static void SetRotateY(GameObject obj, float localEulerAngleY)
        {
            Transform transform = obj.transform;
            transform.localEulerAngles = new Vector3(transform.localEulerAngles.x,
                localEulerAngleY, transform.localEulerAngles.z);
        }

        /// <summary>
        /// This method prepares a mind map node so that
        /// an action including the children can be performed.
        /// For this purpose, the child nodes are added to the parent node.
        /// </summary>
        /// <param name="node">The parent node</param>
        /// <param name="setMode">true, if this method will be called from a set method</param>
        /// <param name="rotationSetMode">true, if this method will be called from rotation action.</param>
        private static void PrepareNodeChilds(GameObject node, bool setMode, bool rotationSetMode = false)
        {
            if (node.CompareTag(Tags.MindMapNode))
            {
                MMNodeValueHolder valueHolder = node.GetComponent<MMNodeValueHolder>();
                foreach (KeyValuePair<GameObject, GameObject> pair in valueHolder.GetAllChildren())
                {
                    /// Adopt the rotation of the parent node.
                    if (rotationSetMode)
                    {
                        pair.Key.transform.localEulerAngles = node.transform.localEulerAngles;
                    }

                    /// Assigns the child nodes to the parent node.
                    pair.Key.transform.SetParent(node.transform);

                    if (!setMode)
                    {
                        /// Enables the collision detection for the childs.
                        if (pair.Key.GetComponent<Rigidbody>() == null)
                        {
                            pair.Key.AddComponent<Rigidbody>().isKinematic = true;
                            pair.Key.AddComponent<CollisionController>();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks if preparing child nodes is necessary, and if not,
        /// deletes all rigid bodies and collision controllers except those of the selected nodes.
        /// </summary>
        /// <param name="node">the parent node</param>
        /// <param name="includeChildren">whether the children should be included for the movement or the rotation.</param>
        /// <param name="rotationSetMode">true, if the method will be called from <see cref="SetRotate"/></param>
        /// <param name="setMode">true, if the method will be called from a Set-Method
        /// (<see cref="SetRotate"/> or <see cref="SetPosition"/>)</param>
        private static void CheckPrepareNodeChilds(GameObject node, bool includeChildren,
            bool rotationSetMode = false, bool setMode = false)
        {
            /// If the children should be included, prepare the child nodes.
            if (includeChildren)
            {
                PrepareNodeChilds(node, setMode, rotationSetMode);
            }
            else
            {
                if (node.CompareTag(Tags.MindMapNode))
                {
                    MMNodeValueHolder valueHolder = node.GetComponent<MMNodeValueHolder>();
                    GameObject surface = GameFinder.GetDrawableSurface(node);
                    string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);
                    /// If this method was not called by a set method (<see cref="SetRotate"/> or <see cref="SetPosition"/>),
                    /// then disable collision detection for the children.
                    if (!setMode)
                    {
                        new RbAndCCDestroyerNetAction(surface.name, surfaceParentName, node.name).Execute();
                        foreach (KeyValuePair<GameObject, GameObject> pair in valueHolder.GetAllChildren())
                        {
                            if (pair.Key.GetComponent<Rigidbody>() != null)
                            {
                                Destroyer.Destroy(pair.Key.GetComponent<Rigidbody>());
                                Destroyer.Destroy(pair.Key.GetComponent<CollisionController>());
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This method separates the child nodes from the parent node
        /// after <see cref="PrepareNodeChilds"/> has been called.
        /// The children are assigned to the original "AttachedObjects"
        /// object of the respective drawable.
        /// </summary>
        /// <param name="obj">The parent node</param>
        private static void PostProcessNode(GameObject obj)
        {
            if (obj.CompareTag(Tags.MindMapNode))
            {
                GameMindMap.ReDrawParentBranchLine(obj);
                GameObject attachedObject = GameFinder.GetAttachedObjectsObject(obj);
                MMNodeValueHolder v = obj.GetComponent<MMNodeValueHolder>();

                /// Assign the children back to the attached object - object of the drawable.
                /// It is necessary to redraw the parent branch line, as it was not moved along with it.
                foreach (KeyValuePair<GameObject, GameObject> pair in v.GetAllChildren())
                {
                    pair.Key.transform.SetParent(attachedObject.transform);
                    pair.Value.transform.SetParent(attachedObject.transform);
                    GameMindMap.ReDrawParentBranchLine(pair.Key);
                }
            }
        }

        /// <summary>
        /// Checks if <see cref="PostProcessNode"/> needs to be executed.
        /// If not, only the branch lines are refreshed.
        /// </summary>
        /// <param name="node">The parent node</param>
        /// <param name="includeChildren">Option if children should be included for the action.</param>
        private static void IsPostProcessNodeNeeded(GameObject node, bool includeChildren)
        {
            if (includeChildren)
            {
                PostProcessNode(node);
            }
            else
            {
                if (node.CompareTag(Tags.MindMapNode))
                {
                    GameMindMap.ReDrawBranchLines(node);
                }
            }
        }

        /// <summary>
        /// Destroys all rigid bodies and collision controllers of all children of the selected node.
        /// </summary>
        /// <param name="node">The selected node</param>
        public static void DestroyRigidBodysAndCollisionControllersOfChildren(GameObject node)
        {
            if (node.CompareTag(Tags.MindMapNode))
            {
                MMNodeValueHolder valueHolder = node.GetComponent<MMNodeValueHolder>();

                /// Disables the collision detection for the child nodes.
                foreach (KeyValuePair<GameObject, GameObject> pair in valueHolder.GetAllChildren())
                {
                    if (pair.Key.GetComponent<Rigidbody>() != null)
                    {
                        Destroyer.Destroy(pair.Key.GetComponent<Rigidbody>());
                    }
                    if (pair.Key.GetComponent<CollisionController>() != null)
                    {
                        Destroyer.Destroy(pair.Key.GetComponent<CollisionController>());
                    }
                }
            }
        }
    }
}
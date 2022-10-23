using System.Collections.Generic;
using SEE.Game;
using SEE.GO;
using SEE.Net;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to create a new marker for a selected node.
    /// </summary>
    internal class MarkAction : AbstractPlayerAction
    {
        /// <summary>
        /// If the user clicks with the mouse hitting a game object representing a graph node,
        /// this graph node is a parent to which a new marker is marked.
        /// <see cref="ReversibleAction.Update"/>.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            bool result = false;

            // FIXME: Needs adaptation for VR where no mouse is available.
            if (Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef _) ==
                HitGraphElement.Node)
            {
                // The hit object is the parent in which to create the new marker
                GameObject parent = raycastHit.collider.gameObject;
                // The parents position
                Vector3 position = parent.transform.position;
                // The scale of the sphere
                Vector3 scale = FindSize(parent, position);

                addedSphere = GameNodeMarker.AddChild(parent, position: position, worldSpaceScale: scale);
                if (addedSphere != null)
                {
                    memento = new Memento(parent, position: position, scale: scale);
                    memento.MarkerID = addedSphere.name;
                    new MarkNetAction(parentID: memento.Parent.name, newMarkerID: memento.MarkerID, memento.Position,
                        memento.Scale).Execute();
                    result = true;
                    currentState = ReversibleAction.Progress.Completed;
                }
                else
                {
                    Debug.LogError($"New Sphere could not be created.\n");
                }
            }

            return result;
        }

        /// <summary>
        /// The sphere that will be added when this action was executed. It is saved so
        /// that it can be removed on Undo().
        /// </summary>
        private GameObject addedSphere;

        /// <summary>
        /// Memento capturing the data necessary to re-do this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// The information we need to re-add a marker whose addition was undone.
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The parent of the new marker.
            /// </summary>
            public readonly GameObject Parent;

            /// <summary>
            /// The position of the new marker in world space.
            /// </summary>
            public readonly Vector3 Position;

            /// <summary>
            /// The scale of the new marker in world space.
            /// </summary>
            public readonly Vector3 Scale;

            /// <summary>
            /// The marker ID for the added marker. It must be kept to re-use the
            /// original name of the marker in Redo().
            /// </summary>
            public string MarkerID;

            /// <summary>
            /// Constructor setting the information necessary to re-do this action.
            /// </summary>
            /// <param name="parent">parent of the new node</param>
            /// <param name="position">position of the new node in world space</param>
            /// <param name="scale">scale of the new node in world space</param>
            public Memento(GameObject parent, Vector3 position, Vector3 scale)
            {
                Parent = parent;
                Position = position;
                Scale = scale;
                MarkerID = null;
            }
        }

        /// <summary>
        /// Undoes this MarkAction.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            if (addedSphere != null)
            {
                new DeleteNetAction(addedSphere.name).Execute();
                Destroyer.DestroyGameObject(addedSphere);
                addedSphere = null;
            }
        }

        /// <summary>
        /// Redoes this MarkAction.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            addedSphere = GameNodeMarker.AddChild(memento.Parent, position: memento.Position,
                worldSpaceScale: memento.Scale, markerID: memento.MarkerID);
            if (addedSphere != null)
            {
                new MarkNetAction(parentID: memento.Parent.name, newMarkerID: memento.MarkerID, memento.Position,
                    memento.Scale).Execute();
            }
        }

        /// <summary>
        /// Returns a new instance of <see cref="MarkAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new MarkAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="MarkAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.NewMarker"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.Mark;
        }

        /// <summary>
        /// Returns all IDs of gameObjects manipulated by this action.
        /// </summary>
        /// <returns>all IDs of gameObjects manipulated by this action</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string>
            {
                memento.Parent.name,
                memento.MarkerID
            };
        }

        /// <summary>
        /// Returns a scale of a square with the given center <see cref="position"/>
        /// that fits into the ground area of <paramref name="parent"/>.
        /// </summary>
        /// <param name="parent">parent in which to fit the rectangle</param>
        /// <param name="position">center position of the rectangle</param>
        /// <returns>the scale of a square (actually a cube, but with a very small height)
        /// with center <see cref="position"/> that fits into the ground area of <paramref name="parent"/></returns>
        private static Vector3 FindSize(GameObject parent, Vector3 position)
        {
            // TODO: We might want to implement something smarter
            // than that, see for instance:
            // https://stackoverflow.com/questions/51574829/how-to-algorithmically-find-the-biggest-rectangle-that-can-fit-in-a-space-with-o
            Vector3 result = parent.transform.lossyScale / 10;
            // The ground area of the result must be a square.
            if (result.x > result.z)
            {
                result.x = result.z;
            }
            else
            {
                result.z = result.x;
            }

            result.y = 0.01f;
            return result;
        }
    }
}
using System.Collections.Generic;
using SEE.Game;
using SEE.GO;
using SEE.Net;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to toggle a marking for a node.
    /// The mark is a white sphere, hovering over the node.
    /// </summary>
    internal class MarkAction : AbstractPlayerAction
    {
        /// <summary>
        /// If the user clicks with the mouse hitting a game object representing a node,
        /// this node gets a mark.
        /// <see cref="ReversibleAction.Update"/>.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            bool result = false;

            // FIXME: Needs adaptation for VR where no mouse is available.
            if (Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef _) == HitGraphElement.Node)
            {
                /// the hit object is the Node which gets a mark as a child.
                GameObject parent = raycastHit.collider.gameObject;
                /// the position of the Node is used for the mark.
                Vector3 position = parent.transform.position;
                /// the scale of the Node is used to make the Sphere fit into the ground space of the node.
                Vector3 scale = FindSize(parent);
                GameNodeMarker.Mark(parent, position: position, worldSpaceScale: scale);
                memento = new Memento(parent, position: position, scale: scale);
                new MarkNetAction(parentID: memento.Parent.name, memento.Position, memento.Scale).Execute();
                result = true;
                currentState = ReversibleAction.Progress.Completed;
            }
            return result;
        }

        /// <summary>
        /// Memento capturing the data necessary to re-do this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// The information we need to re-add a mark whose addition was undone.
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The node which is marked.
            /// </summary>
            public readonly GameObject Parent;
            /// <summary>
            /// The position of the mark in world space.
            /// </summary>
            public readonly Vector3 Position;
            /// <summary>
            /// The scale of the new mark in world space.
            /// </summary>
            public readonly Vector3 Scale;

            /// <summary>
            /// Constructor setting the information necessary to re-do this action.
            /// </summary>
            /// <param name="parent">The node which is marked.</param>
            /// <param name="position">position of the mark in world space.</param>
            /// <param name="scale">scale of the mark in world space.</param>
            public Memento(GameObject parent, Vector3 position, Vector3 scale)
            {
                Parent = parent;
                Position = position;
                Scale = scale;
            }
        }

        /// <summary>
        /// Returns a scale of a cube that fits into the ground area of <paramref name="parent"/>.
        /// </summary>
        /// <param name="parent">parent in which ground area to fit the cube.</param>
        /// <returns>the scale of a cube that fits into the ground area of <paramref name="parent"/>.</returns>
        private static Vector3 FindSize(GameObject parent)
        {
            Vector3 result = parent.transform.lossyScale;
            /// The ground area of the result must be a square.
            if (result.x > result.z)
            {
                result.x = result.z;
            }
            else
            {
                result.z = result.x;
            }
            /// make the square a cube.
            result.y = result.z;
            return result;
        }

        /// <summary>
        /// Undoes this MarkAction.
        /// </summary>
        public override void Undo()
        {
            base.Undo();

            GameNodeMarker.Mark(memento.Parent, position: memento.Position, worldSpaceScale: memento.Scale);
            new MarkNetAction(parentID: memento.Parent.name, memento.Position, memento.Scale).Execute();

        }

        /// <summary>
        /// Redoes this MarkAction.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            GameNodeMarker.Mark(memento.Parent, position: memento.Position, worldSpaceScale: memento.Scale);
            new MarkNetAction(parentID: memento.Parent.name, memento.Position, memento.Scale).Execute();
        }

        /// <summary>
        /// Returns a new instance of <see cref="MarkAction"/>.
        /// </summary
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
        /// <returns><see cref="ActionStateType.NewNode"/></returns>
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
                memento.Parent.name
            };
        }
    }
}
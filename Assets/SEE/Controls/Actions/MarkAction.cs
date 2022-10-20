using SEE.Controls.Actions;
using SEE.Game;
using SEE.GO;
using SEE.Net;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to highlight a node for a selected city.
    /// </summary>
    internal class MarkAction : AbstractPlayerAction
    {

        /// <summary>
        /// The marker that was added
        /// </summary>
        private GameObject marker;

        /// <summary>
        /// Memento capturing the data necessary to re-do this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.MarkAction"/></returns>
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

        /// <summary>
        /// Returns a new instance of <see cref="MarkAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        public override bool Update()
        {
            bool result = false;
            if (Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef _) == HitGraphElement.Node)
            {
                // the hit object is the parent in which to create the new node
                GameObject parent = raycastHit.collider.gameObject;
                // The position at which the parent was hit will be the center point of the new node
                Vector3 position = parent.transform.position;
                marker = GameNodeMarker.CreateMarker(parent, position: position, worldSpaceScale: parent.transform.lossyScale);
                if (marker != null)
                {
                    memento = new Memento(parent, position: position, scale: marker.transform.lossyScale);
                    new MarkNetAction(parentID: memento.Parent.name, memento.Position, memento.Scale).Execute();
                    result = true;
                    currentState = ReversibleAction.Progress.Completed;
                }
                else
                {
                    Debug.LogError($"Marker could not be created.\n");
                }
            }
            return result;
        }

        /// <summary>
        /// Undoes this MarkAction.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            if (marker != null)
            {
                new DeleteNetAction(marker.name).Execute();
                Destroyer.DestroyGameObject(marker);
                marker = null;
            }
        }

        /// <summary>
        /// Redoes this MarkAction.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            marker = GameNodeMarker.CreateMarker(memento.Parent, position: memento.Position, worldSpaceScale: memento.Scale);
            if (marker != null)
            {
                new MarkNetAction(parentID: memento.Parent.name, memento.Position, memento.Scale).Execute();
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
        /// The information we need to re-add a marker whose addition was undone.
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The parent of the marker.
            /// </summary>
            public readonly GameObject Parent;

            /// <summary>
            /// The position of the new node in world space.
            /// </summary>
            public readonly Vector3 Position;

            /// <summary>
            /// The scale of the new node in world space.
            /// </summary>
            public readonly Vector3 Scale;

            /// <summary>
            /// Constructor setting the information necessary to re-do this action.
            /// </summary>
            /// <param name="parent">parent of the marker</param>
            /// <param name="position">position of the marker in world space</param>
            /// <param name="scale">scale of the marker in world space</param>
            public Memento(GameObject parent, Vector3 position, Vector3 scale)
            {
                Parent = parent;
                Position = position;
                Scale = scale;
            }
        }
    }
}

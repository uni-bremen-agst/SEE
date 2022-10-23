using SEE.Game;
using SEE.GO;
using SEE.Net;
using SEE.Net.Actions;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Action to highlight a node for a selected city.
/// </summary>
namespace SEE.Controls.Actions
{
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

        /// <summary>
        /// Repeated once every frame. Executes the mark node action if a node is selected.
        /// </summary>
        /// <returns>True if the action was executed, false otherwise.</returns>
        public override bool Update()
        {
            if (Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef _) == HitGraphElement.Node)
            {
                // the hit object is the parent in which to create the marker
                GameObject parent = raycastHit.collider.gameObject;
                marker = GameNodeMarker.CreateMarker(parent);
                if (marker != null)
                {
                    memento = new Memento(parent);
                    new MarkNetAction(parentID: memento.Parent.name).Execute();
                    currentState = ReversibleAction.Progress.Completed;
                }
                else
                {
                    Debug.LogError("Marker could not be created.\n");
                }
            }
            return currentState.Equals(ReversibleAction.Progress.Completed);
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
            marker = GameNodeMarker.CreateMarker(memento.Parent);
            if (marker != null)
            {
                new MarkNetAction(parentID: memento.Parent.name).Execute();
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
            /// Constructor setting the information necessary to re-do this action.
            /// </summary>
            /// <param name="parent">parent of the marker</param>
            public Memento(GameObject parent)
            {
                Parent = parent;
            }
        }
    }
}

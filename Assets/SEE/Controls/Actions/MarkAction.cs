using SEE.Game;
using SEE.GO;
using SEE.Net;
using SEE.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls.Actions
{
    internal class MarkAction : AbstractPlayerAction
    {

        /// <summary>
        /// Memento capturing the data necessary to re-do this action.
        /// </summary>
        private Memento memento;

        public override bool Update()
        {
            //If the mouse button in pressed and the user looks at a node, execute.
            if (Input.GetMouseButtonDown(0)
               && Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef _) == HitGraphElement.Node)
            {
                // the hit object is the parent in which to create the new node
                GameObject node = raycastHit.collider.gameObject;
                GameObject markerSphere = GameNodeMarker.ToggleMark(node);

                this.memento = new Memento(node);

                // propagate the MarkAction to other clients
                new MarkNetAction(markerSphere).Execute();

                currentState = ReversibleAction.Progress.Completed;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns a new instance of <see cref="MarkAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="MarkAction"/></returns>
        internal static ReversibleAction CreateReversibleAction() => new MarkAction();

        /// <summary>
        /// Returns a new instance of <see cref="MarkAction"/> that can continue
        /// with the user interaction so far.
        /// </summary>
        /// <returns>new instance</returns>
        public override ReversibleAction NewInstance() => new MarkAction();

        /// <summary>
        /// The information we need to re-add a marker whose addition was undone.
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The node that is marked.
            /// </summary>
            public readonly GameObject Node;

            /// <summary>
            /// The marker ID for the added marker. It must be kept to re-use the
            /// original name of the marker in Redo().
            /// </summary>
            public string MarkerID;

            /// <summary>
            /// Constructor setting the information necessary to re-do this action.
            /// </summary>
            /// <param name="node">The node that is marked</param>
            public Memento(GameObject node)
            {
                Node = node;
                MarkerID = node.ID();
            }
        }

        /// <summary>
        /// Returns all IDs of gameObjects manipulated by this action.
        /// </summary>
        /// <returns>all IDs of gameObjects manipulated by this action</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string>
            {
                memento.Node.name,
                memento.MarkerID
            };
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.Mark"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.Mark;
        }
    }
}

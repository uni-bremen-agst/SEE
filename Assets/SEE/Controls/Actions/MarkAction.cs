using System.Collections.Generic;
using SEE.Game;
using SEE.GO;
using SEE.Net;
using SEE.Net.Actions;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to mark or unmark a node.
    /// </summary>
    public class MarkAction : AbstractPlayerAction
    {
        /// <summary>
        /// The game object that was marked.
        /// </summary>
        private GameObject node;
        /// <summary>
        /// The ID of the added sphere.
        /// </summary>
        private string markID;

        /// <summary>
        /// If the user clicks with the mouse hitting a game object representing a graph node,
        /// this graph node will be marked or unmarked, if the node is already marked.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            // FIXME: Needs adaptation for VR where no mouse is available.
            if (Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef _) ==
                HitGraphElement.Node)
            {
                // the hit object is the node that should be marked
                node = raycastHit.collider.gameObject;
                // marks the node
                markID = GameNodeMarker.MarkNode(node);
                if (markID != null)
                {
                    new MarkNetAction(nodeID: node.name, markID: markID).Execute();
                    currentState = ReversibleAction.Progress.Completed;
                    return true;
                }
                Debug.LogError($"Node could not be marked.\n");

            }
            return false;
        }

        /// <summary>
        /// Undoes this MarkAction.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            GameNodeMarker.MarkNode(node: node, markID: markID);
        }

        /// <summary>
        /// Redoes this MarkAction.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            GameNodeMarker.MarkNode(node: node, markID: markID);
            new MarkNetAction(nodeID: node.name, markID: markID).Execute();
        }

        /// <summary>
        /// Returns all IDs of gameObjects manipulted by this action.
        /// </summary>
        /// <returns>all IDs of gameObjects manipulated by this action</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string>
            {
                node.name, markID
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

        /// <summary>
        /// Returns a new instance of <see cref="MarkAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="MarkAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static ReversibleAction CreateReversibleAction()
        {
            
            return new MarkAction();
        }

    }
}
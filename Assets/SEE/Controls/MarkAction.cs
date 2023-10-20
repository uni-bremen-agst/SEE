using System.Collections.Generic;
using SEE.GO;
using SEE.Net.Actions;
using SEE.Utils.History;
using UnityEngine;
using SEE.Audio;
using SEE.Game.SceneManipulation;
using SEE.Utils;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to create a new node for a selected city.
    /// </summary>
    internal class MarkAction : AbstractPlayerAction
    {
        /// <summary>
        /// If the user clicks with the mouse hitting a game object representing a graph node,
        /// this graph node is a parent to which a new node is created and added as a child.
        /// <see cref="IReversibleAction.Update"/>.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            bool result = false;

            // FIXME: Needs adaptation for VR where no mouse is available.
            if (Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef _) == HitGraphElement.Node)
            {
                // the hit object is the parent in which to create the new node
                GameObject parent = raycastHit.collider.gameObject;
                Vector3 position = parent.transform.position;
                Vector3 scale = parent.transform.lossyScale;
                addedSphere = GameNodeMarker.AddSphere(parent, position: position, worldSpaceScale: scale);
                // addedSphere has the scale and position of Vector3.
                // The position at which the parent was hit will be the center point of the addedSphere.
                momento = new Momento(parent, position: position, scale: scale);
                memento.NodeID = addedSphere.name;
                new MarkNetAction(parentID: memento.Parent.name, memento.Position, memento.Scale).Execute();
                result = true;
                CurrentState = IReversibleAction.Progress.Completed;
            }
            return result;
        }

        /// <summary>
        /// The node that was added when this action was executed. It is saved so
        /// that it can be removed on Undo().
        /// </summary>
        private GameObject addedSphere;

        /// <summary>
        /// Memento capturing the data necessary to re-do this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// The information we need to re-add a node whose addition was undone.
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The parent of the new node.
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
            /// The node ID for the added node. It must be kept to re-use the
            /// original name of the node in Redo().
            /// </summary>
            public string NodeID;

            /// <summary>
            /// Constructor setting the information necessary to re-do this action.
            /// </summary>
            /// <param name="child">child that was added</param>
            /// <param name="parent">parent of <paramref name="child"/></param>
            public Memento(GameObject parent, Vector3 position, Vector3 scale)
            {
                Parent = parent;
                Position = position;
                Scale = scale;
                NodeID = null;
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
                Destroyer.Destroy(addedSphere);
                addedSphere = null;
            }
        }

        /// <summary>
        /// Redoes this MarkAction.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            addedSphere = GameNodeMarker.AddSphere(memento.Parent, worldSpacePosition: memento.Position, worldSpaceScale: memento.Scale, nodeID: memento.NodeID);
            if (addedGameNode != null)
            {
                new MarkNetAction(parentID: memento.Parent.name, memento.Position, memento.Scale).Execute();
            }
        }

        /// <summary>
        /// Returns a new instance of <see cref="MarkAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new MarkAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="MarkAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.MarkNode"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.MarkNode;
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
                memento.NodeID
            };
        }
    }
}

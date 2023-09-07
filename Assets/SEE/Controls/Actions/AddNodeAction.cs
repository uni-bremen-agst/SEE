using System.Collections.Generic;
using SEE.GO;
using SEE.Net.Actions;
using SEE.Utils;
using UnityEngine;
using SEE.Audio;
using SEE.Game.SceneManipulation;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to create a new node for a selected city.
    /// </summary>
    internal class AddNodeAction : AbstractPlayerAction
    {
        /// <summary>
        /// If the user clicks with the mouse hitting a game object representing a graph node,
        /// this graph node is a parent to which a new node is created and added as a child.
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
                // the hit object is the parent in which to create the new node
                GameObject parent = raycastHit.collider.gameObject;
                addedGameNode = GameNodeAdder.AddChild(parent);
                // addedGameNode has the scale and position of parent.
                // The position at which the parent was hit will be the center point of the addedGameNode.
                addedGameNode.transform.position = raycastHit.point;
                // PutOn makes sure addedGameNode fits into parent.
                GameNodeMover.PutOn(child: addedGameNode.transform, parent: parent, true);
                memento = new Memento(child: addedGameNode, parent: parent);
                memento.NodeID = addedGameNode.name;
                new AddNodeNetAction(parentID: memento.Parent.name, newNodeID: memento.NodeID, memento.Position, memento.Scale).Execute();
                result = true;
                currentState = ReversibleAction.Progress.Completed;
                AudioManagerImpl.EnqueueSoundEffect(IAudioManager.SoundEffect.NEW_NODE_SOUND, parent);
            }
            return result;
        }

        /// <summary>
        /// The node that was added when this action was executed. It is saved so
        /// that it can be removed on Undo().
        /// </summary>
        private GameObject addedGameNode;

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
            public Memento(GameObject child, GameObject parent)
            {
                Parent = parent;
                Position = child.transform.position;
                Scale = child.transform.lossyScale;
                NodeID = null;
            }
        }

        /// <summary>
        /// Undoes this AddNodeAction.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            if (addedGameNode != null)
            {
                new DeleteNetAction(addedGameNode.name).Execute();
                GameElementDeleter.RemoveNodeFromGraph(addedGameNode);
                Destroyer.Destroy(addedGameNode);
                addedGameNode = null;
            }
        }

        /// <summary>
        /// Redoes this AddNodeAction.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            addedGameNode = GameNodeAdder.AddChild(memento.Parent, worldSpacePosition: memento.Position, worldSpaceScale: memento.Scale, nodeID: memento.NodeID);
            if (addedGameNode != null)
            {
                new AddNodeNetAction(parentID: memento.Parent.name, newNodeID: memento.NodeID, memento.Position, memento.Scale).Execute();
            }
        }

        /// <summary>
        /// Returns a new instance of <see cref="AddNodeAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new AddNodeAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="AddNodeAction"/>.
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
            return ActionStateTypes.NewNode;
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

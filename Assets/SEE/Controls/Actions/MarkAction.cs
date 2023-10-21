using SEE.Utils.History;
using System.Collections.Generic;
using UnityEngine;
using SEE.GO;
using SEE.Utils;
using SEE.Game.Scenemanipulation;
using SEE.Net.Actions;
using SEE.Audio;
using SEE.Game.SceneManipulation;

namespace SEE.Controls.Actions {
    public class MarkAction : AbstractPlayerAction
    {
        /// <summary>
        /// Returns a new instance of <see cref="MarkAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new MarkAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateTypes.Mark"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.Mark;
        }

        /// <summary>
        /// If the user clicks with the mouse hitting a game object representing a graph node,
        /// will be marked. A sphere will appear above the marked node.
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
                // the hit object is the parent in which to create the sphere
                GameObject parent = raycastHit.collider.gameObject;
                Vector3 position = parent.transform.position;
                Vector3 scale = parent.transform.lossyScale;
                // create or delete the sphere
                addedSphere = GameNodeMarker.CreateOrDeleteSphere(parent, scale);
                memento = new Memento(parent, position: position, scale: scale);
                memento.NodeID = addedSphere.name;
                new MarkNetAction(memento.Parent, memento.Position, memento.Scale).Execute();
                result = true;
                CurrentState = IReversibleAction.Progress.Completed;
                AudioManagerImpl.EnqueueSoundEffect(IAudioManager.SoundEffect.NewNodeSound, parent);
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
            /// <param name="markedNode">The marked node</param>
            public Memento(GameObject parent, Vector3 position, Vector3 scale)
            {
                Parent = parent;
                Position = position;
                Scale = scale;
                NodeID = null;
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
                memento.Parent.name,
                memento.NodeID
            };
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
            addedSphere = GameNodeAdder.AddChild(memento.Parent, worldSpacePosition: memento.Position, worldSpaceScale: memento.Scale, nodeID: memento.NodeID);
            if (addedSphere != null)
            {
                new MarkNetAction(memento.Parent, memento.Position, memento.Scale).Execute();
            }
        }
    }

}



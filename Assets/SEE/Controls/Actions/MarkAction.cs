using System.Collections.Generic;
using SEE.GO;
using SEE.Net.Actions;
using SEE.Utils;
using UnityEngine;
using SEE.Utils.History;
using SEE.Audio;
using SEE.Game.SceneManipulation;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to create a new marker.
    /// </summary>
    internal class MarkAction : AbstractPlayerAction
    {
        /// <summary>
        /// If the user clicks with the mouse hitting a game object representing a graph node,
        /// a sphere is created and added as a child.
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
                Vector3 position = GameObjectExtensions.GetTop(parent);
                Vector3 scale = parent.transform.lossyScale;
                addedSphere = GameNodeMarker.NewSphere(parent, scale);
                // addedSphere has the scale of parent and position on top of the parent.
                memento = new Memento(parent, position, scale);
                new MarkNetAction(memento.Parent.name, position, scale).Execute(); //ID: memento.Parent.name, memento.Position, memento.Scale
                result = true;
                CurrentState = IReversibleAction.Progress.Completed;
                AudioManagerImpl.EnqueueSoundEffect(IAudioManager.SoundEffect.NewNodeSound, parent);
            }
            return result;
        }

        /// <summary>
        /// The sphere that was added when this action was executed. It is saved so
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
            /// The parent of the new sphere.
            /// </summary>
            public readonly GameObject Parent;
            /// <summary>
            /// The position of the new sphere in world space.
            /// </summary>
            public readonly Vector3 Position;
            /// <summary>
            /// The scale of the new sphere in world space.
            /// </summary>
            public readonly Vector3 Scale;
            /// <summary>
            /// The parent ID for the added sphere. It must be kept to re-use the
            /// original name of the parent in Redo().
            /// </summary>
            public string ParentID;

            /// <summary>
            /// Constructor setting the information necessary to re-do this action.
            /// </summary>
            /// <param name="parent">parent of <paramref name="child"/></param>
            /// <param name="position">position of the parent</param>
            /// <param name="scale">scale of the parent</param>
            public Memento(GameObject parent, Vector3 position, Vector3 scale)
            {
                Parent = parent;
                Position = position;
                Scale = scale;
                ParentID = parent.ID();
            }
        }

        /// <summary>
        /// Undoes this AddNodeAction.
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
        /// Redoes this AddNodeAction.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            addedSphere = GameNodeMarker.NewSphere(memento.Parent, memento.Scale);
            if (addedSphere != null)
            {
                new MarkNetAction(memento.Parent.name, memento.Position, memento.Scale).Execute();
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
                memento.ParentID
            };
        }
    }
}

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
    /// Action to select and mark an element in a code city.
    /// </summary>
    internal class MarkAction : AbstractPlayerAction
    {
        /// <summary>
        /// If the user clicks with the mouse hitting a game object representing a graph node, this graph node
        /// will be marked with a sphere above.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            bool result = false;

            // FIXME: Needs adaptation for VR where no mouse is available.
            if (Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef _) == HitGraphElement.Node)
            {
                // the hit object is the parent in which to create the sphere marker
                GameObject parent = raycastHit.collider.gameObject;
                // The position at which the parent was hit will be the center point of the new node marker
                Vector3 position = parent.transform.position;
                Vector3 scale = parent.transform.lossyScale;
                addedSphere = GameNodeMarker.addSphere(parent, position: position, worldSpaceScale: scale);
                if (addedSphere != null)
                {
                    memento = new Memento(parent, position: position, scale: scale);
                    memento.NodeID = addedSphere.name;
                    new MarkNetAction(parentID: memento.Parent.name, memento.Position, memento.Scale).Execute();
                    result = true;
                    currentState = ReversibleAction.Progress.Completed;
                }
                else
                {
                    Debug.LogError($"New marker could not be created.\n");
                }
            }
            return result;
        }

        /// <summary>
        /// The game object that was added when this action was executed. It is saved so
        /// that it can be removed by Undo().
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
            /// The node ID for the added marker. It must be kept to re-use the
            /// original name of the marker in Redo().
            /// </summary>
            public string NodeID;

            /// <summary>
            /// Constructor setting the information necessary to re-do this action.
            /// </summary>
            /// <param name="parent">parent of the marker</param>
            /// <param name="position">position of the marker in world space</param>
            /// <param name="scale">scale of the new sphere in world space</param>
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
            addedSphere = GameNodeMarker.addSphere(memento.Parent, position: memento.Position, worldSpaceScale: memento.Scale);
            if (addedSphere != null)
            {
                new MarkNetAction(parentID: memento.Parent.name,memento.Position, memento.Scale).Execute();
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
        /// <returns><see cref="ActionStateType.MarkNode"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.MarkNode;
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
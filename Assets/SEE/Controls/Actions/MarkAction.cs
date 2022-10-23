using System.Collections.Generic;
using SEE.Game;
using SEE.GO;
using SEE.Net;
using SEE.Net.Actions;
using SEE.Utils;
using UnityEngine;


namespace SEE.Controls.Actions
{

    internal class MarkAction : AbstractPlayerAction
    {





        public override bool Update()
        {

            bool result = false;

            if (Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef _) == HitGraphElement.Node)
            {
                

                GameObject parent = raycastHit.collider.gameObject;
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
            }

            return result;
        }

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
            /// <param name="parent">parent of the new node</param>
            /// <param name="position">position of the new node in world space</param>
            /// <param name="scale">scale of the new node in world space</param>
            public Memento(GameObject parent, Vector3 position, Vector3 scale)
            {
                Parent = parent;
                Position = position;
                Scale = scale;
                NodeID = null;
            }
        }

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

        public override void Redo()
        {
            base.Redo();
            addedSphere = GameNodeMarker.addSphere(memento.Parent, position: memento.Position, worldSpaceScale: memento.Scale);
            if (addedSphere != null)
            {
                new MarkNetAction(parentID: memento.Parent.name, memento.Position, memento.Scale).Execute();
            }
        }
        
        public static ReversibleAction CreateReversibleAction()
        {
            return new MarkAction();
        }
        
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
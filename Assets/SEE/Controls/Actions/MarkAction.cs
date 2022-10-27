using System.Collections.Generic;
using System.Diagnostics;
using System;
using SEE.Game;
using SEE.GO;
using SEE.Net;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to mark a node.
    /// </summary>
    internal class MarkAction : AbstractPlayerAction
    {
        /// <summary>
        /// If the user clicks with the mouse hitting a game object representing a graph node,
        /// this graph node is marked by a sphere above it.
        /// <see cref="ReversibleAction.Update"/>.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            bool result = false;
            if (Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef _) == HitGraphElement.Node)
            {
                // the hit object is the parent to which the marking sphere is to be added
                GameObject parent = raycastHit.collider.gameObject;
                bool hasSphere = false;
                // check if parent already has a marker and destroy it
                foreach(Transform child in parent.transform)
                {
                    if(child.name == "Sphere")
                    {
                        Destroyer.DestroyGameObject(child.gameObject);
                        hasSphere = true;
                    }
                }
                // if parent has no marker yet, create one
                if (!hasSphere)
                {
                    addedNodeMarker = GameNodeMarker.AddMarker(parent);
                    if(addedNodeMarker != null)
                    {
                        memento = new Memento(parent);
                        new MarkNetAction(parentID: memento.Parent.name).Execute();
                        result = true;
                        currentState = ReversibleAction.Progress.Completed;
                    }
                    else
                    {
                        UnityEngine.Debug.LogError("Marker could not be created");
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// The sphere that was added when this action was executed. It is saved so
        /// that it can be removed on Undo().
        /// </summary>
        private GameObject addedNodeMarker;

        /// <summary>
        /// Memento capturing the data necessary to re-do this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// The information we need to re-add a sphere whose addition was undone.
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The parent of the new sphere.
            /// </summary>
            public readonly GameObject Parent;
            
            /// <summary>
            /// Constructor setting the information necessary to re-do this action.
            /// </summary>
            /// <param name="parent">parent of the new sphere</param>
            public Memento(GameObject parent)
            {
                Parent = parent;
            }
        }

        /// <summary>
        /// Undoes this MarkAction.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            if (addedNodeMarker != null)
            {
                new DeleteNetAction(addedNodeMarker.name).Execute();
                Destroyer.DestroyGameObject(addedNodeMarker);
                addedNodeMarker = null;
            }
        }

        /// <summary>
        /// Redoes this MarkAction.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            addedNodeMarker = GameNodeMarker.AddMarker(memento.Parent);
            if (addedNodeMarker != null)
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
        /// <returns><see cref="ActionStateType.NewMarker"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.NewMarker;
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
    }
}

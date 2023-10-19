using OpenCVForUnity.ImgprocModule;
using SEE.Audio;
using SEE.Controls.Actions;
using SEE.Game.SceneManipulation;
using SEE.GO;
using SEE.Net.Actions;
using SEE.Utils.History;
using SEE.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// To solve some of these tasks, I looked up some code from the last few years in the repository.
    /// So I think some of the codes serve as sources, so I've included them,
    /// but if looking up codes from recent years is a problem, please let me know.
    /// </summary>

    public class MarkAction : AbstractPlayerAction
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


                GameNodeMarker.NodeMarker(parent);
                memento = new Memento(parent);
                new MarkNetAction(parentID: memento.Parent.name, position, scale).Execute();
                result = true;
                currentState = IReversibleAction.Progress.Completed;
            }

            return result;
        }

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
        public override void Redo()
        {
            base.Redo();
            addedNodeMarker = GameNodeMarker.NodeMarker(memento.Parent);
            if (addedNodeMarker != null)
            {
                new MarkNetAction(parentID: memento.Parent.name, memento.Position, memento.Scale).Execute();
            }
        }

        public static IReversibleAction CreateReversibleAction()
        {
            return new MarkAction();
        }

        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.NewNode"/></returns>
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
                memento.Parent.name
            };
        }
    }
}

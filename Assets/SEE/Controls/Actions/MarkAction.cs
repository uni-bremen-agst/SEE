using System.Collections.Generic;
using SEE.GO;
using SEE.Utils.History;
using UnityEngine;
using SEE.Game.SceneManipulation;
using SEE.Utils;
using SEE.Net;

namespace SEE.Controls.Actions
{
    internal class MarkAction : AbstractPlayerAction
    {
        /// <summary>
        /// The node that was marked when this action was executed. It is saved so
        /// action can be removed on Undo().
        /// </summary>
        private GameObject markedNode;

        /// <summary>
        /// Memento capturing the data necessary to re-do this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// The information we need to re-mark a node.
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The parent of the marker.
            /// </summary>
            public readonly GameObject Parent;

            /// <summary>
            /// Constructor setting the information necessary to re-do this action.
            /// </summary>
            /// <param name="Parent">parent of <paramref name="child"/></param>
            public Memento(GameObject parent)
            {
                Parent = parent;
            }
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

        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        public override bool Update()
        {
            bool result = false;

            // FIXME: Needs adaptation for VR where no mouse is available.
            if (Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef _) == HitGraphElement.Node)
            {
                // the hit object is the parent of the marker-sphere
                GameObject parent = raycastHit.collider.gameObject;

                markedNode = GameNodeMarker.AddMarker(parent);
                    // Node has the scale and position of parent, which is set in the AddMarker-method.

                memento = new Memento(parent: parent);
                new MarkNetAction(parent.name).Execute();
                result = true;
                CurrentState = IReversibleAction.Progress.Completed;
                }
            return result;
        }

        /// <summary>
        /// Undoes this AddNodeAction.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            if (markedNode != null)
            {
                //Object will be destroyed
                new MarkNetAction(memento.Parent.name).Execute();
                Destroyer.Destroy(markedNode);

                markedNode = null;
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

    }
}

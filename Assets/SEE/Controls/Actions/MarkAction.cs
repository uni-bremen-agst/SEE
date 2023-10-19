using System.Collections.Generic;
using SEE.GO;
using SEE.Utils.History;
using UnityEngine;
using SEE.Game.SceneManipulation;
using SEE.Utils;
using SEE.Net;
using System.Linq;

namespace SEE.Controls.Actions
{
    internal class MarkAction : AbstractPlayerAction
    {
        /// <summary>
        /// The node that was marked when this action was executed. It is saved so
        /// that it can be removed on Undo().
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
            /// The position of the marker in world space.
            /// </summary>
            public readonly Vector3 Position;
            /// <summary>
            /// The scale of the marker in world space.
            /// </summary>
            public readonly Vector3 Scale;
            /// <summary>
            /// The node ID for the marker
            /// </summary>
            public string NodeID;

            /// <summary>
            /// Constructor setting the information necessary to re-do this action.
            /// </summary>
            /// <param name="Parent">parent of <paramref name="child"/></param>
            /// <param name="Position">position of the marked node</param>
            /// <param name="Scale">position of the marked node</param>
            /// <param name="NodeID">NodeID of the marker</param>
            public Memento(GameObject parent)
            {
                Parent = parent;
                Position = parent.transform.position;
                Scale = parent.transform.lossyScale;
                NodeID = null;
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
                memento.Parent.name,
                memento.NodeID
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

                // search for marked node
                foreach (Transform node in
                from Transform node in parent.transform
                where node.name == "Sphere"
                select node)
                {
                    return GameNodeMarker.DeleteMarker(node.gameObject);
                }

                markedNode = GameNodeMarker.AddMarker(parent);
                    // Node has the scale and position of parent, which is set in the AddMarker-method.

                memento = new Memento(parent: parent);
                memento.NodeID = markedNode.name;
                new MarkNetAction(parent.name ,memento.NodeID).Execute();
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
                //FIXME: Change doesn't work for client yet
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
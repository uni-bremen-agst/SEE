using System.Collections.Generic;
using SEE.Game;
using SEE.GO;
using SEE.Net;
using SEE.Utils;
using UnityEngine;

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
#if UNITY_ANDROID
            // FIXME: This branch of the the #ifdef and the #else branch should be consolidated.
            // Check for touch input
            if (Input.touchCount != 1)
            {
                return result;
            }
            Touch touch = Input.touches[0];
            Vector3 touchPosition = touch.position;

            if (touch.phase == TouchPhase.Began)
            {
                Ray ray = Camera.main.ScreenPointToRay(touchPosition);
                RaycastHit raycastHit;
                if (Physics.Raycast(ray, out raycastHit))
                {
                    if (raycastHit.collider.tag == DataModel.Tags.Node)
                    {
                        // the hit object is the parent in which to create the new node
                        GameObject parent = raycastHit.collider.gameObject;
                        // The position at which the parent was hit will be the center point of the new node
                        Vector3 position = raycastHit.point;
                        Vector3 scale = FindSize(parent, position);
                        addedGameNode = GameNodeAdder.AddChild(parent, position: position, worldSpaceScale: scale);
                        if (addedGameNode != null)
                        {
                            memento = new Memento(parent, position: position, scale: scale);
                            memento.NodeID = addedGameNode.name;
                            new AddNodeNetAction(parentID: memento.Parent.name, newNodeID: memento.NodeID, memento.Position, memento.Scale).Execute();
                            result = true;
                            currentState = ReversibleAction.Progress.Completed;
                        }
                        else
                        {
                            Debug.LogError($"New node could not be created.\n");
                        }
                    }
                }
            }
#else
            // FIXME: Needs adaptation for VR where no mouse is available.
            if (Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef _) == HitGraphElement.Node)
            {
                // the hit object is the parent in which to create the new node
                GameObject parent = raycastHit.collider.gameObject;
                // The position at which the parent was hit will be the center point of the new node
                Vector3 position = raycastHit.point;
                Vector3 scale = FindSize(parent, position);
                addedGameNode = GameNodeAdder.AddChild(parent, position: position, worldSpaceScale: scale);
                if (addedGameNode != null)
                {
                    memento = new Memento(parent, position: position, scale: scale);
                    memento.NodeID = addedGameNode.name;
                    new AddNodeNetAction(parentID: memento.Parent.name, newNodeID: memento.NodeID, memento.Position, memento.Scale).Execute();
                    result = true;
                    currentState = ReversibleAction.Progress.Completed;
                }
                else
                {
                    Debug.LogError($"New node could not be created.\n");
                }
            }
#endif
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

        /// <summary>
        /// Returns a scale of a square with the given center <see cref="position"/>
        /// that fits into the ground area of <paramref name="parent"/>.
        /// </summary>
        /// <param name="parent">parent in which to fit the rectangle</param>
        /// <param name="position">center position of the rectangle</param>
        /// <returns>the scale of a square (actually a cube, but with a very small height)
        /// with center <see cref="position"/> that fits into the ground area of <paramref name="parent"/></returns>
        private static Vector3 FindSize(GameObject parent, Vector3 position)
        {
            // TODO: We might want to implement something smarter
            // than that, see for instance:
            // https://stackoverflow.com/questions/51574829/how-to-algorithmically-find-the-biggest-rectangle-that-can-fit-in-a-space-with-o
            Vector3 result = parent.transform.lossyScale / 10;
            // The ground area of the result must be a square.
            if (result.x > result.z)
            {
                result.x = result.z;
            }
            else
            {
                result.z = result.x;
            }
            result.y = 0.01f;
            return result;
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
                Destroyer.DestroyGameObject(addedGameNode);
                addedGameNode = null;
            }
        }

        /// <summary>
        /// Redoes this AddNodeAction.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            addedGameNode = GameNodeAdder.AddChild(memento.Parent, position: memento.Position, worldSpaceScale: memento.Scale, nodeID: memento.NodeID);
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
            return ActionStateType.NewNode;
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

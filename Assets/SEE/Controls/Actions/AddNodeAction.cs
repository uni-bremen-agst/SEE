using SEE.GO;
using SEE.Utils;
using SEE.Game;
using UnityEngine;
using SEE.Net;
using System.Collections.Generic;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to create a new node for a selected city.
    /// </summary>
    public class AddNodeAction : AbstractPlayerAction
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
                // the hit object is the parent in which to create the new node as a child
                GameObject parent = raycastHit.collider.gameObject;
                if (parent.TryGetComponentOrLog(out NodeRef parentNodeRef))
                {
                    bool parentWasFormerlyLeaf = parentNodeRef.Value.IsLeaf();

                    // We need to add the child first so that the parent is 
                    // definitely no longer a leaf because a child was added.
                    // The drawing of the node depends upon that (there may
                    // be different metrics chosen for visual attributes depending
                    // upon whether a node is a leaf or inner node.
                    // TODO: Find a fitted scaling and replace filler
                    Vector3 childScale = 2 * FindSize(parent, parent.transform.position);

                    // FIXME: The following will not work if we create multiple children
                    // because all of them would be placed at the same position.
                    Vector3 childPosition = parent.transform.position;
                    childPosition.y = parent.GetRoof() + childScale.y / 2.0f + float.Epsilon;

                    addedGameNode = GameNodeAdder.Add(parent, position: childPosition, worldSpaceScale: childScale);
                    if (addedGameNode != null)
                    {
                        UnityEngine.Assertions.Assert.AreEqual(parent, addedGameNode.transform.parent.gameObject);
                        // If the parent was formerly a leaf, we need to turn it into an inner node.
                        if (parentWasFormerlyLeaf)
                        {
                            SEECity city = parent.ContainingCity();
                            if (city != null)
                            {
                                city.Renderer.ToInnerNode(ref parent);
                                // The height of parent may have changed, hence, we need to adjust
                                // the y co-ordinate of its child again.
                                childPosition.y = parent.GetRoof() + childScale.y / 2.0f + float.Epsilon;
                                addedGameNode.transform.position = childPosition;

                                // FIXME: We need to propagate the migration of this former leaf node
                                // to an inner node to all clients in the network.
                            }
                            else
                            {
                                Debug.LogError($"Parent {parent.name} of new node to be added is not contained in a code city.\n");
                            }
                        }

                        memento = new Memento(parent, position: childPosition, scale: childScale)
                        {
                            NodeID = addedGameNode.name
                        };
                        new AddNodeNetAction(parentID: memento.Parent.name, newNodeID: memento.NodeID, memento.Position, memento.Scale).Execute();

                        result = true;
                        currentState = ReversibleAction.Progress.Completed;
                    }
                    else
                    {
                        Debug.LogError("New node could not be created.\n");
                    }
                }
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
                /// FIXME: If the parent of addedGameNode was formerly a leaf,
                /// we need to turn into a leaf again. 
                /// Implement and call <see cref="GraphRenderer.ToLeaf(ref GameObject)"/>.
                new DeleteNetAction(addedGameNode.name).Execute();
                GameNodeAdder.Remove(addedGameNode);
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
            // FIXME: If the parent of addedGameNode was originally a leaf
            // that was then into an inner node during Update and then 
            // back again into a leaf by way of Undo, it must now become
            // an inner node again.
            /// Call <see cref="GraphRenderer.ToInnerNode(ref GameObject)"/>.
            addedGameNode = GameNodeAdder.Add(memento.Parent, position: memento.Position, worldSpaceScale: memento.Scale, nodeID: memento.NodeID);
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

using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.GO;
using SEE.Net;
using SEE.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to delete the currently selected game object (edge or node).
    /// </summary>
    internal class DeleteAction : AbstractPlayerAction
    {
        /// <summary>
        /// Returns a new instance of <see cref="DeleteAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new DeleteAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="DeleteAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// The waiting time of the animation for moving a node into a garbage can from over the garbage can.
        /// </summary>
        private const float TimeToWait = 1f;

        /// <summary>
        /// The animation time of the animation of moving a node to the top of the garbage can.
        /// </summary>
        private const float TimeForAnimation = 1f;

        /// <summary>
        /// Contains all nodes and edges deleted as explicitly requested by the user.
        /// As a consequence of deleting a node, its ancestors along with their incoming and outgoing
        /// edges may be deleted implicitly, too. All of these are kept in <see cref="deletedNodes"/>
        /// and <see cref="deletedEdges"/>. Yet, if we need to redo a deletion, we need to remember
        /// the explicitly deleted objects.
        /// </summary>
        private ISet<GameObject> explicitlyDeletedNodesAndEdges = new HashSet<GameObject>();

        /// <summary>
        /// A history of all nodes and the graph where they were attached to, deleted by this action.
        /// </summary>
        private Dictionary<GameObject, Graph> deletedNodes { get; set; } = new Dictionary<GameObject, Graph>();

        /// <summary>
        /// A history of the old positions of the nodes deleted by this action.
        /// </summary>
        private Dictionary<GameObject, Vector3> oldPositions = new Dictionary<GameObject, Vector3>();

        /// <summary>
        /// A history of all edges and the graph where they were attached to, deleted by this action.
        /// </summary>
        private Dictionary<GameObject, Graph> deletedEdges { get; set; } = new Dictionary<GameObject, Graph>();

        /// <summary>
        /// A list of ratios of the current localScale and a target scale.
        /// </summary>
        private Dictionary<GameObject, Vector3> shrinkFactors { get; set; } = new Dictionary<GameObject, Vector3>();

        /// <summary>
        ///  A vector for an objects localScale which fits into the garbage can.
        ///  TODO: Currently set to an absolute value. Should be set abstract, e.g., half of the 
        ///  garbage can's diameter. 
        /// </summary>
        private readonly Vector3 defaultGarbageVector = new Vector3(0.1f, 0.1f, 0.1f);

        /// <summary>
        /// Number of animations used for an object's expansion, removing it from the garbage can.
        /// </summary>
        private const float stepsOfExpansionAnimation = 10;

        /// <summary>
        /// The time (in seconds) between animations of expanding a node that is being restored
        /// from the garbage can.
        /// </summary>
        private const float timeBetweenExpansionAnimation = 0.14f;

        /// <summary>
        /// The name of the garbage can gameObject.
        /// </summary>
        private const string GarbageCanName = "GarbageCan";

        /// <summary>
        /// The garbage can the deleted nodes will be moved to. It is the object named 
        /// <see cref="GarbageCanName"/>.
        /// </summary>
        private GameObject garbageCan;

        /// <summary>
        /// True, if the moving process of a node to the garbage can is running, else false.
        /// Avoids multiple calls of coroutine.
        /// </summary>
        private bool animationIsRunning = false;

        /// <summary>
        /// Sets <see cref="garbageCan"/> by retrieving it by name <see cref="GarbageCanName"/>
        /// from the scene.
        /// </summary>
        public override void Awake()
        {
            garbageCan = GameObject.Find(GarbageCanName);
        }

        /// <summary>
        /// Disables the general selection provided by <see cref="SEEInput.Select"/>.
        /// We need to avoid that the selection of graph elements to be deleted 
        /// interferes with the general <see cref="SelectAction"/>.
        /// </summary>
        public override void Start()
        {
            base.Start();
            SEEInput.SelectionEnabled = false;
        }

        /// <summary>
        /// Re-enables the general selection provided by <see cref="SEEInput.Select"/>.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            SEEInput.SelectionEnabled = true;
        }

        /// <summary>
        /// See <see cref="ReversibleAction.Update"/>.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            // FIXME: Needs adaptation for VR where no mouse is available.
            if (!animationIsRunning
                && Input.GetMouseButtonDown(0)
                && Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef _) != HitGraphElement.None)
            {
                // the hit object is the parent in which to create the new node
                GameObject hitGraphElement = raycastHit.collider.gameObject;
                Assert.IsTrue(hitGraphElement.HasNodeRef() || hitGraphElement.HasEdgeRef());
                explicitlyDeletedNodesAndEdges.Add(hitGraphElement);
                bool result = Delete(hitGraphElement);
                hadAnEffect = result;  
                return result; // the selected objects are deleted and this action is done now
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Deletes given <paramref GameObject="selectedObject"/> assumed to be either an
        /// edge or node. If it represents a node, the incoming and outgoing edges and
        /// its ancestors will be removed, too. For the possibility of an undo, the deleted objects will be saved. 
        /// 
        /// Precondition: <paramref name="deletedObject"/> != null.
        /// </summary>
        /// <param name="deletedObject">selected GameObject that along with its children should be removed</param>
        /// <returns>true if <paramref name="deletedObject"/> was actually deleted</returns>
        private bool Delete(GameObject deletedObject)
        {            
            if (deletedObject.CompareTag(Tags.Edge))
            {
                InteractableObject.UnselectAll(true);
                DeleteEdge(deletedObject);
            }
            else if (deletedObject.CompareTag(Tags.Node))
            {
                if (deletedObject.GetNode().IsRoot())
                {
                    Debug.LogError("A root shall not be deleted.\n");
                    return false;
                }
                else
                {
                    InteractableObject.UnselectAll(true);
                    // The selectedObject (a node) and its ancestors are not deleted immediately. Instead we
                    // will run an animation that moves them into a garbage bin. Only when they arrive there,
                    // we will actually delete them.
                    // FIXME: Shouldn't the edges be moved to the garbage bin, too?
                    PlayerSettings.GetPlayerSettings().StartCoroutine(this.MoveNodeToGarbage(deletedObject.AllAncestors()));
                }
            }
            new DeleteNetAction(deletedObject.name).Execute();
            return true;
        }

        /// <summary>
        /// Undoes this DeleteAction.
        /// </summary>
        public override void Undo()
        {
            // Re-add all nodes to their graphs.
            foreach (KeyValuePair<GameObject, Graph> nodeGraphPair in deletedNodes)
            {
                if (nodeGraphPair.Key.TryGetComponentOrLog(out NodeRef nodeRef))
                {
                    if (!nodeGraphPair.Value.Contains(nodeRef.Value))
                    {
                        nodeGraphPair.Value.AddNode(nodeRef.Value);
                    }
                }
            }
            // Re-add all edges to their graphs.
            foreach (KeyValuePair<GameObject, Graph> edgeGraphPair in deletedEdges)
            {

                if (edgeGraphPair.Key.TryGetComponentOrLog(out EdgeRef edgeReference))
                {
                    edgeGraphPair.Value.AddEdge(edgeReference.Value);
                    PlayerSettings.GetPlayerSettings().StartCoroutine(DelayEdges(edgeGraphPair.Key));
                }
            }
            PlayerSettings.GetPlayerSettings().StartCoroutine(this.RemoveNodeFromGarbage(new List<GameObject>(deletedNodes.Keys)));
        }

        /// <summary>
        /// Delays the process of showing a hidden edge having been removed from the garbage can.
        /// </summary>
        private IEnumerator DelayEdges(GameObject edge)
        {
            yield return new WaitForSeconds(TimeForAnimation + TimeToWait);
            edge.SetVisibility(true, true);
        }
        /// <summary>
        /// Redoes this DeleteAction.
        /// </summary>
        public override void Redo()
        {
            foreach (GameObject gameObject in explicitlyDeletedNodesAndEdges)
            {
                Delete(gameObject);
            }
        }

        /// <summary>
        /// Moves all nodes in <paramref name="deletedNodes"/> to the garbage can
        /// using an animation. When they finally arrive there, they will be 
        /// deleted. 
        /// 
        /// Assumption: <paramref name="deletedNodes"/> contains all nodes in a subtree
        /// of the game-node hierarchy. All of them represent graph nodes.
        /// </summary>
        /// <param name="deletedNodes">the deleted nodes which will be moved to the garbage can.</param>
        /// <returns>the waiting time between moving deleted nodes over the garbage can and then into the garbage can</returns>
        private IEnumerator MoveNodeToGarbage(IList<GameObject> deletedNodes)
        {
            animationIsRunning = true;
            // We need to reset the portal of all all deletedNodes so that we can move
            // them to the garbage bin. Otherwise they will become invisible if they 
            // leave their portal.
            foreach (GameObject deletedNode in deletedNodes)
            {
                oldPositions[deletedNode] = deletedNode.transform.position;
                if (!this.deletedNodes.ContainsKey(deletedNode))
                {
                    Portal.SetInfinitePortal(deletedNode);
                }
            }
            MarkAsDeleted(deletedNodes);
            foreach (GameObject deletedNode in deletedNodes)
            {
                Tweens.Move(deletedNode, new Vector3(garbageCan.transform.position.x, garbageCan.transform.position.y + 1.4f, garbageCan.transform.position.z), TimeForAnimation);
            }

            yield return new WaitForSeconds(TimeToWait);

            foreach (GameObject deletedNode in deletedNodes)
            {
                Vector3 shrinkFactor = VectorOperations.DivideVectors(deletedNode.transform.localScale, defaultGarbageVector);
                if (!shrinkFactors.ContainsKey(deletedNode))
                {
                    shrinkFactors.Add(deletedNode, shrinkFactor);
                }
                deletedNode.transform.localScale = Vector3.Scale(deletedNode.transform.localScale, shrinkFactor);
                Tweens.Move(deletedNode, new Vector3(garbageCan.transform.position.x, garbageCan.transform.position.y, garbageCan.transform.position.z), TimeForAnimation);
            }
            yield return new WaitForSeconds(TimeToWait);
            animationIsRunning = false;
        }

        /// <summary>
        /// Coroutine that waits and expands the shrunk object which is currently being removed from the garbage can.
        /// </summary>
        /// <param name="deletedNode">The nodes to be removed from the garbage-can</param>
        /// <returns>the waiting time between moving deleted nodes from the garbage-can and then to the city</returns>
        private IEnumerator WaitAndExpand(GameObject deletedNode)
        {
            yield return new WaitForSeconds(TimeToWait);
            Vector3 shrinkFactor = shrinkFactors[deletedNode];
            float animationsCount = stepsOfExpansionAnimation;
            float exponent = 1 / stepsOfExpansionAnimation;
            shrinkFactor = VectorOperations.ExponentOfVectorCoordinates(shrinkFactor, exponent);

            while (animationsCount > 0)
            {
                deletedNode.transform.localScale = VectorOperations.DivideVectors(shrinkFactor, deletedNode.transform.localScale);
                yield return new WaitForSeconds(timeBetweenExpansionAnimation);
                animationsCount--;
            }
        }
        /// <summary>
        /// Removes all given nodes from the garbage can and back into the city.
        /// </summary>
        /// <param name="deletedNode">The nodes to be removed from the garbage-can</param>
        /// <returns>the waiting time between moving deleted nodes from the garbage-can and then to the city</returns>
        private IEnumerator RemoveNodeFromGarbage(IList<GameObject> deletedNodes)
        {
            animationIsRunning = true;
            // up, out of the garbage can
            foreach (GameObject deletedNode in deletedNodes)
            {
                Tweens.Move(deletedNode, new Vector3(garbageCan.transform.position.x, garbageCan.transform.position.y + 1.4f, garbageCan.transform.position.z), TimeForAnimation);
                PlayerSettings.GetPlayerSettings().StartCoroutine(WaitAndExpand(deletedNode));
            }

            yield return new WaitForSeconds(TimeToWait);

            // back to the original position
            foreach (GameObject node in deletedNodes)
            {
                Tweens.Move(node, oldPositions[node], TimeForAnimation);
            }

            yield return new WaitForSeconds(TimeToWait);
            oldPositions.Clear();
            this.deletedNodes.Clear();
            deletedEdges.Clear();
            animationIsRunning = false;
            InteractableObject.UnselectAll(true);
        }

        /// <summary>
        /// Marks the given <paramref name="gameNodesToDelete"/> as deleted, i.e.,
        /// 1) removes the associated nodes represented by thos <paramref name="gameNodesToDelete"/> 
        ///    from their graph
        /// 2) removes the incoming and outgoing edges of <paramref name="gameNodesToDelete"/>
        ///    from their graph and makes those invisible
        /// 
        /// Assumption: <paramref name="gameNodesToDelete"/> contains all nodes in a subtree
        /// of the game-node hierarchy. All of them represent graph nodes.
        /// </summary>
        /// <param name="gameNodesToDelete">all deleted objects of the last operation</param>
        private void MarkAsDeleted(IList<GameObject> gameNodesToDelete)
        {
            ISet<GameObject> edgesInScene = new HashSet<GameObject>(GameObject.FindGameObjectsWithTag(Tags.Edge));

            // First identify all incoming and outgoing edges for all nodes in gameNodesToDelete
            HashSet<GameObject> implicitlyDeletedEdges = new HashSet<GameObject>();
            foreach (GameObject deletedGameNode in gameNodesToDelete)
            {
                if (deletedGameNode.TryGetComponentOrLog(out NodeRef nodeRef))
                {
                    ISet<string> attachedEdges = nodeRef.GetEdgeIds();

                    foreach (GameObject edge in edgesInScene)
                    {
                        if (edge.activeInHierarchy && attachedEdges.Contains(edge.name))
                        {
                            // We will not immediately delete this edge here, because it may be an
                            // edge inbetween two nodes both contained in gameNodesToDelete, in which
                            // case it will show up as an incoming and outgoing edge.
                            implicitlyDeletedEdges.Add(edge);
                        }
                    }
                }
            }

            // Now delete the incoming and outgoing edges.
            foreach (GameObject implicitlyDeletedEdge in implicitlyDeletedEdges)
            {
                DeleteEdge(implicitlyDeletedEdge);
            }

            // Finally, we remove the nodes themselves.
            foreach (GameObject deletedGameNode in gameNodesToDelete)
            {
                DeleteNode(deletedGameNode);
            }
        }

        /// <summary>
        /// Deletes the given <paramref name="gameNode"/>, that is, it will remove
        /// the associated Node it from its graph. The <paramref name="gameNode"/>
        /// itself is not deleted or made invisible (because it will be needed during 
        /// the animation).
        /// 
        /// Precondition: <paramref name="gameNode"/> must have an <see cref="NodeRef"/>
        /// attached to it.
        /// </summary>
        /// <param name="gameNode">a game object representing an edge</param>
        private void DeleteNode(GameObject gameNode)
        {
            if (gameNode.TryGetComponentOrLog(out NodeRef nodeRef))
            {
                Graph graph = nodeRef.Value.ItsGraph;
                deletedNodes[gameNode] = graph;
                graph.RemoveNode(nodeRef.Value);
            }
        }

        /// <summary>
        /// Deletes the given <paramref name="gameEdge"/>, that is, it will make it
        /// invisible and remove it from its graph.
        /// 
        /// Precondition: <paramref name="gameEdge"/> must have an <see cref="EdgeRef"/>
        /// attached to it.
        /// </summary>
        /// <param name="gameEdge">a game object representing an edge</param>
        private void DeleteEdge(GameObject gameEdge)
        {
            if (gameEdge.TryGetComponentOrLog(out EdgeRef edgeRef))
            {
                gameEdge.SetVisibility(false, true);
                Graph graph = edgeRef.Value.ItsGraph;
                deletedEdges[gameEdge] = graph;
                graph.RemoveEdge(edgeRef.Value);
            }
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.Delete"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.Delete;
        }

        public override List<string> GetChangedObjects()
        {
            List<string> changedObjects = new List<string>();
            foreach(GameObject deletedNode in deletedNodes.Keys)
            {
                changedObjects.Add(deletedNode.name);
            }
            foreach (GameObject deletedEdge in deletedEdges.Keys)
            {
                changedObjects.Add(deletedEdge.name);
            }
            foreach (GameObject deletedObject in explicitlyDeletedNodesAndEdges)
            {
                changedObjects.Add(deletedObject.name);
            }

            return changedObjects.Distinct().ToList();
        }
    }
}

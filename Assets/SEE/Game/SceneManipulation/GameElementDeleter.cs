using Cysharp.Threading.Tasks;
using MoreLinq;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GameObjects;
using SEE.GO;
using SEE.Tools.ReflexionAnalysis;
using SEE.UI.Notification;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Game.SceneManipulation
{
    /// <summary>
    /// Allows to delete nodes and edges.
    /// </summary>
    internal static class GameElementDeleter
    {
        /// <summary>
        /// Removes the graph node associated with the given <paramref name="gameNode"/>
        /// from its graph. <paramref name="gameNode"/> is not actually destroyed.
        ///
        /// Precondition: <paramref name="gameNode"/> must have a valid <see cref="NodeRef"/>; otherwise
        /// an exception will be thrown.
        /// </summary>
        /// <param name="gameNode">game object whose graph node is to be removed from the graph</param>
        public static void RemoveNodeFromGraph(GameObject gameNode)
        {
            Node node = gameNode.GetNode();
            Graph graph = node.ItsGraph;
            graph.RemoveNode(node);
        }

        /// <summary>
        /// Deletes given  <paramref name="deletedObject"/> assumed to be either an
        /// edge or node. If it represents a node, its ancestors and any edges related
        /// to those will be removed, too. If it is an edge, only that edge is deleted.
        ///
        /// Note: The objects are not actually destroyed. They are just marked as deleted
        /// by way of <see cref="GameObjectFader"/>. That means to let them blink and
        /// fade out and eventually be set inactive. However, their corresponding graph node
        /// or edge, respectively, are removed from their graph.
        ///
        /// Precondition: <paramref name="deletedObject"/> != null.
        /// </summary>
        /// <param name="deletedObject">the game object that along with its descendants and
        /// their edges should be removed</param>
        /// <returns>the graph from which <paramref name="deletedObject"/> was removed
        /// along with all descendants of <paramref name="deletedObject"/> and their incoming
        /// and outgoing edges marked as deleted along with <paramref name="deletedObject"/></returns>
        /// <exception cref="InvalidOperationException ">thrown if <paramref name="deletedObject"/> is a root</exception>
        /// <exception cref="ArgumentException">thrown if <paramref name="deletedObject"/> is
        /// neither a node nor an edge</exception>
        public static (GraphElementsMemento, ISet<GameObject>, Dictionary<string, VisualNodeAttributes>) Delete(GameObject deletedObject)
        {
            Dictionary<string, VisualNodeAttributes> deletedNodeTypes = new();
            if (deletedObject.CompareTag(Tags.Edge))
            {
                (GraphElementsMemento gem, ISet<GameObject> setGO) = DeleteEdge(deletedObject);
                return (gem, setGO, deletedNodeTypes);
            }
            else if (deletedObject.CompareTag(Tags.Node))
            {
                Node deletedNode = deletedObject.GetNode();
                if (deletedNode.IsRoot())
                {
                    throw new InvalidOperationException("A root shall not be deleted.\n");
                }
                else
                {
                    // Architecture and implementation root nodes should only be cleared (all children deleted)
                    // instead of deleting the entire node, as it is not possible to add new architecture or
                    // implementation root nodes.
                    if (deletedNode.IsArchitectureOrImplementationRoot())
                    {
                        IEnumerable<Node> children = deletedNode.IsInArchitecture() ?
                            deletedNode.ItsGraph.Nodes().Where(node => node.IsInArchitecture()
                                                                && !node.HasRootToogle()
                                                                && node.Parent != null
                                                                && (!node.Parent.IsInArchitecture()
                                                                    || node.Parent.IsArchitectureOrImplementationRoot())) :
                            deletedNode.ItsGraph.Nodes().Where(node => node.IsInImplementation()
                                                                && !node.HasRootToogle()
                                                                && node.Parent != null
                                                                && (!node.Parent.IsInImplementation()
                                                                    || node.Parent.IsArchitectureOrImplementationRoot()));
                        if (deletedNode.Children().Count() != children.Count()
                            || deletedObject.transform.GetComponentsInChildren<NodeRef>().Count() != deletedNode.PostOrderDescendants().Count)
                        {
                            ShowNotification.Warn("Can't clear.", "Because the mapping process has already started.");
                            return (null, null, deletedNodeTypes);
                        }
                        Dictionary<string, VisualNodeAttributes> deletedNT = CaptureNodeTypesToRemove(deletedNode);
                        deletedNodeTypes = deletedNodeTypes.Concat(deletedNT)
                            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                        ISet<GameObject> deletedGameObjects = new HashSet<GameObject>();
                        SubgraphMemento subgraphMemento = new(deletedNode.ItsGraph);
                        foreach (Node child in deletedNode.Children().ToList())
                        {
                            (GraphElementsMemento mem, ISet<GameObject> deleted, Dictionary<string, VisualNodeAttributes> deletedNTypes) = Delete(child.GameObject());
                            deletedGameObjects.UnionWith(deleted);
                            deletedNodeTypes = deletedNodeTypes.Concat(deletedNTypes)
                                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                            SubgraphMemento subMem = (SubgraphMemento)mem;
                            subMem.Parents.ForEach(pair => subgraphMemento.Parents.Add(pair));
                            subMem.Edges.ForEach(edge => subgraphMemento.Edges.Add(edge));
                        }
                        return (subgraphMemento, deletedGameObjects, deletedNodeTypes);
                    }
                    else
                    {
                        if (deletedNode.IsInArchitecture())
                        {
                            // We first need to remove any mapping that exists to this architecture node.
                            // This triggers the `ReflexionVisualization` observer.
                            // If we didn't do this, the implementation node (whose GameObject descends deletedObject)
                            // would also get deleted along with the architecture node.
                            deletedNode.Incomings.Where(x => x.HasSupertypeOf(ReflexionGraph.MapsToType)).ToList()
                                       .ForEach(deletedNode.ItsGraph.RemoveEdge);
                        }
                        // Note: DeleteTree(deletedObject) assumes that the nodes are still
                        // in the underlying graph, that is why we must call deletedNode.DeleteTree()
                        // after DeleteTree(deletedObject).
                        // TODO: Rather than call DeleteTree, this executor should subscribe as an observer to the Graph.
                        ISet<GameObject> deletedGameObjects = DeleteTree(deletedObject);
                        SubgraphMemento subgraphMemento = deletedNode.DeleteTree();
                        return (subgraphMemento, deletedGameObjects, deletedNodeTypes);
                    }
                }
            }
            else
            {
                throw new ArgumentException($"Game object {deletedObject.name} must be a node or edge.");
            }
        }

        /// <summary>
        /// Identifies the node types belonging to this subgraph and removes them from the graph.
        /// </summary>
        /// <param name="root">The root of this subgraph.</param>
        private static Dictionary<string, VisualNodeAttributes> CaptureNodeTypesToRemove(Node root)
        {
            Dictionary<string, VisualNodeAttributes> deletedNodeTypes = new();
            IEnumerable<string> typesToRemove = GetNodeTypesFromSubgraph(root);
            SEEReflexionCity city = root.GameObject().ContainingCity<SEEReflexionCity>();
            IEnumerable<string> remainingTypes = GetRemainingGraphNodeTypes(root, city);
            IEnumerable<string> typesDifference = typesToRemove.Except(remainingTypes);
            typesDifference.ForEach(type =>
            {
                if (city.NodeTypes.TryGetValue(type, out VisualNodeAttributes visualNodeAttribute))
                {
                    deletedNodeTypes.Add(type, visualNodeAttribute);
                }
            });
            if (root.Children().Count > 0)
            {
                RemoveTypesAfterDeletion().Forget();
            }
            return deletedNodeTypes;

            async UniTask RemoveTypesAfterDeletion()
            {
                GameObject firstChild = root.Children().First()?.GameObject();
                await UniTask.WaitUntil(() => firstChild.activeInHierarchy == false);
                typesDifference.ForEach(type =>
                {
                    city.NodeTypes.Remove(type);
                });
                /// Performs a rebuild for the <see cref="RuntimeConfigMenu"/>.
                if (LocalPlayer.TryGetRuntimeConfigMenu(out RuntimeConfigMenu runtimeConfigMenu))
                {
                    runtimeConfigMenu.PerformUpdate(city);
                }
            }

            IEnumerable<string> GetNodeTypesFromSubgraph(Node subgraph)
            {
                return subgraph.PostOrderDescendantsWithoutItself().Select(node => node.Type).Distinct();
            }

            IEnumerable<string> GetRemainingGraphNodeTypes(Node subgraph, SEEReflexionCity city)
            {
                /// Attention: At this point, the root nodes must come from the graph's nodes list <see cref="Graph.Nodes()"/>.
                /// If the <see cref="ReflexionGraph.ArchitectureRoot"/> or <see cref="ReflexionGraph.ImplementationRoot"/> is used,
                /// it doesn't work because the children are not added to these root nodes reference.
                return subgraph.Type == ReflexionGraph.ArchitectureType ?
                    GetNodeTypesFromSubgraph(city.ReflexionGraph.GetNode(city.ReflexionGraph.ImplementationRoot.ID))
                    : GetNodeTypesFromSubgraph(city.ReflexionGraph.GetNode(city.ReflexionGraph.ArchitectureRoot.ID));
            }
        }

        /// <summary>
        /// Deletes given <paramref name="rootObject"/> if it contains a root node
        /// and is indeed a <see cref="Node.IsRoot"/>.
        /// The <see cref="CitySelectionManager"/> is activated to add a new <see cref="AbstractSEECity"/>.
        /// Additionally, components belonging to the previous <see cref="AbstractSEECity"/> are removed,
        /// along with the actual <see cref="AbstractSEECity"/> and the game object of the root node.
        /// </summary>
        /// <param name="rootObject">The root game object to be deleted.</param>
        /// <returns>True if the deletion was successful, otherwise false.</returns>
        public static bool DeleteRoot(GameObject rootObject)
        {
            if (rootObject.HasNodeRef() && rootObject.IsRoot())
            {
                Transform cityHolder = rootObject.transform.parent;
                rootObject.ItsGraph().NotifyRootNodeDeletion(rootObject.GetNode());
                if (cityHolder.GetComponent<CitySelectionManager>() != null)
                {
                    cityHolder.GetComponent<CitySelectionManager>().enabled = true;
                }
                if (cityHolder.GetComponent<AbstractSEECity>() is SEEReflexionCity)
                {
                    Destroyer.Destroy(cityHolder.GetComponent<ReflexionVisualization>());
                    Destroyer.Destroy(cityHolder.GetComponent<EdgeMeshScheduler>());
                }
                Destroyer.Destroy(cityHolder.GetComponent<AbstractSEECity>());
                Destroyer.Destroy(rootObject);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Deletes the given <paramref name="gameEdge"/>, that is, removes the associated
        /// graph edge from its graph, lets <paramref name="gameEdge"/> fade out, and
        /// finally sets it inactive. The result is the memorized removed associated
        /// graph edge and <paramref name="gameEdge"/> itself.
        ///
        /// Precondition: <paramref name="gameEdge"/> denotes a game edge.
        /// </summary>
        /// <param name="gameEdge">a game object representing an edge</param>
        /// <returns>memento memorizing the deleted graph edge along with the deleted
        /// <paramref name="gameEdge"/></returns>
        /// <exception cref="Exception">thrown if <paramref name="gameEdge"/> has no valid
        /// edge reference</exception>
        private static (GraphElementsMemento, ISet<GameObject>) DeleteEdge(GameObject gameEdge)
        {
            if (gameEdge.TryGetComponent(out EdgeRef edgeRef) && edgeRef.Value != null)
            {
                // The edge memento must be created before the edge is removed;
                // otherwise ItsGraph would be null.
                EdgeMemento edgeMemento = new(edgeRef.Value);
                edgeRef.Value.ItsGraph.RemoveEdge(edgeRef.Value);
                GameObjectFader.FadingOut(gameEdge, SetInactive);
                return (edgeMemento, new HashSet<GameObject>() { gameEdge });
            }
            else
            {
                throw new Exception($"Edge {gameEdge.name} to be deleted has no valid edge reference.");
            }
        }

        /// <summary>
        /// Deletes and returns all game objects representing nodes or edges
        /// of the node tree rooted by <paramref name="root"/>. The nodes
        /// contained in this tree are the transitive descendants of <paramref name="root"/>.
        /// The edges in this tree are those whose source or target node
        /// is contained in the tree.
        /// <para>
        /// Note: The result consists of both nodes and edges.
        /// </para>
        /// Precondition: <paramref name="root"/> is a game node.
        /// </summary>
        /// <param name="root">the root of the tree to be deleted</param>
        /// <returns>all descendants of <paramref name="root"/> and their incoming
        /// and outgoing edges</returns>
        private static ISet<GameObject> DeleteTree(GameObject root)
        {
            /// The descendants of <see cref="root"/> need to be removed.
            IList<GameObject> descendants = root.AllDescendants();
            ISet<GameObject> result = new HashSet<GameObject>(descendants);

            // FIXME: This may be an expensive operation as it iterates over all game objects in the scene.
            // Note: FindGameObjectsWithTag retrieves only active objects. As we set all deleted objects
            // inactive, those objects will not be retrieved here.
            Dictionary<string, GameObject> idToEdge = new Dictionary<string, GameObject>();
            foreach (GameObject edge in GameObject.FindGameObjectsWithTag(Tags.Edge))
            {
                idToEdge[edge.name] = edge;
            }

            /// Adds all edges having a source or target node in the set of descendants
            /// to result.
            foreach (GameObject descendant in descendants)
            {
                if (descendant.TryGetComponentOrLog(out NodeRef nodeRef))
                {
                    foreach (string edgeID in nodeRef.GetIdsOfIncomingOutgoingEdges())
                    {
                        // descendant may have an edge that is currently inactive because it
                        // was deleted, in which case it does not show up in idToEdge.
                        if (idToEdge.TryGetValue(edgeID, out GameObject edge))
                        {
                            result.Add(edge);
                        }
                    }
                }
            }
            Delete(result);
            return result;
        }

        /// <summary>
        /// Sets <paramref name="gameObject"/> inactive.
        /// </summary>
        /// <param name="gameObject">object to be set inactive</param>
        private static void SetInactive(GameObject gameObject)
        {
            gameObject.SetActive(false);
            // Restore the previous alpha value.
            // Required to avoid a bug where the object becomes invisible.
            GameObjectFader.Fade(gameObject, 0, 1.0f);
        }

        /// <summary>
        /// Sets <paramref name="gameObject"/> active.
        /// </summary>
        /// <param name="gameObject">object to be set active</param>
        private static void SetActive(GameObject gameObject)
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Marks all elements in <paramref name="nodesOrEdges"/> by way of
        /// <see cref="GameObjectFader"/> as deleted. That means to let
        /// them blink and fade out and eventually be set inactive.
        ///
        /// Note: The objects are not actually destroyed, neither are they removed
        /// from their graph.
        /// </summary>
        /// <param name="nodesOrEdges">nodes and/or edges to be marked as deleted</param>
        private static void Delete(IEnumerable<GameObject> nodesOrEdges)
        {
            foreach (GameObject nodeOrEdge in nodesOrEdges)
            {
                GameObjectFader.FadingOut(nodeOrEdge, SetInactive);
            }
        }

        /// <summary>
        /// Marks all elements in <paramref name="nodesOrEdges"/> by way of
        /// <see cref="GameObjectFader"/> as alive again. That means to let
        /// them be set active, fade in and blink. All <paramref name="nodesOrEdges"/>
        /// are re-added to their original graph.
        /// <paramref name="nodeTypes"/> will be added to the <see cref="AbstractSEECity.NodeTypes"/>.
        ///
        /// Assumption: The objects were set inactive.
        /// </summary>
        /// <param name="nodesOrEdges">nodes and edge to be marked as alive again</param>
        /// <param name="nodeTypes">node types to be added.</param>
        public static void Revive(ISet<GameObject> nodesOrEdges, Dictionary<string, VisualNodeAttributes> nodeTypes = null)
        {
            RestoreNodeTypes(nodesOrEdges
                .Select(go => go.TryGetNode(out Node node) ? node : null)
                .Where(node => node != null)
                .ToList(),
                nodeTypes);
            ISet<GameObject> filtered = nodesOrEdges
                .Where(go => go.GetComponent<NodeRef>() != null || go.GetComponent<EdgeRef>() != null)
                .ToHashSet();
            RestoreGraph(filtered);
            foreach (GameObject nodeOrEdge in filtered)
            {
                SetActive(nodeOrEdge);
                GameObjectFader.FadingIn(nodeOrEdge);
            }
        }

        /// <summary>
        /// Restores the subgraph defined by the given <paramref name="nodesOrEdges"/>.
        /// All nodes and edges in <paramref name="nodesOrEdges"/> will be re-added to
        /// the underlying graph; in addition, the parentship will be restored, too,
        /// according to the game-node hierarchy.
        ///
        /// Assumption: all <paramref name="nodesOrEdges"/> belong to the same graph.
        /// </summary>
        /// <param name="nodesOrEdges">nodes and edges to be re-added to the graph</param>
        private static void RestoreGraph(IEnumerable<GameObject> nodesOrEdges)
        {
            Graph graph = null; // The graph all nodes and edges belong to.
            IList<GameObject> edges = new List<GameObject>();
            IList<GameObject> nodes = new List<GameObject>();
            // We will first just add the nodes before we establish their parents and edges
            // so that all necessary nodes exist in the graph.
            foreach (GameObject nodeOrEdge in nodesOrEdges)
            {
                if (nodeOrEdge.TryGetComponent(out NodeRef nodeRef))
                {
                    Node node = nodeRef.Value;
                    if (node != null)
                    {
                        if (graph == null)
                        {
                            // The node is not contained in a graph. We need to retrieve its graph
                            // from one of its ancestors.
                            graph = GetGraphOfNode(nodeOrEdge.transform.parent.gameObject);
                            if (graph == null)
                            {
                                throw new Exception($"Graph for node {nodeOrEdge.name} could not be retrieved.");
                            }
                        }
                        // assert: graph != null
                        graph.AddNode(node);
                        nodes.Add(nodeOrEdge);
                    }
                    else
                    {
                        Debug.LogError($"Node {nodeOrEdge.name} to be revived has no valid node reference.\n");
                    }
                }
                else
                {
                    // It is an edge.
                    edges.Add(nodeOrEdge);
                }
            }
            // Now we re-establish the parentship.
            // Assert: Every gameNode has a valid node reference.
            foreach (GameObject gameNode in nodes)
            {
                // Retrieve the parent graph node from the parent game node.
                Transform parentGameNode = gameNode.transform.parent;
                if (parentGameNode != null && parentGameNode.TryGetComponent(out NodeRef parentNodeRef))
                {
                    parentNodeRef.Value.AddChild(gameNode.GetNode());
                }
            }
            // Finally, we add the edges back to the graph.
            foreach (GameObject gameEdge in edges)
            {
                if (gameEdge.TryGetComponentOrLog(out EdgeRef edgeRef))
                {
                    Edge edge = edgeRef.Value;
                    if (edge != null)
                    {
                        // In case we need to revive only edges, but no nodes, graph will still
                        // be null. In that case, we need to retrieve the graph from source/target
                        // of the edge.
                        // The assumption here is that the source of the edge is already in the graph.
                        graph ??= edge.Source.ItsGraph;
                        graph.AddEdge(edge);
                    }
                    else
                    {
                        Debug.LogError($"Edge {gameEdge.name} to be revived has no valid edge reference.\n");
                    }
                }
            }

            /// <summary>
            /// Retrieves the graph to which the given <paramref name="gameNode"/> should
            /// be added by walking up the game-node hierarchy until a game node is
            /// reached whose associated graph node has a valid graph reference.
            /// If no such game node can be found, null is returned.
            /// </summary>
            /// <param name="gameNode">the game node for which to look up the graph</param>
            /// <return>corresponding graph or null</return>
            static Graph GetGraphOfNode(GameObject gameNode)
            {
                while (gameNode != null)
                {
                    if (gameNode.CompareTag(Tags.Node) && gameNode.TryGetComponentOrLog(out NodeRef nodeRef))
                    {
                        Node node = nodeRef.Value;
                        if (node != null)
                        {
                            if (node.ItsGraph != null)
                            {
                                return node.ItsGraph;
                            }
                        }
                        else
                        {
                            Debug.LogError($"Node {gameNode.name} has no valid node reference.\n");
                        }
                    }
                    gameNode = gameNode.transform.parent.gameObject;
                }
                return null;
            }
        }

        /// <summary>
        /// Restores the deleted node types.
        /// </summary>
        /// <param name="nodes">The nodes to be restored.</param>
        /// <param name="nodeTypes">The node types' properties.</param>
        private static void RestoreNodeTypes(List<Node> nodes, Dictionary<string, VisualNodeAttributes> nodeTypes = null)
        {
            if (nodeTypes != null && nodeTypes.Count > 0)
            {
                HashSet<SEEReflexionCity> affectedCities = new();
                nodes.ForEach(node =>
                {
                    GameObject obj = node.GameObject() != null ? node.GameObject() : TryGetFirstParentGameObject(node);
                    if (obj != null && obj.ContainingCity<SEEReflexionCity>() is { } city)
                    {
                        if (!city.NodeTypes.TryGetValue(node.Type, out VisualNodeAttributes _))
                        {
                            city.NodeTypes[node.Type] = nodeTypes[node.Type];
                            affectedCities.Add(city);
                        }
                    }
                });
                /// Performs a rebuild for the <see cref="RuntimeConfigMenu"/>.
                if (LocalPlayer.TryGetRuntimeConfigMenu(out RuntimeConfigMenu runtimeConfigMenu))
                {
                    foreach (SEEReflexionCity city in affectedCities)
                    {
                        runtimeConfigMenu.PerformUpdate(city);
                    }
                }
            }

            static GameObject TryGetFirstParentGameObject(Node node)
            {
                if (node.Parent != null && node.Parent.GameObject() == null)
                {
                    return TryGetFirstParentGameObject(node.Parent);
                }
                else if (node.Parent != null && node.Parent.GameObject() != null)
                {
                    return node.Parent.GameObject();
                }
                return node.GameObject();
            }
        }

        /// <summary>
        /// Restores the specified <see cref="GraphElement"/>s and their corresponding node types.
        /// </summary>
        /// <param name="nodesOrEdges">The graph elements to be restored.</param>
        /// <param name="nodeTypes">The node types to be restored.</param>
        public static void Restore(List<RestoreGraphElement> nodesOrEdges,
            Dictionary<string, VisualNodeAttributes> nodeTypes = null)
        {
            List<Node> createdNodes = new();
            nodesOrEdges
                .OfType<RestoreNodeElement>()
                .OrderBy(node => node.Level)
                .Cast<RestoreGraphElement>()
                .Concat(nodesOrEdges.OfType<RestoreEdgeElement>())
                .ForEach(ele =>
            {
                switch (ele)
                {
                    case RestoreNodeElement nodeEle:
                        GameObject parent = GraphElementIDMap.Find(nodeEle.ParentID);
                        Node node = GameNodeAdder.AddChild(parent, worldSpacePosition: nodeEle.Position,
                                                   worldSpaceScale: nodeEle.Scale, nodeID: nodeEle.ID)
                                                   .GetNode();
                        GameNodeEditor.ChangeName(node, nodeEle.Name);
                        GameNodeEditor.ChangeType(node, nodeEle.NodeType);
                        createdNodes.Add(node);
                        break;
                    case RestoreEdgeElement edgeEle:
                        GameObject from = GraphElementIDMap.Find(edgeEle.FromID);
                        GameObject to = GraphElementIDMap.Find(edgeEle.ToID);
                        GameEdgeAdder.Add(from, to, edgeEle.EdgeType);
                        break;
                }
            });
            RestoreNodeTypes(createdNodes, nodeTypes);
        }
    }
}

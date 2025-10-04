using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.Game.Operator;
using SEE.GameObjects;
using SEE.GO;
using SEE.GraphProviders;
using SEE.Layout;
using SEE.Layout.NodeLayouts;
using SEE.UI.Notification;
using SEE.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Game.CityRendering
{
    /// <summary>
    /// Renders transitions in a <see cref="BranchCity"/> when new commits are detected.
    /// </summary>
    public class TransitionRenderer
    {
        /// <summary>
        /// The time in seconds for how long the node markers should be shown for newly
        /// added or modified nodes.
        /// </summary>
        public int MarkerTime = 10;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="branchCity">the city to be re-drawn</param>
        /// <param name="poller">the poller notifying when there are changes</param>
        /// <param name="markerTime">the time in seconds the new markers should be drawn for;
        /// after that they will be removed again</param>
        public TransitionRenderer(BranchCity branchCity, GitPoller poller, int markerTime)
        {
            this.branchCity = branchCity;
            this.MarkerTime = markerTime;
            this.poller = poller;
            markerFactory = new MarkerFactory(this.branchCity.MarkerAttributes);
            poller.OnChangeDetected += Render;
        }

        /// <summary>
        /// Finalizer to unsubscribe from the poller event.
        /// </summary>
        ~TransitionRenderer()
        {
            poller.OnChangeDetected -= Render;
        }

        /// <summary>
        /// Renders the transition when new commits were detected.
        /// This method is used as a delegate for <see cref="GitPoller.OnChangeDetected"/>.
        /// </summary>
        private void Render()
        {
            RenderAsync().Forget();
        }

        /// <summary>
        /// The poller notifying when there are changes.
        /// </summary>
        private readonly GitPoller poller;

        /// <summary>
        /// The code city where the <see cref="GitBranchesGraphProvider"/> graph provider
        /// was executed and which should be updated when a new commit is detected.
        /// </summary>
        private readonly BranchCity branchCity;

        /// <summary>
        /// <see cref="MarkerFactory"> for generating node markers.
        /// </summary>
        private readonly MarkerFactory markerFactory;

        /// <summary>
        /// The last calculated <see cref="NodeLayout"/> (needed for incremental layouts).
        /// </summary>
        private NodeLayout oldLayout = null;

        /// <summary>
        /// Renders the transition when new commits were detected.
        /// This method implements the actual rendering.
        /// </summary>
        /// <returns>task</returns>
        private async UniTask RenderAsync()
        {
            ShowNewCommitsMessage();

            // Remove markers from previous rendering.
            markerFactory.Clear();

            // Backup old graph
            Graph oldGraph = branchCity.LoadedGraph.Clone() as Graph;
            await branchCity.LoadDataAsync();

            branchCity.LoadedGraph.Diff(oldGraph,
                g => g.Nodes(),
                (g, id) => g.GetNode(id),
                GraphExtensions.AttributeDiff(branchCity.LoadedGraph, oldGraph),
                nodeEqualityComparer,
                out ISet<Node> addedNodes,   // nodes belong to LoadedGraph
                out ISet<Node> removedNodes, // nodes belong to oldGraph
                out ISet<Node> changedNodes, // nodes belong to LoadedGraph
                out ISet<Node> equalNodes);  // nodes belong to LoadedGraph

            // Before we can calculate the new layout, we must ensure that all game nodes
            // representing nodes that are still present in the new graph are reattached
            // so that their metrics are up to date. These are needed to determine the
            // dimensions of the nodes in the new layout.
            equalNodes.UnionWith(changedNodes);
            ReattachNodes(equalNodes);

            NextLayout.Calculate(branchCity.LoadedGraph,
                                 GetGameNode,
                                 branchCity.Renderer,
                                 branchCity.EdgeLayoutSettings.Kind != EdgeLayoutKind.None,
                                 branchCity.gameObject,
                                 out Dictionary<string, ILayoutNode> newNodelayout,
                                 out _,
                                 ref oldLayout);
            // Note: At this point, game nodes for new nodes have been created already.
            // Their NodeRefs refer to nodes in the newly loaded graph.
            // The removedNodes, changedNodes, and equalNodes have NodeRefs still
            // referencing nodes in the oldGraph.
            // That is no problem for removedNodes as these will be removed anyway.
            // Yet, we need to update the game nodes with NodeRefs to all changedNodes
            // and equalNodes.

            ShowRemovedNodes(removedNodes);
            Debug.Log($"Phase 1: Removing {removedNodes.Count} nodes.\n");
            await AnimateNodeDeathAsync(removedNodes, markerFactory);
            Debug.Log($"Phase 1: Finished.\n");

            Debug.Log($"Phase 2: Moving {equalNodes.Count} nodes.\n");
            await AnimateNodeMoveAsync(equalNodes, newNodelayout, branchCity.transform);
            Debug.Log($"Phase 2: Finished.\n");

            ShowChangedNodes(changedNodes);
            // Even the equal nodes need adjustments because the layout could have
            // changed their dimensions. The treemap layout, for instance, may do that.
            // Note that equalNodes is the union of the really equal nodes passed initially
            // as a parameter and the changedNodes.
            Debug.Log($"Phase 3: Changing {changedNodes.Count} nodes.\n");
            await AnimateNodeChangeAsync(equalNodes, changedNodes, newNodelayout, markerFactory);
            Debug.Log($"Phase 3: Finished.\n");

            ShowAddedNodes(addedNodes);
            Debug.Log($"Phase 4: Adding {addedNodes.Count} nodes.\n");
            await AnimateNodeBirthAsync(addedNodes, newNodelayout);
            Debug.Log($"Phase 4: Finished.\n");

            GameNodeHierarchy.Update(branchCity.gameObject);

            MarkNodes(addedNodes, changedNodes);
            // FIXME: Update all author references.
        }

        /// <summary>
        /// Marks all <paramref name="addedNodes"/> as born and all <paramref name="changedNodes"/>
        /// as changed using <see cref="markerFactory"/>.
        /// </summary>
        void MarkNodes(ISet<Node> addedNodes, ISet<Node> changedNodes)
        {
            foreach (Node node in addedNodes)
            {
                markerFactory.MarkBorn(GraphElementIDMap.Find(node.ID, true));
            }
            foreach (Node node in changedNodes)
            {
                markerFactory.MarkChanged(GraphElementIDMap.Find(node.ID, true));
            }
        }

        /// <summary>
        /// If a game node with the ID of the given <paramref name="node"/> exists, it is returned.
        /// Otherwise, a new game node is created and returned with the given <paramref name="node"/>
        /// attached to it.
        /// </summary>
        /// <param name="node">node for which a game node is requested</param>
        /// <returns>existing or new game node</returns>
        private GameObject GetGameNode(Node node)
        {
            GameObject go = GraphElementIDMap.Find(node.ID, false);
            if (go != null)
            {
                return go;
            }
            return branchCity.Renderer.DrawNode(node, branchCity.gameObject);
        }

        /// <summary>
        /// Renders <paramref name="removedNodes"/> and destroys their game objects.
        /// </summary>
        /// <param name="removedNodes">nodes to be removed</param>
        /// <param name="markerFactory">factory used to mark nodes as dead</param>
        /// <returns>task</returns>
        private async UniTask AnimateNodeDeathAsync(ISet<Node> removedNodes, MarkerFactory markerFactory)
        {
            HashSet<GameObject> deads = new();

            foreach (Node node in removedNodes)
            {
                GameObject go = GraphElementIDMap.Find(node.ID, true);
                if (go != null)
                {
                    deads.Add(go);
                    markerFactory.MarkDead(go);
                    IOperationCallback<System.Action> animation = go.NodeOperator().MoveYTo(AbstractSEECity.SkyLevel);
                    animation.OnComplete(() => OnComplete(go));
                    animation.OnKill(() => OnComplete(go));
                }
            }

            await UniTask.WaitUntil(() => deads.Count == 0);

            void OnComplete(GameObject go)
            {
                deads.Remove(go);
                Destroyer.Destroy(go);
            }
        }

        /// <summary>
        /// Creates new game objects for <paramref name="addedNodes"/> and renders them.
        /// </summary>
        /// <param name="addedNodes">nodes to be added</param>
        /// <param name="newNodelayout">the layout to be applied to the new nodes</param>
        /// <returns>task</returns>
        private async UniTask AnimateNodeBirthAsync
            (ISet<Node> addedNodes,
             Dictionary<string, ILayoutNode> newNodelayout)
        {
            // The set of nodes whose birth is still being animated.
            HashSet<GameObject> births = new();

            // The temporary parent object for the new nodes. A new node must have
            // a parent object with a Portal component; otherwise the NodeOperator
            // will not work. Later, we will set the correct parent of the new node.
            // At this time, the code city should have at least the (unique) root game
            // node.
            GameObject parent = branchCity.gameObject.FirstChildNode();

            // First create new game objects for the new nodes, mark them as born,
            // and add the NodeOperator component to them. The NodeOperator will
            // be enabled only at the end of the current frame. We cannot use it
            // earlier.
            foreach (Node node in addedNodes)
            {
                GameObject go = GetGameNode(node);
                go.transform.SetParent(parent.transform);
                // We need the NodeOperator component to animate the birth of the node.
                go.AddOrGetComponent<NodeOperator>();
                births.Add(go);
            }

            // Let the frame be finished so that the node is really added to the scene
            // and its NodeOperator component is enabled.
            // Note: UniTask.Yield() works only while the game is playing.
            await UniTask.Yield();

            // Now we can animate the birth of the new nodes.
            foreach (GameObject go in births)
            {
                // go.name and node.ID are the same.
                ILayoutNode layoutNode = newNodelayout[go.name];
                if (layoutNode != null)
                {
                    Add(go, layoutNode, parent);
                }
                else
                {
                    Debug.LogError($"No layout for node {go.name}.\n");
                }
            }

            await UniTask.WaitUntil(() => births.Count == 0);

            void OnComplete(GameObject go)
            {
                // Now set the correct parent of the new node.
                Node node = go.GetNode();
                if (!node.IsRoot())
                {
                    GameObject parent = GraphElementIDMap.Find(node.Parent.ID, false);
                    if (parent != null)
                    {
                        go.transform.SetParent(parent.transform);
                    }
                }
                births.Remove(go);
            }

            void Add(GameObject gameNode, ILayoutNode layoutNode, GameObject parent)
            {
                // A new node has no layout applied to it yet.
                // If the node is new, we animate it by moving it out from the sky.
                Vector3 initialPosition = layoutNode.CenterPosition;
                initialPosition.y = AbstractSEECity.SkyLevel + layoutNode.AbsoluteScale.y;
                gameNode.transform.position = initialPosition;

                gameNode.SetAbsoluteScale(layoutNode.AbsoluteScale, animate: false);

                // The node is new. Hence, it has no parent yet. It must be contained
                // in a code city though; otherwise the NodeOperator would not work.
                Assert.IsNotNull(parent);
                gameNode.transform.SetParent(parent.transform);

                IOperationCallback<System.Action> animation = gameNode.NodeOperator()
                        .MoveTo(layoutNode.CenterPosition, updateEdges: false);
                animation.OnComplete(() => OnComplete(gameNode));
                animation.OnKill(() => OnComplete(gameNode));
            }
        }

        /// <summary>
        /// Reattaches the given <paramref name="nodes"/> to their corresponding game nodes.
        /// The nodes belong to the new graph but have corresponding game nodes still representing
        /// the same nodes (according to the ID) of the former graph.
        /// </summary>
        /// <param name="nodes">graph nodes of the new graph to be reattached</param>
        private void ReattachNodes(ISet<Node> nodes)
        {
            foreach (Node node in nodes)
            {
                GameObject go = GraphElementIDMap.Find(node.ID, true);
                if (go != null)
                {
                    GraphElementReattacher.ReattachNode(go, node);
                }
            }
        }

        /// <summary>
        /// Animates the move of <paramref name="movedNodes"/> to their new positions
        /// according to <paramref name="newNodelayout"/>. All moved nodes will be
        /// temporarily re-parented to <paramref name="cityTransform"/> so that
        /// the <see cref="NodeOperator"/> can move them individually.
        /// </summary>
        /// <param name="movedNodes">game nodes to be moved</param>
        /// <param name="newNodelayout">new positions</param>
        /// <param name="cityTransform">temporary parent representing the code city
        /// as a whole</param>
        /// <returns>task</returns>
        private async UniTask AnimateNodeMoveAsync
                                (ISet<Node> movedNodes,
                                 Dictionary<string, ILayoutNode> newNodelayout,
                                 Transform cityTransform)
        {
            HashSet<GameObject> moved = new();

            foreach (Node node in movedNodes)
            {
                GameObject go = GraphElementIDMap.Find(node.ID, true);
                if (go != null)
                {
                    ILayoutNode layoutNode = newNodelayout[node.ID];
                    if (layoutNode != null)
                    {
                        if (PositionHasChanged(go, layoutNode))
                        {
                            // We want the animator to move each node separately, which is why we
                            // remove each from the hierarchy; later the node hierarchy will be
                            // re-established. It still needs to be a child of the code city,
                            // however, because methods called in the course of the animation
                            // will try to retrieve the code city from the game node.
                            go.transform.SetParent(cityTransform);

                            moved.Add(go);
                            // Move the node to its new position.
                            IOperationCallback<System.Action> animation = go.NodeOperator()
                              .MoveTo(layoutNode.CenterPosition, updateEdges: false);
                            animation.OnComplete(() => OnComplete(go));
                            animation.OnKill(() => OnComplete(go));
                        }
                    }
                    else
                    {
                        Debug.LogError($"No layout for node {node.ID}.\n");
                    }
                }
            }
            await UniTask.WaitUntil(() => moved.Count == 0);

            // True if the position of the given game object has actually changed
            // by a relevant margin.
            bool PositionHasChanged(GameObject go, ILayoutNode layoutNode)
            {
                Vector3 currentPosition = go.transform.position;
                Vector3 newPosition = layoutNode.CenterPosition;
                return Vector3.Distance(currentPosition, newPosition) > 0.001f;
            }

            void OnComplete(GameObject go)
            {
                moved.Remove(go);
            }
        }

        /// <summary>
        /// Animates the change of <paramref name="nodesToAdjust"/> to their new
        /// scale and style. The nodes in <paramref name="changedNodes"/> will
        /// be marked as changed using <paramref name="markerFactory"/>.
        ///
        /// </summary>
        /// <param name="nodesToAdjust">nodes whose dimensions, dimensions, and
        /// markers need to be adjusted; we assume <paramref name="changedNodes"/>
        /// is a subset of <paramref name="nodesToAdjust"/></param>
        /// <param name="changedNodes">nodes to be marked for a change</param>
        /// <param name="newNodelayout">new layout determining the new scale</param>
        /// <param name="markerFactory">factory for marking as changed</param>
        /// <returns>task</returns>
        private async UniTask AnimateNodeChangeAsync
            (ISet<Node> nodesToAdjust,
             ISet<Node> changedNodes,
             Dictionary<string, ILayoutNode> newNodelayout,
             MarkerFactory markerFactory)
        {
            HashSet<GameObject> changed = new();

            foreach (Node node in nodesToAdjust)
            {
                GameObject go = GraphElementIDMap.Find(node.ID, true);
                if (go != null)
                {
                    ILayoutNode layoutNode = newNodelayout[node.ID];
                    if (layoutNode != null)
                    {
                        // Apply MarkChanged only to the really changed nodes.
                        // nodesToAdjust is the union of changedNodes and equalNodes.
                        if (changedNodes.Contains(node))
                        {
                            // There is a change. It may or may not be the metric determining the style.
                            // We will not further check that and just call the following method.
                            // If there is no change, this method does not need to be called because then
                            // we know that the metric values determining the styles of the former
                            // and the new graph node are the same. A style may include color, material,
                            // and other visual properties of the node itself, exluding size and decorations
                            // such as antenna and marker.
                            branchCity.Renderer.AdjustStyle(go);
                        }
                        changed.Add(go);
                        ScaleTo(go, layoutNode);
                    }
                    else
                    {
                        Debug.LogError($"No layout for node {node.ID}.\n");
                    }
                }
            }

            await UniTask.WaitUntil(() => changed.Count == 0);

            void ScaleTo(GameObject gameNode, ILayoutNode layoutNode)
            {
                // layoutNode.AbsoluteScale is in world space, while the animation by iTween
                // is in local space. Our game objects may be nested in other game objects,
                // hence, the two spaces may be different.
                // We may need to transform layoutNode.AbsoluteScale from world space to local space.
                Assert.IsNotNull(gameNode);
                Assert.IsNotNull(layoutNode);

                Vector3 localScale = gameNode.transform.parent == null ?
                                         layoutNode.AbsoluteScale
                                       : gameNode.transform.parent.InverseTransformVector(layoutNode.AbsoluteScale);

                IOperationCallback<System.Action> animation = gameNode.NodeOperator()
                        .ScaleTo(localScale, updateEdges: false);
                animation.OnComplete(() => OnComplete(gameNode));
                animation.OnKill(() => OnComplete(gameNode));
            }

            void OnComplete(GameObject go)
            {
                // Adjust the antenna and marker position after the scaling has finished;
                // otherwise they would be scaling along with their parent.
                if (changedNodes.Contains(go.GetNode()))
                {
                    // The adjustment of the antenna is needed only if the node has changed.
                    // Similarly to the adjustment of the style, we do not check whether
                    // the metrics determining the antenna have changed or not.
                    branchCity.Renderer.AdjustAntenna(go);
                }
                markerFactory.AdjustMarkerY(go);
                changed.Remove(go);
            }
        }

        /// <summary>
        /// Allows the comparison of two instances of <see cref="Node"/> from different graphs.
        /// </summary>
        private static readonly NodeEqualityComparer nodeEqualityComparer = new();

        /// <summary>
        /// Notifies the user about removed files in the repository.
        /// </summary>
        /// <param name="nodes">The list of removed nodes.</param>
        private static void ShowRemovedNodes(ISet<Node> nodes)
        {
            foreach (Node node in nodes)
            {
                ShowNotification.Info("File removed", $"File {node.ID} was removed from the repository.");
            }
        }

        /// <summary>
        /// Notifies the user about added files in the repository.
        /// </summary>
        /// <param name="nodes">The list of added nodes.</param>
        private static void ShowAddedNodes(ISet<Node> nodes)
        {
            foreach (Node node in nodes)
            {
                ShowNotification.Info("File added", $"File {node.ID} was added to the repository.");
            }
        }

        /// <summary>
        /// Notifies the user about changed files in the repository.
        /// </summary>
        /// <param name="nodes">The list of changed nodes.</param>
        private void ShowChangedNodes(ISet<Node> nodes)
        {
            foreach (Node node in nodes)
            {
                ShowNotification.Info("File changed", $"File {node.ID} was changed.");
            }
        }

        /// <summary>
        /// Shows a message to the user that a new commit was detected.
        /// </summary>
        private void ShowNewCommitsMessage()
        {
            ShowNotification.Info("New commits detected", "Refreshing code city.");
        }
    }
}

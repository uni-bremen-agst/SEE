using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.Game.Operator;
using SEE.GO;
using SEE.GraphProviders;
using SEE.Layout;
using SEE.Layout.NodeLayouts;
using SEE.UI.Notification;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
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
        /// The poller notifying when there are changes.
        /// </summary>
        private readonly GitPoller poller;

        /// <summary>
        /// Renders the transition when new commits were detected.
        /// This method is used as a delegate for <see cref="GitPoller.OnChangeDetected"/>.
        /// </summary>
        private void Render()
        {
            RenderAsync().Forget();
        }

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
                out ISet<Node> addedNodes,
                out ISet<Node> removedNodes,
                out ISet<Node> changedNodes,
                out _);

            NextLayout.Calculate(branchCity.LoadedGraph,
                                 GetGameNode,
                                 branchCity.Renderer,
                                 branchCity.EdgeLayoutSettings.Kind != EdgeLayoutKind.None,
                                 branchCity.gameObject,
                                 out Dictionary<string, ILayoutNode> newNodelayout,
                                 out _,
                                 ref oldLayout);
            // Note: At this point game nodes for new nodes have been created already.

            ShowRemovedNodes(removedNodes);
            Debug.Log($"Phase 1: Removing {removedNodes.Count} nodes.\n");
            await AnimateNodeDeathAsync(removedNodes, markerFactory);
            Debug.Log($"Phase 1: Finished.\n");

            ShowAddedNodes(addedNodes);
            Debug.Log($"Phase 2: Adding {addedNodes.Count} nodes.\n");
            await AnimateNodeBirthAsync(addedNodes, newNodelayout, markerFactory);
            Debug.Log($"Phase 2: Finished.\n");

            ShowChangedNodes(changedNodes);
            Debug.Log($"Phase 3: Changing {changedNodes.Count} nodes.\n");
            await AnimateNodeChangeAsync(changedNodes, newNodelayout, markerFactory);
            Debug.Log($"Phase 3: Finished.\n");

            RemoveMarkerAsync().Forget();
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
                    NodeOperator nodeOperator = go.NodeOperator();
                    nodeOperator.MoveYTo(AbstractSEECity.SkyLevel).OnComplete(() => OnComplete(go));
                }
            }

            await UniTask.WaitUntil(() => deads.Count == 0);

            void OnComplete(GameObject go)
            {
                Debug.Log($"Node {go.name} is destroyed.\n");
                deads.Remove(go);
                Destroyer.Destroy(go);
            }
        }

        /// <summary>
        /// Creates new game objects for <paramref name="addedNodes"/> and renders them.
        /// </summary>
        /// <param name="addedNodes">nodes to be added</param>
        /// <param name="newNodelayout">the layout to be applied to the new nodes</param>
        /// <param name="markerFactory">factory used to mark nodes as born</param>
        /// <returns>task</returns>
        private async UniTask AnimateNodeBirthAsync
            (ISet<Node> addedNodes,
             Dictionary<string, ILayoutNode> newNodelayout,
             MarkerFactory markerFactory)
        {
            // The set of nodes whose birth is still being animated.
            HashSet<GameObject> births = new();

            // The temporary parent object for the new nodes. A new node must have
            // a parent object with a Portal component; otherwise the NodeOperator
            // will not work. Later, we will set the correct parent of the new node.
            GameObject parent = branchCity.gameObject.FirstChildNode();

            // First create new game objects for the new nodes, mark them as born,
            // and add the NodeOperator component to them. The NodeOperator will
            // be enabled only at the end of the current frame. We cannot use it
            // earlier.
            foreach (Node node in addedNodes)
            {
                GameObject go = GetGameNode(node);
                go.transform.SetParent(parent.transform);
                markerFactory.MarkBorn(go);
                // We need the NodeOperator component to animate the birth of the node.
                go.AddOrGetComponent<NodeOperator>();
                births.Add(go);
            }

            // Let the frame be finished so that the node is really added to the scene
            // and its NodeOperator component is enabled.
            // Note: UniTask.Yield() works only while the game is playing.
            Debug.Log($"UniTask.Yield: {Time.frameCount}\n");
            if (Application.isPlaying)
            {
                await UniTask.Yield();
            }
            else
            {
                // In edit mode, we cannot yield. Hence, we just wait a bit.
                // Schedule the next part of the function to run on the next editor update.
                EditorApplication.delayCall += OnNextEditorUpdate;
                await UniTask.WaitUntil(() => nextEditorUpdate);
                nextEditorUpdate = false;
            }
            Debug.Log($"UniTask.Yield: {Time.frameCount}\n");

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

                Debug.Log($"Animating birth of node {gameNode.name} by moving it to {layoutNode.CenterPosition}.\n");
                gameNode.NodeOperator()
                        .MoveTo(layoutNode.CenterPosition, updateEdges: false)
                        .OnComplete(() => OnComplete(gameNode));
            }
        }

        static bool nextEditorUpdate = false;

        private static void OnNextEditorUpdate()
        {
            Debug.Log($"OnNextEditorUpdate: {Time.frameCount}\n");
            nextEditorUpdate = true;
            EditorApplication.delayCall -= OnNextEditorUpdate;
        }

        private async UniTask AnimateNodeChangeAsync
            (ISet<Node> changedNodes,
             Dictionary<string, ILayoutNode> newNodelayout,
             MarkerFactory markerFactory)
        {
            HashSet<GameObject> changed = new();

            foreach (Node node in changedNodes)
            {
                GameObject go = GraphElementIDMap.Find(node.ID, true);
                if (go != null)
                {
                    ILayoutNode layoutNode = newNodelayout[node.ID];
                    if (layoutNode != null)
                    {
                        markerFactory.MarkChanged(go);
                        changed.Add(go);

                        // There is a change. It may or may not be the metric determining the style.
                        // We will not further check that and just call the following method.
                        // If there is no change, this method does not need to be called because then
                        // we know that the metric values determining the style and antenna of the former
                        // and the new graph node are the same.
                        branchCity.Renderer.AdjustStyle(go);
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
                // layoutNode.LocalScale is in world space, while the animation by iTween
                // is in local space. Our game objects may be nested in other game objects,
                // hence, the two spaces may be different.
                // We may need to transform layoutNode.LocalScale from world space to local space.
                Assert.IsNotNull(gameNode);
                Assert.IsNotNull(layoutNode);

                Vector3 localScale = gameNode.transform.parent == null ?
                                         layoutNode.AbsoluteScale
                                       : gameNode.transform.parent.InverseTransformVector(layoutNode.AbsoluteScale);

                gameNode.NodeOperator()
                        .ScaleTo(localScale, updateEdges: false)
                        .OnComplete(() => OnComplete(gameNode));

            }

            void OnComplete(GameObject go)
            {
                if (go is GameObject gameNode)
                {
                    // branchCity.Renderer.AdjustAntenna(gameNode); FIXME
                    markerFactory.AdjustMarkerY(gameNode);
                }
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
        /// Removes all markers after <see cref="MarkerTime"/> seconds.
        /// </summary>
        private async UniTaskVoid RemoveMarkerAsync()
        {
            // MarkerTime is in seconds, but Task.Delay expects milliseconds.
            await Task.Delay(MarkerTime * 1000);
        }

        /// <summary>
        /// The code city where the <see cref="GitBranchesGraphProvider"/> graph provider
        /// was executed and which should be updated when a new commit is detected.
        /// </summary>
        private readonly BranchCity branchCity;

        /// <summary>
        /// MarkerFactory for generating node markers.
        /// </summary>
        private readonly MarkerFactory markerFactory;

        /// <summary>
        /// Shows a message to the user that a new commit was detected.
        /// </summary>
        private void ShowNewCommitsMessage()
        {
            ShowNotification.Info("New commits detected", "Refreshing code city.");
        }
    }
}

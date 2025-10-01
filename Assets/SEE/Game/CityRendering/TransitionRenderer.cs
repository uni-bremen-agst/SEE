using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.Game.Operator;
using SEE.GO;
using SEE.GraphProviders;
using SEE.Layout;
using SEE.UI.Notification;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        /// Renders the transition when new commits were detected.
        /// This method implements the actual rendering.
        /// </summary>
        /// <returns>task</returns>
        private async UniTask RenderAsync()
        {
            ShowNewCommitsMessage();

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

            ShowRemovedNodes(removedNodes);
            AnimateNodeDeathAsync(removedNodes, markerFactory).Forget();

            ShowAddedNodes(addedNodes);
            AnimateNodeBirthAsync(addedNodes, markerFactory).Forget();

            ShowChangedNodes(changedNodes);
            AnimateNodeChangeAsync(changedNodes, markerFactory).Forget();

            try
            {
                //branchCity.ReDrawGraph();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            RemoveMarkerAsync().Forget();
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
                deads.Remove(go);
                Destroyer.Destroy(go);
            }
        }

        /// <summary>
        /// Creates new game objects for <paramref name="addedNodes"/> and renders them.
        /// </summary>
        /// <param name="addedNodes">nodes to be added</param>
        /// <param name="markerFactory">factory used to mark nodes as born</param>
        /// <returns>task</returns>
        private async UniTask AnimateNodeBirthAsync(ISet<Node> addedNodes, MarkerFactory markerFactory)
        {
            // The set of nodes whose birth is still being animated.
            HashSet<GameObject> births = new();
            // The object representing the city and having the <see cref="Portal"/> component.
            GameObject city = branchCity.gameObject;

            foreach (Node node in addedNodes)
            {
                GameObject parent = branchCity.gameObject.FirstChildNode(); // FIXME
                GameObject go = branchCity.Renderer.DrawNode(node, city);
                markerFactory.MarkBorn(go);
                births.Add(go);
                ILayoutNode layoutNode = GetLayout(go, node, branchCity);
                Add(go, layoutNode, parent);
            }

            await UniTask.WaitUntil(() => births.Count == 0);

            void OnComplete(GameObject go)
            {
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

                gameNode.NodeOperator()
                        .MoveTo(layoutNode.CenterPosition, updateEdges: false)
                        .OnComplete(() => OnComplete(gameNode));
            }
        }

        private ILayoutNode GetLayout(GameObject go, Node node, BranchCity branchCity)
        {
            // Center position of the table the city is standing on.
            Vector3 centerPosition = branchCity.transform.parent.position;
            centerPosition.y = AbstractSEECity.SkyLevel - 1;
            LayoutGraphNode layoutNode = new(node)
            {
                CenterPosition = centerPosition,
                AbsoluteScale = go.transform.localScale
            };

            return layoutNode;
        }

        private async UniTask AnimateNodeChangeAsync(ISet<Node> changedNodes, MarkerFactory markerFactory)
        {
            HashSet<GameObject> changed = new();

            foreach (Node node in changedNodes)
            {
                GameObject go = GraphElementIDMap.Find(node.ID, true);
                if (go != null)
                {
                    markerFactory.MarkChanged(go);
                    changed.Add(go);

                    // There is a change. It may or may not be the metric determining the style.
                    // We will not further check that and just call the following method.
                    // If there is no change, this method does not need to be called because then
                    // we know that the metric values determining the style and antenna of the former
                    // and the new graph node are the same.
                    branchCity.Renderer.AdjustStyle(go);

                    ILayoutNode layoutNode = null;
                    ScaleTo(go, layoutNode);
                }
            }

            await UniTask.WaitUntil(() => changed.Count == 0);

            void ScaleTo(GameObject gameNode, ILayoutNode layoutNode)
            {
                // layoutNode.LocalScale is in world space, while the animation by iTween
                // is in local space. Our game objects may be nested in other game objects,
                // hence, the two spaces may be different.
                // We may need to transform layoutNode.LocalScale from world space to local space.
                Vector3 localScale = gameNode.transform.parent == null ?
                                         layoutNode.AbsoluteScale
                                       : gameNode.transform.parent.InverseTransformVector(layoutNode.AbsoluteScale);

                gameNode.NodeOperator()
                        .ScaleTo(localScale, updateEdges: false)
                        .OnComplete(() => OnComplete(gameNode));

            }

            void OnComplete(GameObject go)
            {
                changed.Remove(go);
                Destroyer.Destroy(go);
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
            await Task.Delay(MarkerTime);
            markerFactory.Clear();
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

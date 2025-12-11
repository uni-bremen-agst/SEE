using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MoreLinq;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.City;
using SEE.GO;
using SEE.Utils;
using UnityEngine;
using Node = SEE.DataModel.DG.Node;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Shows connected edges when the user hovers over or selects a node.
    /// </summary>
    public class ShowEdges : InteractableObjectAction
    {
        /// <summary>
        /// True if the object is currently being hovered over.
        /// </summary>
        private bool isHovered;

        /// <summary>
        /// True if the object is currently selected.
        /// </summary>
        private bool isSelected;

        /// <summary>
        /// The city object this edge is rendered in.
        /// </summary>
        private AbstractSEECity codeCity;

        /// <summary>
        /// A token that's used to cancel the transitive edge toggling.
        /// We need to use this instead of the edge operator's built-in conflict mechanism
        /// because the edge operator only controls a single edge, instead of the whole transitive closure.
        /// </summary>
        private CancellationTokenSource edgeToggleToken;

        /// <summary>
        /// The toggle that is used to determine whether an edge is currently selected.
        /// </summary>
        private const string edgeIsSelectedToggle = "IsSelected";

        /// <summary>
        /// The delay between each depth level when showing/hiding the transitive closure of edges.
        /// </summary>
        public static readonly TimeSpan TransitiveDelay = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Registers On() and Off() for the respective hovering and selection events.
        /// </summary>
        protected void OnEnable()
        {
            if (Interactable != null)
            {
                Interactable.SelectIn += SelectionOn;
                Interactable.SelectOut += SelectionOff;
                Interactable.HoverIn += HoverOn;
                Interactable.HoverOut += HoverOff;
            }
            else
            {
                Debug.LogError($"ShowEdges.OnEnable for {name} has no interactable.\n");
            }
        }

        /// <summary>
        /// Unregisters On() and Off() from the respective hovering and selection events.
        /// </summary>
        protected void OnDisable()
        {
            if (Interactable != null)
            {
                Interactable.SelectIn -= SelectionOn;
                Interactable.SelectOut -= SelectionOff;
                Interactable.HoverIn -= HoverOn;
                Interactable.HoverOut -= HoverOff;
                codeCity = null; // Reset codeCity
            }
            else
            {
                Debug.LogError($"ShowEdges.OnDisable for {name} has no interactable.\n");
            }
        }

        /// <summary>
        /// Called when the object is selected. If <paramref name="isInitiator"/> is false, a remote
        /// player has triggered this event and, hence, nothing will be done. Otherwise
        /// the connected edges are shown.
        /// </summary>
        /// <param name="interactableObject">the object being selected</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        private void SelectionOn(InteractableObject interactableObject, bool isInitiator)
        {
            if (isInitiator)
            {
                isSelected = true;
                // if the object is currently hovered over, the edges are already shown.
                // However, a selection must be triggered anyway, as it may need to override a previous selection.
                OnOff(true, true);

                if (gameObject.TryGetNode(out Node node))
                {
                    RelevantEdges(node, followSource: false, followTarget: true, true)
                        .SelectMany(x => x).ForEach(x => x.SetToggle(edgeIsSelectedToggle, true));
                }
            }
        }

        /// <summary>
        /// Called when the object is deselected. If <paramref name="isInitiator"/> is false, a remote
        /// player has triggered this event and, hence, nothing will be done. Otherwise,
        /// the shown edges are hidden unless the object is still hovered.
        /// </summary>
        /// <param name="interactableObject">the object being selected</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        private void SelectionOff(InteractableObject interactableObject, bool isInitiator)
        {
            if (isInitiator)
            {
                isSelected = false;
                if (!isHovered)
                {
                    OnOff(false, true);
                }

                if (gameObject.TryGetNode(out Node node))
                {
                    RelevantEdges(node, followSource: false, followTarget: true, true)
                        .SelectMany(x => x).ForEach(x => x.SetToggle(edgeIsSelectedToggle, false));
                }
            }
        }

        /// <summary>
        /// Called when the object is being hovered over. If <paramref name="isInitiator"/> is false, a remote
        /// player has triggered this event and, hence, nothing will be done. Otherwise
        /// the connected edges are shown.
        /// </summary>
        /// <param name="interactableObject">the object being hovered over</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        private void HoverOn(InteractableObject interactableObject, bool isInitiator)
        {
            if (isInitiator)
            {
                isHovered = true;
                // if the object is currently selected, the edges are already shown
                if (!isSelected)
                {
                    OnOff(true, false);
                }
            }
        }

        /// <summary>
        /// Called when the object is no longer hovered over. If <paramref name="isInitiator"/>
        /// is false, a remote player has triggered this event and, hence, nothing will be done.
        /// Otherwise the connected edges are hidden unless the object is still selected.
        /// </summary>
        /// <param name="interactableObject">the object being hovered over</param>
        /// <param name="isInitiator">true if a local user initiated this call</param>
        private void HoverOff(InteractableObject interactableObject, bool isInitiator)
        {
            if (isInitiator)
            {
                isHovered = false;
                if (!isSelected)
                {
                    OnOff(false, false);
                }
            }
        }

        /// <summary>
        /// Returns a list of lists of edges that are relevant (i.e., should be shown/hidden)
        /// for the given <paramref name="node"/>. The second level of the list corresponds to
        /// the depth within the transitive closure of the edges, ordered by the distance to the given node,
        /// and only becomes relevant if <paramref name="followSource"/> or <paramref name="followTarget"/> is true.
        ///
        /// To give an example, consider the following graph:
        /// <![CDATA[  A <- B -> C -> D -> E <- F  ]]>
        ///
        /// For this graph, the result of this method for node B with <paramref name="followSource"/> = false
        /// and <paramref name="followTarget"/> = true would be [[B-A, B-C], [C-D], [D-E]],
        /// where the first list contains the edges directly originating from B,
        /// the second list contains the edges originating from the nodes in the first list, and so on.
        ///
        /// As a consequence, if both <paramref name="followSource"/> and <paramref name="followTarget"/> are true,
        /// and the graph is connected, the result will contain all edges of the graph.
        /// </summary>
        /// <param name="node">The node for which to find the relevant edges.</param>
        /// <param name="followSource">Whether to follow the source of the edges.</param>
        /// <param name="followTarget">Whether to follow the target of the edges.</param>
        /// <param name="fromSelection">Whether the call is from a selection event.
        /// This is necessary because we do not want hover events to override selection events.</param>
        /// <returns>A list of lists of edges that are relevant for the given node.</returns>
        /// <remarks>It is fine if the graph is cyclic—this method will still terminate.</remarks>
        private List<List<Edge>> RelevantEdges(Node node, bool followSource, bool followTarget, bool fromSelection)
        {
            // Directly connected edges first.
            IEnumerable<IEnumerable<Edge>> edges = IteratedConnectedEdges(_ => null, _ => null);

            if (followSource)
            {
                edges = edges.ZipLongest(IteratedConnectedEdges(x => x.Source, x => x.Target), MergeEdgeLevel);
            }
            if (followTarget)
            {
                edges = edges.ZipLongest(IteratedConnectedEdges(x => x.Target, x => x.Source), MergeEdgeLevel);
            }

            return edges.Select(x => x.ToList()).ToList();

            IEnumerable<IEnumerable<Edge>> IteratedConnectedEdges(Func<Edge, Node> getSuccessorNode,
                                                           Func<Edge, Node> getPredecessorNode)
            {
                HashSet<Node> visitedNodes = new();
                // We will do a breadth-first traversal of the subgraph induced by the connected edges.
                Queue<(Node node, int distance)> nodeQueue = new(new[] { (node, 0) });
                DefaultDictionary<int, List<Edge>> results = new();
                while (nodeQueue.Count > 0)
                {
                    (Node currentNode, int distance) = nodeQueue.Dequeue();
                    if (!visitedNodes.Add(currentNode))
                    {
                        // We already handled this node.
                        continue;
                    }
                    List<Edge> connected = DirectlyConnectedEdges(currentNode).ToList();
                    results[distance].AddRange(connected.Where(x => x.HasToggle(Edge.IsHiddenToggle)
                                                                   // Hover should not override edges shown by selection.
                                                                   && (fromSelection || !x.HasToggle(edgeIsSelectedToggle))));
                    // Queue successors, if there are any.
                    connected.Select(getSuccessorNode)
                             .Where(x => x != null)
                             .Select(x => (x, distance + 1))
                             .ForEach(nodeQueue.Enqueue);
                }
                return results.OrderBy(x => x.Key).Select(x => x.Value);

                IEnumerable<Edge> DirectlyConnectedEdges(Node forNode)
                {
                    return codeCity.EdgeLayoutSettings.AnimateInnerEdges
                        ? forNode.PostOrderDescendants().SelectMany(x => x.Edges.Where(e => RelevantEdge(e, x)))
                        : forNode.Edges.Where(e => RelevantEdge(e, forNode));
                }

                bool RelevantEdge(Edge edge, Node forNode) => getPredecessorNode(edge) == null || getPredecessorNode(edge) == forNode;
            }

            IEnumerable<Edge> MergeEdgeLevel(IEnumerable<Edge> first, IEnumerable<Edge> second)
            {
                return (first ?? Enumerable.Empty<Edge>()).Union(second ?? Enumerable.Empty<Edge>());
            }
        }

        /// <summary>
        /// Shows/hides all incoming/outgoing edges of the node this component is attached to.
        /// </summary>
        /// <param name="show">if true, the edges are shown; otherwise hidden</param>
        /// <param name="fromSelection">Whether the call is from a selection event rather than a hover event.</param>
        private void OnOff(bool show, bool fromSelection)
        {
            if (gameObject.TryGetNode(out Node node))
            {
                codeCity ??= SceneQueries.City(gameObject);
                if (!isSelected)
                {
                    edgeToggleToken?.Cancel();
                    edgeToggleToken = new CancellationTokenSource();
                }
                EdgeLayoutAttributes layout = codeCity.EdgeLayoutSettings;
                List<List<Edge>> edges = RelevantEdges(node,
                                                       followSource: layout.AnimateTransitiveSourceEdges,
                                                       followTarget: layout.AnimateTransitiveTargetEdges,
                                                       fromSelection);
                ToggleEdges(edges, edgeToggleToken.Token).Forget();
            }
            return;

            async UniTaskVoid ToggleEdges(IEnumerable<IList<Edge>> edges, CancellationToken token)
            {
                EdgeAnimationKind animationKind = codeCity.EdgeLayoutSettings.AnimationKind;

                foreach (IList<Edge> edgeLevel in edges)
                {
                    foreach (Edge edge in edgeLevel)
                    {
                        edge.Operator(mustFind: false)?.ShowOrHide(show, animationKind);
                    }
                    if (show)
                    {
                        await UniTask.Delay(TransitiveDelay, cancellationToken: token);
                    }
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }
                }
            }
        }
    }
}

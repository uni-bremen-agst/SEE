using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MoreLinq;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.Game.Operator;
using SEE.GO;
using SEE.Utils;
using UnityEngine;
using Node = SEE.DataModel.DG.Node;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Shows connected edges when the user hovers over or selects a node.
    /// </summary>
    /// <remarks>This component is assumed to be attached to a game node.</remarks>
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
        /// The city object the hovered graph element is rendered in.
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
        /// True if the game object this component is attached to is the
        /// root of the underlying graph.
        /// </summary>
        /// <returns>True if root.</returns>
        /// <exception cref="Exception">Thrown if there is no valid graph node.</exception>
        private bool IsRoot()
        {
            if (!gameObject.TryGetNode(out Node node))
            {
                throw new Exception($"{gameObject.FullName()} has no {nameof(Node)}.\n");
                // If no graph node is associated with this game node,
                // we cannot derive any edges.
            }
            return node.IsRoot();
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
        /// <param name="interactableObject">The object being selected.</param>
        /// <param name="isInitiator">True if a local user initiated this call.</param>
        private void SelectionOn(InteractableObject interactableObject, bool isInitiator)
        {
            if (IsRoot())
            {
                return;
            }
            if (isInitiator)
            {
                isSelected = true;
                // If the object is currently hovered over, the edges are already shown.
                // However, a selection must be triggered anyway, as it may need to override a previous selection.
                OnOff(true, true);
                SetSelectedEdgesTransitivelyForward(true);
            }
        }

        /// <summary>
        /// Called when the object is deselected. If <paramref name="isInitiator"/> is false, a remote
        /// player has triggered this event and, hence, nothing will be done. Otherwise,
        /// the shown edges are hidden unless the object is still hovered.
        /// </summary>
        /// <param name="interactableObject">The object being selected.</param>
        /// <param name="isInitiator">True if a local user initiated this call.</param>
        private void SelectionOff(InteractableObject interactableObject, bool isInitiator)
        {
            if (IsRoot())
            {
                return;
            }
            if (isInitiator)
            {
                isSelected = false;
                if (!isHovered)
                {
                    OnOff(false, true);
                }
                SetSelectedEdgesTransitivelyForward(false);
            }
        }

        /// <summary>
        /// Sets or unsets the <see cref="edgeIsSelectedToggle"/> for all edges
        /// transitively reachable from the node associated with this gameObject
        /// in forward direction.
        /// </summary>
        /// <param name="setToggle">Whether to set or unset the toggle.</param>
        private void SetSelectedEdgesTransitivelyForward(bool setToggle)
        {
            if (gameObject.TryGetNode(out Node node))
            {
                RelevantEdges(node, followSource: false, followTarget: true, true, e => e.HasToggle(Edge.IsHiddenToggle))
                    .SelectMany(x => x).ForEach(e => e.SetToggle(edgeIsSelectedToggle, setToggle));
            }
        }

        /// <summary>
        /// Called when the object is being hovered over. If <paramref name="isInitiator"/> is false, a remote
        /// player has triggered this event and, hence, nothing will be done. Otherwise
        /// the connected edges are shown.
        /// </summary>
        /// <param name="interactableObject">The object being hovered over.</param>
        /// <param name="isInitiator">True if a local user initiated this call.</param>
        private void HoverOn(InteractableObject interactableObject, bool isInitiator)
        {
            if (IsRoot())
            {
                return;
            }
            if (isInitiator)
            {
                isHovered = true;
                // If the object is currently selected, the edges are already shown.
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
        /// <param name="interactableObject">The object being hovered over.</param>
        /// <param name="isInitiator">True if a local user initiated this call.</param>
        private void HoverOff(InteractableObject interactableObject, bool isInitiator)
        {
            if (IsRoot())
            {
                return;
            }
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
        /// <param name="shouldBeFollowed">Determines whether to follow an edge. This predicate
        /// must be true if the edge should be followed.</param>
        /// <returns>A list of lists of edges that are relevant for the given node.</returns>
        /// <remarks>It is fine if the graph is cyclic—this method will still terminate.</remarks>
        private List<List<Edge>> RelevantEdges(Node node, bool followSource, bool followTarget, bool fromSelection, Func<Edge, bool> shouldBeFollowed)
        {
            // Directly connected edges first.
            IEnumerable<IEnumerable<Edge>> edges = IteratedConnectedEdges(_ => null, _ => null, shouldBeFollowed);

            if (followSource)
            {
                edges = edges.ZipLongest(IteratedConnectedEdges(e => e.Source, e => e.Target, shouldBeFollowed), MergeEdgeLevel);
            }
            if (followTarget)
            {
                edges = edges.ZipLongest(IteratedConnectedEdges(e => e.Target, e => e.Source, shouldBeFollowed), MergeEdgeLevel);
            }

            return edges.Select(x => x.ToList()).ToList();

            IEnumerable<IEnumerable<Edge>> IteratedConnectedEdges(Func<Edge, Node> getSuccessorNode,
                                                                  Func<Edge, Node> getPredecessorNode,
                                                                  Func<Edge, bool> shouldBeFollowed)
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
                    results[distance].AddRange(connected.Where(e => shouldBeFollowed(e)
                                                                   // Hover should not override edges shown by selection.
                                                                   && (fromSelection || !e.HasToggle(edgeIsSelectedToggle))));
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
        /// This does not only depend upon <paramref name="show"/> but also on the active
        /// <see cref="ShowEdgeStrategy"/>.
        ///
        /// If <see cref="ShowEdgeStrategy.Always"/> is active, we show all edges no matter
        /// whether <see cref="show"/> is false. Likewise, if <see cref="ShowEdgeStrategy.Never"/>
        /// is active, we do not show edges even if <paramref name="show"/> is true. If
        /// <see cref="ShowEdgeStrategy.OnHoverOnly"/> is active, <paramref name="show"/> must
        /// be true to show an edge.
        ///
        /// In addition, we will highlight an edge if <see cref="ShowEdgeStrategy.Always"/> is active
        /// and <paramref name="show"/> is true. There is no need to highlight shown edges if
        /// <see cref="ShowEdgeStrategy.OnHoverOnly"/> is active because there are only the
        /// edges associated with the current node currently shown. A user can follow the edges
        /// visually without any highlighting.
        ///
        /// The nodes associated with the currently shown edges will be highlighted if and only
        /// if <paramref name="show"/> is true, no matter what <see cref="ShowEdgeStrategy"/>
        /// is active. Since nodes are always visible, we need to distinguish between those
        /// related to the current node and all other nodes not related to the current node.
        /// We hightlight the related nodes even if <see cref="ShowEdgeStrategy.Never"/>
        /// is active to provide a visual hint of their relatedness.
        ///
        /// </summary>
        /// <param name="show">If true, the edges are shown; otherwise hidden.</param>
        /// <param name="fromSelection">Whether the call is from a selection event rather than a hover event.</param>
        private void OnOff(bool show, bool fromSelection)
        {
            if (gameObject.TryGetNode(out Node node))
            {
                codeCity ??= gameObject.ContainingCity();
                if (!isSelected)
                {
                    edgeToggleToken?.Cancel();
                    edgeToggleToken = new CancellationTokenSource();
                }
                EdgeLayoutAttributes layout = codeCity.EdgeLayoutSettings;
                List<List<Edge>> edges = RelevantEdges(node,
                                                       followSource: layout.AnimateTransitiveSourceEdges,
                                                       followTarget: layout.AnimateTransitiveTargetEdges,
                                                       fromSelection,
                                                       e => layout.ShowEdges != ShowEdgeStrategy.OnHoverOnly
                                                            || e.HasToggle(Edge.IsHiddenToggle));
                //Dump(edges);

                ShowEdges(edges,
                          (layout.ShowEdges == ShowEdgeStrategy.Always || show) && layout.ShowEdges != ShowEdgeStrategy.Never,
                          layout.ShowEdges == ShowEdgeStrategy.Always && show,
                          show,
                          codeCity.EdgeLayoutSettings.AnimationKind, edgeToggleToken.Token).Forget();
            }
            return;

            static async UniTaskVoid ShowEdges
                 (IEnumerable<IList<Edge>> edges,  // edges to be considered
                  bool showEdges,                  // whether edges should be shown
                  bool highlightEdges,             // whether shown edges should be highlighted
                  bool highlightNodes,             // whether nodes related to the edges are to be highlighted
                  EdgeAnimationKind animationKind, // how to animate the edges when they are shown or hidden
                  CancellationToken token)         // for cancellation
            {
                foreach (IList<Edge> edgeLevel in edges)
                {
                    foreach (Edge edge in edgeLevel)
                    {
                        EdgeOperator edgeOperator = edge.Operator(mustFind: false);
                        edgeOperator?.ShowOrHide(showEdges, animationKind);
                        HighlightNode(edge.Source, highlightNodes);
                        HighlightNode(edge.Target, highlightNodes);
                        Highlight(edgeOperator, highlightEdges);
                    }
                    if (showEdges)
                    {
                        await UniTask.Delay(TransitiveDelay, cancellationToken: token);
                    }
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }
                }

                static void HighlightNode(Node node, bool highlight)
                {
                    Highlight(node.Operator(), highlight);
                }

                static void Highlight(GraphElementOperator op, bool highlight)
                {
                    if (highlight)
                    {
                        op?.GlowIn();
                    }
                    else
                    {
                        op?.GlowOut();
                    }
                }
            }
        }
    }
}

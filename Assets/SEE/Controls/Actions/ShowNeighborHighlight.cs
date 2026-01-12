using System.Collections.Generic;
using SEE.DataModel.DG;
using SEE.Game.Operator;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Highlights direct neighbor nodes (connected by a single edge) when the user selects/clicks
    /// a node. The connected edges will change color and the neighbor nodes will
    /// receive a subtle glow effect.
    /// </summary>
    public class ShowNeighborHighlight : InteractableObjectAction
    {
        /// <summary>
        /// True if the object is currently selected.
        /// </summary>
        private bool isSelected;

        /// <summary>
        /// The set of currently highlighted neighbor game objects.
        /// </summary>
        private readonly HashSet<GameObject> highlightedNeighbors = new();

        /// <summary>
        /// The set of currently highlighted edges.
        /// </summary>
        private readonly HashSet<GameObject> highlightedEdges = new();

        /// <summary>
        /// The original colors of the highlighted edges, to restore them later.
        /// </summary>
        private readonly Dictionary<GameObject, (Color start, Color end)> originalEdgeColors = new();

        /// <summary>
        /// The highlight color for connected edges (purple).
        /// </summary>
        private static readonly Color edgeHighlightColor = new(0.6f, 0.2f, 0.8f, 1f); // Purple

        /// <summary>
        /// Maximum number of neighbors to highlight to avoid overwhelming visual effects.
        /// </summary>
        private const int maxNeighborsToHighlight = 10;

        /// <summary>
        /// Scale factor for highlighted neighbor buildings (subtle size increase).
        /// </summary>
        private const float popScaleFactor = 1.08f;

        /// <summary>
        /// Glow intensity for neighbor buildings (visible but not overwhelming).
        /// </summary>
        private const float neighborGlowFactor = 0.4f;

        /// <summary>
        /// The original scales of the highlighted neighbors, to restore them later.
        /// </summary>
        private readonly Dictionary<GameObject, Vector3> originalNeighborScales = new();

        /// <summary>
        /// Registers for selection events only (not hover).
        /// </summary>
        protected void OnEnable()
        {
            if (Interactable != null)
            {
                Interactable.SelectIn += SelectionOn;
                Interactable.SelectOut += SelectionOff;
            }
            else
            {
                Debug.LogError($"{nameof(ShowNeighborHighlight)}.{nameof(OnEnable)} for {name} has no interactable.\n");
            }
        }

        /// <summary>
        /// Unregisters from selection events.
        /// </summary>
        protected void OnDisable()
        {
            if (Interactable != null)
            {
                Interactable.SelectIn -= SelectionOn;
                Interactable.SelectOut -= SelectionOff;
            }
            else
            {
                Debug.LogError($"{nameof(ShowNeighborHighlight)}.{nameof(OnDisable)} for {name} has no interactable.\n");
            }
        }

        /// <summary>
        /// Called when the object is selected/clicked. If <paramref name="isInitiator"/> is false, a remote
        /// player has triggered this event and, hence, nothing will be done. Otherwise
        /// the neighbor nodes are highlighted.
        /// </summary>
        /// <param name="interactableObject">The object being selected.</param>
        /// <param name="isInitiator">True if a local user initiated this call.</param>
        private void SelectionOn(InteractableObject interactableObject, bool isInitiator)
        {
            if (isInitiator)
            {
                isSelected = true;
                HighlightNeighbors(true);
            }
        }

        /// <summary>
        /// Called when the object is deselected. If <paramref name="isInitiator"/> is false, a remote
        /// player has triggered this event and, hence, nothing will be done. Otherwise
        /// the neighbor highlight is removed.
        /// </summary>
        /// <param name="interactableObject">The object being selected.</param>
        /// <param name="isInitiator">True if a local user initiated this call.</param>
        private void SelectionOff(InteractableObject interactableObject, bool isInitiator)
        {
            if (isInitiator)
            {
                isSelected = false;
                HighlightNeighbors(false);
            }
        }

        /// <summary>
        /// Highlights or unhighlights the direct neighbor nodes connected by edges.
        /// </summary>
        /// <param name="highlight">If true, highlight neighbors; otherwise, remove highlight.</param>
        private void HighlightNeighbors(bool highlight)
        {
            if (!gameObject.TryGetNode(out Node node))
            {
                return;
            }

            if (highlight)
            {
                // Get all directly connected edges
                IEnumerable<Edge> connectedEdges = GetDirectlyConnectedEdges(node);
                int neighborCount = 0;

                foreach (Edge edge in connectedEdges)
                {
                    // Limit the number of neighbors to highlight for better UX
                    if (neighborCount >= maxNeighborsToHighlight)
                    {
                        break;
                    }

                    // Get the neighbor node (the other end of the edge)
                    Node neighborNode = edge.Source == node ? edge.Target : edge.Source;

                    // Find the game object for the neighbor node
                    GameObject neighborGameObject = neighborNode.GameObject();
                    if (neighborGameObject != null && !highlightedNeighbors.Contains(neighborGameObject))
                    {
                        HighlightNeighborNode(neighborGameObject, true);
                        highlightedNeighbors.Add(neighborGameObject);
                        neighborCount++;
                    }

                    // Find the game object for the edge and highlight it
                    GameObject edgeGameObject = edge.GameObject();
                    if (edgeGameObject != null && !highlightedEdges.Contains(edgeGameObject))
                    {
                        HighlightEdge(edgeGameObject, true);
                        highlightedEdges.Add(edgeGameObject);
                    }
                }
            }
            else
            {
                // Remove highlight from all neighbor nodes
                foreach (GameObject neighborGameObject in highlightedNeighbors)
                {
                    if (neighborGameObject != null)
                    {
                        HighlightNeighborNode(neighborGameObject, false);
                    }
                }
                highlightedNeighbors.Clear();
                originalNeighborScales.Clear();

                // Remove highlight from all edges
                foreach (GameObject edgeGameObject in highlightedEdges)
                {
                    if (edgeGameObject != null)
                    {
                        HighlightEdge(edgeGameObject, false);
                    }
                }
                highlightedEdges.Clear();
                originalEdgeColors.Clear();
            }
        }

        /// <summary>
        /// Gets all edges directly connected to the given node (not descendants).
        /// Only returns edges where this node is the actual source or target.
        /// </summary>
        /// <param name="node">The node to get edges for.</param>
        /// <returns>All edges directly connected to the node (both incoming and outgoing).</returns>
        private static IEnumerable<Edge> GetDirectlyConnectedEdges(Node node)
        {
            // Only get edges where this exact node is the source or target
            // This ensures we only highlight true direct neighbors
            return node.Edges;
        }

        /// <summary>
        /// Applies or removes highlight effect on a neighbor node.
        /// Applies glow and subtle scale increase to make the building stand out.
        /// </summary>
        /// <param name="neighborGameObject">The neighbor node's game object.</param>
        /// <param name="highlight">True to highlight, false to remove highlight.</param>
        private void HighlightNeighborNode(GameObject neighborGameObject, bool highlight)
        {
            NodeOperator nodeOp = neighborGameObject.NodeOperator();
            if (nodeOp == null)
            {
                return;
            }

            if (highlight)
            {
                // Store original scale if not already stored
                if (!originalNeighborScales.ContainsKey(neighborGameObject))
                {
                    originalNeighborScales[neighborGameObject] = neighborGameObject.transform.localScale;
                }

                // Apply glow effect to the neighbor building
                nodeOp.GlowIn(neighborGlowFactor);

                // Apply subtle scale increase
                Vector3 originalScale = originalNeighborScales[neighborGameObject];
                Vector3 targetScale = originalScale * popScaleFactor;
                nodeOp.ScaleTo(targetScale, 0.2f);
            }
            else
            {
                // Remove glow effect
                nodeOp.GlowOut(0.2f);

                // Restore original scale
                if (originalNeighborScales.TryGetValue(neighborGameObject, out Vector3 originalScale))
                {
                    nodeOp.ScaleTo(originalScale, 0.2f);
                }
            }
        }

        /// <summary>
        /// Applies or removes highlight effect on an edge.
        /// Color change and data flow animation only (no glow to avoid visual clutter).
        /// </summary>
        /// <param name="edgeGameObject">The edge's game object.</param>
        /// <param name="highlight">True to highlight, false to remove highlight.</param>
        private void HighlightEdge(GameObject edgeGameObject, bool highlight)
        {
            EdgeOperator edgeOp = edgeGameObject.EdgeOperator();
            if (edgeOp == null)
            {
                return;
            }

            if (highlight)
            {
                // Store original color if not already stored
                if (!originalEdgeColors.ContainsKey(edgeGameObject))
                {
                    originalEdgeColors[edgeGameObject] = edgeOp.TargetColor;
                }

                // Change edge color to highlight color (no glow - it accumulates too much)
                edgeOp.ChangeColorsTo((edgeHighlightColor, edgeHighlightColor), 0.15f);

                // Animate data flow on the edge
                edgeOp.AnimateDataFlow(true);
            }
            else
            {
                // Restore original color
                if (originalEdgeColors.TryGetValue(edgeGameObject, out (Color start, Color end) originalColor))
                {
                    edgeOp.ChangeColorsTo(originalColor, 0.15f);
                }

                // Stop data flow animation
                edgeOp.AnimateDataFlow(false);
            }
        }

        /// <summary>
        /// Cleanup when the component is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            // Remove all highlights
            HighlightNeighbors(false);
        }
    }
}

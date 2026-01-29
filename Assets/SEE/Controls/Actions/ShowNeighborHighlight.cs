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
        /// The set of currently highlighted neighbor game nodes.
        /// </summary>
        private readonly HashSet<GameObject> highlightedNeighbors = new();

        /// <summary>
        /// The set of currently highlighted edges.
        /// </summary>
        private readonly HashSet<GameObject> highlightedEdges = new();

        /// <summary>
        /// Maximum number of neighbors to highlight to avoid overwhelming visual effects.
        /// </summary>
        private const int maxNeighborsToHighlight = 10;

        /// <summary>
        /// Glow intensity for neighbor buildings (visible but not overwhelming).
        /// </summary>
        private const float neighborGlowFactor = 0.4f;

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
                int neighborCount = 0;

                foreach (Edge edge in node.Edges)
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

                // Remove highlight from all edges
                foreach (GameObject edgeGameObject in highlightedEdges)
                {
                    if (edgeGameObject != null)
                    {
                        HighlightEdge(edgeGameObject, false);
                    }
                }
                highlightedEdges.Clear();
            }
        }

        /// <summary>
        /// Applies or removes highlight (glow) effect on <paramref name="gameNode"/>.
        /// </summary>
        /// <param name="gameNode">The game object representing the node to be highlighted.</param>
        /// <param name="highlight">True to highlight, false to remove highlight.</param>
        private void HighlightNeighborNode(GameObject gameNode, bool highlight)
        {
            NodeOperator nodeOp = gameNode.NodeOperator();
            if (highlight)
            {
                // Apply glow effect to the neighbor building
                nodeOp.GlowIn(neighborGlowFactor);
            }
            else
            {
                // Remove glow effect
                nodeOp.GlowOut(0.2f);
            }
        }

        /// <summary>
        /// Applies or removes highlight (glow) effect on an edge.
        /// Color change and data flow animation only (no glow to avoid visual clutter).
        /// </summary>
        /// <param name="edgeGameObject">The edge's game object.</param>
        /// <param name="highlight">True to highlight, false to remove highlight.</param>
        private void HighlightEdge(GameObject edgeGameObject, bool highlight)
        {
            EdgeOperator edgeOp = edgeGameObject.EdgeOperator();

            if (highlight)
            {
                edgeOp.GlowIn(neighborGlowFactor);

                // Animate data flow on the edge
                edgeOp.AnimateDataFlow(true);
            }
            else
            {
                edgeOp.GlowOut(0.2f);
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

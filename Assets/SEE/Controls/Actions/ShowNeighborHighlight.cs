using System.Collections.Generic;
using System.Linq;
using SEE.DataModel.DG;
using SEE.Game.Operator;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Highlights direct neighbor nodes (connected by a single edge) when the user selects/clicks
    /// a node. Both the neighbor nodes and connected edges receive a subtle glow effect.
    /// Node scale and edge color are not modified as they have semantics.
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
        /// Relative timing to glow on or off.
        /// </summary>
        private const float glowTimingFactor = 0.4f;

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
        /// Unregisters from selection events and clears any active highlights.
        /// </summary>
        protected void OnDisable()
        {
            // Clear any active highlights before unregistering events
            HighlightNeighbors(false);

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
        /// <param name="interactableObject">The object being deselected.</param>
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
            if (highlight)
            {
                if (!gameObject.TryGetNode(out Node node))
                {
                    return;
                }

                foreach (Edge edge in node.Edges)
                {
                    // Get the neighbor node (the other end of the edge)
                    Node neighborNode = edge.Source == node ? edge.Target : edge.Source;

                    // Find the game object for the neighbor node
                    GameObject neighborGameObject = neighborNode.GameObject();
                    if (neighborGameObject != null && !highlightedNeighbors.Contains(neighborGameObject))
                    {
                        HighlightNode(neighborGameObject, true);
                        highlightedNeighbors.Add(neighborGameObject);
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
                // Remove highlight from all valid neighbor nodes that are not currently selected
                // (handles concurrent selection where mutual neighbors remain highlighted)
                foreach (GameObject neighborGameObject in highlightedNeighbors
                             .Where(go => go != null && !IsGameObjectSelected(go)))
                {
                    HighlightNode(neighborGameObject, false);
                }
                highlightedNeighbors.Clear();

                // Remove highlight from all valid edges where neither endpoint is selected
                foreach (GameObject edgeGameObject in highlightedEdges
                             .Where(go => go != null && !IsEdgeConnectedToSelectedNode(go)))
                {
                    HighlightEdge(edgeGameObject, false);
                }
                highlightedEdges.Clear();
            }
        }

        /// <summary>
        /// Applies or removes highlight (glow) effect on <paramref name="gameNode"/>.
        /// </summary>
        /// <param name="gameNode">The game object representing the node to be highlighted.</param>
        /// <param name="highlight">True to highlight, false to remove highlight.</param>
        private static void HighlightNode(GameObject gameNode, bool highlight)
        {
            Highlight(gameNode.NodeOperator(), highlight);
        }

        /// <summary>
        /// Applies or removes highlight effect on an edge using glow and data flow animation.
        /// </summary>
        /// <param name="edgeGameObject">The edge's game object.</param>
        /// <param name="highlight">True to highlight, false to remove highlight.</param>
        private static void HighlightEdge(GameObject edgeGameObject, bool highlight)
        {
            EdgeOperator edgeOp = edgeGameObject.EdgeOperator();
            Highlight(edgeOp, highlight);
            edgeOp.AnimateDataFlow(highlight);
        }

        /// <summary>
        /// Applies glow effect using <see cref="op"/> if <paramref name="highlight"/>
        /// or otherwise removes it.
        /// </summary>
        /// <param name="op">The operator to be used to apply/remove the glow effect.</param>
        /// <param name="highlight">Whether the effect is to be removed or applied.</param>
        private static void Highlight(GraphElementOperator op, bool highlight)
        {
            if (highlight)
            {
                op.GlowIn(glowTimingFactor);
            }
            else
            {
                op.GlowOut(glowTimingFactor);
            }
        }

        /// <summary>
        /// Checks if the given game object is currently selected.
        /// </summary>
        /// <param name="go">The game object to check.</param>
        /// <returns>True if the object has an InteractableObject component that is selected.</returns>
        private static bool IsGameObjectSelected(GameObject go)
        {
            return go.TryGetComponent(out InteractableObject interactable) && interactable.IsSelected;
        }

        /// <summary>
        /// Checks if an edge is connected to any currently selected node.
        /// </summary>
        /// <param name="edgeGameObject">The edge game object to check.</param>
        /// <returns>True if either endpoint of the edge is currently selected.</returns>
        private static bool IsEdgeConnectedToSelectedNode(GameObject edgeGameObject)
        {
            if (!edgeGameObject.TryGetEdge(out Edge edge))
            {
                return false;
            }

            GameObject sourceGO = edge.Source?.GameObject();
            GameObject targetGO = edge.Target?.GameObject();

            return (sourceGO != null && IsGameObjectSelected(sourceGO))
                || (targetGO != null && IsGameObjectSelected(targetGO));
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

using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.Game.Operator;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Shows or hides all edges of a hovered code city.
    /// </summary>
    /// <remarks>This behaviour is meant to be attached to a player.</remarks>
    public class ShowEdgesAction : MonoBehaviour
    {
        /// <summary>
        /// If the user requests to toggle the visibility of the edges, all edges
        /// in the code city containing the node the user is currently hovering
        /// over are shown or hidden, respectively.
        /// </summary>
        private void Update()
        {
            if (SEEInput.ToggleEdges()
                && Raycasting.RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef _) != HitGraphElement.None)
            {
                ShowOrHide(raycastHit.collider.gameObject);
            }
        }

        /// <summary>
        /// The duration of the showing/hiding animation in seconds.
        /// </summary>
        private const float ANIMATION_DURATION = 1.0f;

        /// <summary>
        /// Shows or hides all edges of the code city the <paramref name="hoveredGraphElement"/> belongs to.
        /// </summary>
        /// <param name="hoveredGraphElement">the graph element currently being hovered</param>
        private static void ShowOrHide(GameObject hoveredGraphElement)
        {
            AbstractSEECity codeCity = hoveredGraphElement.ContainingCity();
            if (codeCity != null)
            {
                foreach (GameObject gameEdge in codeCity.gameObject.AllEdges())
                {
                    if (gameEdge.TryGetEdge(out Edge edge))
                    {
                        EdgeOperator edgeOperator = gameEdge.AddOrGetComponent<EdgeOperator>();
                        if (edge.HasToggle(Edge.IsHiddenToggle))
                        {
                            edge.UnsetToggle(Edge.IsHiddenToggle);
                            edgeOperator.Show(codeCity.EdgeLayoutSettings.AnimationKind, ANIMATION_DURATION);
                        }
                        else
                        {
                            edge.SetToggle(Edge.IsHiddenToggle);
                            edgeOperator.Hide(codeCity.EdgeLayoutSettings.AnimationKind, ANIMATION_DURATION);
                        }
                    }
                }
            }
            else
            {
                Debug.LogError($"Node {hoveredGraphElement.name} is not contained in a code city.\n");
            }
        }
    }
}

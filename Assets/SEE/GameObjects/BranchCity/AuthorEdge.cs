using SEE.Game.City;
using SEE.GO;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace SEE.GameObjects
{
    /// <summary>
    /// Attribute of an author edge connecting an <see cref="AuthorSphere"/>
    /// to <see cref="AuthorRef"/>.
    /// </summary>
    /// <remarks>This component will be attached to connections between authors and their edited files.</remarks>
    public class AuthorEdge : SerializedMonoBehaviour
    {
        /// <summary>
        /// Reference to the target node this edge connects to.
        /// </summary>
        public AuthorRef FileNode;

        /// <summary>
        /// Reference to the <see cref="GameObjects.AuthorSphere"/> that this edge originates from.
        /// </summary>
        public AuthorSphere AuthorSphere;

        /// <summary>
        /// The width of the edge.
        /// </summary>
        public float Width;

        /// <summary>
        /// Draws the edge between the <see cref="AuthorSphere"/> and the <see cref="FileNode"/>.
        /// </summary>
        internal void Draw()
        {
            // The edge inherits the material of the AuthorSphere.
            Material authorMaterial = AuthorSphere.gameObject.GetComponent<Renderer>().sharedMaterial;

            Vector3[] linePoints = new Vector3[2];
            linePoints[0] = AuthorSphere.gameObject.transform.position;
            linePoints[1] = FileNode.gameObject.GetRoofCenter();

            LineFactory.Draw(gameObject, linePoints, Width, authorMaterial);

            // Initial visibility of the edge must be set according to the current strategy.
            ShowOrHide(isHovered: false);
        }

        /// <summary>
        /// Sets the visibility of the edge.
        /// </summary>
        /// <param name="isHovered">whether any end of the edge (author or file node) is currently hovered</param>
        internal void ShowOrHide(bool isHovered)
        {
            if (gameObject.ContainingCity() is BranchCity branchCity)
            {
                bool show = branchCity.ShowAuthorEdges switch
                {
                    ShowAuthorEdgeStrategy.ShowAlways => true,
                    ShowAuthorEdgeStrategy.ShowOnHoverOnly => isHovered,
                    ShowAuthorEdgeStrategy.ShowOnHoverOrWithMultipleAuthors => isHovered
                                                                               || FileNode.Count >= branchCity.AuthorThreshold,
                    _ => throw new NotImplementedException(),
                };

                if (TryGetComponent(out LineRenderer lineRenderer))
                {
                    lineRenderer.enabled = show;
                }
            }
        }

        /// <summary>
        /// Updates the positions of the edge's line renderer to match the current positions
        /// of the <see cref="AuthorSphere"/> and the <see cref="FileNode"/>.
        /// </summary>
        internal void UpdateLayout()
        {
            if (TryGetComponent(out LineRenderer lineRenderer))
            {
                lineRenderer.SetPosition(0, AuthorSphere.gameObject.transform.position);
                lineRenderer.SetPosition(1, FileNode.gameObject.GetRoofCenter());
            }
        }

        /// <summary>
        /// Updates the visibility of the associated line renderer based on the number of authors  and the visibility
        /// strategy of the containing city.
        /// </summary>
        /// <remarks>This method enables the line renderer if the containing city is a <see
        /// cref="BranchCity"/>  with a visibility strategy of <see
        /// cref="ShowAuthorEdgeStrategy.ShowOnHoverOrWithMultipleAuthors"/>, and the number of authors meets or
        /// exceeds the city's author threshold.</remarks>
        /// <param name="numberOfAuthors">The number of authors associated with the current object.</param>
        /// <returns>True if the visibility of the line renderer was changed; otherwise, false.</returns>
        internal bool UpdateVisibility(int numberOfAuthors)
        {
            if (gameObject.ContainingCity() is BranchCity branchCity
                && branchCity.ShowAuthorEdges == ShowAuthorEdgeStrategy.ShowOnHoverOrWithMultipleAuthors)
            {
                if (TryGetComponent(out LineRenderer lineRenderer))
                {
                    bool isLineVisible = lineRenderer.enabled;
                    lineRenderer.enabled = numberOfAuthors >= branchCity.AuthorThreshold;
                    return isLineVisible != lineRenderer.enabled;
                }
            }
            return false;
        }
    }
}

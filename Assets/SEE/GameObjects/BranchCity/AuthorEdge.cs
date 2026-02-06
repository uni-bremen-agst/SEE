using SEE.Game.City;
using SEE.GO;
using SEE.GO.Factories;
using System;
using UnityEngine;

namespace SEE.GameObjects.BranchCity
{
    /// <summary>
    /// Attribute of an author edge connecting an <see cref="AuthorSphere"/>
    /// to <see cref="AuthorRef"/>.
    /// </summary>
    /// <remarks>This component will be attached to connections between authors and their edited files.</remarks>
    [RequireComponent(typeof(LineRenderer))]
    public class AuthorEdge : VCSDecorator
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
        /// The line renderer to draw the line of this author edge.
        /// </summary>
        private LineRenderer lineRenderer;

        /// <summary>
        /// Sets <see cref="lineRenderer"/>. If no <see cref="LineRenderer"/> is
        /// available, the component is disabled. This can hardly ever happen because
        /// of our <see cref="RequireComponent"/> annotation above.
        /// </summary>
        private void Start()
        {
            /// We should have a LineRenderer according to the <see cref="RequireComponent"/>
            /// annotation above.
            if (!gameObject.TryGetComponentOrLog(out lineRenderer))
            {
                enabled = false;
            }
        }

        /// <summary>
        /// Draws the edge between the <see cref="AuthorSphere"/> and the <see cref="FileNode"/>.
        /// </summary>
        internal void Draw()
        {
            Vector3[] linePoints = new Vector3[2];
            linePoints[0] = AuthorSphere.gameObject.transform.position;
            linePoints[1] = FileNode.gameObject.GetRoofCenter();

            lineRenderer = LineFactory.Draw(gameObject, linePoints, Width, AuthorSphere.LineMaterial);

            // Initial visibility of the edge must be set according to the current strategy.
            ShowOrHide(isHovered: false);
        }

        /// <summary>
        /// Sets the visibility of the edge.
        /// </summary>
        /// <param name="isHovered">Whether any end of the edge (author or file node) is currently hovered.</param>
        internal void ShowOrHide(bool isHovered)
        {
            if (City != null)
            {
                bool show = City.ShowAuthorEdges switch
                {
                    ShowAuthorEdgeStrategy.Always => true,
                    ShowAuthorEdgeStrategy.OnHoverOnly => isHovered,
                    ShowAuthorEdgeStrategy.OnHoverOrWithMultipleAuthors => isHovered
                                                                           || FileNode.Count >= City.AuthorThreshold,
                    _ => throw new NotImplementedException(),
                };

                lineRenderer.enabled = show;
            }
        }

        /// <summary>
        /// Updates the positions of the edge's line renderer to match the current positions
        /// of the <see cref="AuthorSphere"/> and the <see cref="FileNode"/>.
        /// </summary>
        private void Update()
        {
            if (lineRenderer.enabled)
            {
                lineRenderer.SetPosition(0, AuthorSphere.gameObject.transform.position);
                lineRenderer.SetPosition(1, FileNode.gameObject.GetRoofCenter());
            }
        }

        /// <summary>
        /// Updates the visibility of the associated line renderer based on the number of authors and the visibility
        /// strategy of the containing city.
        /// </summary>
        /// <remarks>This method enables the line renderer if the containing city is a <see
        /// cref="BranchCity"/>  with a visibility strategy of <see
        /// cref="ShowAuthorEdgeStrategy.OnHoverOrWithMultipleAuthors"/>, and the number of authors meets or
        /// exceeds the city's author threshold.</remarks>
        /// <param name="numberOfAuthors">The number of authors associated with the current object.</param>
        /// <returns>True if the visibility of the line renderer was changed; otherwise, false.</returns>
        internal bool UpdateVisibility(int numberOfAuthors)
        {
            if (City != null
                && City.ShowAuthorEdges == ShowAuthorEdgeStrategy.OnHoverOrWithMultipleAuthors)
            {
                bool isLineVisible = lineRenderer.enabled;
                lineRenderer.enabled = numberOfAuthors >= City.AuthorThreshold;
                return isLineVisible != lineRenderer.enabled;
            }
            return false;
        }
    }
}

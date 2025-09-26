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
            ShowOrHide(isHovered: true);
        }

        /// <summary>
        /// Sets the visibility of the edge.
        /// </summary>
        /// <param name="isHovered">whether any end of the edge (author or file node) is currently hovered</param>
        internal void ShowOrHide(bool isHovered)
        {
            if (gameObject.ContainingCity() is BranchCity branchCity)
            {
                bool show = branchCity.ShowAuthorEdgesStrategy switch
                {
                    ShowAuthorEdgeStrategy.ShowAlways => true,
                    ShowAuthorEdgeStrategy.ShowOnHoverOnly => isHovered,
                    ShowAuthorEdgeStrategy.ShowOnHoverOrWithMultipleAuthors => isHovered
                                                                               || FileNode.Edges.Count >= branchCity.AuthorThreshold,
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
        internal void Update()
        {
            if (TryGetComponent(out LineRenderer lineRenderer))
            {
                lineRenderer.SetPosition(0, AuthorSphere.gameObject.transform.position);
                lineRenderer.SetPosition(1, FileNode.gameObject.GetRoofCenter());
            }
        }
    }
}

using SEE.Game.Drawable.ActionHelpers;
using SEE.Game.Drawable.ValueHolders;
using SEE.GO;
using SEE.UI.Menu.Drawable.Shapes;
using SEE.UI.Notification;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions.Drawable.DrawShapes
{
    /// <summary>
    /// Manages the interaction state of shape previews.
    /// </summary>
    internal sealed class DrawShapePreviewController
    {
        /// <summary>
        /// Whether a shape preview is currently active.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Whether the preview position is fixed.
        /// </summary>
        public bool IsFixed { get; private set; }

        /// <summary>
        /// The fixed preview position.
        /// </summary>
        public Vector3 FixedPosition { get; private set; }

        /// <summary>
        /// Whether an active or fixed preview currently exists.
        /// </summary>
        public bool IsActiveOrFixed => IsActive || IsFixed;

        /// <summary>
        /// Determines whether the current preview must be removed after
        /// changing the selected shape type.
        /// </summary>
        /// <param name="drawing">Whether a shape is currently being drawn.</param>
        /// <returns>Whether the current preview must be removed.</returns>
        public bool ShouldDisable(bool drawing)
        {
            return ShapeMenu.GetSelectedShape() == ShapePointsCalculator.Shape.Line
                    && IsActiveOrFixed
                || drawing
                    && ShapeMenu.GetSelectedShape() != ShapePointsCalculator.Shape.Line;
        }

        /// <summary>
        /// Gets the fixed preview position if the fixed preview should be updated.
        /// </summary>
        /// <param name="drawing">Whether a shape is currently being drawn.</param>
        /// <param name="position">The fixed preview position.</param>
        /// <returns>Whether the fixed preview should be updated.</returns>
        public bool TryGetFixedPosition(bool drawing, out Vector3 position)
        {
            position = FixedPosition;

            if (!drawing
                && !SEEInput.LeftMouseDown()
                && IsFixed
                && ShapeMenu.GetSelectedShape() != ShapePointsCalculator.Shape.Line)
            {
                IsActive = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the current surface hit if an unfixed preview should be updated.
        /// </summary>
        /// <param name="drawing">Whether a shape is currently being drawn.</param>
        /// <param name="raycastHit">The current surface hit.</param>
        /// <returns>Whether an unfixed preview should be updated.</returns>
        public bool TryGetUnfixedHit(bool drawing, out RaycastHit raycastHit)
        {
            raycastHit = default;

            if (!drawing
                && !SEEInput.LeftMouseInteraction()
                && !IsFixed
                && Selector.SelectQueryHasOrIsSurfaceWithoutMouse(out raycastHit)
                && ShapeMenu.GetSelectedShape() != ShapePointsCalculator.Shape.Line)
            {
                IsActive = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Fixes the current preview position if requested by the user.
        /// </summary>
        /// <param name="raycastHit">The surface hit used as fixed position.</param>
        /// <returns>Whether the preview position was fixed.</returns>
        public bool TryFixPosition(out RaycastHit raycastHit)
        {
            raycastHit = default;

            if (Selector.SelectQueryHasOrIsSurfaceWithoutMouse(out raycastHit)
                && SEEInput.MouseDown(MouseButton.Middle)
                && !Input.GetKey(KeyCode.LeftControl)
                && ShapeMenu.GetSelectedShape() != ShapePointsCalculator.Shape.Line)
            {
                IsFixed = true;
                FixedPosition = raycastHit.point;

                ShowNotification.Info(
                    "Fix position set.",
                    "The fixed position for the shape preview has been set.");

                return true;
            }

            return false;
        }

        /// <summary>
        /// Releases the fixed preview position if requested by the user.
        /// </summary>
        /// <returns>Whether the fixed preview position was released.</returns>
        public bool TryReleaseFixedPosition()
        {
            if (SEEInput.MouseDown(MouseButton.Middle)
                && IsFixed
                && Input.GetKey(KeyCode.LeftControl)
                && ShapeMenu.GetSelectedShape() != ShapePointsCalculator.Shape.Line)
            {
                IsFixed = false;
                FixedPosition = Vector3.zero;

                ShowNotification.Info(
                    "Fix position released.",
                    "The fixed position for the shape preview was released.");

                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates the associated page of the current preview and ensures
        /// that the preview is visible on the active page.
        /// </summary>
        /// <param name="shape">The current preview shape.</param>
        /// <param name="surface">The drawable surface containing the preview.</param>
        public void UpdateAssociatedPage(GameObject shape, GameObject surface)
        {
            if (shape == null || surface == null)
            {
                return;
            }

            int currentPage = surface.GetComponent<DrawableHolder>().CurrentPage;

            foreach (AssociatedPageHolder holder
                     in shape.GetComponentsInChildren<AssociatedPageHolder>(true))
            {
                holder.AssociatedPage = currentPage;
            }

            shape.SetActive(true);
        }

        /// <summary>
        /// Resets the preview state.
        /// </summary>
        public void Reset()
        {
            IsActive = false;
            IsFixed = false;
            FixedPosition = Vector3.zero;
        }
    }
}

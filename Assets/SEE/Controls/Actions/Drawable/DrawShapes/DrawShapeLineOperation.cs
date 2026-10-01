using SEE.Game.Drawable;
using SEE.Game.Drawable.ActionHelpers;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.Line;
using SEE.GO;
using SEE.Net.Actions.Drawable;
using SEE.UI.Menu.Drawable.Line;
using SEE.UI.Menu.Drawable.Shapes;
using SEE.UI.Notification;
using SEE.Utils;
using System;
using System.Linq;
using UnityEngine;

namespace SEE.Controls.Actions.Drawable.DrawShapes
{
    /// <summary>
    /// Handles the interactive drawing workflow of a line shape.
    /// This includes starting the line, previewing and adding points,
    /// partial undo, cancellation, and finalizing its geometry.
    /// </summary>
    internal sealed class DrawShapeLineOperation
    {
        /// <summary>
        /// The committed positions of the currently drawn line.
        /// </summary>
        private Vector3[] positions = new Vector3[1];

        /// <summary>
        /// The positions currently shown by the line preview.
        /// This may include a temporary mouse-following end point.
        /// </summary>
        private Vector3[] previewPositions;

        /// <summary>
        /// The fill-out color of the currently drawn line.
        /// </summary>
        private Color? fillOutColor;

        /// <summary>
        /// Whether finishing the line was requested through the shape menu.
        /// </summary>
        private bool finishRequested;

        /// <summary>
        /// Whether the line menu must be refreshed once the line contains enough points
        /// to support fill-out editing.
        /// </summary>
        private bool needsEditModeRefresh;

        /// <summary>
        /// Whether a line shape is currently being drawn.
        /// </summary>
        internal bool IsDrawing { get; private set; }

        /// <summary>
        /// The positions currently shown by the line preview.
        /// </summary>
        internal Vector3[] PreviewPositions => previewPositions;

        /// <summary>
        /// The current fill-out color of the line.
        /// </summary>
        internal Color? FillOutColor => fillOutColor;

        /// <summary>
        /// Starts drawing a line shape at the given world position.
        /// </summary>
        /// <param name="surface">The drawable surface on which the line is drawn.</param>
        /// <param name="worldPosition">The selected first position in world space.</param>
        /// <returns>The newly created line object.</returns>
        internal GameObject Start(GameObject surface, Vector3 worldPosition)
        {
            positions = new Vector3[1];
            positions[0] = worldPosition;

            GameObject shape = GameLineDrawer.StartDrawing(
                surface,
                positions,
                ValueHolder.CurrentColorKind,
                ValueHolder.CurrentPrimaryColor,
                ValueHolder.CurrentSecondaryColor,
                ValueHolder.CurrentThickness,
                ValueHolder.CurrentLineKind,
                ValueHolder.CurrentTiling);

            positions[0] =
                shape.transform.InverseTransformPoint(positions[0])
                - ValueHolder.DistanceToDrawable;

            previewPositions = positions;

            LineConf configuration = LineConf.GetLine(shape);
            configuration.RendererPositions = positions;

            new DrawNetAction(
                surface.name,
                GameFinder.GetDrawableSurfaceParentName(surface),
                configuration).Execute();

            fillOutColor = LineMenu.GetFillOutColorForDrawing();
            finishRequested = false;
            needsEditModeRefresh = false;
            IsDrawing = true;

            return shape;
        }

        /// <summary>
        /// Requests finishing the current line through the shape menu.
        /// The request is accepted only after at least two points have been placed.
        /// </summary>
        internal void RequestFinish()
        {
            if (IsDrawing && positions.Length > 1)
            {
                finishRequested = true;
            }
        }

        /// <summary>
        /// Returns whether finishing through the shape menu was requested and consumes
        /// the request.
        /// </summary>
        /// <returns>Whether the current line should be finished.</returns>
        internal bool ConsumeFinishRequest()
        {
            if (!finishRequested)
            {
                return false;
            }

            finishRequested = false;
            return true;
        }

        /// <summary>
        /// Determines whether the line menu must be refreshed later because fill-out
        /// editing is not yet available for the current number of points.
        /// </summary>
        /// <param name="shape">The currently drawn line.</param>
        internal void PrepareEditModeRefresh(GameObject shape)
        {
            needsEditModeRefresh =
                IsDrawing
                && fillOutColor != null
                && shape != null
                && GameLineGeometry.DifferentPositionCounter(shape) < 3;
        }

        /// <summary>
        /// Determines whether the line menu can now be refreshed to enable fill-out editing.
        /// </summary>
        /// <param name="shape">The currently drawn line.</param>
        /// <returns>Whether the line menu should be refreshed.</returns>
        internal bool ShouldRefreshEditMode(GameObject shape)
        {
            return needsEditModeRefresh
                   && shape != null
                   && GameLineGeometry.DifferentPositionCounter(shape) > 2;
        }

        /// <summary>
        /// Marks the pending edit-mode refresh as completed.
        /// </summary>
        internal void AcknowledgeEditModeRefresh()
        {
            needsEditModeRefresh = false;
        }

        /// <summary>
        /// Updates the temporary end point of the currently drawn line.
        /// If the pointer leaves the original drawable surface, the temporary
        /// preview is restored to the last committed line state.
        /// </summary>
        /// <param name="shape">The currently drawn line.</param>
        /// <param name="surface">The drawable surface containing the line.</param>
        /// <param name="lineCapController">
        /// The controller used to update the line-cap preview.
        /// </param>
        internal void UpdatePreview(GameObject shape, GameObject surface,
            DrawShapeLineCapController lineCapController)
        {
            if (!IsDrawing
                || SEEInput.LeftMouseInteraction()
                || ShapeMenu.GetSelectedShape() != ShapePointsCalculator.Shape.Line
                || !Selector.SelectQueryHasOrIsSurfaceWithoutMouse(out RaycastHit raycastHit))
            {
                return;
            }

            if (!Queries.SameDrawableSurface(surface, raycastHit.collider.gameObject))
            {
                RestoreCommittedPreview(shape, surface, lineCapController);
                return;
            }

            Vector3 newPosition =
                shape.transform.InverseTransformPoint(raycastHit.point)
                - ValueHolder.DistanceToDrawable;

            Vector3[] newPositions = new Vector3[positions.Length + 1];
            Array.Copy(
                sourceArray: positions,
                destinationArray: newPositions,
                length: positions.Length);

            newPosition.z = 0;
            newPositions[^1] = newPosition;
            previewPositions = newPositions;

            if (GameLineGeometry.DifferentPositionCounter(newPositions) > 2)
            {
                fillOutColor ??= LineConf.GetFillOutColor(LineConf.GetLine(shape));

                GameLineDrawer.Drawing(shape, newPositions, fillOutColor);
                lineCapController.ApplyPreview(shape, newPositions, fillOutColor);

                new DrawNetAction(
                    surface.name,
                    GameFinder.GetDrawableSurfaceParentName(surface),
                    LineConf.GetLine(shape)).Execute();

                if (fillOutColor != null)
                {
                    RegisterFillOutCallbacks(shape, surface);

                    new DrawingFillOutNetAction(
                        surface.name,
                        GameFinder.GetDrawableSurfaceParentName(surface),
                        shape.name,
                        fillOutColor.Value).Execute();
                }
            }
            else
            {
                GameLineDrawer.Drawing(shape, newPositions);
                lineCapController.ApplyPreview(shape, newPositions, fillOutColor);

                new DrawNetAction(
                    surface.name,
                    GameFinder.GetDrawableSurfaceParentName(surface),
                    LineConf.GetLine(shape)).Execute();
            }
        }

        /// <summary>
        /// Restores the visible line preview to the last committed positions.
        /// This removes a temporary point when the pointer leaves the drawable
        /// surface on which the line drawing was started.
        /// </summary>
        /// <param name="shape">The currently drawn line.</param>
        /// <param name="surface">The drawable surface containing the line.</param>
        /// <param name="lineCapController">
        /// The controller used to restore the line-cap preview.
        /// </param>
        private void RestoreCommittedPreview(GameObject shape, GameObject surface,
            DrawShapeLineCapController lineCapController)
        {
            if (shape == null
                || surface == null
                || object.ReferenceEquals(previewPositions, positions))
            {
                return;
            }

            previewPositions = positions;

            if (positions.Length > 2)
            {
                fillOutColor ??= LineConf.GetFillOutColor(LineConf.GetLine(shape));
                GameLineDrawer.Drawing(shape, positions, fillOutColor);
            }
            else
            {
                GameLineDrawer.Drawing(shape, positions);
            }

            if (positions.Length > 1)
            {
                lineCapController.ApplyPreview(shape, positions, fillOutColor);
            }
            else
            {
                GameLineCapRenderer.RemoveLineCaps(shape);
            }

            new DrawNetAction(
                surface.name,
                GameFinder.GetDrawableSurfaceParentName(surface),
                LineConf.GetLine(shape)).Execute();
        }

        /// <summary>
        /// Adds a committed point to the currently drawn line if requested by the user.
        /// </summary>
        /// <param name="shape">The currently drawn line.</param>
        /// <param name="surface">The drawable surface containing the line.</param>
        /// <param name="lineCapController">
        /// The controller used to update the line-cap preview.
        /// </param>
        internal void AddPoint(GameObject shape, GameObject surface,
            DrawShapeLineCapController lineCapController)
        {
            if (!SEEInput.LeftMouseDown()
                || Input.GetKey(KeyCode.LeftControl)
                || Input.GetKey(KeyCode.LeftShift)
                || !IsDrawing
                || ShapeMenu.GetSelectedShape() != ShapePointsCalculator.Shape.Line
                || !Selector.SelectQueryHasOrIsSurfaceWithoutMouse(out RaycastHit raycastHit)
                || !Queries.SameDrawableSurface(surface, raycastHit.collider.gameObject))
            {
                return;
            }

            Vector3 newPosition =
                shape.transform.InverseTransformPoint(raycastHit.point)
                - ValueHolder.DistanceToDrawable;

            if (newPosition == positions.Last())
            {
                return;
            }

            Vector3[] newPositions = new Vector3[positions.Length + 1];
            Array.Copy(
                sourceArray: positions,
                destinationArray: newPositions,
                length: positions.Length);

            newPositions[^1] = newPosition;
            positions = newPositions;
            previewPositions = newPositions;

            if (positions.Length > 2)
            {
                fillOutColor ??= LineConf.GetFillOutColor(LineConf.GetLine(shape));

                GameLineDrawer.Drawing(shape, positions, fillOutColor);
                lineCapController.ApplyPreview(shape, positions, fillOutColor);

                new DrawNetAction(
                    surface.name,
                    GameFinder.GetDrawableSurfaceParentName(surface),
                    LineConf.GetLine(shape)).Execute();

                if (fillOutColor != null)
                {
                    RegisterFillOutCallbacks(shape, surface);

                    new DrawingFillOutNetAction(
                        surface.name,
                        GameFinder.GetDrawableSurfaceParentName(surface),
                        shape.name,
                        fillOutColor.Value).Execute();
                }
            }
            else
            {
                GameLineDrawer.Drawing(shape, positions);
                lineCapController.ApplyPreview(shape, positions, fillOutColor);

                new DrawNetAction(
                    surface.name,
                    GameFinder.GetDrawableSurfaceParentName(surface),
                    LineConf.GetLine(shape)).Execute();
            }
        }

        /// <summary>
        /// Adds a final point if finishing with Ctrl and the left mouse button was requested.
        /// </summary>
        /// <param name="shape">The currently drawn line.</param>
        /// <param name="surface">The drawable surface containing the line.</param>
        /// <returns>Whether a final point was added and the line should now be finished.</returns>
        internal bool TryAddFinalPoint(GameObject shape, GameObject surface)
        {
            if (!SEEInput.MouseUp(MouseButton.Left)
                || !Input.GetKey(KeyCode.LeftControl)
                || !IsDrawing
                || positions.Length == 0
                || ShapeMenu.GetSelectedShape() != ShapePointsCalculator.Shape.Line
                || !Selector.SelectQueryHasOrIsSurfaceWithoutMouse(out RaycastHit hit)
                || !Queries.SameDrawableSurface(surface, hit.collider.gameObject))
            {
                return false;
            }

            Vector3 newPosition =
                shape.transform.InverseTransformPoint(hit.point)
                - ValueHolder.DistanceToDrawable;

            Vector3[] newPositions = new Vector3[positions.Length + 1];
            Array.Copy(
                sourceArray: positions,
                destinationArray: newPositions,
                length: positions.Length);

            newPositions[^1] = newPosition;
            positions = newPositions;
            previewPositions = newPositions;

            GameLineDrawer.Drawing(shape, positions);

            new DrawingNetAction(
                surface.name,
                GameFinder.GetDrawableSurfaceParentName(surface),
                shape.name,
                newPosition,
                newPositions.Length - 1).Execute();

            return true;
        }

        /// <summary>
        /// Determines whether the current line should be finished without adding another point.
        /// </summary>
        /// <returns>Whether the line should be finished.</returns>
        internal bool ShouldFinishWithoutFinalPoint()
        {
            return SEEInput.MouseUp(MouseButton.Middle)
                   && IsDrawing
                   && positions.Length > 1
                   && ShapeMenu.GetSelectedShape() == ShapePointsCalculator.Shape.Line;
        }

        /// <summary>
        /// Finalizes the geometry of the currently drawn line.
        /// </summary>
        /// <param name="shape">The currently drawn line.</param>
        /// <param name="surface">The drawable surface containing the line.</param>
        /// <param name="lineCapController">
        /// The controller used to apply the final line caps.
        /// </param>
        /// <returns>The finalized line object and its configuration.</returns>
        internal (GameObject Shape, LineConf Configuration) Finish(
            GameObject shape,
            GameObject surface,
            DrawShapeLineCapController lineCapController)
        {
            GameLineDrawer.Drawing(shape, positions);
            shape.GetComponent<LineRenderer>().loop = ShapeMenu.GetBoolValue();

            GameObject finishedShape =
                GameLineGeometry.SetPivot(shape, fillOutColor);

            LineConf configuration = lineCapController.ApplyFinal(
                finishedShape,
                surface,
                LineConf.GetLine(finishedShape));

            Reset();

            return (finishedShape, configuration);
        }

        /// <summary>
        /// Cancels the currently drawn line when requested by the user.
        /// </summary>
        /// <param name="shape">The currently drawn line.</param>
        /// <param name="surface">The drawable surface containing the line.</param>
        /// <returns>Whether the line drawing was canceled.</returns>
        internal bool TryCancel(GameObject shape, GameObject surface)
        {
            if (!IsDrawing || !SEEInput.Cancel())
            {
                return false;
            }

            ShowNotification.Info(
                "Line-Shape drawing canceled.",
                "The drawing of the shape art line has been canceled.");

            new EraseNetAction(
                surface.name,
                GameFinder.GetDrawableSurfaceParentName(surface),
                shape.name).Execute();

            Destroyer.Destroy(shape);
            ShapeMenu.DisablePartUndo();

            Reset();

            if (LineMenu.Instance.IsInEditMode())
            {
                LineMenu.Instance.Disable();
                ShapeMenu.OpenLineMenuInCorrectMode();
            }

            return true;
        }

        /// <summary>
        /// Removes the most recently committed line point when requested by the user.
        /// If too few points remain, the current line is canceled instead.
        /// </summary>
        /// <param name="shape">The currently drawn line.</param>
        /// <param name="surface">The drawable surface containing the line.</param>
        /// <param name="ignorePartUndoButton">
        /// Whether the keyboard input should be ignored because this method was invoked
        /// through the part-undo button.
        /// </param>
        /// <returns>Whether removing the point canceled and destroyed the line.</returns>
        internal bool RemoveLastPoint(GameObject shape, GameObject surface,
            bool ignorePartUndoButton = false)
        {
            if (!IsDrawing || (!SEEInput.PartUndo() && !ignorePartUndoButton))
            {
                return false;
            }

            if (shape.GetComponent<LineRenderer>().positionCount >= 3)
            {
                ShowNotification.Info(
                    "Last point removed.",
                    "The last placed point of the line has been removed.");

                LineRenderer renderer = shape.GetComponent<LineRenderer>();
                renderer.positionCount -= 2;

                positions = positions
                    .ToList()
                    .GetRange(0, positions.Length - 1)
                    .ToArray();

                previewPositions = positions;
                fillOutColor ??= LineConf.GetFillOutColor(LineConf.GetLine(shape));

                if (positions.Length > 1)
                {
                    GameLineDrawer.Drawing(shape, positions, fillOutColor);
                }
                else
                {
                    if (fillOutColor != null)
                    {
                        UnityEngine.Object.DestroyImmediate(
                            shape.FindDescendant(ValueHolder.FillOut));

                        new DeleteFillOutNetAction(
                            surface.name,
                            GameFinder.GetDrawableSurfaceParentName(surface),
                            shape.name).Execute();

                        LineMenu.AssignFillOutForEditing(null, null, () => { });
                    }

                    GameLineDrawer.Drawing(shape, positions);
                }

                new DrawNetAction(
                    surface.name,
                    GameFinder.GetDrawableSurfaceParentName(surface),
                    LineConf.GetLine(shape)).Execute();

                return false;
            }

            ShowNotification.Info(
                "Line-shape drawing canceled.",
                "The drawing of the shape-art line has been canceled.");

            new EraseNetAction(
                surface.name,
                GameFinder.GetDrawableSurfaceParentName(surface),
                shape.name).Execute();

            Destroyer.Destroy(shape);
            ShapeMenu.DisablePartUndo();
            Reset();

            return true;
        }

        /// <summary>
        /// Registers the fill-out editing callbacks for the currently drawn line.
        /// </summary>
        /// <param name="shape">The currently drawn line.</param>
        /// <param name="surface">The drawable surface containing the line.</param>
        internal void RegisterFillOutCallbacks(GameObject shape, GameObject surface)
        {
            if (!IsDrawing || shape == null || fillOutColor == null)
            {
                return;
            }

            LineMenu.AssignFillOutForEditing(
                fillOutColor,
                color =>
                {
                    fillOutColor = color;
                    ValueHolder.CurrentFillOutStatus = true;
                    ValueHolder.CurrentTertiaryColor = color;

                    GameLineFillOut.ChangeFillOutColor(shape, color);

                    new EditLineFillOutColorNetAction(
                        surface.name,
                        GameFinder.GetDrawableSurfaceParentName(surface),
                        shape.name,
                        color).Execute();
                },
                () =>
                {
                    fillOutColor = null;
                    ValueHolder.CurrentFillOutStatus = false;
                });
        }

        /// <summary>
        /// Resets all transient state of the line drawing operation.
        /// </summary>
        internal void Reset()
        {
            positions = new Vector3[1];
            previewPositions = null;
            fillOutColor = null;
            finishRequested = false;
            needsEditModeRefresh = false;
            IsDrawing = false;
        }
    }
}

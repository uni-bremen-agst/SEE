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
using SEE.Utils.History;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Controls.Actions.Drawable.DrawShapes
{
    /// <summary>
    /// Allows the user to draw a shape.
    /// </summary>
    public class DrawShapesAction : DrawableAction
    {
        #region Fields
        /// <summary>
        /// The object holding the line renderer.
        /// </summary>
        private GameObject shape;

        /// <summary>
        /// Gets or sets the currently drawn or previewed shape
        /// and synchronizes it with the shape menu.
        /// </summary>
        private GameObject Shape
        {
            get
            {
                return shape;
            }
            set
            {
                shape = value;
                ShapeMenu.SetCurrentPreviewShape(value);
            }
        }

        /// <summary>
        /// The positions of the line in local space.
        /// </summary>
        private Vector3[] positions = new Vector3[1];

        /// <summary>
        /// Saves all the information needed to revert or repeat this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// This struct can store all the information needed to revert or repeat a <see cref="DrawShapesAction"/>.
        /// </summary>
        private readonly struct Memento
        {
            /// <summary>
            /// The drawable surface where the shape is displayed.
            /// </summary>
            public readonly DrawableConfig Surface;
            /// <summary>
            /// The configuration of the shape.
            /// </summary>
            public readonly LineConf Shape;

            /// <summary>
            /// The constructor, which simply assigns its parameters to the fields in this class.
            /// </summary>
            /// <param name="surface">The drawable surface where the shape is displayed.</param>
            /// <param name="shape">The configuration of the shape.</param>
            public Memento(GameObject surface, LineConf shape)
            {
                Surface = DrawableConfigManager.GetDrawableConfig(surface);
                Shape = shape;
            }
        }

        /// <summary>
        /// True if the action is drawing.
        /// Also necessary to identify whether the line shape was successfully drawn.
        /// </summary>
        private bool drawing = false;

        /// <summary>
        /// True if the user finished the line shape drawing via menu.
        /// </summary>
        private bool finishDrawingViaButton = false;

        /// <summary>
        /// Manages line-cap configuration and state for the current preview.
        /// </summary>
        private readonly DrawShapeLineCapController lineCapController = new();

        /// <summary>
        /// Manages the interaction state of shape previews.
        /// </summary>
        private readonly DrawShapePreviewController previewController = new();

        /// <summary>
        /// Status if the line menu was changes to edit mode.
        /// </summary>
        private bool editMode = false;

        /// <summary>
        /// Status indicating whether the edit mode has been fully initialized.
        /// When a <see cref="ShapePointsCalculator.Shape.Line"> is being drawn and fill out is enabled,
        /// the fill out status will not be activated if there are fewer than three points, as the fill out functionality
        /// is only allowed with three points or more.
        /// When this status is set, the edit menu must be re-initialized once three points are reached.
        /// </summary>
        private bool needRefreshEditMode = false;

        /// <summary>
        /// Status and color if a shape has activates the fill out option.
        /// </summary>
        private Color? shapeFillOut = null;

        /// <summary>
        /// The positions currently used for the visible preview.
        /// For line shapes this may include the temporary mouse-following end point
        /// that has not been committed yet.
        /// </summary>
        private Vector3[] currentPreviewPositions;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Registers the finish-button callback for the current action instance.
        /// </summary>
        public override void Awake()
        {
            base.Awake();
            ShapeMenu.SetCurrentPreviewShape(null);
            ShapeMenu.AssignFinishButton(() =>
            {
                if (drawing && positions.Length > 1
                    && ShapeMenu.GetSelectedShape() == ShapePointsCalculator.Shape.Line)
                {
                    finishDrawingViaButton = true;
                }
            });
        }

        /// <summary>
        /// Stops the action, resets action-specific menu state and
        /// destroys an unfinished shape preview.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            ShapeMenu.DisablePartUndo();

            if (drawing && Shape != null
                || Shape != null && previewController.IsActiveOrFixed)
            {
                new EraseNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), Shape.name).Execute();
                Destroyer.Destroy(Shape);
            }
            Shape = null;
            ResetPreviewState();
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.DrawShapes"/>.
        /// Specifically: Allows the user to draw a shape.
        /// For all shapes except Line, a single click on the drawable is sufficient to draw the desired shape.
        /// Simply enter the desired values in the Shape Menu.
        /// For the Line shape type, multiple clicks (one for each point) are required.
        /// </summary>
        /// <returns>Whether this action is finished.</returns>
        public override bool Update()
        {
            /// Offers a preview for a shape representation on a fixed chosen position.
            if (previewController.TryGetFixedPosition(drawing, out Vector3 fixedPreviewPosition))
            {
                ShapePreview(fixedPreviewPosition);
            }

            /// Disables the preview if the user selects <see cref="ShapePointsCalculator.Shape.Line"/>.
            DisableShapePreview();

            if (Shape != null && LineMenu.Instance.IsInDrawingMode() && !editMode)
            {
                editMode = true;

                if (ShapeMenu.GetSelectedShape() == ShapePointsCalculator.Shape.Line
                    && LineMenu.GetFillOutColorForDrawing() != null
                    && GameLineGeometry.DifferentPositionCounter(Shape) < 3)
                {
                    needRefreshEditMode = true;
                }

                ShapeMenu.OpenLineMenuInCorrectMode();
                RegisterLinePreviewFillOutCallbacks();
            }
            else if (needRefreshEditMode && GameLineGeometry.DifferentPositionCounter(Shape) > 2)
            {
                needRefreshEditMode = false;
                ShapeMenu.OpenLineMenuInCorrectMode();
                RegisterLinePreviewFillOutCallbacks();
            }
            else if (Shape == null && LineMenu.Instance.IsInEditMode() && !editMode)
            {
                ShapeMenu.OpenLineMenuInCorrectMode();
            }

            lineCapController.RefreshPreviewIfMenuChanged(
                Shape,
                Surface,
                currentPreviewPositions,
                shapeFillOut,
                drawing || previewController.IsActiveOrFixed);

            previewController.UpdateAssociatedPage(Shape, Surface);

            if (!Raycasting.IsMouseOverGUI())
            {
                /// Offers a preview for shape representation.
                if (previewController.TryGetUnfixedHit(drawing, out RaycastHit previewHit))
                {
                    Surface = GameFinder.GetDrawableSurface(previewHit.collider.gameObject);
                    ShapePreview(previewHit.point);
                }

                /// Marks a position as fixed for the shape preview.
                if (previewController.TryFixPosition(out RaycastHit fixedPreviewHit))
                {
                    Surface = GameFinder.GetDrawableSurface(fixedPreviewHit.collider.gameObject);
                }

                /// Releases the fixed position.
                if (previewController.TryReleaseFixedPosition())
                {
                    Surface = null;
                }

                /// Block for initiating shape drawing.
                /// All shapes, except for straight lines, are also completed within this block.
                if (Selector.SelectQueryHasOrIsDrawableSurface(out RaycastHit raycastHit, true, true)
                    && !drawing)
                {
                    return ShapeDrawing(raycastHit);
                }

                /// This block provides a line preview to select the desired position of the next line point.
                LineShapePreview();

                /// With this block, the user can add a new point to the line.
                AddLineShapePoint();

                /// With left shift key can the loop option of the shape menu be toggled.
                if (Input.GetKeyDown(KeyCode.LeftShift))
                {
                    ShapeMenu.SetBoolValue(!ShapeMenu.GetBoolValue());
                }

                /// Block for successfully completing the line.
                /// It adds a final point to the line.
                /// It requires a left-click with the left Ctrl key held down.
                if (SEEInput.MouseUp(MouseButton.Left)
                    && Input.GetKey(KeyCode.LeftControl)
                    && drawing
                    && positions.Length > 0
                    && ShapeMenu.GetSelectedShape() == ShapePointsCalculator.Shape.Line
                    && Selector.SelectQueryHasOrIsSurfaceWithoutMouse(out RaycastHit hit))
                {
                    Vector3 newPosition = Shape.transform.InverseTransformPoint(hit.point) - ValueHolder.DistanceToDrawable;
                    Vector3[] newPositions = new Vector3[positions.Length + 1];
                    Array.Copy(sourceArray: positions, destinationArray: newPositions, length: positions.Length);
                    newPositions[newPositions.Length - 1] = newPosition;
                    positions = newPositions;

                    GameLineDrawer.Drawing(Shape, positions);
                    new DrawingNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface),
                                         Shape.name, newPosition, newPositions.Length - 1).Execute();
                    FinishDrawing();
                    return true;
                }

                /// Block for successfully completing the line without adding a new point.
                /// It requires a wheel-click.
                if (SEEInput.MouseUp(MouseButton.Middle)
                    && drawing
                    && positions.Length > 1
                    && ShapeMenu.GetSelectedShape() == ShapePointsCalculator.Shape.Line)
                {
                    FinishDrawing();
                    return true;
                }
            }
            /// This block is outside the !Raycasting.IsMouseOverGUI check to allow
            /// the immediate detection of a click on the Finish button of the menu,
            /// even if the mouse cursor is still over the GUI.
            if (finishDrawingViaButton)
            {
                FinishDrawing();
                return true;
            }

            /// Block for canceling the drawing of a line shape.
            CancelDrawing();

            /// Block for removing the last point during the drawing of a line shape.
            RemoveLastPoint();

            return false;
        }
        #endregion

        #region Drawing State
        /// <summary>
        /// Provides the option to cancel drawing a line shape with the escape button.
        /// </summary>
        private void CancelDrawing()
        {
            if (drawing && SEEInput.Cancel())
            {
                ShowNotification.Info("Line-Shape drawing canceled.",
                    "The drawing of the shape art line has been canceled.");
                new EraseNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), Shape.name).Execute();
                Destroyer.Destroy(Shape);
                ShapeMenu.DisablePartUndo();
                positions = new Vector3[1];
                ResetPreviewState();
                drawing = false;
                Shape = null;
                editMode = false;
                shapeFillOut = null;
                if (LineMenu.Instance.IsInEditMode())
                {
                    LineMenu.Instance.Disable();
                    ShapeMenu.OpenLineMenuInCorrectMode();
                }
            }
        }

        /// <summary>
        /// Finish the drawing of the line shape.
        /// It must be a separate method as it can be called from two different points.
        /// </summary>
        private void FinishDrawing()
        {
            GameLineDrawer.Drawing(Shape, positions);
            Shape.GetComponent<LineRenderer>().loop = ShapeMenu.GetBoolValue();
            Shape = GameLineGeometry.SetPivot(Shape, shapeFillOut);
            LineConf finalShape = lineCapController.ApplyFinal(Shape, Surface, LineConf.GetLine(Shape));
            memento = new Memento(Surface, finalShape);
            new DrawNetAction(memento.Surface.ID, memento.Surface.ParentID, finalShape).Execute();
            CurrentState = IReversibleAction.Progress.Completed;
            drawing = false;
            ResetPreviewState();
        }

        /// <summary>
        /// Provides the option to remove the last added point.
        /// Press the caps lock key for this action.
        /// If the line does not have enough points to remove, it will be deleted.
        /// </summary>
        /// <param name="ignorePartUndoButton">True if the key input should be ignored.
        /// Will be used for the part undo button of the <see cref="ShapeMenu"/>.</param>
        private void RemoveLastPoint(bool ignorePartUndoButton = false)
        {
            if (drawing && (SEEInput.PartUndo() || ignorePartUndoButton))
            {
                if (Shape.GetComponent<LineRenderer>().positionCount >= 3)
                {
                    ShowNotification.Info("Last point removed.",
                        "The last placed point of the line has been removed.");
                    LineRenderer renderer = Shape.GetComponent<LineRenderer>();
                    renderer.positionCount -= 2;
                    positions = positions.ToList().GetRange(0, positions.Length - 1).ToArray();
                    currentPreviewPositions = positions;
                    shapeFillOut ??= LineConf.GetFillOutColor(LineConf.GetLine(shape));
                    if (positions.Length > 1)
                    {
                        GameLineDrawer.Drawing(Shape, positions, shapeFillOut);
                    }
                    else
                    {
                        if (shapeFillOut != null)
                        {
                            UnityEngine.Object.DestroyImmediate(Shape.FindDescendant(ValueHolder.FillOut));
                            new DeleteFillOutNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), Shape.name).Execute();
                            LineMenu.AssignFillOutForEditing(null, null, () => { });
                        }
                        GameLineDrawer.Drawing(Shape, positions);
                    }
                    new DrawNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), LineConf.GetLine(Shape)).Execute();
                }
                else
                {
                    ShowNotification.Info("Line-shape drawing canceled.",
                        "The drawing of the shape-art line has been canceled.");
                    new EraseNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), Shape.name).Execute();
                    Destroyer.Destroy(Shape);
                    ShapeMenu.DisablePartUndo();
                    positions = new Vector3[1];
                    drawing = false;
                    Shape = null;
                    ResetPreviewState();
                    editMode = false;
                    shapeFillOut = null;
                }
            }
        }
        #endregion

        #region Creation
        /// <summary>
        /// Performs the drawing of shapes.
        /// However, for straight lines, only the drawing is initialized.
        /// To do this, the <see cref="GetSelectedShapePosition(Vector3, Vector3)"/> method is
        /// first called to determine the positions.
        /// Subsequently, for the selected shape (if it is not a line), the <see cref="DrawShape(Vector3)"/> method is called.
        /// </summary>
        /// <param name="raycastHit">The raycast hit of the selection.</param>
        /// <returns>Whatever the shape creation is completed.</returns>
        private bool ShapeDrawing(RaycastHit raycastHit)
        {
            Surface = GameFinder.GetDrawableSurface(raycastHit.collider.gameObject);
            drawing = true;
            Vector3 convertedHitPoint;

            if (!previewController.IsFixed)
            {
                convertedHitPoint = GameLineGeometry.GetConvertedPosition(Surface, raycastHit.point);
                GetSelectedShapePosition(convertedHitPoint, raycastHit.point);
            }
            else
            {
                convertedHitPoint = GameLineGeometry.GetConvertedPosition(
                    Surface,
                    previewController.FixedPosition);
            }

            /// This block draws and completes the action for all shapes except lines.
            if (ShapeMenu.GetSelectedShape() != ShapePointsCalculator.Shape.Line)
            {
                return DrawShape(convertedHitPoint);
            }
            return false;
        }

        /// <summary>
        /// Calculates the points for the selected shape based
        /// on the chosen values in the <see cref="ShapeMenu"/>.
        /// For the Line shape, only the first point is set,
        /// as the others cannot be calculated and must be chosen by the user.
        /// </summary>
        /// <param name="convertedHitPoint">The hit point in local space, depending on the chosen drawable.</param>
        /// <param name="hitpoint">The hit point of the raycast hit.</param>
        private void GetSelectedShapePosition(Vector3 convertedHitPoint, Vector3 hitpoint)
        {
            switch (ShapeMenu.GetSelectedShape())
            {
                case ShapePointsCalculator.Shape.Line:
                    positions[0] = hitpoint;
                    Shape = GameLineDrawer.StartDrawing(Surface, positions, ValueHolder.CurrentColorKind,
                        ValueHolder.CurrentPrimaryColor, ValueHolder.CurrentSecondaryColor,
                        ValueHolder.CurrentThickness, ValueHolder.CurrentLineKind,
                        ValueHolder.CurrentTiling);
                    positions[0] = Shape.transform.InverseTransformPoint(positions[0]) - ValueHolder.DistanceToDrawable;
                    currentPreviewPositions = positions;
                    ShapeMenu.ActivatePartUndo(() => RemoveLastPoint(true));
                    LineConf conf = LineConf.GetLine(Shape);
                    conf.RendererPositions = positions;
                    new DrawNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), conf).Execute();
                    shapeFillOut = LineMenu.GetFillOutColorForDrawing();
                    break;
                case ShapePointsCalculator.Shape.Square:
                    positions = ShapePointsCalculator.Square(convertedHitPoint, ShapeMenu.GetValue1());
                    break;
                case ShapePointsCalculator.Shape.Rectangle:
                    positions = ShapePointsCalculator.Rectangle(convertedHitPoint, ShapeMenu.GetValue1(),
                        ShapeMenu.GetValue2());
                    break;
                case ShapePointsCalculator.Shape.Rhombus:
                    positions = ShapePointsCalculator.Rhombus(convertedHitPoint, ShapeMenu.GetValue1(),
                        ShapeMenu.GetValue2());
                    break;
                case ShapePointsCalculator.Shape.Kite:
                    positions = ShapePointsCalculator.Kite(convertedHitPoint, ShapeMenu.GetValue1(),
                        ShapeMenu.GetValue2(), ShapeMenu.GetValue3());
                    break;
                case ShapePointsCalculator.Shape.Triangle:
                    positions = ShapePointsCalculator.Triangle(convertedHitPoint, ShapeMenu.GetValue1(),
                        ShapeMenu.GetValue2());
                    break;
                case ShapePointsCalculator.Shape.Circle:
                    positions = ShapePointsCalculator.Circle(convertedHitPoint, ShapeMenu.GetValue1());
                    break;
                case ShapePointsCalculator.Shape.Ellipse:
                    positions = ShapePointsCalculator.Ellipse(convertedHitPoint, ShapeMenu.GetValue1(),
                        ShapeMenu.GetValue2());
                    break;
                case ShapePointsCalculator.Shape.Parallelogram:
                    positions = ShapePointsCalculator.Parallelogram(convertedHitPoint, ShapeMenu.GetValue1(),
                        ShapeMenu.GetValue2(), ShapeMenu.GetOffset());
                    break;
                case ShapePointsCalculator.Shape.Trapezoid:
                    positions = ShapePointsCalculator.Trapezoid(convertedHitPoint, ShapeMenu.GetValue1(),
                        ShapeMenu.GetValue2(), ShapeMenu.GetValue3());
                    break;
                case ShapePointsCalculator.Shape.Polygon:
                    positions = ShapePointsCalculator.Polygon(convertedHitPoint, ShapeMenu.GetValue1(),
                        ShapeMenu.GetVertices());
                    break;
                case ShapePointsCalculator.Shape.HalfCircle:
                    positions = ShapePointsCalculator.HalfCircle(convertedHitPoint, ShapeMenu.GetValue1(),
                        ShapeMenu.GetOrientation());
                    break;
                case ShapePointsCalculator.Shape.Arc:
                    positions = ShapePointsCalculator.Arc(convertedHitPoint, ShapeMenu.GetValue1(),
                        ShapeMenu.GetAngle1(), ShapeMenu.GetAngle2(), ShapeMenu.GetVertices());
                    break;
                case ShapePointsCalculator.Shape.UML:
                    GetSelectedUMLShapePosition(convertedHitPoint);
                    break;
            }
        }

        /// <summary>
        /// Calculates the points for the selected UML shape based
        /// on the chosen values in the <see cref="ShapeMenu"/>.
        /// </summary>
        /// <param name="convertedHitPoint">The hit point in local space, depending on the chosen drawable.</param>
        private void GetSelectedUMLShapePosition(Vector3 convertedHitPoint)
        {
            switch (ShapeMenu.GetSelectedUMLShape())
            {
                case UMLShapePointsCalculator.UMLShape.Actor:
                    positions = UMLShapePointsCalculator.Actor(convertedHitPoint, ShapeMenu.GetValue1());
                    break;
                case UMLShapePointsCalculator.UMLShape.Note:
                    positions = UMLShapePointsCalculator.Note(convertedHitPoint, ShapeMenu.GetValue1(),
                        ShapeMenu.GetValue2());
                    break;
                case UMLShapePointsCalculator.UMLShape.Package:
                    positions = UMLShapePointsCalculator.Package(convertedHitPoint,
                        ShapeMenu.GetValue1(), ShapeMenu.GetValue2(), ShapeMenu.GetValue3(), ShapeMenu.GetValue4());
                    break;
                case UMLShapePointsCalculator.UMLShape.ProvideInterf:
                    positions = UMLShapePointsCalculator.ProvideInterface(convertedHitPoint, ShapeMenu.GetValue1(),
                        ShapeMenu.GetOrientation());
                    break;
                case UMLShapePointsCalculator.UMLShape.ReceiveInterf:
                    positions = UMLShapePointsCalculator.ReceiveInterface(convertedHitPoint, ShapeMenu.GetValue1(),
                        ShapeMenu.GetOrientation());
                    break;
                case UMLShapePointsCalculator.UMLShape.SendActivity:
                    positions = UMLShapePointsCalculator.SendActivity(convertedHitPoint, ShapeMenu.GetValue1(),
                        ShapeMenu.GetValue2(), ShapeMenu.GetOrientation());
                    break;
                case UMLShapePointsCalculator.UMLShape.ReceiveActivity:
                    positions = UMLShapePointsCalculator.ReceiveActivity(convertedHitPoint, ShapeMenu.GetValue1(),
                        ShapeMenu.GetValue2(), ShapeMenu.GetOrientation());
                    break;
            }
        }

        /// <summary>
        /// Creates the calculated shape if it has at least three different positions.
        /// This ensures that the Mesh Collider can be created.
        /// Subsequently, the pivot point of the shape is set,
        /// and the action is completed by creating a Memento and setting the progress state to Completed.
        /// If the shape cannot provide three different points, the action is reset.
        /// </summary>
        /// <param name="convertedHitPoint">The hit point in local space, depending on the chosen drawable.</param>
        /// <returns>Whatever the state of the shape creation is completed.</returns>
        private bool DrawShape(Vector3 convertedHitPoint)
        {
            if (GameLineGeometry.DifferentPositionCounter(positions) > 1)
            {
                BlinkEffect.Deactivate(Shape);

                LineConf currentShape = LineConf.GetLine(Shape);
                Color? fillOutColor = LineConf.GetFillOutColor(currentShape);

                // Restore the original, unshortened shape geometry before moving the pivot.
                GameLineDrawer.Drawing(
                    Shape,
                    positions,
                    fillOutColor);

                Shape = GameLineGeometry.SetPivotShape(
                    Shape,
                    convertedHitPoint,
                    fillOutColor,
                    true);

                currentShape = lineCapController.ApplyFinal(
                    Shape,
                    Surface,
                    LineConf.GetLine(Shape));

                previewController.Reset();

                memento = new Memento(Surface, currentShape);

                new DrawNetAction(
                    memento.Surface.ID,
                    memento.Surface.ParentID,
                    currentShape).Execute();

                CurrentState = IReversibleAction.Progress.Completed;
                drawing = false;
                ResetPreviewState();

                return true;
            }
            else
            {
                positions = new Vector3[1];
                ResetPreviewState();
                drawing = false;
                new EraseNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), Shape.name).Execute();
                Destroyer.Destroy(Shape);
                Shape = null;
                editMode = false;
                return false;
            }
        }
        #endregion

        #region Shape Preview
        /// <summary>
        /// Disables the shape preview and deletes the preview.
        /// </summary>
        private void DisableShapePreview()
        {
            if (!previewController.ShouldDisable(drawing))
            {
                return;
            }

            drawing = false;
            editMode = false;
            previewController.Reset();

            new EraseNetAction(
                Surface.name,
                GameFinder.GetDrawableSurfaceParentName(Surface),
                Shape.name).Execute();

            Destroyer.Destroy(Shape);

            positions = new Vector3[1];
            ResetPreviewState();
        }

        /// <summary>
        /// Draws a shape preview.
        /// </summary>
        /// <param name="position">The position where the preview should be drawn.</param>
        private void ShapePreview(Vector3 position)
        {
            Vector3 convertedHitPoint = GameLineGeometry.GetConvertedPosition(Surface, position);
            GetSelectedShapePosition(convertedHitPoint, position);
            currentPreviewPositions = positions;

            if (Shape == null)
            {
                Shape = GameLineDrawer.DrawLine(Surface, "", positions, ValueHolder.CurrentColorKind,
                    ValueHolder.CurrentPrimaryColor, ValueHolder.CurrentSecondaryColor, ValueHolder.CurrentThickness, false,
                    ValueHolder.CurrentLineKind, ValueHolder.CurrentTiling, fillOutColor: LineMenu.GetFillOutColorForDrawing());
                shapeFillOut = LineMenu.GetFillOutColorForDrawing();
                Shape.GetComponent<LineRenderer>().loop = false;
                Shape.AddOrGetComponent<BlinkEffect>();
                lineCapController.ApplyPreview(Shape, positions, shapeFillOut);
                new DrawNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), LineConf.GetLine(Shape)).Execute();
            }
            else
            {
                shapeFillOut ??= LineConf.GetFillOutColor(LineConf.GetLine(Shape));
                RegisterPreviewFillOutCallbacks();
                if (shapeFillOut != null && BlinkEffect.CanFillOutBeAdded(shape))
                {
                    BlinkEffect.AddFillOutToEffect(shape);
                }
                GameLineDrawer.Drawing(Shape, positions, fillOutColor: shapeFillOut);
                lineCapController.ApplyPreview(Shape, positions, shapeFillOut);
                new DrawNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), LineConf.GetLine(Shape)).Execute();
            }
        }
        #endregion

        #region Line Preview
        /// <summary>
        /// This method provides a line preview for the user
        /// to select the desired position of the next line point.
        /// </summary>
        private void LineShapePreview()
        {
            if (drawing && !SEEInput.LeftMouseInteraction()
                && Selector.SelectQueryHasOrIsSurfaceWithoutMouse(out RaycastHit raycastHit)
                && ShapeMenu.GetSelectedShape() == ShapePointsCalculator.Shape.Line
                && Queries.DrawableSurfaceNullOrSame(Surface, raycastHit.collider.gameObject))
            {
                Vector3 newPosition = Shape.transform.InverseTransformPoint(raycastHit.point) - ValueHolder.DistanceToDrawable;
                Vector3[] newPositions = new Vector3[positions.Length + 1];
                Array.Copy(sourceArray: positions, destinationArray: newPositions, length: positions.Length);
                newPosition.z = 0;
                newPositions[^1] = newPosition;
                currentPreviewPositions = newPositions;
                if (GameLineGeometry.DifferentPositionCounter(newPositions) > 2)
                {
                    shapeFillOut ??= LineConf.GetFillOutColor(LineConf.GetLine(Shape));
                    GameLineDrawer.Drawing(Shape, newPositions, shapeFillOut);
                    lineCapController.ApplyPreview(Shape, newPositions, shapeFillOut);
                    new DrawNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface),
                        LineConf.GetLine(Shape)).Execute();
                    if (shapeFillOut != null)
                    {
                        RegisterPreviewFillOutCallbacks();
                        new DrawingFillOutNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), Shape.name,
                            shapeFillOut.Value).Execute();
                    }
                }
                else
                {
                    GameLineDrawer.Drawing(Shape, newPositions);
                    lineCapController.ApplyPreview(Shape, newPositions, shapeFillOut);
                    new DrawNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface),
                        LineConf.GetLine(Shape)).Execute();
                }
            }
        }

        /// <summary>
        /// Provides the function to add a new point in the Line shape.
        /// However, the new point must be different from the previous one.
        /// This requires a left mouse click, with neither the left Shift
        /// nor the left Ctrl key pressed.
        /// </summary>
        private void AddLineShapePoint()
        {
            if (SEEInput.LeftMouseDown() && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift)
                && Selector.SelectQueryHasOrIsSurfaceWithoutMouse(out RaycastHit raycastHit)
                && drawing && ShapeMenu.GetSelectedShape() == ShapePointsCalculator.Shape.Line
                && Queries.DrawableSurfaceNullOrSame(Surface, raycastHit.collider.gameObject))
            {
                Vector3 newPosition = Shape.transform.InverseTransformPoint(raycastHit.point) - ValueHolder.DistanceToDrawable;
                if (newPosition != positions.Last())
                {
                    Vector3[] newPositions = new Vector3[positions.Length + 1];
                    Array.Copy(sourceArray: positions, destinationArray: newPositions, length: positions.Length);
                    newPositions[newPositions.Length - 1] = newPosition;
                    positions = newPositions;
                    currentPreviewPositions = newPositions;

                    if (positions.Length > 2)
                    {
                        shapeFillOut ??= LineConf.GetFillOutColor(LineConf.GetLine(shape));
                        GameLineDrawer.Drawing(Shape, positions, shapeFillOut);
                        lineCapController.ApplyPreview(Shape, positions, shapeFillOut);
                        new DrawNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface),
                            LineConf.GetLine(Shape)).Execute();
                        if (shapeFillOut != null)
                        {
                            RegisterPreviewFillOutCallbacks();
                            new DrawingFillOutNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface),
                                Shape.name, shapeFillOut.Value).Execute();
                        }
                    }
                    else
                    {
                        GameLineDrawer.Drawing(Shape, positions);
                        lineCapController.ApplyPreview(Shape, positions, shapeFillOut);
                        new DrawNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface),
                            LineConf.GetLine(Shape)).Execute();
                    }

                }
            }
        }

        /// <summary>
        /// Resets the cached preview state and prepares the selected line caps
        /// for the next shape.
        /// </summary>
        private void ResetPreviewState()
        {
            currentPreviewPositions = null;
            lineCapController.Reset();
            ShapeMenu.ResetLineCapVisualOverrides();
        }

        /// <summary>
        /// Registers the fill-out callbacks of the currently drawn line at the line menu.
        /// </summary>
        private void RegisterLinePreviewFillOutCallbacks()
        {
            if (Shape == null
                || ShapeMenu.GetSelectedShape() != ShapePointsCalculator.Shape.Line
                || shapeFillOut == null)
            {
                return;
            }

            RegisterPreviewFillOutCallbacks();
        }


        /// <summary>
        /// Registers the fill-out callbacks of the current preview at the line menu.
        /// </summary>
        private void RegisterPreviewFillOutCallbacks()
        {
            if (Shape == null)
            {
                return;
            }

            LineMenu.AssignFillOutForEditing(
                shapeFillOut,
                color =>
                {
                    shapeFillOut = color;
                    ValueHolder.CurrentFillOutStatus = true;
                    ValueHolder.CurrentTertiaryColor = color;

                    GameLineFillOut.ChangeFillOutColor(Shape, color);

                    new EditLineFillOutColorNetAction(
                        Surface.name,
                        GameFinder.GetDrawableSurfaceParentName(Surface),
                        Shape.name,
                        color).Execute();
                },
                () =>
                {
                    shapeFillOut = null;
                    ValueHolder.CurrentFillOutStatus = false;
                });
        }
        #endregion

        #region Undo Redo
        /// <summary>
        /// Reverts this action, i.e., deletes the drawn shape.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            if (Shape == null)
            {
                Shape = GameFinder.FindAttachedOrLocalDescendant(memento.Surface.GetDrawableSurface(), memento.Shape.ID);
            }
            if (Shape != null)
            {
                new EraseNetAction(memento.Surface.ID, memento.Surface.ParentID, memento.Shape.ID).Execute();
                Destroyer.Destroy(Shape);
            }
        }

        /// <summary>
        /// Repeats this action, i.e., redraws the shape.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            Shape = GameLineDrawer.ReDrawLine(memento.Surface.GetDrawableSurface(), memento.Shape);
            if (Shape != null)
            {
                new DrawNetAction(memento.Surface.ID, memento.Surface.ParentID, LineConf.GetLine(Shape)).Execute();
            }
        }
        #endregion

        #region Factory
        /// <summary>
        /// A new instance of <see cref="DrawShapesAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>New instance of <see cref="DrawShapesAction"/>.</returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new DrawShapesAction();
        }

        /// <summary>
        /// A new instance of <see cref="DrawShapesAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>New instance of <see cref="DrawShapesAction"/>.</returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }
        #endregion

        #region Metadata
        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.DrawShapes"/>.</returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.DrawShapes;
        }

        /// <summary>
        /// The set of IDs of all gameObjects changed by this action.
        /// <see cref="ReversibleAction.GetActionStateType"/>
        /// </summary>
        /// <returns>The ID of the created shape.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            if (memento.Surface == null)
            {
                return new HashSet<string>();
            }
            else
            {
                return new HashSet<string>
                {
                    memento.Shape.ID
                };
            }
        }
        #endregion
    }
}

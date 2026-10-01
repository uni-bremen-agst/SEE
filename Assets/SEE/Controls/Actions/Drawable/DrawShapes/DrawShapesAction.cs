using SEE.Game.Drawable;
using SEE.Game.Drawable.ActionHelpers;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.Line;
using SEE.GO;
using SEE.Net.Actions.Drawable;
using SEE.UI.Menu.Drawable.Line;
using SEE.UI.Menu.Drawable.Shapes;
using SEE.Utils;
using SEE.Utils.History;
using System.Collections.Generic;
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
        /// The calculated positions of the currently previewed or created non-line shape.
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
        /// Manages line-cap configuration and state for the current preview.
        /// </summary>
        private readonly DrawShapeLineCapController lineCapController = new();

        /// <summary>
        /// Manages the interaction state of shape previews.
        /// </summary>
        private readonly DrawShapePreviewController previewController = new();

        /// <summary>
        /// Handles the interactive workflow for drawing line shapes.
        /// </summary>
        private readonly DrawShapeLineOperation lineOperation = new();

        /// <summary>
        /// Status if the line menu was changes to edit mode.
        /// </summary>
        private bool editMode = false;

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
            ShapeMenu.AssignFinishButton(lineOperation.RequestFinish);
        }

        /// <summary>
        /// Stops the action, resets action-specific menu state and
        /// destroys an unfinished shape preview.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            ShapeMenu.DisablePartUndo();

            if (lineOperation.IsDrawing && Shape != null
                || Shape != null && previewController.IsActiveOrFixed)
            {
                new EraseNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), Shape.name).Execute();
                Destroyer.Destroy(Shape);
            }
            Shape = null;
            lineOperation.Reset();
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
            if (previewController.TryGetFixedPosition(lineOperation.IsDrawing, out Vector3 fixedPreviewPosition))
            {
                ShapePreview(fixedPreviewPosition);
            }

            /// Disables the preview if the user selects <see cref="ShapePointsCalculator.Shape.Line"/>.
            DisableShapePreview();

            if (Shape != null && LineMenu.Instance.IsInDrawingMode() && !editMode)
            {
                editMode = true;

                if (lineOperation.IsDrawing)
                {
                    lineOperation.PrepareEditModeRefresh(Shape);
                }

                ShapeMenu.OpenLineMenuInCorrectMode();

                if (lineOperation.IsDrawing)
                {
                    lineOperation.RegisterFillOutCallbacks(Shape, Surface);
                }
            }
            else if (lineOperation.ShouldRefreshEditMode(Shape))
            {
                lineOperation.AcknowledgeEditModeRefresh();
                ShapeMenu.OpenLineMenuInCorrectMode();
                lineOperation.RegisterFillOutCallbacks(Shape, Surface);
            }
            else if (Shape == null && LineMenu.Instance.IsInEditMode() && !editMode)
            {
                ShapeMenu.OpenLineMenuInCorrectMode();
            }

            Vector3[] previewPositions = lineOperation.IsDrawing
                ? lineOperation.PreviewPositions
                : currentPreviewPositions;

            Color? previewFillOutColor = lineOperation.IsDrawing
                ? lineOperation.FillOutColor
                : shapeFillOut;

            lineCapController.RefreshPreviewIfMenuChanged(
                Shape,
                Surface,
                previewPositions,
                previewFillOutColor,
                lineOperation.IsDrawing || previewController.IsActiveOrFixed);

            previewController.UpdateAssociatedPage(Shape, Surface);

            if (!Raycasting.IsMouseOverGUI())
            {
                /// Offers a preview for shape representation.
                if (previewController.TryGetUnfixedHit(lineOperation.IsDrawing, out RaycastHit previewHit))
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
                    && !lineOperation.IsDrawing)
                {
                    return ShapeDrawing(raycastHit);
                }

                /// This block provides a line preview to select the desired position of the next line point.
                lineOperation.UpdatePreview(Shape, Surface, lineCapController);

                /// With this block, the user can add a new point to the line.
                lineOperation.AddPoint(Shape, Surface, lineCapController);

                /// With left shift key can the loop option of the shape menu be toggled.
                if (Input.GetKeyDown(KeyCode.LeftShift))
                {
                    ShapeMenu.SetBoolValue(!ShapeMenu.GetBoolValue());
                }

                /// Block for successfully completing the line.
                /// It adds a final point to the line.
                /// It requires a left-click with the left Ctrl key held down.
                if (lineOperation.TryAddFinalPoint(Shape, Surface))
                {
                    FinishLineDrawing();
                    return true;
                }

                /// Block for successfully completing the line without adding a new point.
                /// It requires a wheel-click.
                if (lineOperation.ShouldFinishWithoutFinalPoint())
                {
                    FinishLineDrawing();
                    return true;
                }
            }
            /// This block is outside the !Raycasting.IsMouseOverGUI check to allow
            /// the immediate detection of a click on the Finish button of the menu,
            /// even if the mouse cursor is still over the GUI.
            if (lineOperation.ConsumeFinishRequest())
            {
                FinishLineDrawing();
                return true;
            }

            /// Block for canceling the drawing of a line shape.
            if (lineOperation.TryCancel(Shape, Surface))
            {
                Shape = null;
                editMode = false;
                ResetPreviewState();
            }

            /// Block for removing the last point during the drawing of a line shape.
            RemoveLastLinePoint();

            return false;
        }
        #endregion

        #region Drawing State
        /// <summary>
        /// Finishes the currently drawn line and completes this action.
        /// </summary>
        private void FinishLineDrawing()
        {
            (GameObject finishedShape, LineConf finalShape) =
                lineOperation.Finish(Shape, Surface, lineCapController);

            Shape = finishedShape;
            memento = new Memento(Surface, finalShape);

            new DrawNetAction(
                memento.Surface.ID,
                memento.Surface.ParentID,
                finalShape).Execute();

            CurrentState = IReversibleAction.Progress.Completed;
            ResetPreviewState();
        }

        /// <summary>
        /// Removes the last point from the currently drawn line.
        /// If too few points remain, the line is canceled and the action state is cleaned up.
        /// </summary>
        /// <param name="ignorePartUndoButton">
        /// Whether the keyboard input should be ignored because this call originates
        /// from the part-undo button.
        /// </param>
        private void RemoveLastLinePoint(bool ignorePartUndoButton = false)
        {
            if (!lineOperation.RemoveLastPoint(
                    Shape,
                    Surface,
                    ignorePartUndoButton))
            {
                return;
            }

            Shape = null;
            editMode = false;
            ResetPreviewState();
        }
        #endregion

        #region Creation
        /// <summary>
        /// Starts drawing the selected shape or immediately completes a calculated non-line shape.
        /// </summary>
        /// <param name="raycastHit">The raycast hit defining the selected position.</param>
        /// <returns>Whether the shape creation was completed.</returns>
        private bool ShapeDrawing(RaycastHit raycastHit)
        {
            Surface = GameFinder.GetDrawableSurface(raycastHit.collider.gameObject);

            if (ShapeMenu.GetSelectedShape() == ShapePointsCalculator.Shape.Line)
            {
                Shape = lineOperation.Start(Surface, raycastHit.point);
                ShapeMenu.ActivatePartUndo(() => RemoveLastLinePoint(true));
                return false;
            }

            Vector3 convertedHitPoint;

            if (!previewController.IsFixed)
            {
                convertedHitPoint =
                    GameLineGeometry.GetConvertedPosition(Surface, raycastHit.point);

                GetSelectedShapePosition(convertedHitPoint);
            }
            else
            {
                convertedHitPoint = GameLineGeometry.GetConvertedPosition(
                    Surface,
                    previewController.FixedPosition);
            }

            return DrawShape(convertedHitPoint);
        }

        /// <summary>
        /// Calculates the points for the selected non-line shape based
        /// on the values configured in the <see cref="ShapeMenu"/>.
        /// </summary>
        /// <param name="convertedHitPoint">
        /// The selected position in local space of the drawable surface.
        /// </param>
        private void GetSelectedShapePosition(Vector3 convertedHitPoint)
        {
            switch (ShapeMenu.GetSelectedShape())
            {
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
                ResetPreviewState();

                return true;
            }
            else
            {
                positions = new Vector3[1];
                ResetPreviewState();
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
            if (!previewController.ShouldDisable(lineOperation.IsDrawing))
            {
                return;
            }

            editMode = false;
            previewController.Reset();

            new EraseNetAction(
                Surface.name,
                GameFinder.GetDrawableSurfaceParentName(Surface),
                Shape.name).Execute();

            Destroyer.Destroy(Shape);

            positions = new Vector3[1];
            lineOperation.Reset();
            ResetPreviewState();
        }

        /// <summary>
        /// Draws a shape preview.
        /// </summary>
        /// <param name="position">The position where the preview should be drawn.</param>
        private void ShapePreview(Vector3 position)
        {
            Vector3 convertedHitPoint = GameLineGeometry.GetConvertedPosition(Surface, position);
            GetSelectedShapePosition(convertedHitPoint);
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

using SEE.Game.Drawable;
using SEE.Game.Drawable.ActionHelpers;
using SEE.Game.Drawable.Configurations;
using SEE.GO;
using SEE.Net.Actions.Drawable;
using SEE.UI.Menu.Drawable;
using SEE.UI.Notification;
using SEE.Utils;
using SEE.Utils.History;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// Allows the user to draw a shape.
    /// </summary>
    public class DrawShapesAction : DrawableAction
    {
        /// <summary>
        /// The object holding the line renderer.
        /// </summary>
        private GameObject shape;

        /// <summary>
        /// The current shape. Will needed for open the <see cref="LineMenu"/> in the correct mode.
        /// </summary>
        public static GameObject currentShape;

        /// <summary>
        /// Property of the shape.
        /// </summary>
        private GameObject Shape {
            get { return shape; }
            set { shape = value;
                currentShape = value;
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
        /// Position for the fixed shape preview.
        /// </summary>
        private Vector3 shapePreviewFixPosition;
        /// <summary>
        /// Status if the fixed shape preview is active.
        /// </summary>
        private bool shapePreviewFix = false;

        /// <summary>
        /// Status if the preview is active.
        /// </summary>
        private bool shapePreview = false;

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
        /// Enables the shape menu.
        /// </summary>
        public override void Awake()
        {
            base.Awake();
            ShapeMenu.Enable();
            ShapeMenu.AssignFinishButton(() =>
            {
                if (drawing && positions.Length > 1 &&
                    ShapeMenu.GetSelectedShape() == ShapePointsCalculator.Shape.Line)
                {
                    finishDrawingViaButton = true;
                }
            });
        }

        /// <summary>
        /// Stops the action. It disables the shape menu and
        /// destroys the line shape if it is not successfully completed.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            ShapeMenu.Disable();
            if (drawing && Shape != null
                || Shape != null && (shapePreview ||shapePreviewFix))
            {
                new EraseNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), Shape.name).Execute();
                Destroyer.Destroy(Shape);
            }
            Shape = null;
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.DrawShapes"/>.
        /// Specifically: Allows the user to draw a shape.
        /// For all shapes except Line, a single click on the drawable is sufficient to draw the desired shape.
        /// Simply enter the desired values in the Shape Menu.
        /// For the Line shape type, multiple clicks (one for each point) are required.
        /// </summary>
        /// <returns>Whether this action is finished</returns>
        public override bool Update()
        {
            /// Offers a preview for a shape representation on a fixed chosen position.
            ShapePreviewFixed();
            /// Disables the preview if the user selects <see cref="ShapePointsCalculator.Shape.Line"/>
            DisableShapePreview();

            if (Shape != null && LineMenu.Instance.IsInDrawingMode() && !editMode)
            {
                editMode = true;
                if (ShapeMenu.GetSelectedShape() == ShapePointsCalculator.Shape.Line
                    && LineMenu.GetFillOutColorForDrawing() != null
                    && GameDrawer.DifferentPositionCounter(Shape) < 3)
                {
                   needRefreshEditMode = true;
                }
                ShapeMenu.OpenLineMenuInCorrectMode();
            }
            else if (needRefreshEditMode && GameDrawer.DifferentPositionCounter(Shape) > 2)
            {
                needRefreshEditMode = false;
                ShapeMenu.OpenLineMenuInCorrectMode();
            }
            else if (Shape == null && LineMenu.Instance.IsInEditMode() && !editMode)
            {
                ShapeMenu.OpenLineMenuInCorrectMode();
            }

            if (!Raycasting.IsMouseOverGUI())
            {
                /// Offers a preview for shape representation.
                ShapePreviewUnfixed();
                /// Marks a position as fixed for the shape preview.
                ShapePreviewFixPosition();
                /// Releases the fixed position.
                ShapePreviewReleasePosition();

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
                    ShapeMenu.GetLoopManager().isOn = !ShapeMenu.GetLoopManager().isOn;
                    ShapeMenu.GetLoopManager().UpdateUI();
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

                    GameDrawer.Drawing(Shape, positions);
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
            if (!shapePreviewFix)
            {
                convertedHitPoint = GameDrawer.GetConvertedPosition(Surface, raycastHit.point);
                GetSelectedShapePosition(convertedHitPoint, raycastHit.point);
            } else
            {
                convertedHitPoint = GameDrawer.GetConvertedPosition(Surface, shapePreviewFixPosition);
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
                    Shape = GameDrawer.StartDrawing(Surface, positions, ValueHolder.CurrentColorKind,
                        ValueHolder.CurrentPrimaryColor, ValueHolder.CurrentSecondaryColor,
                        ValueHolder.CurrentThickness, ValueHolder.CurrentLineKind,
                        ValueHolder.CurrentTiling);
                    positions[0] = Shape.transform.InverseTransformPoint(positions[0]) - ValueHolder.DistanceToDrawable;
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
                        ShapeMenu.GetValue2(), ShapeMenu.GetValue4());
                    break;
                case ShapePointsCalculator.Shape.Trapezoid:
                    positions = ShapePointsCalculator.Trapezoid(convertedHitPoint, ShapeMenu.GetValue1(),
                        ShapeMenu.GetValue2(), ShapeMenu.GetValue3());
                    break;
                case ShapePointsCalculator.Shape.Polygon:
                    positions = ShapePointsCalculator.Polygon(convertedHitPoint, ShapeMenu.GetValue1(),
                        ShapeMenu.GetVertices());
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
            if (GameDrawer.DifferentPositionCounter(positions) > 1)
            {
                BlinkEffect.Deactivate(Shape);
                LineConf currentShape = LineConf.GetLine(Shape);
                Shape = GameDrawer.SetPivotShape(Shape, convertedHitPoint, LineConf.GetFillOutColor(currentShape));
                shapePreview = shapePreviewFix = false;
                currentShape = LineConf.GetLine(Shape);
                memento = new Memento(Surface, currentShape);
                new DrawNetAction(memento.Surface.ID, memento.Surface.ParentID, currentShape).Execute();
                CurrentState = IReversibleAction.Progress.Completed;
                drawing = false;
                return true;
            }
            else
            {
                positions = new Vector3[1];
                drawing = false;
                new EraseNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), Shape.name).Execute();
                Destroyer.Destroy(Shape);
                Shape = null;
                editMode = false;
                return false;
            }
        }

        /// <summary>
        /// Disables the shape preview and deletes the preview.
        /// </summary>
        private void DisableShapePreview()
        {
            if (ShapeMenu.GetSelectedShape() == ShapePointsCalculator.Shape.Line
                && (shapePreview || shapePreviewFix)
                || (drawing && ShapeMenu.GetSelectedShape() != ShapePointsCalculator.Shape.Line))
            {
                drawing = false;
                shapePreview = false;
                shapePreviewFix = false;
                editMode = false;
                shapePreviewFixPosition = Vector3.zero;
                new EraseNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), Shape.name).Execute();
                Destroyer.Destroy(Shape);
                positions = new Vector3[1];
            }
        }

        /// <summary>
        /// Releases a fixed position for the shape preview.
        /// </summary>
        private void ShapePreviewReleasePosition()
        {
            if (SEEInput.MouseDown(MouseButton.Middle)
                && shapePreviewFix
                && Input.GetKey(KeyCode.LeftControl)
                && ShapeMenu.GetSelectedShape() != ShapePointsCalculator.Shape.Line)
            {
                shapePreviewFix = false;
                shapePreviewFixPosition = Vector3.zero;
                Surface = null;
                ShowNotification.Info("Fix position released.", "The fixed position for the shape preview was released.");
            }
        }

        /// <summary>
        /// Allows a position to be marked where the preview is held.
        /// It can then be further configured until the confirming left click.
        /// </summary>
        private void ShapePreviewFixPosition()
        {
            if (Selector.SelectQueryHasOrIsSurfaceWithoutMouse(out RaycastHit raycastHit)
                && SEEInput.MouseDown(MouseButton.Middle)
                && !Input.GetKey(KeyCode.LeftControl)
                && ShapeMenu.GetSelectedShape() != ShapePointsCalculator.Shape.Line)
            {
                shapePreviewFix = true;
                shapePreviewFixPosition = raycastHit.point;
                Surface = GameFinder.GetDrawableSurface(raycastHit.collider.gameObject);
                ShowNotification.Info("Fix position set.", "The fixed position for the shape preview has been set.");
            }
        }

        /// <summary>
        /// Draws a shape preview.
        /// </summary>
        /// <param name="position">The position where the preview should be drawn.</param>
        private void ShapePreview(Vector3 position)
        {
            Vector3 convertedHitPoint = GameDrawer.GetConvertedPosition(Surface, position);
            GetSelectedShapePosition(convertedHitPoint, position);

            if (Shape == null)
            {
                Shape = GameDrawer.DrawLine(Surface, "", positions, ValueHolder.CurrentColorKind,
                    ValueHolder.CurrentPrimaryColor, ValueHolder.CurrentSecondaryColor, ValueHolder.CurrentThickness, false,
                    ValueHolder.CurrentLineKind, ValueHolder.CurrentTiling, fillOutColor: LineMenu.GetFillOutColorForDrawing());
                shapeFillOut = LineMenu.GetFillOutColorForDrawing();
                Shape.GetComponent<LineRenderer>().loop = false;
                Shape.AddOrGetComponent<BlinkEffect>();
                new DrawNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), LineConf.GetLine(Shape)).Execute();
            }
            else
            {
                shapeFillOut ??= LineConf.GetFillOutColor(LineConf.GetLine(Shape));
                LineMenu.AssignFillOutForEditing(shapeFillOut, color =>
                {
                    shapeFillOut = color;
                    GameEdit.ChangeFillOutColor(shape, color);
                    new EditLineFillOutColorNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), shape.name, color).Execute();
                }, () => shapeFillOut = null);
                if (shapeFillOut != null && BlinkEffect.CanFillOutBeAdded(shape))
                {
                    BlinkEffect.AddFillOutToEffect(shape);
                }
                GameDrawer.Drawing(Shape, positions, fillOutColor: shapeFillOut);
                new DrawNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), LineConf.GetLine(Shape)).Execute();
            }
        }

        /// <summary>
        /// Draws a shape preview that follows the pointer.
        /// </summary>
        private void ShapePreviewUnfixed()
        {
            if (!drawing && !SEEInput.LeftMouseInteraction()
                && !shapePreviewFix
                && Selector.SelectQueryHasOrIsSurfaceWithoutMouse(out RaycastHit raycastHit)
                && ShapeMenu.GetSelectedShape() != ShapePointsCalculator.Shape.Line)
            {
                shapePreview = true;
                Surface = GameFinder.GetDrawableSurface(raycastHit.collider.gameObject);
                ShapePreview(raycastHit.point);
            }
        }

        /// <summary>
        /// Draws a shape preview on a fix position.
        /// The position must be chosen with a right mouse click.
        /// </summary>
        private void ShapePreviewFixed()
        {
            if (!drawing && !SEEInput.LeftMouseDown()
                && shapePreviewFix
                && ShapeMenu.GetSelectedShape() != ShapePointsCalculator.Shape.Line)
            {
                shapePreview = true;
                ShapePreview(shapePreviewFixPosition);
            }
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
                    shapeFillOut ??= LineConf.GetFillOutColor(LineConf.GetLine(shape));
                    if (positions.Length > 1)
                    {
                        GameDrawer.Drawing(Shape, positions, shapeFillOut);
                    }
                    else
                    {
                        if (shapeFillOut != null)
                        {
                            GameObject.DestroyImmediate(GameFinder.FindChild(Shape, ValueHolder.FillOut));
                            new DeleteFillOutNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), Shape.name).Execute();
                            LineMenu.AssignFillOutForEditing(null, null, () => { });
                        }
                        GameDrawer.Drawing(Shape, positions);
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
                    editMode = false;
                    shapeFillOut = null;
                }
            }
        }

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
                if (GameDrawer.DifferentPositionCounter(newPositions) > 2)
                {
                    shapeFillOut ??= LineConf.GetFillOutColor(LineConf.GetLine(Shape));
                    GameDrawer.Drawing(Shape, newPositions, shapeFillOut);
                    new DrawingNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), Shape.name, newPosition, newPositions.Length - 1).Execute();
                    if (shapeFillOut != null)
                    {
                        LineMenu.AssignFillOutForEditing(shapeFillOut, color => {
                            shapeFillOut = color;
                            GameEdit.ChangeFillOutColor(shape, color);
                            new EditLineFillOutColorNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), shape.name, color).Execute();
                        }, () => shapeFillOut = null);
                        new DrawingFillOutNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), Shape.name, shapeFillOut.Value).Execute();
                    }
                }
                else
                {
                    GameDrawer.Drawing(Shape, newPositions);
                    new DrawingNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), Shape.name, newPosition, newPositions.Length - 1).Execute();
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

                    if (positions.Length > 2)
                    {
                        shapeFillOut ??= LineConf.GetFillOutColor(LineConf.GetLine(shape));
                        GameDrawer.Drawing(Shape, positions, shapeFillOut);
                        new DrawingNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface),
                                         Shape.name, newPosition, newPositions.Length - 1).Execute();
                        if (shapeFillOut != null)
                        {
                            LineMenu.AssignFillOutForEditing(shapeFillOut, color => {
                                shapeFillOut = color; GameEdit.ChangeFillOutColor(shape, color);
                                new EditLineFillOutColorNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), shape.name, color).Execute();
                            }, () => shapeFillOut = null);
                            new DrawingFillOutNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), Shape.name, shapeFillOut.Value).Execute();
                        }
                    }
                    else
                    {
                        GameDrawer.Drawing(Shape, positions);
                        new DrawingNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface),
                                         Shape.name, newPosition, newPositions.Length - 1).Execute();
                    }

                }
            }
        }

        /// <summary>
        /// Finish the drawing of the line shape.
        /// It must be a separate method as it can be called from two different points.
        /// </summary>
        private void FinishDrawing()
        {
            GameDrawer.Drawing(Shape, positions);
            Shape.GetComponent<LineRenderer>().loop = ShapeMenu.GetLoopManager().isOn;
            Shape = GameDrawer.SetPivot(Shape, shapeFillOut);
            LineConf currentShape = LineConf.GetLine(Shape);
            memento = new Memento(Surface, currentShape);
            new DrawNetAction(memento.Surface.ID, memento.Surface.ParentID, currentShape).Execute();
            CurrentState = IReversibleAction.Progress.Completed;
            drawing = false;
        }

        /// <summary>
        /// Reverts this action, i.e., deletes the drawn shape.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            if (Shape == null)
            {
                Shape = GameFinder.FindChild(memento.Surface.GetDrawableSurface(), memento.Shape.Id);
            }
            if (Shape != null)
            {
                new EraseNetAction(memento.Surface.ID, memento.Surface.ParentID, memento.Shape.Id).Execute();
                Destroyer.Destroy(Shape);
            }
        }

        /// <summary>
        /// Repeats this action, i.e., redraws the shape.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            Shape = GameDrawer.ReDrawLine(memento.Surface.GetDrawableSurface(), memento.Shape);
            if (Shape != null)
            {
                new DrawNetAction(memento.Surface.ID, memento.Surface.ParentID, LineConf.GetLine(Shape)).Execute();
            }
        }

        /// <summary>
        /// A new instance of <see cref="DrawShapesAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="DrawShapesAction"/></returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new DrawShapesAction();
        }

        /// <summary>
        /// A new instance of <see cref="DrawShapesAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="DrawShapesAction"/></returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.DrawShapes"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.DrawShapes;
        }

        /// <summary>
        /// The set of IDs of all gameObjects changed by this action.
        /// <see cref="ReversibleAction.GetActionStateType"/>
        /// </summary>
        /// <returns>The id of the created shape</returns>
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
                    memento.Shape.Id
                };
            }
        }
    }
}

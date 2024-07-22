using LibGit2Sharp;
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
        /// Stops the action. It disable the shape menu and
        /// destroys the line shape if it is not successfully completed.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            ShapeMenu.Disable();
            if (drawing && shape != null
                || shape != null && (shapePreview ||shapePreviewFix))
            {
                new EraseNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), shape.name).Execute();
                Destroyer.Destroy(shape);
            }
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
                if (Queries.MouseUp(MouseButton.Left) 
                    && Input.GetKey(KeyCode.LeftControl)
                    && drawing 
                    && positions.Length > 0
                    && ShapeMenu.GetSelectedShape() == ShapePointsCalculator.Shape.Line
                    && Selector.SelectQueryHasOrIsSurfaceWithoutMouse(out RaycastHit hit))
                {
                    Vector3 newPosition = shape.transform.InverseTransformPoint(hit.point) - ValueHolder.DistanceToDrawable;
                    Vector3[] newPositions = new Vector3[positions.Length + 1];
                    Array.Copy(sourceArray: positions, destinationArray: newPositions, length: positions.Length);
                    newPositions[newPositions.Length - 1] = newPosition;
                    positions = newPositions;

                    GameDrawer.Drawing(shape, positions);
                    new DrawingNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), shape.name, newPosition, newPositions.Length - 1).Execute();
                    //new DrawNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface),
                    //    LineConf.GetLine(shape)).Execute();
                    FinishDrawing();
                    return true;
                }

                /// Block for successfully completing the line without adding a new point.
                /// It requires a wheel-click.
                if (Queries.MouseUp(MouseButton.Middle)
                    && drawing 
                    && positions.Length > 1
                    && ShapeMenu.GetSelectedShape() == ShapePointsCalculator.Shape.Line)
                {
                    FinishDrawing();
                    return true;
                }
            }
            /// This block is outside the !Raycasting.IsMouseOverGUI check to allow
            /// the immediate detection of a click on
            /// the Finish button of the menu,
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
                new EraseNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), shape.name).Execute();
                Destroyer.Destroy(shape);
                ShapeMenu.DisablePartUndo();
                positions = new Vector3[1];
                drawing = false;
                shape = null;
            }
        }

        /// <summary>
        /// Provides the option to remove the last added point.
        /// Press the caps lock key for this action.
        /// If the line does not have enough points to remove, it will be deleted.
        /// </summary>
        /// <param name="ignorePartUndoButton">True if the key input should be ignored. Will used for the part undo button of the <see cref="ShapeMenu"/>.</param>
        private void RemoveLastPoint(bool ignorePartUndoButton = false)
        {
            if (drawing && (SEEInput.PartUndo() || ignorePartUndoButton))
            {
                if (shape.GetComponent<LineRenderer>().positionCount >= 3)
                {
                    ShowNotification.Info("Last point removed.", 
                        "The last placed point of the line has been removed.");
                    LineRenderer renderer = shape.GetComponent<LineRenderer>();
                    renderer.positionCount -= 2;
                    positions = positions.ToList().GetRange(0, positions.Length - 1).ToArray();
                    //renderer.SetPositions(positions);
                    GameDrawer.Drawing(shape, positions);
                    new DrawNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), LineConf.GetLine(shape)).Execute();
                }
                else
                {
                    ShowNotification.Info("Line-Shape drawing canceled.", 
                        "The drawing of the shape art line has been canceled.");
                    new EraseNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), shape.name).Execute();
                    Destroyer.Destroy(shape);
                    ShapeMenu.DisablePartUndo();
                    positions = new Vector3[1];
                    drawing = false;
                    shape = null;
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
                    shape = GameDrawer.StartDrawing(Surface, positions, ValueHolder.CurrentColorKind,
                        ValueHolder.CurrentPrimaryColor, ValueHolder.CurrentSecondaryColor,
                        ValueHolder.CurrentThickness, ValueHolder.CurrentLineKind,
                        ValueHolder.CurrentTiling);
                    positions[0] = shape.transform.InverseTransformPoint(positions[0]) - ValueHolder.DistanceToDrawable;
                    ShapeMenu.ActivatePartUndo(() => RemoveLastPoint(true));
                    LineConf conf = LineConf.GetLine(shape);
                    conf.RendererPositions = positions;
                    new DrawNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), conf).Execute();
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
                BlinkEffect.Deactivate(shape);
                shape = GameDrawer.SetPivotShape(shape, convertedHitPoint);
                shapePreview = shapePreviewFix = false;
                LineConf currentShape = LineConf.GetLine(shape);
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
                shape = null;
                return false;
            }
        }

        /// <summary>
        /// Disables the shape preview and deletes the preview.
        /// </summary>
        private void DisableShapePreview()
        {
            if (ShapeMenu.GetSelectedShape() == ShapePointsCalculator.Shape.Line
                && (shapePreview || shapePreviewFix))
            {
                shapePreview = false;
                shapePreviewFix = false;
                shapePreviewFixPosition = Vector3.zero;
                new EraseNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), shape.name).Execute();
                Destroyer.Destroy(shape);
                positions = new Vector3[1];
            }
        }

        /// <summary>
        /// Releases a fixed position for the shape preview.
        /// </summary>
        private void ShapePreviewReleasePosition()
        {
            if (Queries.MouseDown(MouseButton.Middle) 
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
                && Queries.MouseDown(MouseButton.Middle)
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

            if (shape == null)
            {
                shape = GameDrawer.DrawLine(Surface, "", positions, ValueHolder.CurrentColorKind,
                    ValueHolder.CurrentPrimaryColor, ValueHolder.CurrentSecondaryColor, ValueHolder.CurrentThickness, false,
                    ValueHolder.CurrentLineKind, ValueHolder.CurrentTiling);
                shape.GetComponent<LineRenderer>().loop = false;
                shape.AddOrGetComponent<BlinkEffect>();
                new DrawNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), LineConf.GetLine(shape)).Execute();
            }
            else
            {
                GameDrawer.Drawing(shape, positions);
                new DrawNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), LineConf.GetLine(shape)).Execute();
            }
        }

        /// <summary>
        /// Draws a shape preview that follows the pointer.
        /// </summary>
        private void ShapePreviewUnfixed()
        {
            if (!drawing && !Queries.LeftMouseInteraction()
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
            if (!drawing && !Queries.LeftMouseDown()
                && shapePreviewFix
                && ShapeMenu.GetSelectedShape() != ShapePointsCalculator.Shape.Line)
            {
                shapePreview = true;
                ShapePreview(shapePreviewFixPosition);
            }
        }

        /// <summary>
        /// This method provides a line preview for the user
        /// to select the desired position of the next line point.
        /// </summary>
        private void LineShapePreview()
        {
            if (drawing && !Queries.LeftMouseInteraction() 
                && Selector.SelectQueryHasOrIsSurfaceWithoutMouse(out RaycastHit raycastHit)
                && ShapeMenu.GetSelectedShape() == ShapePointsCalculator.Shape.Line
                && Queries.DrawableSurfaceNullOrSame(Surface, raycastHit.collider.gameObject))
            {
                Vector3 newPosition = shape.transform.InverseTransformPoint(raycastHit.point) - ValueHolder.DistanceToDrawable;
                Vector3[] newPositions = new Vector3[positions.Length + 1];
                Array.Copy(sourceArray: positions, destinationArray: newPositions, length: positions.Length);
                newPositions[^1] = newPosition;
                GameDrawer.Drawing(shape, newPositions);
                new DrawingNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), shape.name, newPosition, newPositions.Length - 1).Execute();
                //new DrawNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), LineConf.GetLine(shape)).Execute();
            }
        }

        /// <summary>
        /// Provides the function to add a new point in the Line shape.
        /// However, the new point must be different from the previous one.
        /// This requires a left mouse click, with neither the left Shift nor the left Ctrl key pressed.
        /// </summary>
        private void AddLineShapePoint()
        {
            if (Queries.LeftMouseDown() && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift) 
                && Selector.SelectQueryHasOrIsSurfaceWithoutMouse(out RaycastHit raycastHit)
                && drawing && ShapeMenu.GetSelectedShape() == ShapePointsCalculator.Shape.Line
                && Queries.DrawableSurfaceNullOrSame(Surface, raycastHit.collider.gameObject))
            {
                Vector3 newPosition = shape.transform.InverseTransformPoint(raycastHit.point) - ValueHolder.DistanceToDrawable;
                if (newPosition != positions.Last())
                {
                    Vector3[] newPositions = new Vector3[positions.Length + 1];
                    Array.Copy(sourceArray: positions, destinationArray: newPositions, length: positions.Length);
                    newPositions[newPositions.Length - 1] = newPosition;
                    positions = newPositions;

                    GameDrawer.Drawing(shape, positions);
                    new DrawingNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), shape.name, newPosition, newPositions.Length - 1).Execute();
                    //new DrawNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface), LineConf.GetLine(shape)).Execute();
                }
            }
        }

        /// <summary>
        /// Finish the drawing of the line shape.
        /// It must be a separate method as it can be called from two different points.
        /// </summary>
        private void FinishDrawing()
        {
            GameDrawer.Drawing(shape, positions);
            shape.GetComponent<LineRenderer>().loop = ShapeMenu.GetLoopManager().isOn;
            shape = GameDrawer.SetPivot(shape);
            LineConf currentShape = LineConf.GetLine(shape);
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
            if (shape == null)
            {
                shape = GameFinder.FindChild(memento.Surface.GetDrawableSurface(), memento.Shape.Id);
            }
            if (shape != null)
            {
                new EraseNetAction(memento.Surface.ID, memento.Surface.ParentID, memento.Shape.Id).Execute();
                Destroyer.Destroy(shape);
            }
        }

        /// <summary>
        /// Repeats this action, i.e., redraws the shape.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            shape = GameDrawer.ReDrawLine(memento.Surface.GetDrawableSurface(), memento.Shape);
            if (shape != null)
            {
                new DrawNetAction(memento.Surface.ID, memento.Surface.ParentID, LineConf.GetLine(shape)).Execute();
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
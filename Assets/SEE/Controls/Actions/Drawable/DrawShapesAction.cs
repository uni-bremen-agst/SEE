using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;
using SEE.Net.Actions.Drawable;
using Assets.SEE.Game.Drawable;
using System.Linq;
using SEE.Game;
using System;
using Assets.SEE.Game.UI.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// Allows the user to draw a shape.
    /// </summary>
    public class DrawShapesAction : AbstractPlayerAction
    {
        /// <summary>
        /// The object holding the line renderer.
        /// </summary>
        private GameObject shape;

        /// <summary>
        /// The drawable where the shape is displayed.
        /// </summary>
        private GameObject drawable;

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
        private struct Memento
        {
            /// <summary>
            /// The drawable where the shape is displayed.
            /// </summary>
            public readonly DrawableConfig drawable;
            /// <summary>
            /// The configuration of the shape.
            /// </summary>
            public readonly LineConf shape;

            /// <summary>
            /// The constructor, which simply assigns its only parameter to a field in this class.
            /// </summary>
            /// <param name="drawable">The drawable where the shape is displayed.</param>
            /// <param name="shape">The configuration of the shape.</param>
            public Memento(GameObject drawable, LineConf shape)
            {
                this.drawable = DrawableConfigManager.GetDrawableConfig(drawable);
                this.shape = shape;
            }
        }

        /// <summary>
        /// Representing if the action is drawing. 
        /// Also necessary to identifier if the line shape was successfully drawed.
        /// </summary>
        private bool drawing = false;

        private bool finishDrawingViaButton = false;

        /// <summary>
        /// Enables the shape menu
        /// </summary>
        public override void Awake()
        {
            ShapeMenu.Enable();
            ShapeMenu.AssignFinishButton(() => 
            {
                if (drawing && positions.Length > 1 && ShapeMenu.GetSelectedShape() == GameShapesCalculator.Shape.Line)
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
            ShapeMenu.Disable();
            if (drawing && shape != null)
            {
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
        /// <returns>Whether this Action is finished</returns>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                /// Block for initiating drawing, the lower if blocks are relevant for the line shape type, 
                /// for all others, the drawing is completed within this block. 
                /// The shape selected from the Shape Menu is then drawn based on the entered values (except for line).
                if (Input.GetMouseButtonDown(0) &&
                    Raycasting.RaycastAnything(out RaycastHit raycastHit) &&
                    (raycastHit.collider.gameObject.CompareTag(Tags.Drawable) ||
                    GameFinder.hasDrawable(raycastHit.collider.gameObject))
                    && !drawing)
                {
                    drawable = raycastHit.collider.gameObject.CompareTag(Tags.Drawable) ?
                        raycastHit.collider.gameObject : GameFinder.GetDrawable(raycastHit.collider.gameObject);
                    drawing = true;
                    Vector3 convertedHitPoint = GameDrawer.GetConvertedPosition(drawable, raycastHit.point);
                   
                    switch (ShapeMenu.GetSelectedShape())
                    {
                        case GameShapesCalculator.Shape.Line:

                            positions[0] = raycastHit.point;
                            shape = GameDrawer.StartDrawing(drawable, positions, ValueHolder.currentColorKind, 
                                ValueHolder.currentPrimaryColor, ValueHolder.currentSecondaryColor, ValueHolder.currentThickness,
                                ValueHolder.currentLineKind, ValueHolder.currentTiling);
                            positions[0] = shape.transform.InverseTransformPoint(positions[0]) - ValueHolder.distanceToDrawable;
                            break;
                        case GameShapesCalculator.Shape.Square:
                            positions = GameShapesCalculator.Square(convertedHitPoint, ShapeMenu.GetValue1());
                            break;
                        case GameShapesCalculator.Shape.Rectangle:
                            positions = GameShapesCalculator.Rectanlge(convertedHitPoint, ShapeMenu.GetValue1(), ShapeMenu.GetValue2());
                            break;
                        case GameShapesCalculator.Shape.Rhombus:
                            positions = GameShapesCalculator.Rhombus(convertedHitPoint, ShapeMenu.GetValue1(), ShapeMenu.GetValue2());
                            break;
                        case GameShapesCalculator.Shape.Kite:
                            positions = GameShapesCalculator.Kite(convertedHitPoint, ShapeMenu.GetValue1(), ShapeMenu.GetValue2(), ShapeMenu.GetValue3());
                            break;
                        case GameShapesCalculator.Shape.Triangle:
                            positions = GameShapesCalculator.Triangle(convertedHitPoint, ShapeMenu.GetValue1(), ShapeMenu.GetValue2());
                            break;
                        case GameShapesCalculator.Shape.Circle:
                            positions = GameShapesCalculator.Circle(convertedHitPoint, ShapeMenu.GetValue1());
                            break;
                        case GameShapesCalculator.Shape.Ellipse:
                            positions = GameShapesCalculator.Ellipse(convertedHitPoint, ShapeMenu.GetValue1(), ShapeMenu.GetValue2());
                            break;
                        case GameShapesCalculator.Shape.Parallelogram:
                            positions = GameShapesCalculator.Parallelogram(convertedHitPoint, ShapeMenu.GetValue1(), ShapeMenu.GetValue2(), ShapeMenu.GetValue4());
                            break;
                        case GameShapesCalculator.Shape.Trapezoid:
                            positions = GameShapesCalculator.Trapezoid(convertedHitPoint, ShapeMenu.GetValue1(), ShapeMenu.GetValue2(), ShapeMenu.GetValue3());
                            break;
                        case GameShapesCalculator.Shape.Polygon:
                            positions = GameShapesCalculator.Polygon(convertedHitPoint, ShapeMenu.GetValue1(), ShapeMenu.GetVertices());
                            break;
                    }

                    /// This block draws and completes the action for all shapes except lines.
                    if (ShapeMenu.GetSelectedShape() != GameShapesCalculator.Shape.Line)
                    {
                        if (GameDrawer.DifferentPositionCounter(positions) > 1)
                        {
                            shape = GameDrawer.DrawLine(drawable, "", positions, ValueHolder.currentColorKind,
                                ValueHolder.currentPrimaryColor, ValueHolder.currentSecondaryColor, ValueHolder.currentThickness, false,
                                ValueHolder.currentLineKind, ValueHolder.currentTiling);
                            shape.GetComponent<LineRenderer>().loop = false;
                            shape = GameDrawer.SetPivotShape(shape, convertedHitPoint);
                            LineConf currentShape = LineConf.GetLine(shape);
                            memento = new Memento(drawable, currentShape);
                            new DrawOnNetAction(memento.drawable.ID, memento.drawable.ParentID, currentShape).Execute();
                            currentState = ReversibleAction.Progress.Completed;
                            drawing = false;
                            return true;
                        } else
                        {
                            positions = new Vector3[1];
                            drawing = false;
                            shape = null;
                        }
                    }
                }

                /// This block provides a line preview to select the desired position of the next line point.
                if (drawing && !Input.GetMouseButton(0) && !Input.GetMouseButtonDown(0) &&
                    Raycasting.RaycastAnything(out RaycastHit rh) &&
                    (rh.collider.gameObject.CompareTag(Tags.Drawable) ||
                    GameFinder.hasDrawable(rh.collider.gameObject)) &&
                    ShapeMenu.GetSelectedShape() == GameShapesCalculator.Shape.Line
                    && (drawable == null || drawable != null && GameFinder.GetDrawable(rh.collider.gameObject).Equals(drawable)))
                {
                    Vector3 newPosition = shape.transform.InverseTransformPoint(rh.point) - ValueHolder.distanceToDrawable;
                    Vector3[] newPositions = new Vector3[positions.Length + 1];
                    Array.Copy(sourceArray: positions, destinationArray: newPositions, length: positions.Length);
                    newPositions[newPositions.Length - 1] = newPosition;
                    GameDrawer.Drawing(shape ,newPositions);
                    new DrawOnNetAction(drawable.name, GameFinder.GetDrawableParentName(drawable), LineConf.GetLine(shape)).Execute();
                }

                /// With this block, the user can add a new point to the line. 
                /// This requires a left mouse click, with neither the left Shift nor the left Ctrl key pressed.
                if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift) &&
                    Raycasting.RaycastAnything(out RaycastHit hit) &&
                    (hit.collider.gameObject.CompareTag(Tags.Drawable) ||
                    GameFinder.hasDrawable(hit.collider.gameObject))
                    && drawing && ShapeMenu.GetSelectedShape() == GameShapesCalculator.Shape.Line
                    && (drawable == null || drawable != null && GameFinder.GetDrawable(hit.collider.gameObject).Equals(drawable)))
                {
                    Vector3 newPosition = shape.transform.InverseTransformPoint(hit.point) - ValueHolder.distanceToDrawable;
                    if (newPosition != positions.Last())
                    {
                        Vector3[] newPositions = new Vector3[positions.Length + 1];
                        Array.Copy(sourceArray: positions, destinationArray: newPositions, length: positions.Length);
                        newPositions[newPositions.Length - 1] = newPosition;
                        positions = newPositions;

                        GameDrawer.Drawing(shape, positions);
                        new DrawOnNetAction(drawable.name, GameFinder.GetDrawableParentName(drawable), LineConf.GetLine(shape)).Execute();
                    }
                }
                
                /// With left shift key can the loop option of the shape menu be toggled.
                if (Input.GetKeyDown(KeyCode.LeftShift))
                {
                    ShapeMenu.GetLoopManager().isOn = !ShapeMenu.GetLoopManager().isOn;
                    ShapeMenu.GetLoopManager().UpdateUI();
                }

                /// Block for successfully completing the line. It requires a left-click with the left Ctrl key held down.
                if (Input.GetMouseButtonUp(0) && Input.GetKey(KeyCode.LeftControl)
                    && drawing && positions.Length > 1 && ShapeMenu.GetSelectedShape() == GameShapesCalculator.Shape.Line)
                {
                    FinishDrawing();
                    return true;
                }
            }
            if (finishDrawingViaButton)
            {
                FinishDrawing();
                return true;
            }
            return false;
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
            memento = new Memento(drawable, currentShape);
            new DrawOnNetAction(memento.drawable.ID, memento.drawable.ParentID, currentShape).Execute();
            currentState = ReversibleAction.Progress.Completed;
            drawing = false;
        }

        /// <summary>
        /// Reverts this action, i.e., deletes the drawed shape.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            if (shape == null)
            {
                shape = GameFinder.FindChild(memento.drawable.GetDrawable(), memento.shape.id);
            }
            if (shape != null)
            {
                new EraseNetAction(memento.drawable.ID, memento.drawable.ParentID, memento.shape.id).Execute();
                Destroyer.Destroy(shape);
            }
        }

        /// <summary>
        /// Repeats this action, i.e., redraws the shape.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            shape = GameDrawer.ReDrawLine(memento.drawable.GetDrawable(), memento.shape);
            if (shape != null)
            {
                new DrawOnNetAction(memento.drawable.ID, memento.drawable.ParentID, LineConf.GetLine(shape)).Execute();
            }
        }

        /// <summary>
        /// A new instance of <see cref="DrawShapesAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="DrawShapesAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new DrawShapesAction();
        }

        /// <summary>
        /// A new instance of <see cref="DrawShapesAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="DrawShapesAction"/></returns>
        public override ReversibleAction NewInstance()
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
        /// Because this action does not actually change any game object, 
        /// an empty set is always returned.
        /// </summary>
        /// <returns>The id of the created shape</returns>
        public override HashSet<string> GetChangedObjects()
        {
            if (memento.drawable == null)
            {
                return new HashSet<string>();
            }
            else
            {
                return new HashSet<string>
                {
                    memento.shape.id
                };
            }
        }
    }
}
using SEE.Controls.Actions;
using SEE.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.SEE.Net.Actions.Whiteboard;
using SEE.Net.Actions;
using Assets.SEE.Game.Drawable;
using Assets.SEE.Game;
using System.Linq;
using SEE.Game;
using System;
using UnityEngine.UI;
using Assets.SEE.Game.UI.Drawable;
using TMPro;
using RTG;
using static RootMotion.FinalIK.HitReactionVRIK;

namespace Assets.SEE.Controls.Actions.Drawable
{
    public class DrawShapesAction : AbstractPlayerAction
    {
        /// <summary>
        /// The object holding the line renderer.
        /// </summary>
        private static GameObject shape;

        private static GameObject drawable;

        /// <summary>
        /// The positions of the line in local space.
        /// </summary>
        private Vector3[] positions = new Vector3[1];

        private Memento memento;

        private static bool drawing = false;
        private static bool firstStart = true;

        private static HSVPicker.ColorPicker picker;
        private static ThicknessSliderController thicknessSlider;
        private const string drawableSwitchPrefab = "Prefabs/UI/Drawable/DrawableSwitch";
        private const string drawableShapePrefab = "Prefabs/UI/Drawable/DrawableShapeMenu";
        private static GameObject drawableSwitch;
        private static GameObject shapeMenu;
        private static Button previousBtn;
        private static Button nextBtn;
        private static TMP_Text shapeLabel;
        private static GameShapesCalculator.Shapes selectedShape;
        private static Button shapeBtn;
        private static Button configBtn;
        private static GameObject objValue1;
        private static FloatValueSliderController sliderValue1;
        private static GameObject objValue2;
        private static FloatValueSliderController sliderValue2;
        private static GameObject objValue3;
        private static FloatValueSliderController sliderValue3;
        private static GameObject objValue4;
        private static FloatValueSliderController sliderValue4;
        private static GameObject objVertices;
        private static IntValueSliderController sliderVertices;
        private static GameObject objInfo;
        private static Button infoBtn;
        private static GameObject objImage;
        private static Image infoImage;

        private static float value1;
        private static float value2;
        private static float value3;
        private static float value4;
        private static int vertices;
        private static bool infoVisibility;
        
        private void InitSwitchMenu()
        {
            drawableSwitch = PrefabInstantiator.InstantiatePrefab(drawableSwitchPrefab,
                                    GameObject.Find("UI Canvas").transform, false);
            drawableSwitch.AddComponent<MenuDestroyer>().SetAllowedState(GetActionStateType());

            shapeBtn = drawableSwitch.GetComponentsInChildren<Button>()[0];
            shapeBtn.onClick.AddListener(ShapeOnClick);
            configBtn = drawableSwitch.GetComponentsInChildren<Button>()[1];
            configBtn.onClick.AddListener(ConfigOnClick);
            shapeBtn.interactable = false;
        }
        private void InitShapeMenu()
        {
            shapeMenu = PrefabInstantiator.InstantiatePrefab(drawableShapePrefab,
                                                GameObject.Find("UI Canvas").transform, false);
            shapeMenu.AddComponent<MenuDestroyer>().SetAllowedState(GetActionStateType());

            GameObject shapeSelection = GameDrawableFinder.FindChild(shapeMenu, "ShapeSelection");
            previousBtn = shapeSelection.GetComponentsInChildren<Button>()[0];
            previousBtn.onClick.AddListener(PreviousShape);
            nextBtn = shapeSelection.GetComponentsInChildren<Button>()[1];
            nextBtn.onClick.AddListener(NextShape);
            shapeLabel = shapeMenu.GetComponentInChildren<TMP_Text>();

            objValue1 = GameDrawableFinder.FindChild(shapeMenu, "Value1");
            sliderValue1 = objValue1.GetComponent<FloatValueSliderController>();
            sliderValue1.onValueChanged.AddListener(value => { value1 = value; });

            objValue2 = GameDrawableFinder.FindChild(shapeMenu, "Value2");
            sliderValue2 = objValue2.GetComponent<FloatValueSliderController>();
            sliderValue2.onValueChanged.AddListener(value => { value2 = value; });

            objValue3 = GameDrawableFinder.FindChild(shapeMenu, "Value3");
            sliderValue3 = objValue3.GetComponent<FloatValueSliderController>();
            sliderValue3.onValueChanged.AddListener(value => { value3 = value; });

            objValue4 = GameDrawableFinder.FindChild(shapeMenu, "Value4");
            sliderValue4 = objValue4.GetComponent<FloatValueSliderController>();
            sliderValue4.onValueChanged.AddListener(value => { value4 = value; });

            objVertices = GameDrawableFinder.FindChild(shapeMenu, "Vertices");
            sliderVertices = objVertices.GetComponent<IntValueSliderController>();
            vertices = sliderVertices.GetValue();
            sliderVertices.onValueChanged.AddListener(value => { vertices = value; });

            objInfo = GameDrawableFinder.FindChild(shapeMenu, "Info");
            infoBtn = objInfo.GetComponentInChildren<Button>();
            infoVisibility = false;
            infoBtn.onClick.AddListener(ToggleInfo);

            objImage = GameDrawableFinder.FindChild(shapeMenu, "Image");
            infoImage = objImage.GetComponent<Image>();


            SetSelectedShape(GameShapesCalculator.GetShapes()[0]);
        }

        private void ToggleInfo()
        {
            infoVisibility = !infoVisibility;
            objImage.SetActive(infoVisibility);
            if (infoVisibility)
            {
                LoadImage();
            }
        }

        private void LoadImage()
        {
            string path = "";
            switch (selectedShape)
            {
                case GameShapesCalculator.Shapes.Square:
                    path = "Textures/Drawable/Test";
                    break;
                case GameShapesCalculator.Shapes.Rectangle:
                    path = "Textures/Drawable/Rectangle";
                    break;
                case GameShapesCalculator.Shapes.Rhombus:
                    path = "Textures/Drawable/Rhombus";
                    break;
                case GameShapesCalculator.Shapes.Kite:
                    path = "Textures/Drawable/Kite";
                    break;
                case GameShapesCalculator.Shapes.Triangle:
                    path = "Textures/Drawable/Triangle";
                    break;
                case GameShapesCalculator.Shapes.Circle:
                    path = "Textures/Drawable/Circle";
                    break;
                case GameShapesCalculator.Shapes.Ellipse:
                    path = "Textures/Drawable/Ellipse";
                    break;
                case GameShapesCalculator.Shapes.Parallelogram:
                    path = "Textures/Drawable/Parallelogram";
                    break;
                case GameShapesCalculator.Shapes.Trapezoid:
                    path = "Textures/Drawable/Trapezoid";
                    break;
                case GameShapesCalculator.Shapes.Polygon:
                    path = "Textures/Drawable/Polygon";
                    break;
            }
            infoImage.sprite = Resources.Load<Sprite>(path); ;
            Debug.Log(infoImage.sprite.name);
        }

        private void InitConfigMenu()
        {
            thicknessSlider = DrawableHelper.drawableMenu.GetComponentInChildren<ThicknessSliderController>();
            thicknessSlider.AssignValue(DrawableHelper.currentThickness);
            thicknessSlider.onValueChanged.AddListener(thickness =>
            {
                DrawableHelper.currentThickness = thickness;
            });

            picker = DrawableHelper.drawableMenu.GetComponent<HSVPicker.ColorPicker>();
            picker.AssignColor(DrawableHelper.currentColor);
            picker.onValueChanged.AddListener(DrawableHelper.colorAction = color =>
            {
                DrawableHelper.currentColor = color;
            });
        }

        public override void Awake()
        {
            if (firstStart)
            {
                InitSwitchMenu();
                InitShapeMenu();
                InitConfigMenu();
                firstStart = false; 
            } else
            {
                drawableSwitch.SetActive(true);
                if (!shapeBtn.interactable)
                {
                    shapeMenu.SetActive(true);
                } else
                {
                    shapeMenu.SetActive(false);
                    DrawableHelper.enableDrawableMenu(false, new DrawableHelper.MenuPoint[] {DrawableHelper.MenuPoint.Layer, DrawableHelper.MenuPoint.Loop});
                }
            }
        }

        private int GetIndexOfSelectedShape()
        {
            return GameShapesCalculator.GetShapes().IndexOf(selectedShape);
        }

        private void NextShape()
        {
            int index = GetIndexOfSelectedShape() + 1;
            if (index >= GameShapesCalculator.GetShapes().Count)
            {
                index = 0;
            }
            SetSelectedShape(GameShapesCalculator.GetShapes()[index]);

        }

        private void PreviousShape()
        {
            int index = GetIndexOfSelectedShape() - 1;
            if (index < 0)
            {
                index = GameShapesCalculator.GetShapes().Count - 1;
            }
            SetSelectedShape(GameShapesCalculator.GetShapes()[index]);
        }

        private void SetSelectedShape(GameShapesCalculator.Shapes shape)
        {
            selectedShape = shape;
            shapeLabel.text = shape.ToString();
            ChangeMenu();
        }

        private void AllValuesReset()
        {
            objValue1.SetActive(true);
            objValue2.SetActive(true);
            objValue3.SetActive(true);
            objValue4.SetActive(true);
            objVertices.SetActive(true);

            sliderValue1.ResetToMin();
            sliderValue2.ResetToMin();
            sliderValue3.ResetToMin();
            sliderValue4.ResetToMin();
            sliderVertices.ResetToMin();
            infoVisibility = false;
        }
        private void AllValuesDisable()
        {
            objValue1.SetActive(false);
            objValue2.SetActive(false);
            objValue3.SetActive(false);
            objValue4.SetActive(false);
            objVertices.SetActive(false);
            objInfo.SetActive(false);
            objImage.SetActive(false);
        }
        private void ChangeMenu()
        {
            AllValuesReset();
            AllValuesDisable();
            switch (selectedShape)
            {
                case GameShapesCalculator.Shapes.Line:
                    break;
                case GameShapesCalculator.Shapes.Square:
                    objValue1.SetActive(true);
                    objValue1.GetComponentsInChildren<TMP_Text>()[0].text = "a";
                    objInfo.SetActive(true);
                    break;
                case GameShapesCalculator.Shapes.Rectangle:
                    objValue1.SetActive(true);
                    objValue1.GetComponentsInChildren<TMP_Text>()[0].text = "a";
                    objValue2.SetActive(true);
                    objValue2.GetComponentsInChildren<TMP_Text>()[0].text = "b";
                    objInfo.SetActive(true);
                    break;
                case GameShapesCalculator.Shapes.Rhombus:
                    objValue1.SetActive(true);
                    objValue1.GetComponentsInChildren<TMP_Text>()[0].text = "f";
                    objValue2.SetActive(true);
                    objValue2.GetComponentsInChildren<TMP_Text>()[0].text = "e";
                    objInfo.SetActive(true);
                    break;
                case GameShapesCalculator.Shapes.Kite:
                    objValue1.SetActive(true);
                    objValue1.GetComponentsInChildren<TMP_Text>()[0].text = "f1";
                    objValue2.SetActive(true);
                    objValue2.GetComponentsInChildren<TMP_Text>()[0].text = "f2";
                    objValue3.SetActive(true);
                    objValue3.GetComponentsInChildren<TMP_Text>()[0].text = "e";
                    objInfo.SetActive(true);
                    break;
                case GameShapesCalculator.Shapes.Triangle:
                    objValue1.SetActive(true);
                    objValue1.GetComponentsInChildren<TMP_Text>()[0].text = "c";
                    objValue2.SetActive(true);
                    objValue2.GetComponentsInChildren<TMP_Text>()[0].text = "h";
                    objInfo.SetActive(true);
                    break;
                case GameShapesCalculator.Shapes.Circle:
                    objValue1.SetActive(true);
                    objValue1.GetComponentsInChildren<TMP_Text>()[0].text = "Radius";
                    objInfo.SetActive(true);
                    break;
                case GameShapesCalculator.Shapes.Ellipse:
                    objValue1.SetActive(true);
                    objValue1.GetComponentsInChildren<TMP_Text>()[0].text = "X-Scale";
                    objValue2.SetActive(true);
                    objValue2.GetComponentsInChildren<TMP_Text>()[0].text = "Y-Scale";
                    objInfo.SetActive(true);
                    break;
                case GameShapesCalculator.Shapes.Parallelogram:
                    objValue1.SetActive(true);
                    objValue1.GetComponentsInChildren<TMP_Text>()[0].text = "a";
                    objValue2.SetActive(true);
                    objValue2.GetComponentsInChildren<TMP_Text>()[0].text = "h";
                    objValue4.SetActive(true);
                    objValue4.GetComponentsInChildren<TMP_Text>()[0].text = "Shift";
                    objInfo.SetActive(true);
                    break;
                case GameShapesCalculator.Shapes.Trapezoid:
                    objValue1.SetActive(true);
                    objValue1.GetComponentsInChildren<TMP_Text>()[0].text = "a";
                    objValue2.SetActive(true);
                    objValue2.GetComponentsInChildren<TMP_Text>()[0].text = "c";
                    objValue3.SetActive(true);
                    objValue3.GetComponentsInChildren<TMP_Text>()[0].text = "h";
                    objInfo.SetActive(true);
                    break;
                case GameShapesCalculator.Shapes.Polygon:
                    objValue1.SetActive(true);
                    objValue1.GetComponentsInChildren<TMP_Text>()[0].text = "Length";
                    objVertices.SetActive(true);
                    objInfo.SetActive(true);
                    break;
            }
        }

        private void ConfigOnClick()
        {
            configBtn.interactable = false;
            shapeBtn.interactable = true;
            DrawableHelper.enableDrawableMenu(false, new DrawableHelper.MenuPoint[] { DrawableHelper.MenuPoint.Layer, DrawableHelper.MenuPoint.Loop });
            shapeMenu.SetActive(false);
        }

        private void ShapeOnClick()
        {
            shapeBtn.interactable = false;
            configBtn.interactable = true;
            DrawableHelper.disableDrawableMenu();
            shapeMenu.SetActive(true);
        }

        public override void Stop()
        {
            DrawableHelper.disableDrawableMenu();
        }

        public static void Reset()
        {
            firstStart = true;
            drawing = false;
            shape = null;
        }
        
        /// <summary>
        /// Continues the line at the point of the mouse position and draws it.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            bool result = false;

            if (!Raycasting.IsMouseOverGUI())
            {
                if (Input.GetMouseButtonDown(0) &&
                    Raycasting.RaycastAnything(out RaycastHit raycastHit) &&
                    (raycastHit.collider.gameObject.CompareTag(Tags.Drawable) ||
                    GameDrawableFinder.hasDrawableParent(raycastHit.collider.gameObject))
                    && !drawing)
                {
                    drawable = raycastHit.collider.gameObject.CompareTag(Tags.Drawable) ?
                        raycastHit.collider.gameObject : GameDrawableFinder.FindDrawableParent(raycastHit.collider.gameObject);
                    drawing = true;
                    Vector3 convertedHitPoint = GameDrawer.GetConvertedPosition(drawable, raycastHit.point);
                   
                    switch (selectedShape)
                    {
                        case GameShapesCalculator.Shapes.Line:

                            positions[0] = raycastHit.point;
                            shape = GameDrawer.StartDrawing(drawable, positions, DrawableHelper.currentColor, DrawableHelper.currentThickness);
                            positions[0] = shape.transform.InverseTransformPoint(positions[0]) - DrawableHelper.distanceToBoard;
                            shape.AddComponent<MenuDestroyer>().SetAllowedState(GetActionStateType());
                            break;
                        case GameShapesCalculator.Shapes.Square:
                            positions = GameShapesCalculator.Square(convertedHitPoint, value1);
                            break;
                        case GameShapesCalculator.Shapes.Rectangle:
                            positions = GameShapesCalculator.Rectanlge(convertedHitPoint, value1, value2);
                            break;
                        case GameShapesCalculator.Shapes.Rhombus:
                            positions = GameShapesCalculator.Rhombus(convertedHitPoint, value1, value2);
                            break;
                        case GameShapesCalculator.Shapes.Kite:
                            positions = GameShapesCalculator.Kite(convertedHitPoint, value1, value2, value3);
                            break;
                        case GameShapesCalculator.Shapes.Triangle:
                            positions = GameShapesCalculator.Triangle(convertedHitPoint, value1, value2);
                            break;
                        case GameShapesCalculator.Shapes.Circle:
                            positions = GameShapesCalculator.Circle(convertedHitPoint, value1);
                            break;
                        case GameShapesCalculator.Shapes.Ellipse:
                            positions = GameShapesCalculator.Ellipse(convertedHitPoint, value1, value2);
                            break;
                        case GameShapesCalculator.Shapes.Parallelogram:
                            positions = GameShapesCalculator.Parallelogram(convertedHitPoint, value1, value2, value4);
                            break;
                        case GameShapesCalculator.Shapes.Trapezoid:
                            positions = GameShapesCalculator.Trapezoid(convertedHitPoint, value1, value2, value3);
                            break;
                        case GameShapesCalculator.Shapes.Polygon:
                            positions = GameShapesCalculator.Polygon(convertedHitPoint, value1, vertices);
                            break;
                    }

                    if (selectedShape != GameShapesCalculator.Shapes.Line)
                    {
                        if (GameDrawer.DifferentPositionCounter(positions) > 1)
                        {
                            shape = GameDrawer.ReDrawRawLine(drawable, "", positions, DrawableHelper.currentColor, DrawableHelper.currentThickness, DrawableHelper.orderInLayer, true);
                            memento = new Memento(drawable, shape.name, positions, DrawableHelper.currentColor,
                               DrawableHelper.currentThickness, shape.GetComponent<LineRenderer>().sortingOrder);
                            memento.loop = true;
                            new DrawOnNetAction(memento.drawable.name, GameDrawableFinder.GetDrawableParentName(memento.drawable), memento.id,
                            memento.positions, memento.color, memento.thickness, memento.loop).Execute();
                            result = true;
                            currentState = ReversibleAction.Progress.Completed;
                            positions = new Vector3[1];
                            drawing = false;
                            shape = null;

                            return Input.GetMouseButtonDown(0);
                        } else
                        {
                            drawing = false;
                        }
                    }
                }
                if (drawing && !Input.GetMouseButton(0) && !Input.GetMouseButtonDown(0) &&
                    Raycasting.RaycastAnything(out RaycastHit rh) &&
                    (rh.collider.gameObject.CompareTag(Tags.Drawable) ||
                    GameDrawableFinder.hasDrawableParent(rh.collider.gameObject)) &&
                    selectedShape == GameShapesCalculator.Shapes.Line)
                {
                    Vector3 newPosition = shape.transform.InverseTransformPoint(rh.point) - DrawableHelper.distanceToBoard;
                    // Add newPosition to the line renderer.
                    Vector3[] newPositions = new Vector3[positions.Length + 1];
                    Array.Copy(sourceArray: positions, destinationArray: newPositions, length: positions.Length);
                    newPositions[newPositions.Length - 1] = newPosition;
                    GameDrawer.Drawing(shape ,newPositions);
                    new DrawOnNetAction(drawable.name, GameDrawableFinder.GetDrawableParentName(drawable),
                            shape.name, newPositions, DrawableHelper.currentColor, DrawableHelper.currentThickness, false).Execute();
                }

                if (Input.GetMouseButtonDown(0) &&
                    Raycasting.RaycastAnything(out RaycastHit hit) &&
                    (hit.collider.gameObject.CompareTag(Tags.Drawable) ||
                    GameDrawableFinder.hasDrawableParent(hit.collider.gameObject))
                    && drawing && selectedShape == GameShapesCalculator.Shapes.Line)
                {
                    Vector3 newPosition = shape.transform.InverseTransformPoint(hit.point) - DrawableHelper.distanceToBoard;
                    if (newPosition != positions.Last())
                    {
                        // Add newPosition to the line renderer.
                        Vector3[] newPositions = new Vector3[positions.Length + 1];
                        Array.Copy(sourceArray: positions, destinationArray: newPositions, length: positions.Length);
                        newPositions[newPositions.Length - 1] = newPosition;
                        positions = newPositions;

                        GameDrawer.Drawing(shape, positions);
                        memento = new Memento(drawable, shape.name, positions, DrawableHelper.currentColor,
                            DrawableHelper.currentThickness, shape.GetComponent<LineRenderer>().sortingOrder);
                        memento.loop = false;
                        new DrawOnNetAction(memento.drawable.name, GameDrawableFinder.GetDrawableParentName(memento.drawable),
                            memento.id, memento.positions, memento.color, memento.thickness, memento.loop).Execute();
                        currentState = ReversibleAction.Progress.InProgress;
                        if (shape.GetComponent<MenuDestroyer>() != null)
                        {
                            Destroyer.Destroy(shape.GetComponent<MenuDestroyer>());
                        }
                    }
                }

                if (Input.GetMouseButtonUp(1) && !Input.GetKey(KeyCode.LeftControl) && drawing && positions.Length > 1 && selectedShape == GameShapesCalculator.Shapes.Line)
                {
                    GameDrawer.Drawing(shape, positions);
                    memento.positions = positions;
                    memento.loop = false;
                    GameDrawer.FinishDrawing(shape, memento.loop);
                    new DrawOnNetAction(memento.drawable.name, GameDrawableFinder.GetDrawableParentName(memento.drawable), memento.id,
                        memento.positions, memento.color, memento.thickness, memento.loop).Execute();
                    result = true;
                    currentState = ReversibleAction.Progress.Completed;
                    positions = new Vector3[1];
                    drawing = false;
                    shape = null;

                    return Input.GetMouseButtonUp(1);
                }

                if (Input.GetMouseButtonUp(1) && Input.GetKey(KeyCode.LeftControl) && drawing && positions.Length > 1 && selectedShape == GameShapesCalculator.Shapes.Line)
                {
                    GameDrawer.Drawing(shape, positions);
                    memento.positions = positions;
                    memento.loop = true;
                    GameDrawer.FinishDrawing(shape, memento.loop);
                    new DrawOnNetAction(memento.drawable.name, GameDrawableFinder.GetDrawableParentName(memento.drawable), memento.id,
                        memento.positions, memento.color, memento.thickness, memento.loop).Execute();
                    result = true;
                    currentState = ReversibleAction.Progress.Completed;
                    positions = new Vector3[1];
                    drawing = false;
                    shape = null;

                    return Input.GetMouseButtonUp(1);
                }
            }
            return result;
        }

        private struct Memento
        {
            public readonly GameObject drawable;

            public Vector3[] positions;

            public readonly Color color;

            public readonly float thickness;

            public readonly int orderInLayer;

            public readonly string id;

            public bool loop;

            public Memento(GameObject drawable, string id, Vector3[] positions, Color color, float thickness, int orderInLayer)
            {
                this.drawable = drawable;
                this.positions = positions;
                this.color = color;
                this.thickness = thickness;
                this.orderInLayer = orderInLayer;
                this.id = id;
                this.loop = false;
            }
        }

        /// <summary>
        /// Destroys the drawn line.
        /// See <see cref="ReversibleAction.Undo()"/>.
        /// </summary>
        public override void Undo()
        {
            base.Undo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> properly.
            if (shape == null)
            {
                shape = GameDrawableFinder.FindChild(memento.drawable, memento.id);
            }
            if (shape != null)
            {
                new EraseNetAction(memento.drawable.name, GameDrawableFinder.GetDrawableParentName(memento.drawable), memento.id).Execute();
                Destroyer.Destroy(shape.transform.parent.gameObject);
                shape = null;
            }
        }

        /// <summary>
        /// Redraws the drawn line (setting up <see cref="line"/> and adds <see cref="renderer"/> 
        /// before that).
        /// See <see cref="ReversibleAction.Undo()"/>.
        /// </summary>
        public override void Redo()
        {
            base.Redo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> properly.
            shape = GameDrawer.ReDrawRawLine(memento.drawable, memento.id, memento.positions, memento.color,
                memento.thickness, memento.orderInLayer, memento.loop);
            if (shape != null)
            {
                new DrawOnNetAction(memento.drawable.name, GameDrawableFinder.GetDrawableParentName(memento.drawable),
                    memento.id, memento.positions, memento.color,
                    memento.thickness, memento.orderInLayer, memento.loop).Execute();
            }
        }

        /// <summary>
        /// A new instance of <see cref="DrawOnAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="DrawOnAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new DrawShapesAction();
        }

        /// <summary>
        /// A new instance of <see cref="DrawOnAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="DrawOnAction"/></returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.DrawOnWhiteboard"/></returns>
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
        /// <returns>an empty set</returns>
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
                    memento.drawable.name,
                    memento.id
                };
            }
        }
    }
}
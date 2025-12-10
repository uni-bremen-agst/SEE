using Michsky.UI.ModernUIPack;
using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.UI.Drawable;
using SEE.UI.Notification;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static SEE.Game.Drawable.ActionHelpers.ShapePointsCalculator;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// The class for the shape menu. It delivers an instance.
    /// Use ShapeMenu.Enable() and ShapeMenu.Disable()
    /// There are Getters for the necessary values:
    /// GetSelectedShape() for the selected shape type.
    /// GetValue1() - GetValue4(), GetVertices()
    /// </summary>
    public static class ShapeMenu
    {
        #region Variables
        /// <summary>
        /// The prefab for the switch that can open the shape menu and the config menu (line menu).
        /// </summary>
        private const string drawableSwitchPrefab = "Prefabs/UI/Drawable/ShapeSwitch";
        /// <summary>
        /// The prefab for the shape menu, it contains the shape type, the necessary values and a info box.
        /// </summary>
        private const string drawableShapePrefab = "Prefabs/UI/Drawable/ShapeMenu";
        /// <summary>
        /// The instance of the switch.
        /// </summary>
        private static GameObject drawableSwitch;
        /// <summary>
        /// The instance of the shape menu.
        /// </summary>
        private static GameObject shapeMenu;
        /// <summary>
        /// The selector for the shape kind.
        /// </summary>
        private static HorizontalSelector selector;
        /// <summary>
        /// The instance for the open shape menu button.
        /// </summary>
        private static Button shapeBtn;
        /// <summary>
        /// The instance for the open shape menu button manager.
        /// </summary>
        private static ButtonManagerBasic shapeBMB;
        /// <summary>
        /// The instance for the open config menu (line menu) button.
        /// </summary>
        private static Button configBtn;
        /// <summary>
        /// The instance for the open config menu button manager.
        /// </summary>
        private static ButtonManagerBasic configBMB;
        /// <summary>
        /// The instance for the layer of the value1.
        /// </summary>
        private static GameObject objValue1;
        /// <summary>
        /// The float value slider controller for value1.
        /// </summary>
        private static FloatValueSliderController sliderValue1;
        /// <summary>
        /// The instance for the layer of the value2.
        /// </summary>
        private static GameObject objValue2;
        /// <summary>
        /// The float value slider controller for value2.
        /// </summary>
        private static FloatValueSliderController sliderValue2;
        /// <summary>
        /// The instance for the layer of the value3.
        /// </summary>
        private static GameObject objValue3;
        /// <summary>
        /// The float value slider controller for value3.
        /// </summary>
        private static FloatValueSliderController sliderValue3;
        /// <summary>
        /// The instance for the layer of the value4.
        /// </summary>
        private static GameObject objValue4;
        /// <summary>
        /// The float value slider controller for value4.
        /// </summary>
        private static FloatValueSliderController sliderValue4;
        /// <summary>
        /// The instance for the layer of the vertices.
        /// </summary>
        private static GameObject objVertices;
        /// <summary>
        /// The float value slider controller for vertices.
        /// </summary>
        private static IntValueSliderController sliderVertices;
        /// <summary>
        /// The instance for the layer of the info box.
        /// </summary>
        private static GameObject objInfo;
        /// <summary>
        /// The instance for the information button. It can open or close the information box.
        /// </summary>
        private static ButtonManagerBasic infoBMB;
        /// <summary>
        /// The instance for the layer for the image.
        /// </summary>
        private static GameObject objImage;
        /// <summary>
        /// The instance of the image.
        /// </summary>
        private static Image infoImage;
        /// <summary>
        /// The instance for the layer for the finish button.
        /// </summary>
        private static GameObject objFinish;
        /// <summary>
        /// The instance of the finish button.
        /// </summary>
        private static ButtonManagerBasic finishBMB;
        /// <summary>
        /// The instance for the part undo button.
        /// </summary>
        private static GameObject objPartUndo;
        /// <summary>
        /// The manager of the part undo button.
        /// </summary>
        private static ButtonManagerBasic partUndoBMB;
        /// <summary>
        /// The instance for the layer for the switch.
        /// </summary>
        private static GameObject objLoop;
        /// <summary>
        /// The instance of the loop manager.
        /// </summary>
        private static SwitchManager loopManager;
        /// <summary>
        /// The instance for the layer of the dragger info button.
        /// </summary>
        private static GameObject draggerInfoObj;
        /// <summary>
        /// The instance for the dragger info button.
        /// </summary>
        private static ButtonManagerBasic draggerInfoBMB;
        #endregion

        /// The following block are the value holders for the chosen values:
        #region ValueHolders
        /// <summary>
        /// Contains the current selected shape type.
        /// </summary>
        private static Shape selectedShape;
        /// <summary>
        /// Contains the current chosen value1 value.
        /// </summary>
        private static float value1;
        /// <summary>
        /// Contains the current chosen value2 value.
        /// </summary>
        private static float value2;
        /// <summary>
        /// Contains the current chosen value3 value.
        /// </summary>
        private static float value3;
        /// <summary>
        /// Contains the current chosen value4 value.
        /// </summary>
        private static float value4;
        /// <summary>
        /// Contains the current chosen vertices value.
        /// </summary>
        private static int vertices;
        /// <summary>
        /// Is the visibility of the information box.
        /// </summary>
        private static bool infoVisibility;
        #endregion

        /// <summary>
        /// The inital constructor of the shape menu.
        /// It calls the init methods for the three menu parts.
        /// </summary>
        static ShapeMenu()
        {
            InitSwitchMenu();
            InitShapeMenu();
            InitConfigMenu();
        }

        #region Getters
        /// <summary>
        /// Gets the current selected shape type
        /// </summary>
        /// <returns>the selected shape type</returns>
        public static Shape GetSelectedShape()
        {
            return selectedShape;
        }

        /// <summary>
        /// Gets the value of value1
        /// </summary>
        /// <returns>value1</returns>
        public static float GetValue1() { return value1; }

        /// <summary>
        /// Gets the value of value2
        /// </summary>
        /// <returns>value2</returns>
        public static float GetValue2() { return value2; }

        /// <summary>
        /// Gets the value of value3
        /// </summary>
        /// <returns>value3</returns>
        public static float GetValue3() { return value3; }

        /// <summary>
        /// Gets the value of value4
        /// </summary>
        /// <returns>value4</returns>
        public static float GetValue4() { return value4; }

        /// <summary>
        /// Gets the value of vertices
        /// </summary>
        /// <returns>vertices</returns>
        public static int GetVertices() { return vertices; }
        #endregion

        /// <summary>
        /// Enables the switch menu with the shape menu and the config menu (line menu).
        /// It binds the shape and config menu to the switch menu.
        /// </summary>
        public static void Enable()
        {
            drawableSwitch.SetActive(true);
            if (!shapeBtn.interactable)
            {
                shapeMenu.SetActive(true);
                BindShapeMenu();
            }
            else
            {
                shapeMenu.SetActive(false);
                LineMenu.Instance.EnableForDrawing();
                BindLineMenu();
            }
        }

        /// <summary>
        /// Disables the menus.
        /// </summary>
        public static void Disable()
        {
            DisablePartUndo();
            shapeMenu.SetActive(false);
            LineMenu.Instance.Disable();
            drawableSwitch.SetActive(false);
        }

        /// <summary>
        /// Initializes the switch menu. It adds the handlers for the shape menu and for the config menu.
        /// By default, the shape menu is selected.
        /// </summary>
        private static void InitSwitchMenu()
        {
            /// Instantiate the switch menu.
            drawableSwitch = PrefabInstantiator.InstantiatePrefab(drawableSwitchPrefab,
                                                                  UICanvas.Canvas.transform, false);

            /// Initialize the button for calling the shape menu.
            shapeBtn = drawableSwitch.GetComponentsInChildren<Button>()[0];
            shapeBMB = drawableSwitch.GetComponentsInChildren<ButtonManagerBasic>()[0];
            shapeBMB.clickEvent.AddListener(ShapeOnClick);

            /// Initialize the button for calling the config menu.
            configBtn = drawableSwitch.GetComponentsInChildren<Button>()[1];
            configBMB = drawableSwitch.GetComponentsInChildren<ButtonManagerBasic>()[1];
            configBMB.clickEvent.AddListener(ConfigOnClick);
            shapeBtn.interactable = false;
            shapeBMB.enabled = false;
        }
        /// <summary>
        /// Initializes the shape menu.
        /// It adds the necessary handlers to the components and sets the selected shape to line.
        /// </summary>
        private static void InitShapeMenu()
        {
            /// Instantiate the shape menu.
            shapeMenu = PrefabInstantiator.InstantiatePrefab(drawableShapePrefab,
                                                             UICanvas.Canvas.transform, false);

            /// Initialize a selector for the shape kind.
            selector = shapeMenu.GetComponentInChildren<HorizontalSelector>();

            /// Creates an item for every shape.
            foreach (Shape shape in GetShapes())
            {
                selector.CreateNewItem(shape.ToString());
            }
            /// Sets the selected shape to the menu.
            selector.selectorEvent.AddListener(index =>
            {
                SetSelectedShape(GetShapes()[index]);
            });
            selector.defaultIndex = 0;

            /// Initialize the different values for the shape calculation:
            objValue1 = GameFinder.FindChild(shapeMenu, "Value1");
            sliderValue1 = objValue1.GetComponent<FloatValueSliderController>();
            sliderValue1.onValueChanged.AddListener(value => { value1 = value; });

            objValue2 = GameFinder.FindChild(shapeMenu, "Value2");
            sliderValue2 = objValue2.GetComponent<FloatValueSliderController>();
            sliderValue2.onValueChanged.AddListener(value => { value2 = value; });

            objValue3 = GameFinder.FindChild(shapeMenu, "Value3");
            sliderValue3 = objValue3.GetComponent<FloatValueSliderController>();
            sliderValue3.onValueChanged.AddListener(value => { value3 = value; });

            objValue4 = GameFinder.FindChild(shapeMenu, "Value4");
            sliderValue4 = objValue4.GetComponent<FloatValueSliderController>();
            sliderValue4.onValueChanged.AddListener(value => { value4 = value; });

            objVertices = GameFinder.FindChild(shapeMenu, "Vertices");
            sliderVertices = objVertices.GetComponent<IntValueSliderController>();
            vertices = sliderVertices.GetValue();
            sliderVertices.OnValueChanged.AddListener(value => { vertices = value; });

            /// Initialize the shape info.
            objInfo = GameFinder.FindChild(shapeMenu, "InfoPlaceHolder");
            infoBMB = objInfo.GetComponentInChildren<ButtonManagerBasic>();
            infoVisibility = false;
            infoBMB.clickEvent.AddListener(ToggleInfo);

            /// Initialize the shape info image.
            objImage = GameFinder.FindChild(shapeMenu, "Image");
            infoImage = objImage.GetComponent<Image>();

            /// Initialize the shape loop option. Only available for <see cref="Shape.Line"/>.
            objLoop = GameFinder.FindChild(shapeMenu, "Loop");
            loopManager = objLoop.GetComponentInChildren<SwitchManager>();

            /// Initialize the finish button. Also only for <see cref="Shape.Line"/>.
            objFinish = GameFinder.FindChild(shapeMenu, "FinishBtn");
            finishBMB = objFinish.GetComponent<ButtonManagerBasic>();

            /// Initialize the part undo button. Also only for <see cref="Shape.Line"/>.
            objPartUndo = GameFinder.FindChild(shapeMenu, "PartUndoBtn");
            partUndoBMB = objPartUndo.GetComponent<ButtonManagerBasic>();
            objPartUndo.AddComponent<UIHoverTooltip>().SetMessage("Part Undo");
            objPartUndo.SetActive(false);

            /// Initialize the dragger info button.
            draggerInfoObj = GameFinder.FindChild(shapeMenu, "DraggerInfo");
            draggerInfoBMB = draggerInfoObj.GetComponent<ButtonManagerBasic>();
            draggerInfoBMB.clickEvent.AddListener(() =>
            {
                if (selectedShape == Shape.Line)
                {
                    ShowNotification.Info("Control instructions",
                        "Left mouse button = Adds a point to the line.\n"
                        + "Middle mouse button / mouse wheel click = Ends drawing the line without adding an additional point.\n"
                        + "Left Ctrl key + left mouse button = Ends drawing and adds a final point.");
                } else
                {
                    ShowNotification.Info("Control instructions",
                        "Middle mouse button / mouse wheel click = Fixes a point for the shape preview.\n"
                        + "Left Ctrl key + middle mouse button = Releases the fixed point.");
                }
            });

            /// Sets the initial selected shape.
            SetSelectedShape(GetShapes()[0]);
        }

        /// <summary>
        /// Action for the information button.
        /// It toggles the visibility of the information box.
        /// </summary>
        private static void ToggleInfo()
        {
            infoVisibility = !infoVisibility;
            objImage.SetActive(infoVisibility);
            if (infoVisibility)
            {
                /// Loads the image of the selected shape.
                LoadImage();
            }
            /// Re-calculate the shape menu height.
            MenuHelper.CalculateHeight(shapeMenu);
        }

        /// <summary>
        /// Activates the visibility of the part undo button and
        /// assigns an action to it.
        /// </summary>
        /// <param name="action">The action that should be assigned</param>
        public static void ActivatePartUndo(UnityAction action)
        {
            objPartUndo.SetActive(true);
            partUndoBMB.clickEvent.RemoveAllListeners();
            partUndoBMB.clickEvent.AddListener(action);
        }

        /// <summary>
        /// Disables the visibility of the part undo button.
        /// </summary>
        public static void DisablePartUndo()
        {
            objPartUndo.SetActive(false);
        }

        /// <summary>
        /// Loads the image of the selected shape into the information image.
        /// </summary>
        private static void LoadImage()
        {
            string path = "";
            switch (selectedShape)
            {
                case Shape.Square:
                    path = "Textures/Drawable/Square";
                    break;
                case Shape.Rectangle:
                    path = "Textures/Drawable/Rectangle";
                    break;
                case Shape.Rhombus:
                    path = "Textures/Drawable/Rhombus";
                    break;
                case Shape.Kite:
                    path = "Textures/Drawable/Kite";
                    break;
                case Shape.Triangle:
                    path = "Textures/Drawable/Triangle";
                    break;
                case Shape.Circle:
                    path = "Textures/Drawable/Circle";
                    break;
                case Shape.Ellipse:
                    path = "Textures/Drawable/Ellipse";
                    break;
                case Shape.Parallelogram:
                    path = "Textures/Drawable/Parallelogram";
                    break;
                case Shape.Trapezoid:
                    path = "Textures/Drawable/Trapezoid";
                    break;
                case Shape.Polygon:
                    path = "Textures/Drawable/Polygon";
                    break;
            }
            infoImage.sprite = Resources.Load<Sprite>(path);
        }

        /// <summary>
        /// Initializes the config menu.
        /// It adds the necessary handlers to the components.
        /// </summary>
        private static void InitConfigMenu()
        {
            LineMenu.Instance.EnableForDrawing();
            LineMenu.Instance.Enable();
        }

        /// <summary>
        /// Sets the selected shape type.
        /// The name will be displayed in the shape label.
        /// </summary>
        /// <param name="shape">The selected shape type.</param>
        private static void SetSelectedShape(Shape shape)
        {
            selectedShape = shape;
            ChangeMenu();
        }

        /// <summary>
        /// Resets all the values for the shapes to their minimum.
        /// </summary>
        private static void AllValuesReset()
        {
            /// Ensures that all objects are active.
            objValue1.SetActive(true);
            objValue2.SetActive(true);
            objValue3.SetActive(true);
            objValue4.SetActive(true);
            objVertices.SetActive(true);
            objLoop.SetActive(true);
            objFinish.SetActive(true);

            sliderValue1.ResetToMin();
            sliderValue2.ResetToMin();
            sliderValue3.ResetToMin();
            sliderValue4.ResetToMin();
            sliderVertices.ResetToMin();
            loopManager.isOn = false;
            infoVisibility = false;
        }
        /// <summary>
        /// Disables all the values.
        /// </summary>
        private static void AllValuesDisable()
        {
            objValue1.SetActive(false);
            objValue2.SetActive(false);
            objValue3.SetActive(false);
            objValue4.SetActive(false);
            objVertices.SetActive(false);
            objInfo.SetActive(false);
            objImage.SetActive(false);
            objFinish.SetActive(false);
            objLoop.SetActive(false);
        }
        /// <summary>
        /// Changes the menu for the selected shape.
        /// It displays only the necessary values for the selected shape.
        /// The values are renamed to match the shape appropriately,
        /// so that the values correspond to the explanations in the images of the information boxes.
        /// </summary>
        private static void ChangeMenu()
        {
            /// Resets the values.
            AllValuesReset();
            /// Disables all values.
            AllValuesDisable();
            /// Enables the values necessary for the shape.
            /// And names the values according to the shape.
            switch (selectedShape)
            {
                case Shape.Line:
                    objFinish.SetActive(true);
                    objLoop.SetActive(true);
                    break;
                case Shape.Square:
                    objValue1.SetActive(true);
                    objValue1.GetComponentsInChildren<TMP_Text>()[0].text = "a";
                    objInfo.SetActive(true);
                    break;
                case Shape.Rectangle:
                    objValue1.SetActive(true);
                    objValue1.GetComponentsInChildren<TMP_Text>()[0].text = "a";
                    objValue2.SetActive(true);
                    objValue2.GetComponentsInChildren<TMP_Text>()[0].text = "b";
                    objInfo.SetActive(true);
                    break;
                case Shape.Rhombus:
                    objValue1.SetActive(true);
                    objValue1.GetComponentsInChildren<TMP_Text>()[0].text = "f";
                    objValue2.SetActive(true);
                    objValue2.GetComponentsInChildren<TMP_Text>()[0].text = "e";
                    objInfo.SetActive(true);
                    break;
                case Shape.Kite:
                    objValue1.SetActive(true);
                    objValue1.GetComponentsInChildren<TMP_Text>()[0].text = "f1";
                    objValue2.SetActive(true);
                    objValue2.GetComponentsInChildren<TMP_Text>()[0].text = "f2";
                    objValue3.SetActive(true);
                    objValue3.GetComponentsInChildren<TMP_Text>()[0].text = "e";
                    objInfo.SetActive(true);
                    break;
                case Shape.Triangle:
                    objValue1.SetActive(true);
                    objValue1.GetComponentsInChildren<TMP_Text>()[0].text = "c";
                    objValue2.SetActive(true);
                    objValue2.GetComponentsInChildren<TMP_Text>()[0].text = "h";
                    objInfo.SetActive(true);
                    break;
                case Shape.Circle:
                    objValue1.SetActive(true);
                    objValue1.GetComponentsInChildren<TMP_Text>()[0].text = "Radius";
                    objInfo.SetActive(true);
                    break;
                case Shape.Ellipse:
                    objValue1.SetActive(true);
                    objValue1.GetComponentsInChildren<TMP_Text>()[0].text = "X-Scale";
                    objValue2.SetActive(true);
                    objValue2.GetComponentsInChildren<TMP_Text>()[0].text = "Y-Scale";
                    objInfo.SetActive(true);
                    break;
                case Shape.Parallelogram:
                    objValue1.SetActive(true);
                    objValue1.GetComponentsInChildren<TMP_Text>()[0].text = "a";
                    objValue2.SetActive(true);
                    objValue2.GetComponentsInChildren<TMP_Text>()[0].text = "h";
                    objValue4.SetActive(true);
                    objValue4.GetComponentsInChildren<TMP_Text>()[0].text = "Shift";
                    objInfo.SetActive(true);
                    break;
                case Shape.Trapezoid:
                    objValue1.SetActive(true);
                    objValue1.GetComponentsInChildren<TMP_Text>()[0].text = "a";
                    objValue2.SetActive(true);
                    objValue2.GetComponentsInChildren<TMP_Text>()[0].text = "c";
                    objValue3.SetActive(true);
                    objValue3.GetComponentsInChildren<TMP_Text>()[0].text = "h";
                    objInfo.SetActive(true);
                    break;
                case Shape.Polygon:
                    objValue1.SetActive(true);
                    objValue1.GetComponentsInChildren<TMP_Text>()[0].text = "Length";
                    objVertices.SetActive(true);
                    objInfo.SetActive(true);
                    break;
            }
            /// Re-calculate the shape menu height.
            MenuHelper.CalculateHeight(shapeMenu);
        }

        /// <summary>
        /// Opens the <see cref="LineMenu"/> in the correct mode.
        /// </summary>
        public static void OpenLineMenuInCorrectMode()
        {
            ConfigOnClick();
        }

        /// <summary>
        /// Enables the config menu (line menu) and ensures that the menus (shape and config)
        /// are mutually exclusive.
        /// </summary>
        private static void ConfigOnClick()
        {
            configBtn.interactable = false;
            configBMB.enabled = false;
            shapeBMB.enabled = true;
            shapeBtn.interactable = true;
            if (DrawShapesAction.currentShape == null)
            {
                LineMenu.Instance.EnableForDrawing();
            }
            else
            {
                LineMenu.Instance.EnableForEditing(DrawShapesAction.currentShape,
                    LineConf.Get(DrawShapesAction.currentShape));
            }
            MenuHelper.CalculateHeight(LineMenu.Instance.GameObject);
            /// Binds the config menu to the switch menu.
            BindLineMenu();
            shapeMenu.SetActive(false);
        }

        /// <summary>
        /// Binds the line menu on the shape switch.
        /// </summary>
        private static void BindLineMenu()
        {
            LineMenu.Instance.GameObject.transform.SetParent(drawableSwitch.transform.Find("Content"));
            GameFinder.FindChild(LineMenu.Instance.GameObject, "Dragger").GetComponent<WindowDragger>().enabled = false;
        }

        /// <summary>
        /// Binds the shape menu on the shape switch.
        /// </summary>
        private static void BindShapeMenu()
        {
            shapeMenu.transform.SetParent(drawableSwitch.transform.Find("Content"));
            GameFinder.FindChild(shapeMenu, "Dragger").GetComponent<WindowDragger>().enabled = false;
        }

        /// <summary>
        /// Enables the shape and ensures that the menus (shape and config) are mutually exclusive.
        /// </summary>
        private static void ShapeOnClick()
        {
            shapeBtn.interactable = false;
            shapeBMB.enabled = false;
            configBtn.interactable = true;
            configBMB.enabled = true;
            LineMenu.Instance.Disable();
            BindShapeMenu();
            shapeMenu.SetActive(true);
        }

        /// <summary>
        /// Assigns an action to the finish button.
        /// </summary>
        /// <param name="action">The action that should be assigned</param>
        public static void AssignFinishButton(UnityAction action)
        {
            finishBMB.clickEvent.RemoveAllListeners();
            finishBMB.clickEvent.AddListener(action);
        }

        /// <summary>
        /// Retruns the switch manager for the loop attribut.
        /// </summary>
        /// <returns>the switch manager for the loop.</returns>
        public static SwitchManager GetLoopManager()
        {
            return loopManager;
        }
    }
}

using Assets.SEE.Game.Drawable;
using Michsky.UI.ModernUIPack;
using SEE.Utils;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Assets.SEE.Game.Drawable.GameShapesCalculator;

namespace Assets.SEE.Game.UI.Drawable
{
    /// <summary>
    /// The class for the shape menu. It delievers a instance.
    /// Use ShapeMenu.Enable() and ShapeMenu.Disable()
    /// There are Getters for the necressary values:
    /// GetSelectedShape() for the selected shape type.
    /// GetValue1() - GetValue4(), GetVertices()
    /// </summary>
    public static class ShapeMenu
    {
        /// <summary>
        /// The prefab for the switch that can open the shape menu and the config menu (line menu).
        /// </summary>
        private const string drawableSwitchPrefab = "Prefabs/UI/Drawable/Switch";
        /// <summary>
        /// The prefab for the shape menu, it contains the shape type, the necressary values and a info box.
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


        /// The following block are the value holders for the chosen values:

        /// <summary>
        /// Contains the current selected shape type.
        /// </summary>
        private static GameShapesCalculator.Shapes selectedShape;
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

        /// <summary>
        /// Gets the current selected shape type
        /// </summary>
        /// <returns>the selected shape type</returns>
        public static GameShapesCalculator.Shapes GetSelectedShape()
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

        /// <summary>
        /// Enables the switch with the shape menu and the config menu (line menu)
        /// </summary>
        public static void Enable()
        {
            drawableSwitch.SetActive(true);
            if (!shapeBtn.interactable)
            {
                shapeMenu.SetActive(true);
            }
            else
            {
                shapeMenu.SetActive(false);
                LineMenu.enableLineMenu(false, new LineMenu.MenuLayer[] { LineMenu.MenuLayer.Layer, LineMenu.MenuLayer.Loop });
            }
        }

        /// <summary>
        /// Disables the menu's.
        /// </summary>
        public static void Disable()
        {
            shapeMenu.SetActive(false);
            LineMenu.disableLineMenu();
            drawableSwitch.SetActive(false);
        }

        /// <summary>
        /// Init the switch menu. It adds the Handler for the shape menu and for the config menu.
        /// By default, the shape menu is selected.
        /// </summary>
        private static void InitSwitchMenu()
        {
            drawableSwitch = PrefabInstantiator.InstantiatePrefab(drawableSwitchPrefab,
                                    GameObject.Find("UI Canvas").transform, false);

            shapeBtn = drawableSwitch.GetComponentsInChildren<Button>()[0];
            shapeBMB = drawableSwitch.GetComponentsInChildren<ButtonManagerBasic>()[0];
            shapeBMB.clickEvent.AddListener(ShapeOnClick);
            
            configBtn = drawableSwitch.GetComponentsInChildren<Button>()[1];
            configBMB = drawableSwitch.GetComponentsInChildren<ButtonManagerBasic>()[1];
            configBMB.clickEvent.AddListener(ConfigOnClick);
            shapeBtn.interactable = false;
            shapeBMB.enabled = false;
        }
        /// <summary>
        /// Init the shape menu.
        /// It adds the necessary Handler to the components and sets the selected shape to line.
        /// </summary>
        private static void InitShapeMenu()
        {
            shapeMenu = PrefabInstantiator.InstantiatePrefab(drawableShapePrefab,
                                                GameObject.Find("UI Canvas").transform, false);
            selector = shapeMenu.GetComponentInChildren<HorizontalSelector>();
            foreach (Shapes shape in GameShapesCalculator.GetShapes())
            {
                selector.CreateNewItem(shape.ToString());
            }
            selector.selectorEvent.AddListener(index =>
            {
                SetSelectedShape(GameShapesCalculator.GetShapes()[index]);
            });
            selector.defaultIndex = 0;

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
            infoBMB = objInfo.GetComponent<ButtonManagerBasic>();
            infoVisibility = false;
            infoBMB.clickEvent.AddListener(ToggleInfo);

            objImage = GameDrawableFinder.FindChild(shapeMenu, "Image");
            infoImage = objImage.GetComponent<Image>();

            SetSelectedShape(GameShapesCalculator.GetShapes()[0]);
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
                LoadImage();
            }
        }

        /// <summary>
        /// This method loads the image of the selected shape into the information image.
        /// </summary>
        private static void LoadImage()
        {
            string path = "";
            switch (selectedShape)
            {
                case GameShapesCalculator.Shapes.Square:
                    path = "Textures/Drawable/Square";
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
            infoImage.sprite = Resources.Load<Sprite>(path);
        }

        /// <summary>
        /// Init the config menu. 
        /// It adds the necressary Handler to the components.
        /// </summary>
        private static void InitConfigMenu()
        {
            LineMenu.InitDrawing();
        }

        /// <summary>
        /// This method sets the selected shape type.
        /// The name will be displayed in the shape label.
        /// </summary>
        /// <param name="shape">The selected shape type.</param>
        private static void SetSelectedShape(GameShapesCalculator.Shapes shape)
        {
            selectedShape = shape;
            ChangeMenu();
        }

        /// <summary>
        /// Resets all the values for the shape's to their minimum.
        /// </summary>
        private static void AllValuesReset()
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
        }
        /// <summary>
        /// Changes the menu for the selected shape.
        /// It displayes only the necressary values for the selected shape.
        /// The values are renamed to match the shape appropriately, 
        /// so that the values correspond to the explanations in the images of the information boxes.
        /// </summary>
        private static void ChangeMenu()
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

        /// <summary>
        /// Enables the config menu (line menu) and it ensures that the menus (shape and config) are mutually exclusive.
        /// </summary>
        private static void ConfigOnClick()
        {
            configBtn.interactable = false;
            configBMB.enabled = false;
            shapeBMB.enabled = true;
            shapeBtn.interactable = true;
            LineMenu.enableLineMenu(false, new LineMenu.MenuLayer[] { LineMenu.MenuLayer.Layer, LineMenu.MenuLayer.Loop });
            shapeMenu.SetActive(false);
        }

        /// <summary>
        /// Enables the shape and it ensures that the menus (shape and config) are mutually exclusive.
        /// </summary>
        private static void ShapeOnClick()
        {
            shapeBtn.interactable = false;
            shapeBMB.enabled = false;
            configBtn.interactable = true;
            configBMB.enabled = true;
            LineMenu.disableLineMenu();
            shapeMenu.SetActive(true);
        }
    }
}
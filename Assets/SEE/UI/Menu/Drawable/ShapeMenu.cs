using Michsky.UI.ModernUIPack;
using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using SEE.Game.Drawable.ActionHelpers;
using SEE.Game.Drawable.Configurations;
using SEE.UI.Drawable;
using SEE.UI.Notification;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static SEE.Game.Drawable.ActionHelpers.LineCapPointsCalculator;
using static SEE.Game.Drawable.ActionHelpers.ShapePointsCalculator;
using static SEE.Game.Drawable.ActionHelpers.UMLShapePointsCalculator;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// The class for the shape menu. It delivers an instance.
    /// Use ShapeMenu.Enable() and ShapeMenu.Disable()
    /// There are Getters for the necessary values:
    /// GetSelectedShape() for the selected shape type.
    /// GetSelectedUMLShape() for the selected UML shape type.
    /// GetValue1() - GetValue4(), GetOffset() GetVertices()
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
        /// The selector for the UML shapes.
        /// </summary>
        private static HorizontalSelector umlShapeSelector;
        /// <summary>
        /// The instance for the layer of the UML shape selector.
        /// </summary>
        private static GameObject objUMLShapeSelector;
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
        /// The float value slider controller for value3.
        /// </summary>
        private static FloatValueSliderController sliderValue4;
        /// <summary>
        /// The instance for the layer of angle1.
        /// </summary>
        private static GameObject objAngle1;
        /// <summary>
        /// The float value slider controller for angle1.
        /// </summary>
        private static FloatValueSliderController sliderAngle1;
        /// <summary>
        /// The instance for the layer of angle2.
        /// </summary>
        private static GameObject objAngle2;
        /// <summary>
        /// The float value slider controller for angle2.
        /// </summary>
        private static FloatValueSliderController sliderAngle2;
        /// <summary>
        /// The instance for the layer of the offset.
        /// </summary>
        private static GameObject objOffset;
        /// <summary>
        /// The float value slider controller for offset.
        /// </summary>
        private static FloatValueSliderController sliderOffset;
        /// <summary>
        /// The instance for the layer of the vertices.
        /// </summary>
        private static GameObject objVertices;
        /// <summary>
        /// The instance for the layer for the bool switch.
        /// </summary>
        private static GameObject objBoolValue;
        /// <summary>
        /// The instance of the bool value manager.
        /// </summary>
        private static SwitchManager boolValueManager;
        /// <summary>
        /// The float value slider controller for vertices.
        /// </summary>
        private static IntValueSliderController sliderVertices;
        /// <summary>
        /// The selector for the orientation.
        /// </summary>
        private static HorizontalSelector orientationSelector;
        /// <summary>
        /// The instance for the layer of the orientation selector.
        /// </summary>
        private static GameObject objOrientation;
        /// <summary>
        /// The instance of the orientation text.
        /// </summary>
        private static GameObject objOrientationText;
        /// <summary>
        /// The instance for the layer of the info box.
        /// </summary>
        private static GameObject objInfo;
        /// <summary>
        /// The selector for the line start cap.
        /// </summary>
        private static HorizontalSelector lineStartSelector;
        /// <summary>
        /// The instance of the line start cap selector.
        /// </summary>
        private static GameObject objLineStart;
        /// <summary>
        /// The instance of the line start cap text.
        /// </summary>
        private static GameObject objLineStartText;
        /// <summary>
        /// The selector for the line end cap.
        /// </summary>
        private static HorizontalSelector lineEndSelector;
        /// <summary>
        /// The instance of the line end cap selector.
        /// </summary>
        private static GameObject objLineEnd;
        /// <summary>
        /// The instance of the line end cap text.
        /// </summary>
        private static GameObject objLineEndText;
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
        /// Contains the current selected UML shape type (only relevant if <see cref="selectedShape"/> is <see cref="Shape.UML"/>).
        /// </summary>
        private static UMLShape selectedUMLShape;
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
        /// Contains the current chosen angle1 value.
        /// </summary>
        private static float angle1;
        /// <summary>
        /// Contains the current chosen angle2 value.
        /// </summary>
        private static float angle2;
        /// <summary>
        /// Contains the current chosen offset value.
        /// </summary>
        private static float offset;
        /// <summary>
        /// Contains the current chosen vertices value.
        /// </summary>
        private static int vertices;
        /// <summary>
        /// Contains the current chosen <see cref="Orientation"/> value.
        /// </summary>
        public static Orientation orientation;
        /// <summary>
        /// The currently selected start line-cap configuration.
        /// </summary>
        private static LineCapConf lineStartCapConf;
        /// <summary>
        /// The currently selected end line-cap configuration.
        /// </summary>
        private static LineCapConf lineEndCapConf;
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
        /// <returns>The selected shape type.</returns>
        public static Shape GetSelectedShape()
        {
            return selectedShape;
        }

        /// <summary>
        /// Gets the current selected UML shape type
        /// </summary>
        /// <returns>The selected UML shape type.</returns>
        public static UMLShape GetSelectedUMLShape()
        {
            return selectedUMLShape;
        }

        /// <summary>
        /// Gets the value of value1
        /// </summary>
        /// <returns>Value1.</returns>
        public static float GetValue1() { return value1; }

        /// <summary>
        /// Gets the value of value2
        /// </summary>
        /// <returns>Value2.</returns>
        public static float GetValue2() { return value2; }

        /// <summary>
        /// Gets the value of value3
        /// </summary>
        /// <returns>Value3.</returns>
        public static float GetValue3() { return value3; }

        /// <summary>
        /// Gets the value of value4
        /// </summary>
        /// <returns>Value3.</returns>
        public static float GetValue4() { return value4; }

        /// <summary>
        /// Gets the value of angle1.
        /// </summary>
        /// <returns>Angle1.</returns>
        public static float GetAngle1() { return angle1; }

        /// <summary>
        /// Gets the value of angle2
        /// </summary>
        /// <returns>Angle2.</returns>
        public static float GetAngle2() { return angle2; }

        /// <summary>
        /// Gets the value of offset
        /// </summary>
        /// <returns>Value4.</returns>
        public static float GetOffset() { return offset; }

        /// <summary>
        /// Gets the value of vertices
        /// </summary>
        /// <returns>Vertices.</returns>
        public static int GetVertices() { return vertices; }

        /// <summary>
        /// Gets the value of <see cref="boolValueManager"/>.
        /// </summary>
        /// <returns>True if the toggle is enabled; otherwise, false.</returns>
        public static bool GetBoolValue() { return boolValueManager.isOn; }

        /// <summary>
        /// Gets the currently selected orientation for the shape.
        /// </summary>
        /// <returns>The selected <see cref="Orientation"/>.</returns>
        public static Orientation GetOrientation() { return orientation; }

        /// <summary>
        /// Gets the currently selected start line-cap configuration.
        /// </summary>
        /// <returns>The selected start line-cap configuration.</returns>
        public static LineCapConf GetLineStartCapConf()
        {
            return lineStartCapConf != null
                ? (LineCapConf)lineStartCapConf.Clone()
                : LineCapConf.CreateNone();
        }

        /// <summary>
        /// Gets the currently selected end line-cap configuration.
        /// </summary>
        /// <returns>The selected end line-cap configuration.</returns>
        public static LineCapConf GetLineEndCapConf()
        {
            return lineEndCapConf != null
                ? (LineCapConf)lineEndCapConf.Clone()
                : LineCapConf.CreateNone();
        }

        /// <summary>
        /// Gets the currently selected line cap for the start of the line.
        /// </summary>
        /// <returns>The selected <see cref="LineCap"/> for the line start.</returns>
        public static LineCap GetLineStartCap() { return lineStartCapConf?.CapKind ?? LineCap.None; }

        /// <summary>
        /// Gets the currently selected line cap for the end of the line.
        /// </summary>
        /// <returns>The selected <see cref="LineCap"/> for the line end.</returns>
        public static LineCap GetLineEndCap() { return lineEndCapConf?.CapKind ?? LineCap.None; }

        /// <summary>
        /// Retruns the switch manager for the loop attribut.
        /// </summary>
        /// <returns>The switch manager for the loop.</returns>
        public static SwitchManager GetLoopManager()
        {
            return loopManager;
        }
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
            // Instantiate the shape menu.
            shapeMenu = PrefabInstantiator.InstantiatePrefab(
                drawableShapePrefab,
                UICanvas.Canvas.transform,
                false);

            // Selectors
            InitializeSelector(
                shapeMenu,
                "ShapeSelection",
                GetShapes(),
                selected => SetSelectedShape(selected),
                out selector,
                out _);

            InitializeSelector(
                shapeMenu,
                "UMLShapeSelection",
                GetUMLShapes(),
                selected => SetSelectedUMLShape(selected),
                out umlShapeSelector,
                out objUMLShapeSelector);

            InitializeSelector(
                shapeMenu,
                "Orientation",
                "OrientationText",
                GetOrientations(),
                selected => orientation = selected,
                out orientationSelector,
                out objOrientation,
                out objOrientationText);

            InitializeSelector(
                shapeMenu,
                "LineStart",
                "LineStartText",
                GetAllLineCaps(),
                selected =>
                {
                    LineCapConf conf = GetLineStartCapConf();
                    conf.CapKind = selected;
                    SetLineStartCap(conf);
                },
                out lineStartSelector,
                out objLineStart,
                out objLineStartText);

            InitializeSelector(
                shapeMenu,
                "LineEnd",
                "LineEndText",
                GetAllLineCaps(),
                selected =>
                {
                    LineCapConf conf = GetLineEndCapConf();
                    conf.CapKind = selected;
                    SetLineEndCap(conf);
                },
                out lineEndSelector,
                out objLineEnd,
                out objLineEndText);

            // Float values
            InitializeFloatSlider(shapeMenu, "Value1", value => value1 = value, out sliderValue1, out objValue1);
            InitializeFloatSlider(shapeMenu, "Value2", value => value2 = value, out sliderValue2, out objValue2);
            InitializeFloatSlider(shapeMenu, "Value3", value => value3 = value, out sliderValue3, out objValue3);
            InitializeFloatSlider(shapeMenu, "Value4", value => value4 = value, out sliderValue4, out objValue4);

            InitializeFloatSlider(shapeMenu, "Angle1", value => angle1 = value, out sliderAngle1, out objAngle1);
            InitializeFloatSlider(shapeMenu, "Angle2", value => angle2 = value, out sliderAngle2, out objAngle2);

            InitializeFloatSlider(shapeMenu, "Offset", value => offset = value, out sliderOffset, out objOffset);

            // Int values
            InitializeIntSlider(shapeMenu, "Vertices", value => vertices = value, out sliderVertices, out objVertices);

            // Bool value
            objBoolValue = GameFinder.FindAttachedOrLocalDescendant(shapeMenu, "BoolValue");
            boolValueManager = objBoolValue.GetComponentInChildren<SwitchManager>();

            // Info
            objInfo = GameFinder.FindAttachedOrLocalDescendant(shapeMenu, "InfoPlaceHolder");
            infoBMB = objInfo.GetComponentInChildren<ButtonManagerBasic>();
            infoVisibility = false;
            infoBMB.clickEvent.AddListener(ToggleInfo);

            objImage = GameFinder.FindAttachedOrLocalDescendant(shapeMenu, "Image");
            infoImage = objImage.GetComponent<Image>();

            // Line-specific UI
            objLoop = GameFinder.FindAttachedOrLocalDescendant(shapeMenu, "Loop");
            loopManager = objLoop.GetComponentInChildren<SwitchManager>();

            objFinish = GameFinder.FindAttachedOrLocalDescendant(shapeMenu, "FinishBtn");
            finishBMB = objFinish.GetComponent<ButtonManagerBasic>();

            objPartUndo = GameFinder.FindAttachedOrLocalDescendant(shapeMenu, "PartUndoBtn");
            partUndoBMB = objPartUndo.GetComponent<ButtonManagerBasic>();
            objPartUndo.AddComponent<UIHoverTooltip>().SetMessage("Part Undo");
            objPartUndo.SetActive(false);

            // Dragger info
            draggerInfoObj = GameFinder.FindAttachedOrLocalDescendant(shapeMenu, "DraggerInfo");
            draggerInfoBMB = draggerInfoObj.GetComponent<ButtonManagerBasic>();
            draggerInfoBMB.clickEvent.AddListener(() =>
            {
                if (selectedShape == Shape.Line)
                {
                    ShowNotification.Info(
                        "Control instructions",
                        "Left mouse button = Adds a point to the line.\n"
                        + "Middle mouse button / mouse wheel click = Ends drawing the line without adding an additional point.\n"
                        + "Left Ctrl key + left mouse button = Ends drawing and adds a final point.");
                }
                else
                {
                    ShowNotification.Info(
                        "Control instructions",
                        "Middle mouse button / mouse wheel click = Fixes a point for the shape preview.\n"
                        + "Left Ctrl key + middle mouse button = Releases the fixed point.");
                }
            });

            // Initial state
            SetSelectedShape(Shape.Line);
        }

        /// <summary>
        /// Initializes a <see cref="HorizontalSelector"/> with the given values and binds the selection callback.
        /// </summary>
        /// <typeparam name="T">The type of the selectable values.</typeparam>
        /// <param name="parent">The parent object containing the selector.</param>
        /// <param name="childName">The name of the child object.</param>
        /// <param name="values">The selectable values displayed in the selector.</param>
        /// <param name="onSelected">Callback invoked when a value is selected.</param>
        /// <param name="selector">The resulting selector component.</param>
        /// <param name="selectorObject">The resulting selector <see cref="GameObject"/>.</param>
        private static void InitializeSelector<T>(
            GameObject parent,
            string childName,
            List<T> values,
            Action<T> onSelected,
            out HorizontalSelector selector,
            out GameObject selectorObject)
        {
            selectorObject = GameFinder.FindAttachedOrLocalDescendant(parent, childName);
            selector = selectorObject.GetComponent<HorizontalSelector>();

            foreach (T value in values)
            {
                selector.CreateNewItem(value.ToString());
            }

            selector.selectorEvent.AddListener(index =>
            {
                onSelected(values[index]);
            });

            selector.defaultIndex = 0;
        }

        /// <summary>
        /// Initializes a <see cref="HorizontalSelector"/> with the given values, binds the selection callback,
        /// and resolves an additional text object associated with the selector.
        /// </summary>
        /// <typeparam name="T">The type of the selectable values.</typeparam>
        /// <param name="parent">The parent object containing the selector and its related text object.</param>
        /// <param name="childName">The name of the selector object.</param>
        /// <param name="textChildName">The name of the related text object.</param>
        /// <param name="values">The selectable values displayed in the selector.</param>
        /// <param name="onSelected">Callback invoked when a value is selected.</param>
        /// <param name="selector">The resulting selector component.</param>
        /// <param name="selectorObject">The resulting selector <see cref="GameObject"/>.</param>
        /// <param name="textObject">The resulting text <see cref="GameObject"/> associated with the selector.</param>
        private static void InitializeSelector<T>(
            GameObject parent,
            string childName,
            string textChildName,
            List<T> values,
            Action<T> onSelected,
            out HorizontalSelector selector,
            out GameObject selectorObject,
            out GameObject textObject)
        {
            InitializeSelector(
                parent,
                childName,
                values,
                onSelected,
                out selector,
                out selectorObject);

            textObject = GameFinder.FindAttachedOrLocalDescendant(parent, textChildName);
        }

        /// <summary>
        /// Initializes a <see cref="FloatValueSliderController"/> and binds its value changed callback.
        /// </summary>
        /// <param name="parent">The parent object containing the slider.</param>
        /// <param name="childName">The name of the child object.</param>
        /// <param name="onValueChanged">Callback invoked when the slider value changes.</param>
        /// <param name="slider">The resulting slider component.</param>
        /// <param name="sliderObject">The resulting slider GameObject.</param>
        private static void InitializeFloatSlider(
            GameObject parent,
            string childName,
            Action<float> onValueChanged,
            out FloatValueSliderController slider,
            out GameObject sliderObject)
        {
            sliderObject = GameFinder.FindAttachedOrLocalDescendant(parent, childName);
            slider = sliderObject.GetComponent<FloatValueSliderController>();

            slider.onValueChanged.AddListener(value =>
            {
                onValueChanged(value);
            });
        }

        /// <summary>
        /// Initializes an <see cref="IntValueSliderController"/> and binds its value changed callback.
        /// The current slider value is assigned immediately before the change listener is registered.
        /// </summary>
        /// <param name="parent">The parent object containing the slider.</param>
        /// <param name="childName">The name of the child object.</param>
        /// <param name="onValueChanged">Callback invoked when the slider value changes.</param>
        /// <param name="slider">The resulting slider component.</param>
        /// <param name="sliderObject">The resulting slider GameObject.</param>
        private static void InitializeIntSlider(
            GameObject parent,
            string childName,
            Action<int> onValueChanged,
            out IntValueSliderController slider,
            out GameObject sliderObject)
        {
            sliderObject = GameFinder.FindAttachedOrLocalDescendant(parent, childName);
            slider = sliderObject.GetComponent<IntValueSliderController>();

            onValueChanged(slider.GetValue());

            slider.OnValueChanged.AddListener(value =>
            {
                onValueChanged(value);
            });
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
        /// <param name="action">The action that should be assigned.</param>
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
        /// Sets the selected UML shape type.
        /// The name will be displayed in the UML shape label.
        /// </summary>
        /// <param name="umlShape">The selected UML shape type.</param>
        private static void SetSelectedUMLShape(UMLShape umlShape)
        {
            selectedUMLShape = umlShape;
            ChangeMenu();
        }

        /// <summary>
        /// Resets all the values for the shapes to their minimum.
        /// </summary>
        private static void AllValuesReset()
        {
            /// Ensures that all objects are active.
            objUMLShapeSelector.SetActive(true);
            objValue1.SetActive(true);
            objValue2.SetActive(true);
            objValue3.SetActive(true);
            objValue4.SetActive(true);
            objAngle1.SetActive(true);
            objAngle2.SetActive(true);
            objOffset.SetActive(true);
            objVertices.SetActive(true);
            objBoolValue.SetActive(true);
            SetOrientationActive(true);
            SetLineStartActive(true);
            SetLineEndActive(true);
            objLoop.SetActive(true);
            objFinish.SetActive(true);

            sliderValue1.ResetToMin();
            sliderValue2.ResetToMin();
            sliderValue3.ResetToMin();
            sliderValue4.ResetToMin();
            sliderAngle1.ResetToMin();
            sliderAngle2.ResetToMin();
            sliderOffset.ResetToMin();
            sliderVertices.ResetToMin();
            boolValueManager.isOn = false;
            ResetSelector(orientationSelector);
            ResetSelector(lineStartSelector);
            ResetSelector(lineEndSelector);

            loopManager.isOn = false;
            infoVisibility = false;
            SetLineCaps(LineCapConf.CreateNone(), LineCapConf.CreateNone());
            orientation = Orientation.Up;

            static void ResetSelector(HorizontalSelector selector)
            {
                selector.index = 0;
                selector.defaultIndex = 0;
                selector.UpdateUI();
            }
        }
        /// <summary>
        /// Disables all the values.
        /// </summary>
        private static void AllValuesDisable()
        {
            objUMLShapeSelector.SetActive(false);
            objValue1.SetActive(false);
            objValue2.SetActive(false);
            objValue3.SetActive(false);
            objValue4.SetActive(false);
            objAngle1.SetActive(false);
            objAngle2.SetActive(false);
            objOffset.SetActive(false);
            objVertices.SetActive(false);
            objBoolValue.SetActive(false);
            SetOrientationActive(false);
            SetLineStartActive(false);
            SetLineEndActive(false);
            objInfo.SetActive(false);
            objImage.SetActive(false);
            objFinish.SetActive(false);
            objLoop.SetActive(false);
        }

        /// <summary>
        /// Sets whether the line start cap selector and its text are visible.
        /// </summary>
        /// <param name="isActive">
        /// True to show the line start cap selector; otherwise, false.
        /// </param>
        private static void SetLineStartActive(bool isActive)
        {
            SetUIElementActive(objLineStart, objLineStartText, isActive);
        }

        /// <summary>
        /// Sets whether the line end cap selector and its text are visible.
        /// </summary>
        /// <param name="isActive">
        /// True to show the line end cap selector; otherwise, false.
        /// </param>
        private static void SetLineEndActive(bool isActive)
        {
            SetUIElementActive(objLineEnd, objLineEndText, isActive);
        }

        /// <summary>
        /// Sets whether the orientation selector and its text are visible.
        /// </summary>
        /// <param name="isActive">
        /// True to show the orientation selector; otherwise, false.
        /// </param>
        private static void SetOrientationActive(bool isActive)
        {
            SetUIElementActive(objOrientation, objOrientationText, isActive);
        }

        /// <summary>
        /// Sets the active state of a UI element and its associated label.
        /// </summary>
        /// <param name="uiObject">The UI <see cref="GameObject"/>.</param>
        /// <param name="labelObject">The associated label <see cref="GameObject"/>.</param>
        /// <param name="isActive">
        /// True to enable both objects; otherwise, false.
        /// </param>
        private static void SetUIElementActive(GameObject uiObject, GameObject labelObject, bool isActive)
        {
            uiObject.SetActive(isActive);
            labelObject.SetActive(isActive);
        }

        /// <summary>
        /// Changes the menu for the selected shape.
        /// It displays only the necessary values for the selected shape.
        /// The values are renamed to match the shape appropriately,
        /// so that the values correspond to the explanations in the images of the information boxes.
        /// </summary>
        private static void ChangeMenu()
        {
            // Resets the values.
            AllValuesReset();
            // Disables all values.
            AllValuesDisable();
            // Enables the values necessary for the shape.
            // And names the values according to the shape.
            switch (selectedShape)
            {
                case Shape.Line:
                    objFinish.SetActive(true);
                    objLoop.SetActive(true);
                    SetLineStartActive(true);
                    SetLineEndActive(true);
                    break;
                case Shape.Square:
                    ActivateAndConfigurateValue(objValue1, "a");
                    objInfo.SetActive(true);
                    break;
                case Shape.Rectangle:
                    ActivateAndConfigurateValue(objValue1, "a");
                    ActivateAndConfigurateValue(objValue2, "b");
                    objInfo.SetActive(true);
                    break;
                case Shape.Rhombus:
                    ActivateAndConfigurateValue(objValue1, "f");
                    ActivateAndConfigurateValue(objValue2, "e");
                    objInfo.SetActive(true);
                    break;
                case Shape.Kite:
                    ActivateAndConfigurateValue(objValue1, "f1");
                    ActivateAndConfigurateValue(objValue2, "f2");
                    ActivateAndConfigurateValue(objValue3, "e");
                    objInfo.SetActive(true);
                    break;
                case Shape.Triangle:
                    ActivateAndConfigurateValue(objValue1, "c");
                    ActivateAndConfigurateValue(objValue2, "h");
                    objInfo.SetActive(true);
                    break;
                case Shape.Circle:
                    ActivateAndConfigurateValue(objValue1, "Radius");
                    objInfo.SetActive(true);
                    break;
                case Shape.HalfCircle:
                    ActivateAndConfigurateValue(objValue1, "Radius");
                    SetOrientationActive(true);
                    break;
                case Shape.Ellipse:
                    ActivateAndConfigurateValue(objValue1, "X-Scale");
                    ActivateAndConfigurateValue(objValue2, "Y-Scale");
                    objInfo.SetActive(true);
                    break;
                case Shape.Parallelogram:
                    ActivateAndConfigurateValue(objValue1, "a");
                    ActivateAndConfigurateValue(objValue2, "h");
                    ActivateAndConfigurateValue(objOffset, "Shift");
                    objInfo.SetActive(true);
                    break;
                case Shape.Trapezoid:
                    ActivateAndConfigurateValue(objValue1, "a");
                    ActivateAndConfigurateValue(objValue2, "c");
                    ActivateAndConfigurateValue(objValue3, "h");
                    objInfo.SetActive(true);
                    break;
                case Shape.Polygon:
                    ActivateAndConfigurateValue(objValue1, "Length");
                    objVertices.SetActive(true);
                    objInfo.SetActive(true);
                    break;
                case Shape.Arc:
                    ActivateAndConfigurateValue(objValue1, "Radius");
                    ActivateAndConfigurateValue(objAngle1, "Start Angle");
                    ActivateAndConfigurateValue(objAngle2, "End Angle", 360);
                    ActivateAndConfigurateValue(objVertices, "Verticies", PointsCalculator.DefaultVertices);
                    break;
                case Shape.UML:
                    ChangeUMLMenu();
                    break;
            }
            /// Re-calculate the shape menu height.
            MenuHelper.CalculateHeight(shapeMenu);
        }

        /// <summary>
        /// Changes the menu for the selected UML shape.
        /// It displays only the necessary values for the selected UML shape.
        /// </summary>
        private static void ChangeUMLMenu()
        {
            if (selectedShape != Shape.UML)
            {
                return;
            }
            objUMLShapeSelector.SetActive(true);

            switch(selectedUMLShape)
            {
                case UMLShape.Actor:
                    ActivateAndConfigurateValue(objValue1, "Length", 10);
                    break;
                case UMLShape.Note:
                    ActivateAndConfigurateValue(objValue1, "a", 30);
                    ActivateAndConfigurateValue(objValue2, "b", 20);
                    break;
                case UMLShape.Package:
                    ActivateAndConfigurateValue(objValue1, "a", 30);
                    ActivateAndConfigurateValue(objValue2, "b", 20);
                    ActivateAndConfigurateValue(objValue3, "Title-Width", 15);
                    ActivateAndConfigurateValue(objValue4, "Title-Height");
                    break;
                case UMLShape.ProvideInterf:
                    ActivateAndConfigurateValue(objValue1, "Radius", 10);
                    ActivateAndConfigurateOrientation(Orientation.Left);
                    break;
                case UMLShape.ReceiveInterf:
                    ActivateAndConfigurateValue(objValue1, "Radius", 10);
                    ActivateAndConfigurateOrientation(Orientation.Right);
                    break;
                case UMLShape.SendActivity:
                    ActivateAndConfigurateValue(objValue1, "a", 20);
                    ActivateAndConfigurateValue(objValue2, "b", 10);
                    ActivateAndConfigurateOrientation(Orientation.Right);
                    break;
                case UMLShape.ReceiveActivity:
                    ActivateAndConfigurateValue(objValue1, "a", 20);
                    ActivateAndConfigurateValue(objValue2, "b", 10);
                    ActivateAndConfigurateOrientation(Orientation.Left);
                    break;
            }
        }

        /// <summary>
        /// Activates a value object and optionally sets its identifier text and slider default value.
        /// </summary>
        /// <param name="valueObj">The GameObject to activate and configure.</param>
        /// <param name="identifier">Optional label to display in the TMP_Text component.</param>
        /// <param name="defaultValue">Optional default slider value (ignored if <= 0).</param>
        private static void ActivateAndConfigurateValue(GameObject valueObj, string identifier = null, int? defaultValue = null)
        {
            if (valueObj == null)
            {
                return;
            }
            valueObj.SetActive(true);

            if (!string.IsNullOrWhiteSpace(identifier))
            {
                TMP_Text tmpText = valueObj.GetComponentsInChildren<TMP_Text>().FirstOrDefault();
                if (tmpText != null)
                {
                    tmpText.text = identifier;
                }
            }
            if (defaultValue.HasValue)
            {
                SliderManager sliderManager = valueObj.GetComponentInChildren<SliderManager>();
                if (sliderManager != null)
                {
                    sliderManager.mainSlider.value = defaultValue.Value;
                }
            }
        }

        /// <summary>
        /// Activates a value object and optionally sets its identifier text and slider default value.
        /// </summary>
        /// <param name="defaultOrientation">The default orientation.</param>
        private static void ActivateAndConfigurateOrientation(Orientation defaultOrientation)
        {
            SetOrientationActive(true);
            orientation = defaultOrientation;
            orientationSelector.index = GetOrientations().IndexOf(defaultOrientation);
            orientationSelector.defaultIndex = GetOrientations().IndexOf(defaultOrientation);
            orientationSelector.UpdateUI();
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
            GameFinder.FindAttachedOrLocalDescendant(LineMenu.Instance.GameObject, "Dragger").GetComponent<WindowDragger>().enabled = false;
        }

        /// <summary>
        /// Binds the shape menu on the shape switch.
        /// </summary>
        private static void BindShapeMenu()
        {
            shapeMenu.transform.SetParent(drawableSwitch.transform.Find("Content"));
            GameFinder.FindAttachedOrLocalDescendant(shapeMenu, "Dragger").GetComponent<WindowDragger>().enabled = false;
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
        /// <param name="action">The action that should be assigned.</param>
        public static void AssignFinishButton(UnityAction action)
        {
            finishBMB.clickEvent.RemoveAllListeners();
            finishBMB.clickEvent.AddListener(action);
        }

        /// <summary>
        /// Sets the selected line-cap configurations and updates the selector UI.
        /// </summary>
        /// <param name="startCapConf">The start line-cap configuration.</param>
        /// <param name="endCapConf">The end line-cap configuration.</param>
        public static void SetLineCaps(LineCapConf startCapConf, LineCapConf endCapConf)
        {
            SetLineStartCap(startCapConf);
            SetLineEndCap(endCapConf);
        }

        /// <summary>
        /// Sets the selected start line-cap configuration.
        /// </summary>
        /// <param name="capConf">The start line-cap configuration.</param>
        private static void SetLineStartCap(LineCapConf capConf)
        {
            lineStartCapConf = capConf != null
                ? (LineCapConf)capConf.Clone()
                : LineCapConf.CreateNone();

            SetSelectorIndex(
                lineStartSelector,
                GetAllLineCaps().IndexOf(lineStartCapConf.CapKind));
        }

        /// <summary>
        /// Sets the selected end line-cap configuration.
        /// </summary>
        /// <param name="capConf">The end line-cap configuration.</param>
        private static void SetLineEndCap(LineCapConf capConf)
        {
            lineEndCapConf = capConf != null
                ? (LineCapConf)capConf.Clone()
                : LineCapConf.CreateNone();

            SetSelectorIndex(
                lineEndSelector,
                GetAllLineCaps().IndexOf(lineEndCapConf.CapKind));
        }

        /// <summary>
        /// Updates a selector to the given index if the selector has already been initialized.
        /// </summary>
        /// <param name="selector">The selector to update.</param>
        /// <param name="index">The index to select.</param>
        private static void SetSelectorIndex(HorizontalSelector selector, int index)
        {
            if (selector == null || index < 0)
            {
                return;
            }

            selector.index = index;
            selector.defaultIndex = index;
            selector.UpdateUI();
        }
    }
}

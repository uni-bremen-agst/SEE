using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.UI.Drawable;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.UI.Menu.Drawable.Shapes
{
    /// <summary>
    /// Holds the UI references used by the shape menu.
    /// All referenced objects and components are resolved once when this class
    /// is created and can afterwards be accessed without repeatedly searching
    /// the menu hierarchy.
    /// </summary>
    internal sealed class ShapeMenuControls
    {
        /// <summary>
        /// The switch object containing the shape and line configuration menus.
        /// </summary>
        internal GameObject SwitchObject { get; }

        /// <summary>
        /// The content transform of the switch object.
        /// </summary>
        internal Transform SwitchContent { get; }

        /// <summary>
        /// The root object of the shape menu.
        /// </summary>
        internal GameObject MenuObject { get; }

        /// <summary>
        /// The button for opening the shape menu.
        /// </summary>
        internal Button ShapeButton { get; }

        /// <summary>
        /// The button manager for opening the shape menu.
        /// </summary>
        internal ButtonManagerBasic ShapeButtonManager { get; }

        /// <summary>
        /// The button for opening the line configuration menu.
        /// </summary>
        internal Button ConfigButton { get; }

        /// <summary>
        /// The button manager for opening the line configuration menu.
        /// </summary>
        internal ButtonManagerBasic ConfigButtonManager { get; }

        /// <summary>
        /// The selector for choosing the general shape type.
        /// </summary>
        internal HorizontalSelector ShapeSelector { get; }

        /// <summary>
        /// The selector for choosing the UML shape type.
        /// </summary>
        internal HorizontalSelector UMLShapeSelector { get; }

        /// <summary>
        /// The object containing the UML shape selector.
        /// </summary>
        internal GameObject UMLShapeSelectorObject { get; }

        /// <summary>
        /// The object containing the first shape value.
        /// </summary>
        internal GameObject Value1Object { get; }

        /// <summary>
        /// The slider controlling the first shape value.
        /// </summary>
        internal FloatValueSliderController Value1Slider { get; }

        /// <summary>
        /// The object containing the second shape value.
        /// </summary>
        internal GameObject Value2Object { get; }

        /// <summary>
        /// The slider controlling the second shape value.
        /// </summary>
        internal FloatValueSliderController Value2Slider { get; }

        /// <summary>
        /// The object containing the third shape value.
        /// </summary>
        internal GameObject Value3Object { get; }

        /// <summary>
        /// The slider controlling the third shape value.
        /// </summary>
        internal FloatValueSliderController Value3Slider { get; }

        /// <summary>
        /// The object containing the fourth shape value.
        /// </summary>
        internal GameObject Value4Object { get; }

        /// <summary>
        /// The slider controlling the fourth shape value.
        /// </summary>
        internal FloatValueSliderController Value4Slider { get; }

        /// <summary>
        /// The object containing the first angle value.
        /// </summary>
        internal GameObject Angle1Object { get; }

        /// <summary>
        /// The slider controlling the first angle value.
        /// </summary>
        internal FloatValueSliderController Angle1Slider { get; }

        /// <summary>
        /// The object containing the second angle value.
        /// </summary>
        internal GameObject Angle2Object { get; }

        /// <summary>
        /// The slider controlling the second angle value.
        /// </summary>
        internal FloatValueSliderController Angle2Slider { get; }

        /// <summary>
        /// The object containing the shape offset.
        /// </summary>
        internal GameObject OffsetObject { get; }

        /// <summary>
        /// The slider controlling the shape offset.
        /// </summary>
        internal FloatValueSliderController OffsetSlider { get; }

        /// <summary>
        /// The object containing the number of vertices.
        /// </summary>
        internal GameObject VerticesObject { get; }

        /// <summary>
        /// The slider controlling the number of vertices.
        /// </summary>
        internal IntValueSliderController VerticesSlider { get; }

        /// <summary>
        /// The object containing the boolean shape option.
        /// </summary>
        internal GameObject BoolValueObject { get; }

        /// <summary>
        /// The switch controlling the boolean shape option.
        /// </summary>
        internal SwitchManager BoolValueManager { get; }

        /// <summary>
        /// The original sibling index of the boolean shape option.
        /// </summary>
        internal int BoolValueDefaultSiblingIndex { get; }

        /// <summary>
        /// The selector for choosing the shape orientation.
        /// </summary>
        internal HorizontalSelector OrientationSelector { get; }

        /// <summary>
        /// The object containing the orientation selector.
        /// </summary>
        internal GameObject OrientationObject { get; }

        /// <summary>
        /// The label belonging to the orientation selector.
        /// </summary>
        internal GameObject OrientationTextObject { get; }

        /// <summary>
        /// The object containing the information controls.
        /// </summary>
        internal GameObject InfoObject { get; }

        /// <summary>
        /// The button manager for toggling the information image.
        /// </summary>
        internal ButtonManagerBasic InfoButtonManager { get; }

        /// <summary>
        /// The object containing the information image.
        /// </summary>
        internal GameObject ImageObject { get; }

        /// <summary>
        /// The information image displaying the selected shape.
        /// </summary>
        internal Image InfoImage { get; }

        /// <summary>
        /// The selector for choosing the start line cap.
        /// </summary>
        internal HorizontalSelector LineStartSelector { get; }

        /// <summary>
        /// The object containing the start line-cap selector.
        /// </summary>
        internal GameObject LineStartObject { get; }

        /// <summary>
        /// The label belonging to the start line-cap selector.
        /// </summary>
        internal GameObject LineStartTextObject { get; }

        /// <summary>
        /// The selector for choosing the end line cap.
        /// </summary>
        internal HorizontalSelector LineEndSelector { get; }

        /// <summary>
        /// The object containing the end line-cap selector.
        /// </summary>
        internal GameObject LineEndObject { get; }

        /// <summary>
        /// The label belonging to the end line-cap selector.
        /// </summary>
        internal GameObject LineEndTextObject { get; }

        /// <summary>
        /// The object containing the finish button.
        /// </summary>
        internal GameObject FinishObject { get; }

        /// <summary>
        /// The button manager for finishing line drawing.
        /// </summary>
        internal ButtonManagerBasic FinishButtonManager { get; }

        /// <summary>
        /// The object containing the partial undo button.
        /// </summary>
        internal GameObject PartUndoObject { get; }

        /// <summary>
        /// The button manager for partial undo.
        /// </summary>
        internal ButtonManagerBasic PartUndoButtonManager { get; }

        /// <summary>
        /// The object containing the drawing control information button.
        /// </summary>
        internal GameObject DraggerInfoObject { get; }

        /// <summary>
        /// The button manager for the drawing control information button.
        /// </summary>
        internal ButtonManagerBasic DraggerInfoButtonManager { get; }

        /// <summary>
        /// The window dragger belonging to the shape menu.
        /// </summary>
        internal WindowDragger MenuDragger { get; }

        /// <summary>
        /// Resolves and stores all UI references belonging to the shape menu.
        /// </summary>
        /// <param name="switchObject">
        /// The switch object containing the shape and line configuration menus.
        /// </param>
        /// <param name="menuObject">
        /// The root object of the shape menu.
        /// </param>
        internal ShapeMenuControls(GameObject switchObject, GameObject menuObject)
        {
            SwitchObject = switchObject;
            SwitchContent = switchObject.transform.Find("Content");
            MenuObject = menuObject;

            Button[] switchButtons = switchObject.GetComponentsInChildren<Button>();
            ButtonManagerBasic[] switchButtonManagers =
                switchObject.GetComponentsInChildren<ButtonManagerBasic>();

            ShapeButton = switchButtons[0];
            ShapeButtonManager = switchButtonManagers[0];

            ConfigButton = switchButtons[1];
            ConfigButtonManager = switchButtonManagers[1];

            GameObject shapeSelectorObject =
                GameFinder.FindAttachedOrLocalDescendant(menuObject, "ShapeSelection");
            ShapeSelector =
                shapeSelectorObject.GetComponent<HorizontalSelector>();

            UMLShapeSelectorObject =
                GameFinder.FindAttachedOrLocalDescendant(menuObject, "UMLShapeSelection");
            UMLShapeSelector =
                UMLShapeSelectorObject.GetComponent<HorizontalSelector>();

            Value1Object =
                GameFinder.FindAttachedOrLocalDescendant(menuObject, "Value1");
            Value1Slider =
                Value1Object.GetComponent<FloatValueSliderController>();

            Value2Object =
                GameFinder.FindAttachedOrLocalDescendant(menuObject, "Value2");
            Value2Slider =
                Value2Object.GetComponent<FloatValueSliderController>();

            Value3Object =
                GameFinder.FindAttachedOrLocalDescendant(menuObject, "Value3");
            Value3Slider =
                Value3Object.GetComponent<FloatValueSliderController>();

            Value4Object =
                GameFinder.FindAttachedOrLocalDescendant(menuObject, "Value4");
            Value4Slider =
                Value4Object.GetComponent<FloatValueSliderController>();

            Angle1Object =
                GameFinder.FindAttachedOrLocalDescendant(menuObject, "Angle1");
            Angle1Slider =
                Angle1Object.GetComponent<FloatValueSliderController>();

            Angle2Object =
                GameFinder.FindAttachedOrLocalDescendant(menuObject, "Angle2");
            Angle2Slider =
                Angle2Object.GetComponent<FloatValueSliderController>();

            OffsetObject =
                GameFinder.FindAttachedOrLocalDescendant(menuObject, "Offset");
            OffsetSlider =
                OffsetObject.GetComponent<FloatValueSliderController>();

            VerticesObject =
                GameFinder.FindAttachedOrLocalDescendant(menuObject, "Vertices");
            VerticesSlider =
                VerticesObject.GetComponent<IntValueSliderController>();

            BoolValueObject =
                GameFinder.FindAttachedOrLocalDescendant(menuObject, "BoolValue");
            BoolValueManager =
                BoolValueObject.GetComponentInChildren<SwitchManager>();
            BoolValueDefaultSiblingIndex =
                BoolValueObject.transform.GetSiblingIndex();

            OrientationObject =
                GameFinder.FindAttachedOrLocalDescendant(menuObject, "Orientation");
            OrientationSelector =
                OrientationObject.GetComponent<HorizontalSelector>();
            OrientationTextObject =
                GameFinder.FindAttachedOrLocalDescendant(menuObject, "OrientationText");

            InfoObject =
                GameFinder.FindAttachedOrLocalDescendant(menuObject, "InfoPlaceHolder");
            InfoButtonManager =
                InfoObject.GetComponentInChildren<ButtonManagerBasic>();

            ImageObject =
                GameFinder.FindAttachedOrLocalDescendant(menuObject, "Image");
            InfoImage =
                ImageObject.GetComponent<Image>();

            LineStartObject =
                GameFinder.FindAttachedOrLocalDescendant(menuObject, "LineStart");
            LineStartSelector =
                LineStartObject.GetComponent<HorizontalSelector>();
            LineStartTextObject =
                GameFinder.FindAttachedOrLocalDescendant(menuObject, "LineStartText");

            LineEndObject =
                GameFinder.FindAttachedOrLocalDescendant(menuObject, "LineEnd");
            LineEndSelector =
                LineEndObject.GetComponent<HorizontalSelector>();
            LineEndTextObject =
                GameFinder.FindAttachedOrLocalDescendant(menuObject, "LineEndText");

            FinishObject =
                GameFinder.FindAttachedOrLocalDescendant(menuObject, "FinishBtn");
            FinishButtonManager =
                FinishObject.GetComponent<ButtonManagerBasic>();

            PartUndoObject =
                GameFinder.FindAttachedOrLocalDescendant(BoolValueObject, "PartUndoBtn");
            PartUndoButtonManager =
                PartUndoObject.GetComponent<ButtonManagerBasic>();

            DraggerInfoObject =
                GameFinder.FindAttachedOrLocalDescendant(menuObject, "DraggerInfo");
            DraggerInfoButtonManager =
                DraggerInfoObject.GetComponent<ButtonManagerBasic>();

            GameObject dragger =
                GameFinder.FindAttachedOrLocalDescendant(menuObject, "Dragger");
            MenuDragger =
                dragger.GetComponent<WindowDragger>();
        }
    }
}

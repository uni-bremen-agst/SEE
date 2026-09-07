using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.UI.Drawable;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Holds the UI references shared by the drawing and editing parts of the
    /// <see cref="LineMenu"/>.
    /// All referenced objects and components are resolved once when this class
    /// is created and can afterwards be accessed without searching the menu hierarchy again.
    /// </summary>
    internal sealed class LineMenuControls
    {
        /// <summary>
        /// The game object containing the line-kind selector.
        /// </summary>
        internal GameObject LineKindSelectionObject { get; }

        /// <summary>
        /// The label of the line-kind selector.
        /// </summary>
        internal GameObject LineKindTextObject { get; }

        /// <summary>
        /// The selector for choosing the line kind.
        /// </summary>
        internal HorizontalSelector LineKindSelector { get; }

        /// <summary>
        /// The game object containing the color-kind selector.
        /// </summary>
        internal GameObject ColorKindSelectionObject { get; }

        /// <summary>
        /// The selector for choosing the color kind.
        /// </summary>
        internal HorizontalSelector ColorKindSelector { get; }

        /// <summary>
        /// The game object containing the tiling control.
        /// </summary>
        internal GameObject TilingObject { get; }

        /// <summary>
        /// The controller for the tiling value.
        /// </summary>
        internal FloatValueSliderController TilingSlider { get; }

        /// <summary>
        /// The game object containing the order-in-layer control.
        /// </summary>
        internal GameObject LayerObject { get; }

        /// <summary>
        /// The slider used to enable or disable order-in-layer editing.
        /// </summary>
        internal Slider LayerSlider { get; }

        /// <summary>
        /// The controller for the order-in-layer value.
        /// </summary>
        internal LayerSliderController LayerSliderController { get; }

        /// <summary>
        /// The game object containing the thickness control.
        /// </summary>
        internal GameObject ThicknessObject { get; }

        /// <summary>
        /// The controller for the line thickness.
        /// </summary>
        internal ThicknessSliderController ThicknessSlider { get; }

        /// <summary>
        /// The game object containing the loop control.
        /// </summary>
        internal GameObject LoopObject { get; }

        /// <summary>
        /// The switch manager for the loop state.
        /// </summary>
        internal SwitchManager LoopManager { get; }

        /// <summary>
        /// The button manager for selecting the primary color.
        /// </summary>
        internal ButtonManagerBasic PrimaryColorButtonManager { get; }

        /// <summary>
        /// The button manager for selecting the secondary color.
        /// </summary>
        internal ButtonManagerBasic SecondaryColorButtonManager { get; }

        /// <summary>
        /// The button manager for selecting the color-kind area.
        /// </summary>
        internal ButtonManagerBasic ColorKindButtonManager { get; }

        /// <summary>
        /// The button manager for selecting the fill-out area.
        /// </summary>
        internal ButtonManagerBasic FillOutButtonManager { get; }

        /// <summary>
        /// The game object containing the fill-out controls.
        /// </summary>
        internal GameObject FillOutObject { get; }

        /// <summary>
        /// The switch manager for the fill-out state.
        /// </summary>
        internal SwitchManager FillOutManager { get; }

        /// <summary>
        /// The game object containing the color-area selector.
        /// </summary>
        internal GameObject ColorAreaSelectorObject { get; }

        /// <summary>
        /// The game object containing the color-type selector.
        /// </summary>
        internal GameObject ColorTypeSelectorObject { get; }

        /// <summary>
        /// The game object containing the color picker.
        /// </summary>
        internal GameObject ColorPickerObject { get; }

        /// <summary>
        /// The color picker used by the line menu.
        /// </summary>
        internal HSVPicker.ColorPicker ColorPicker { get; }

        /// <summary>
        /// The game object of the return button.
        /// </summary>
        internal GameObject ReturnButtonObject { get; }

        /// <summary>
        /// The button manager of the return button.
        /// </summary>
        internal ButtonManagerBasic ReturnButtonManager { get; }

        /// <summary>
        /// The window dragger of the line menu.
        /// </summary>
        internal WindowDragger WindowDragger { get; }

        /// <summary>
        /// Resolves and stores all UI references shared by the line menu.
        /// </summary>
        /// <param name="lineMenu">
        /// The game object containing the complete line-menu hierarchy.
        /// </param>
        internal LineMenuControls(GameObject lineMenu)
        {
            LineKindSelectionObject =
                GameFinder.FindAttachedOrLocalDescendant(lineMenu, "LineKindSelection");
            LineKindTextObject =
                GameFinder.FindAttachedOrLocalDescendant(lineMenu, "LineKindText");

            LineKindSelector =
                LineKindSelectionObject.GetComponent<HorizontalSelector>();

            ColorKindSelectionObject =
                GameFinder.FindAttachedOrLocalDescendant(lineMenu, "ColorKindSelection");
            ColorKindSelector =
                ColorKindSelectionObject.GetComponent<HorizontalSelector>();

            TilingObject =
                GameFinder.FindAttachedOrLocalDescendant(lineMenu, "Tiling");
            TilingSlider =
                TilingObject.GetComponentInChildren<FloatValueSliderController>(true);

            LayerObject =
                GameFinder.FindAttachedOrLocalDescendant(lineMenu, "Layer");
            LayerSlider =
                LayerObject.GetComponentInChildren<Slider>(true);
            LayerSliderController =
                LayerObject.GetComponentInChildren<LayerSliderController>(true);

            ThicknessObject =
                GameFinder.FindAttachedOrLocalDescendant(lineMenu, "Thickness");
            ThicknessSlider =
                ThicknessObject.GetComponentInChildren<ThicknessSliderController>(true);

            LoopObject =
                GameFinder.FindAttachedOrLocalDescendant(lineMenu, "Loop");
            LoopManager =
                LoopObject.GetComponentInChildren<SwitchManager>(true);

            GameObject primaryColorButton =
                GameFinder.FindAttachedOrLocalDescendant(lineMenu, "PrimaryColorBtn");
            PrimaryColorButtonManager =
                primaryColorButton.GetComponent<ButtonManagerBasic>();
            PrimaryColorButtonManager.buttonVar =
                primaryColorButton.GetComponent<Button>();

            GameObject secondaryColorButton =
                GameFinder.FindAttachedOrLocalDescendant(lineMenu, "SecondaryColorBtn");
            SecondaryColorButtonManager =
                secondaryColorButton.GetComponent<ButtonManagerBasic>();
            SecondaryColorButtonManager.buttonVar =
                secondaryColorButton.GetComponent<Button>();

            GameObject colorKindButton =
                GameFinder.FindAttachedOrLocalDescendant(lineMenu, "ColorKindBtn");
            ColorKindButtonManager =
                colorKindButton.GetComponent<ButtonManagerBasic>();
            ColorKindButtonManager.buttonVar =
                colorKindButton.GetComponent<Button>();

            GameObject fillOutButton =
                GameFinder.FindAttachedOrLocalDescendant(lineMenu, "FillOutBtn");
            FillOutButtonManager =
                fillOutButton.GetComponent<ButtonManagerBasic>();
            FillOutButtonManager.buttonVar =
                fillOutButton.GetComponent<Button>();

            FillOutObject =
                GameFinder.FindAttachedOrLocalDescendant(lineMenu, "FillOut");
            FillOutManager =
                FillOutObject.GetComponentInChildren<SwitchManager>(true);

            ColorAreaSelectorObject =
                GameFinder.FindAttachedOrLocalDescendant(lineMenu, "ColorAreaSelector");

            ColorTypeSelectorObject =
                GameFinder.FindAttachedOrLocalDescendant(lineMenu, "ColorTypeSelector");

            ColorPickerObject =
                GameFinder.FindAttachedOrLocalDescendant(lineMenu, "Picker 2.0");
            ColorPicker =
                ColorPickerObject.GetComponentInChildren<HSVPicker.ColorPicker>(true);

            ReturnButtonObject =
                GameFinder.FindAttachedOrLocalDescendant(lineMenu, "ReturnBtn");
            ReturnButtonManager =
                ReturnButtonObject.GetComponent<ButtonManagerBasic>();

            GameObject dragger =
                GameFinder.FindAttachedOrLocalDescendant(lineMenu, "Dragger");
            WindowDragger = dragger.GetComponent<WindowDragger>();
        }
    }
}

using Michsky.UI.ModernUIPack;
using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.Net.Actions.Drawable;
using SEE.UI.Drawable;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static SEE.Game.Drawable.GameDrawer;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// This class holds the instance for the line menu.
    /// </summary>
    public static class LineMenu
    {
        #region variables
        /// <summary>
        /// The location where the line menu prefeb is placed.
        /// </summary>
        private const string lineMenuPrefab = "Prefabs/UI/Drawable/LineMenu";

        /// <summary>
        /// The instance of the line menu.
        /// </summary>
        public static readonly GameObject instance;

        /// <summary>
        /// The color action to be executed additionally during the onChangeValue of the HSV Color Picker.
        /// </summary>
        public static UnityAction<Color> colorAction;

        /// <summary>
        /// The float action to be executed additionally during the onChangeValue of the tiling slider.
        /// </summary>
        public static UnityAction<float> tilingAction;

        /// <summary>
        /// The additionally action for the line kind selector.
        /// </summary>
        private static UnityAction<int> lineKindAction;

        /// <summary>
        /// The additionally action for the color kind selector.
        /// </summary>
        private static UnityAction<int> colorKindAction;

        /// <summary>
        /// The transform of the content object.
        /// </summary>
        private static Transform content;

        /// <summary>
        /// Holds the current selected line kind.
        /// </summary>
        private static LineKind selectedLineKind;

        /// <summary>
        /// Holds the current selected color kind.
        /// </summary>
        private static ColorKind selectedColorKind;

        /// <summary>
        /// The controller for the tiling slider.
        /// </summary>
        private static FloatValueSliderController tilingSlider;

        /// <summary>
        /// The selector for the line kind.
        /// </summary>
        private static HorizontalSelector lineKindSelector;

        /// <summary>
        /// The selector for the color kind.
        /// </summary>
        private static HorizontalSelector colorKindSelector;

        /// <summary>
        /// The switch manager for the loop.
        /// </summary>
        private static SwitchManager loopManager;

        /// <summary>
        /// Button manager for chose primary color.
        /// </summary>
        private static ButtonManagerBasic primaryColorBMB;

        /// <summary>
        /// Button manager for chose secondary color.
        /// </summary>
        private static ButtonManagerBasic secondaryColorBMB;

        /// <summary>
        /// The HSVPicker ColorPicker component of the line menu.
        /// </summary>
        private static HSVPicker.ColorPicker picker;

        /// <summary>
        /// Button manager for chose color kind area.
        /// </summary>
        private static ButtonManagerBasic colorKindBMB;

        /// <summary>
        /// Button manager for chose fill out area.
        /// </summary>
        private static ButtonManagerBasic fillOutBMB;

        /// <summary>
        /// The switch manager for the fill out status.
        /// </summary>
        private static SwitchManager fillOutManager;

        /// <summary>
        /// The modes in which the menu can be opened.
        /// </summary>
        private enum Mode
        {
            None,
            Drawing,
            Edit
        }

        /// <summary>
        /// The current mode.
        /// </summary>
        private static Mode mode;
        #endregion

        /// <summary>
        /// An enum with the menu points that can be hide.
        /// </summary>
        public enum MenuLayer
        {
            LineKind,
            Thickness,
            Layer,
            Loop,
            All
        }

        /// <summary>
        /// The constructor. It creates the instance for the line menu and 
        /// adds the menu layer to the corresponding game object's.
        /// By default, the menu is hidden.
        /// </summary>
        static LineMenu()
        {
            /// Instantiates the menu.
            instance = PrefabInstantiator.InstantiatePrefab(lineMenuPrefab,
                GameObject.Find("UI Canvas").transform, false);

            /// Initialize the content area
            content = instance.transform.Find("Content");

            /// Disables the ability to return to the previous menu. 
            /// Intended only for editing MindMap nodes.
            DisableReturn();

            /// Initialize and sets up the line kind selector.
            InitLineKindSelectorConstructor();

            /// Initialize and sets up the color kind selector.
            InitColorKindSelectorConstructor();

            /// Initialize the remaining GUI elements.
            loopManager = GameFinder.FindChild(instance, "Loop").GetComponentInChildren<SwitchManager>();
            primaryColorBMB = GameFinder.FindChild(instance, "PrimaryColorBtn").GetComponent<ButtonManagerBasic>();
            primaryColorBMB.buttonVar = GameFinder.FindChild(instance, "PrimaryColorBtn").GetComponent<Button>();
            primaryColorBMB.clickEvent.AddListener(MutuallyExclusiveColorButtons);
            primaryColorBMB.buttonVar.interactable = false;
            secondaryColorBMB = GameFinder.FindChild(instance, "SecondaryColorBtn").GetComponent<ButtonManagerBasic>();
            secondaryColorBMB.buttonVar = GameFinder.FindChild(instance, "SecondaryColorBtn").GetComponent<Button>();
            secondaryColorBMB.clickEvent.AddListener(MutuallyExclusiveColorButtons);
            tilingSlider = GameFinder.FindChild(instance, "Tiling").GetComponentInChildren<FloatValueSliderController>();
            picker = instance.GetComponentInChildren<HSVPicker.ColorPicker>();
            mode = Mode.None;
            instance.SetActive(false);
        }

        /// <summary>
        /// Initialize the default line kind selector for the constructor.
        /// </summary>
        private static void InitLineKindSelectorConstructor()
        {
            lineKindSelector = GameFinder.FindChild(instance, "LineKindSelection")
                .GetComponent<HorizontalSelector>();

            /// Creates the items for the line kind selector.
            foreach (LineKind kind in GetLineKinds())
            {
                lineKindSelector.CreateNewItem(kind.ToString());
            }

            /// Register the handler for switching the line kind.
            lineKindSelector.selectorEvent.AddListener(index =>
            {
                if (GetLineKinds()[index] == LineKind.Dashed)
                {
                    /// Enables the tiling layer for a dashed line kind.
                    EnableTilingFromLineMenu();
                }
                else
                {
                    /// Disables the tiling layer.
                    DisableTilingFromLineMenu();
                }

                /// If you want to switch to <see cref="LineKind.Solid"/> but 
                /// previously a Dashed LineKind with <see cref="ColorKind.TwoDashed"/> was active, 
                /// you need also to switch the <see cref="ColorKind"/> to <see cref="ColorKind.Monochrome"/>.
                if (GetLineKinds()[index] == LineKind.Solid &&
                    selectedColorKind == ColorKind.TwoDashed)
                {
                    AssignColorKind(ColorKind.Monochrome);
                    colorKindSelector.label.text = ColorKind.Monochrome.ToString();
                    colorKindSelector.index = 0;
                    colorKindSelector.UpdateUI();
                }
                AssignLineKind(GetLineKinds()[index]);
            });
            lineKindSelector.defaultIndex = 0;
        }

        /// <summary>
        /// Initialize the default color kind selector for the constructor.
        /// </summary>
        private static void InitColorKindSelectorConstructor()
        {
            colorKindSelector = GameFinder.FindChild(instance, "ColorKindSelection")
                .GetComponent<HorizontalSelector>();
            bool isDashed = selectedLineKind != LineKind.Solid;

            /// Creates the items for the color kind selector.
            foreach (ColorKind kind in GetColorKinds(true))
            {
                colorKindSelector.CreateNewItem(kind.ToString());
            }

            /// Register the handler for switching the color kind.
            colorKindSelector.selectorEvent.AddListener(index =>
            {
                bool isDashed = selectedLineKind != LineKind.Solid;
                ColorKind newColorKind = GetColorKinds(true)[index];

                /// Skips the <see cref="ColorKind.TwoDashed"/> for the <see cref="LineKind.Solid"/>.
                if (!isDashed && newColorKind == ColorKind.TwoDashed)
                {
                    if (selectedColorKind == ColorKind.Monochrome)
                    {
                        newColorKind = ColorKind.Gradient;
                        colorKindSelector.label.text = ColorKind.Gradient.ToString();
                    }
                    else
                    {
                        newColorKind = ColorKind.Monochrome;
                        colorKindSelector.label.text = ColorKind.Monochrome.ToString();
                    }
                }
                AssignColorKind(newColorKind);
                colorKindSelector.index = GetIndexOfSelectedColorKind();

            });

            /// Sets the default selected item
            colorKindSelector.defaultIndex = 0;
        }

        /// <summary>
        /// Gets the state if the menu is already opened.
        /// </summary>
        /// <returns>true, if the menu is alreay opened. Otherwise false.</returns>
        public static bool IsOpen()
        {
            return instance.activeInHierarchy;
        }

        public static bool IsInDrawingMode()
        {
            return instance.activeInHierarchy && mode == Mode.Drawing;
        }

        public static bool IsInEditMode()
        {
            return instance.activeInHierarchy && mode == Mode.Edit;
        }

        /// <summary>
        /// Enables all line menu layers,
        /// sets the parent back to UI Canvas, enables the window dragger 
        /// and then hides the line menu.
        /// The parent of the line menu can be switched through the <see cref="DrawShapesAction"/>
        /// </summary>
        public static void DisableLineMenu()
        {
            EnableLineMenuLayers();
            instance.transform.SetParent(GameObject.Find("UI Canvas").transform);
            GameFinder.FindChild(instance, "Dragger").GetComponent<WindowDragger>().enabled = true;
            DisableReturn();
            mode = Mode.None;
            instance.SetActive(false);
        }

        #region Enable Line Menu
        /// <summary>
        /// Enables the line menu. And resets the additional Handler if the parameter for that is true.
        /// Also it can hide some menu layer.
        /// </summary>
        /// <param name="removeListeners">The bool, if the Handler should be reset.</param>
        /// <param name="withoutMenuLayer">An array of menu layers that should hide.</param>
        public static void EnableLineMenu(bool removeListeners = true, MenuLayer[] withoutMenuLayer = null)
        {
            /// Removes the listeners of the GUI elements, if the <paramref name="removeListeners"/> is true.
            if (removeListeners)
            {
                RemoveListeners();
            }
            mode = Mode.None;
            /// Disables the given <see cref="MenuLayer"/>.
            if (withoutMenuLayer != null)
            {
                foreach (MenuLayer menuPoint in withoutMenuLayer)
                {
                    switch (menuPoint)
                    {
                        case MenuLayer.LineKind:
                            DisableLineKindFromLineMenu();
                            break;
                        case MenuLayer.Thickness:
                            DisableThicknessFromLineMenu();
                            break;
                        case MenuLayer.Layer:
                            DisableLayerFromLineMenu();
                            break;
                        case MenuLayer.Loop:
                            DisableLoopFromLineMenu();
                            break;
                        case MenuLayer.All:
                            DisableLineKindFromLineMenu();
                            DisableThicknessFromLineMenu();
                            DisableLayerFromLineMenu();
                            DisableLoopFromLineMenu();
                            break;
                    }
                }
            }

            /// Disables the tiling, if the line has a <see cref="LineKind.Solid"/>.
            if (selectedLineKind != LineKind.Dashed)
            {
                DisableTilingFromLineMenu();
            }

            /// Enables the menu.
            instance.SetActive(true);

            /// Calculates the height of the menu.
            MenuHelper.CalculateHeight(instance, true);
        }

        /// <summary>
        /// Enables the line menu for drawing.
        /// </summary>
        public static void EnableForDrawing()
        {
            EnableLineMenu(withoutMenuLayer: new MenuLayer[] { MenuLayer.Layer, MenuLayer.Loop });
            InitDrawing();
            mode = Mode.Drawing;
            /// Calculates the height of the menu.
            MenuHelper.CalculateHeight(instance, true);
        }

        /// <summary>
        /// Init the Handlers for the Drawing.
        /// </summary>
        private static void InitDrawing()
        {
            /// Initialize the tiling slider and 
            /// save the changes in the global value for the tiling <see cref="ValueHolder.CurrentTiling"/>.
            tilingSlider.onValueChanged.AddListener(tilingAction = tiling =>
            {
                ValueHolder.CurrentTiling = tiling;
            });

            /// Sets up the line kind selector.
            SetUpLineKindSelectorForDrawing();

            /// Sets up the color kind selector.
            SetUpColorKindSelectorForDrawing();

            /// Sets up the primary color button. 
            SetUpPrimaryColorButtonForDrawing();

            /// Sets up the secondary color button. 
            SetUpSecondaryColorButtonForDrawing();

            /// Sets up the thickness slider.
            SetUpOutlineThicknessSliderForDrawing();

            /// Assigns the current primary color to the <see cref="HSVPicker.ColorPicker"/>.
            picker.AssignColor(ValueHolder.CurrentPrimaryColor);
            picker.onValueChanged.AddListener(colorAction = color => ValueHolder.CurrentPrimaryColor = color);

            /// At least re-calculate the menu heigt.
            MenuHelper.CalculateHeight(instance, true);
        }

        /// <summary>
        /// Sets up the line kind selector with the current selected <see cref="LineKind"/> and
        /// saves the changes in the global value for it. <see cref="ValueHolder.CurrentLineKind"/>.
        /// </summary>
        private static void SetUpLineKindSelectorForDrawing()
        {
            /// Assigns the current chosen line kind to the menu variable.
            AssignLineKind(ValueHolder.CurrentLineKind);

            /// Gets the index of the current chosen line kind.
            lineKindSelector.index = GetIndexOfSelectedLineKind();

            /// Updates the selector.
            lineKindSelector.UpdateUI();

            /// Removes the old action of the selector
            if (lineKindAction != null)
            {
                lineKindSelector.selectorEvent.RemoveListener(lineKindAction);
            }

            /// Creates the new action for changing the line kind on the selector.
            lineKindAction = index =>
            {
                ValueHolder.CurrentLineKind = GetLineKinds()[index];

                /// If you want to switch to <see cref="LineKind.Solid"/> but 
                /// previously a Dashed LineKind with <see cref="ColorKind.TwoDashed"/> was active, 
                /// you need also to switch the <see cref="ColorKind"/> to <see cref="ColorKind.Monochrome"/>.
                if (ValueHolder.CurrentLineKind == LineKind.Solid &&
                    ValueHolder.CurrentColorKind == ColorKind.TwoDashed)
                {
                    ValueHolder.CurrentColorKind = ColorKind.Monochrome;
                }
            };

            /// Add the action to the selector.
            lineKindSelector.selectorEvent.AddListener(lineKindAction);
        }

        /// <summary>
        /// Sets up the color kind selector with the current selected <see cref="ColorKind"/> and
        /// saves the changes global value for it. <see cref="ValueHolder.CurrentColorKind"/>.
        /// </summary>
        private static void SetUpColorKindSelectorForDrawing()
        {
            /// Assigns the current chosen color kind to the menu variable.
            AssignColorKind(ValueHolder.CurrentColorKind);

            /// Gets the index of the current chosen color kind.
            colorKindSelector.index = GetIndexOfSelectedColorKind();

            /// Updates the selector.
            colorKindSelector.UpdateUI();

            /// Removes the old action of the selector
            if (colorKindAction != null)
            {
                colorKindSelector.selectorEvent.RemoveListener(colorKindAction);
            }

            /// Creates the new action for changing the color kind on the selector.
            colorKindAction = index =>
            {
                ValueHolder.CurrentColorKind = GetColorKinds(true)[index];

                /// Set the secondary color when it is transparent.
                if (ValueHolder.CurrentColorKind != ColorKind.Monochrome
                    && ValueHolder.CurrentSecondaryColor == Color.clear)
                {
                    ValueHolder.CurrentSecondaryColor = ValueHolder.CurrentPrimaryColor;
                }
            };

            /// Adds the action to the selector.
            colorKindSelector.selectorEvent.AddListener(colorKindAction);
        }

        /// <summary>
        /// Set up the primary color button for drawing mode.
        /// They mutually exclude each other with the secondary button. This means only one can be activated at a time.
        /// It saves the changes in the global value for the primary color <see cref="ValueHolder.CurrentPrimaryColor"/>.
        /// </summary>
        private static void SetUpPrimaryColorButtonForDrawing()
        {
            /// Removes old handler.
            primaryColorBMB.clickEvent.RemoveAllListeners();
            /// Adds the mutually exclusive mode.
            primaryColorBMB.clickEvent.AddListener(MutuallyExclusiveColorButtons);
            /// Adds the new handler for saving in global value.
            primaryColorBMB.clickEvent.AddListener(() =>
            {
                AssignColorArea(color => ValueHolder.CurrentPrimaryColor = color, ValueHolder.CurrentPrimaryColor);
            });
            /// Makes the button not clickable.
            primaryColorBMB.buttonVar.interactable = false;
        }

        /// <summary>
        /// Set up the secondary color button for drawing mode.
        /// They mutually exclude each other with the primary button. This means only one can be activated at a time.
        /// It saves the changes in the global value for the secondary color <see cref="ValueHolder.CurrentSecondaryColor"/>.
        /// </summary>
        private static void SetUpSecondaryColorButtonForDrawing()
        {
            /// Removes old handler.
            secondaryColorBMB.clickEvent.RemoveAllListeners();
            /// Adds the mutually exclusive mode.
            secondaryColorBMB.clickEvent.AddListener(MutuallyExclusiveColorButtons);
            /// Adds the new handler for saving in global value.
            secondaryColorBMB.clickEvent.AddListener(() =>
            {
                /// If the <see cref="LineKind"/> was <see cref="LineKind.Solid"/> before, 
                /// the secondary color is clear. 
                /// Therefore, a random color is added first, 
                /// and if the color's alpha is 0, it is set to 255 to ensure the color is not transparent.
                if (ValueHolder.CurrentSecondaryColor == Color.clear)
                {
                    ValueHolder.CurrentSecondaryColor = Random.ColorHSV();
                }
                if (ValueHolder.CurrentSecondaryColor.a == 0)
                {
                    ValueHolder.CurrentSecondaryColor = new Color(ValueHolder.CurrentSecondaryColor.r,
                        ValueHolder.CurrentSecondaryColor.g, ValueHolder.CurrentSecondaryColor.b, 255);
                }
                AssignColorArea(color => { ValueHolder.CurrentSecondaryColor = color; }, ValueHolder.CurrentSecondaryColor);
            });
            /// Makes the button not clickable.
            secondaryColorBMB.buttonVar.interactable = true;
        }
        /// <summary>
        /// Set up the outline thickness slider for drawing mode.
        /// The changes will be saved in the global value <see cref="ValueHolder.CurrentThickness"/>.
        /// </summary>
        private static void SetUpOutlineThicknessSliderForDrawing()
        {
            ThicknessSliderController thicknessSlider = instance.GetComponentInChildren<ThicknessSliderController>();
            /// Assigns the current value to the slider.
            thicknessSlider.AssignValue(ValueHolder.CurrentThickness);
            /// Add the handler.
            thicknessSlider.OnValueChanged.AddListener(thickness =>
            {
                ValueHolder.CurrentThickness = thickness;
            });
        }

        /// <summary>
        /// This method provides the line menu for editing, adding the necessary Handler to the respective components.
        /// </summary>
        /// <param name="selectedLine">The selected line object for editing.</param>
        public static void EnableForEditing(GameObject selectedLine, DrawableType newValueHolder, UnityAction returnCall = null)
        {
            if (newValueHolder is LineConf lineHolder)
            {
                /// Enables the line menu.
                EnableLineMenu();

                /// Set up the return button.
                SetUpReturnButtonForEditing(returnCall);

                LineRenderer renderer = selectedLine.GetComponent<LineRenderer>();
                GameObject surface = GameFinder.GetDrawableSurface(selectedLine);
                string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);

                /// Set up the line kind selector.
                SetUpLineKindSelectorForEditing(selectedLine, renderer, lineHolder, surface, surfaceParentName);

                /// Set up the color kind selector.
                SetUpColorKindSelectorForEditing(selectedLine, renderer, lineHolder, surface, surfaceParentName);

                /// Adds the action that should be executed if the tiling silder changed.
                /// It only is available for <see cref="LineKind.Dashed"/>.
                tilingSlider.onValueChanged.AddListener(tilingAction = tiling =>
                {
                    ChangeLineKind(selectedLine, LineKind.Dashed, tiling);
                    lineHolder.LineKind = LineKind.Dashed;
                    lineHolder.Tiling = tiling;
                    new ChangeLineKindNetAction(surface.name, surfaceParentName, selectedLine.name,
                            LineKind.Dashed, tiling).Execute();
                });

                /// Sets up the primary color button. 
                SetUpPrimaryColorButtonForEditing(selectedLine, lineHolder, surface, surfaceParentName);

                /// Sets up the secondary color button. 
                SetUpSecondaryColorButtonForEditing(selectedLine, lineHolder, surface, surfaceParentName);

                /// Set up the outline thickness slider.
                SetUpOutlineThicknessSliderForEditing(selectedLine, renderer, lineHolder, surface, surfaceParentName);

                /// Set up the order in layer slider.
                SetUpOrderInLayerSliderForEditing(selectedLine, lineHolder, surface, surfaceParentName);

                /// Set up the loop switch.
                SetUpLoopSwitchForEditing(selectedLine, lineHolder, surface, surfaceParentName);

                /// Set up the <see cref="HSVPicker.ColorPicker"/>.
                SetUpColorPickerForEditing(selectedLine, renderer, lineHolder, surface, surfaceParentName);

                mode = Mode.Edit;

                /// Re-calculates the menu height.
                MenuHelper.CalculateHeight(instance, true);
            }
        }

        /// <summary>
        /// Set up the return button.
        /// If the return call action is not null, 
        /// activate the return button and add the action to it. 
        /// This only occurs when editing mind map nodes. 
        /// In this case, the layer slider is set to inactive, as the order of a mind map node must be changed directly on the node.
        /// </summary>
        /// <param name="returnCall">The return call back to the parent menu.</param>
        private static void SetUpReturnButtonForEditing(UnityAction returnCall)
        {
            if (returnCall != null)
            {
                EnableReturn();
                ButtonManagerBasic returnBtn = GameFinder.FindChild(instance, "ReturnBtn").GetComponent<ButtonManagerBasic>();
                returnBtn.clickEvent.RemoveAllListeners();
                returnBtn.clickEvent.AddListener(returnCall);
                GameFinder.FindChild(instance, "Layer").GetComponentInChildren<Slider>().interactable = false;
            }
        }

        /// <summary>
        /// Set ups the line kind selector for editing mode.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="renderer">The line renderer of the selected line.</param>
        /// <param name="lineHolder">The configuration which holds the changes.</param>
        /// <param name="surface">The drawable surface on which the line is displayed.</param>
        /// <param name="surfaceParentName">The parent id of the drawable surface.</param>
        private static void SetUpLineKindSelectorForEditing(GameObject selectedLine, LineRenderer renderer,
            LineConf lineHolder, GameObject surface, string surfaceParentName)
        {
            /// Assigns the current <see cref="LineKind"/> of the selected line to the menu variable.
            AssignLineKind(selectedLine.GetComponent<LineValueHolder>().LineKind, renderer.textureScale.x);

            /// Gets and sets the current selected line kind index.
            lineKindSelector.index = GetIndexOfSelectedLineKind();

            /// Updates the selector.
            lineKindSelector.UpdateUI();

            /// Removes the current line kind action of the line kind selector.
            if (lineKindAction != null)
            {
                lineKindSelector.selectorEvent.RemoveListener(lineKindAction);
            }

            /// Creates a new line kind selector action
            lineKindAction = index =>
            {
                /// The action should be executed when the new <see cref="LineKind"/> is not <see cref="LineKind.Dashed"/>. 
                /// This is because it does not work without an additionally set tiling.
                if (GetLineKinds()[index] != LineKind.Dashed)
                {
                    lineHolder.LineKind = GetLineKinds()[index];

                    /// If you want to switch to <see cref="LineKind.Solid"/> but 
                    /// previously a Dashed LineKind with <see cref="ColorKind.TwoDashed"/> was active, 
                    /// you need also to switch the <see cref="ColorKind"/> to <see cref="ColorKind.Monochrome"/>.
                    if (lineHolder.LineKind == LineKind.Solid &&
                        lineHolder.ColorKind == ColorKind.TwoDashed)
                    {
                        lineHolder.ColorKind = ColorKind.Monochrome;
                        ChangeColorKind(selectedLine, lineHolder.ColorKind, lineHolder);
                        new ChangeColorKindNetAction(surface.name, surfaceParentName,
                            LineConf.GetLineWithoutRenderPos(selectedLine), lineHolder.ColorKind).Execute();
                    }

                    /// Apply the line kind change.
                    ChangeLineKind(selectedLine, lineHolder.LineKind, lineHolder.Tiling);
                    new ChangeLineKindNetAction(surface.name, surfaceParentName, selectedLine.name,
                            lineHolder.LineKind, lineHolder.Tiling).Execute();
                }
            };

            /// Adds the line kind selector action.
            lineKindSelector.selectorEvent.AddListener(lineKindAction);
        }

        /// <summary>
        /// Set ups the color kind selector for editing mode.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="renderer">The line renderer of the selected line.</param>
        /// <param name="lineHolder">The configuration which holds the changes.</param>
        /// <param name="surface">The drawable surface on which the line is displayed.</param>
        /// <param name="surfaceParentName">The parent id of the drawable surface.</param>
        private static void SetUpColorKindSelectorForEditing(GameObject selectedLine, LineRenderer renderer,
            LineConf lineHolder, GameObject surface, string surfaceParentName)
        {
            /// Assigns the current <see cref="ColorKind"/> of the selected line to the menu variable.
            AssignColorKind(lineHolder.ColorKind);
            /// Gets and sets the current selected color kind index.
            colorKindSelector.index = GetIndexOfSelectedColorKind();
            /// Updates the selector.
            colorKindSelector.UpdateUI();

            /// Removes the current color kind action of the color kind selector.
            if (colorKindAction != null)
            {
                colorKindSelector.selectorEvent.RemoveListener(colorKindAction);
            }

            /// Creates a new color kind selector action
            colorKindAction = index =>
            {
                lineHolder.ColorKind = GetColorKinds(true)[index];
                ChangeColorKind(selectedLine, lineHolder.ColorKind, lineHolder);
                new ChangeColorKindNetAction(surface.name, surfaceParentName, LineConf.GetLineWithoutRenderPos(selectedLine),
                    lineHolder.ColorKind).Execute();

                /// Activates the primary color button, if it changes to <see cref="ColorKind.Monochrome"/>
                if (lineHolder.ColorKind == ColorKind.Monochrome
                    && !secondaryColorBMB.buttonVar.IsInteractable())
                {
                    picker.onValueChanged.RemoveListener(colorAction);
                    MutuallyExclusiveColorButtons();
                    SetUpColorPickerForEditing(selectedLine, renderer, lineHolder, surface, surfaceParentName);
                }
            };

            /// Adds the color kind selector action.
            colorKindSelector.selectorEvent.AddListener(colorKindAction);
        }

        /// <summary>
        /// Set up the primary color button for editing mode.
        /// They mutually exclude each other with the secondary button. This means only one can be activated at a time.
        /// Furthermore, the action that should be executed on a color change is added.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="lineHolder">The configuration which holds the changes.</param>
        /// <param name="surface">The drawable surface on which the line is displayed.</param>
        /// <param name="surfaceParentName">The parent id of the drawable surface.</param>
        private static void SetUpPrimaryColorButtonForEditing(GameObject selectedLine, LineConf lineHolder,
            GameObject surface, string surfaceParentName)
        {
            /// Removes the old handler
            primaryColorBMB.clickEvent.RemoveAllListeners();
            /// Add mutually exclusive mode.
            primaryColorBMB.clickEvent.AddListener(MutuallyExclusiveColorButtons);
            /// Add new handler for <see cref="HSVPicker.ColorPicker"/>
            primaryColorBMB.clickEvent.AddListener(() =>
            {
                AssignColorArea(color =>
                {
                    GameEdit.ChangePrimaryColor(selectedLine, color);
                    lineHolder.PrimaryColor = color;
                    new EditLinePrimaryColorNetAction(surface.name, surfaceParentName, selectedLine.name, color).Execute();
                }, lineHolder.PrimaryColor);
            });
            /// Makes the button unclickable.
            primaryColorBMB.buttonVar.interactable = false;
        }

        /// <summary>
        /// Set up the secondary color button for editing mode.
        /// They mutually exclude each other with the primary button. This means only one can be activated at a time.
        /// Furthermore, the action that should be executed on a color change is added.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="lineHolder">The configuration which holds the changes.</param>
        /// <param name="surface">The drawable surface on which the line is displayed.</param>
        /// <param name="surfaceParentName">The parent id of the drawable surface.</param>
        private static void SetUpSecondaryColorButtonForEditing(GameObject selectedLine, LineConf lineHolder,
            GameObject surface, string surfaceParentName)
        {
            /// Removes the old handler.
            secondaryColorBMB.clickEvent.RemoveAllListeners();
            /// Add mutually exclusive mode.
            secondaryColorBMB.clickEvent.AddListener(MutuallyExclusiveColorButtons);

            /// Add new handler for <see cref="HSVPicker.ColorPicker"/>
            secondaryColorBMB.clickEvent.AddListener(() =>
            {
                /// If the <see cref="LineKind"/> was <see cref="LineKind.Solid"/> before, 
                /// the secondary color is clear. 
                /// Therefore, a random color is added first, 
                /// and if the color's alpha is 0, it is set to 255 to ensure the color is not transparent.
                if (lineHolder.SecondaryColor == Color.clear)
                {
                    lineHolder.SecondaryColor = Random.ColorHSV();
                }
                if (lineHolder.SecondaryColor.a == 0)
                {
                    lineHolder.SecondaryColor = new Color(lineHolder.SecondaryColor.r,
                        lineHolder.SecondaryColor.g, lineHolder.SecondaryColor.b, 255);
                }
                AssignColorArea(color =>
                {
                    GameEdit.ChangeSecondaryColor(selectedLine, color);
                    lineHolder.SecondaryColor = color;
                    new EditLineSecondaryColorNetAction(surface.name, surfaceParentName,
                        selectedLine.name, color).Execute();
                }, lineHolder.SecondaryColor);
            });
            /// Makes the button unclickable.
            secondaryColorBMB.buttonVar.interactable = true;
        }

        /// <summary>
        /// Set up the outline thickness slider for editing mode 
        /// with the current thickness of the selected line.
        /// Furthermore, the action that should be executed on a thickness change is added.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="renderer">The line renderer of the selected line.</param>
        /// <param name="lineHolder">The configuration which holds the changes.</param>
        /// <param name="surface">The drawable surface on which the line is displayed.</param>
        /// <param name="surfaceParentName">The parent id of the drawable surface.</param>
        private static void SetUpOutlineThicknessSliderForEditing(GameObject selectedLine, LineRenderer renderer,
            LineConf lineHolder, GameObject surface, string surfaceParentName)
        {
            ThicknessSliderController thicknessSlider = instance.GetComponentInChildren<ThicknessSliderController>();
            /// Assigns the current value to the slider.
            thicknessSlider.AssignValue(renderer.startWidth);
            /// Adds the handler for changing.
            thicknessSlider.OnValueChanged.AddListener(thickness =>
            {
                /// The thickness must be greater then zero.
                if (thickness > 0.0f)
                {
                    GameEdit.ChangeThickness(selectedLine, thickness);
                    lineHolder.Thickness = thickness;
                    new EditLineThicknessNetAction(surface.name, surfaceParentName,
                        selectedLine.name, thickness).Execute();
                }
            });
        }

        /// <summary>
        /// Sets up the order in layer slider for editing mode
        /// with the current order of the selected line.
        /// Furthermore, the action that should be executed on a order change is added.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="lineHolder">The configuration which holds the changes.</param>
        /// <param name="surface">The drawable surface on which the line is displayed.</param>
        /// <param name="surfaceParentName">The parent id of the drawable surface.</param>
        private static void SetUpOrderInLayerSliderForEditing(GameObject selectedLine, LineConf lineHolder,
            GameObject surface, string surfaceParentName)
        {
            LayerSliderController layerSlider = instance.GetComponentInChildren<LayerSliderController>();
            layerSlider.AssignMaxOrder(surface.GetComponent<DrawableHolder>().OrderInLayer);
            /// Assigns the current value to the slider.
            layerSlider.AssignValue(lineHolder.OrderInLayer);
            /// Adds the handler for changing.
            layerSlider.OnValueChanged.AddListener(layerOrder =>
            {
                GameEdit.ChangeLayer(selectedLine, layerOrder);
                lineHolder.OrderInLayer = layerOrder;
                new EditLayerNetAction(surface.name, surfaceParentName,
                    selectedLine.name, layerOrder).Execute();
            });
        }

        /// <summary>
        /// Sets up the switch for the line loop for editing mode
        /// with the current loop of the selected line.
        /// Furthermore, the function has been added to enable and disable the loop.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="lineHolder">The configuration which holds the changes.</param>
        /// <param name="surface">The drawable surface on which the line is displayed.</param>
        /// <param name="surfaceParentName">The parent id of the drawable surface.</param>
        private static void SetUpLoopSwitchForEditing(GameObject selectedLine, LineConf lineHolder,
            GameObject surface, string surfaceParentName)
        {
            /// Removes the old on handler.
            loopManager.OnEvents.RemoveAllListeners();

            /// Add the handler for turning on the switch.
            /// It enables the loop.
            loopManager.OnEvents.AddListener(() =>
            {
                GameEdit.ChangeLoop(selectedLine, true);
                lineHolder.Loop = true;
                new EditLineLoopNetAction(surface.name, surfaceParentName, selectedLine.name, true).Execute();
            });
            /// Removes the old off handler.
            loopManager.OffEvents.RemoveAllListeners();
            /// Adds the handler for turning off the switch.
            loopManager.OffEvents.AddListener(() =>
            {
                GameEdit.ChangeLoop(selectedLine, false);
                lineHolder.Loop = false;
                new EditLineLoopNetAction(surface.name, surfaceParentName, selectedLine.name, false).Execute();
            });

            /// Update the switch to the current value.
            loopManager.isOn = lineHolder.Loop;
            /// Updates the switch.
            RefreshLoop();
        }

        /// <summary>
        /// Set up the color picker for editing mode.
        /// It assigns the current primary line color and adds the initial handler.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="renderer">The line renderer of the selected line.</param>
        /// <param name="lineHolder">The configuration which holds the changes.</param>
        /// <param name="surface">The drawable surface on which the line is displayed.</param>
        /// <param name="surfaceParentName">The parent id of the drawable surface.</param>
        private static void SetUpColorPickerForEditing(GameObject selectedLine, LineRenderer renderer,
            LineConf lineHolder, GameObject surface, string surfaceParentName)
        {
            /// Assign the color to the <see cref="HSVPicker.ColorPicker"/> depending on 
            /// the current <see cref="ColorKind"/> of the selected line.
            switch (lineHolder.ColorKind)
            {
                case ColorKind.Monochrome:
                    picker.AssignColor(renderer.material.color);
                    break;
                case ColorKind.Gradient:
                    picker.AssignColor(renderer.startColor);
                    break;
                case ColorKind.TwoDashed:
                    picker.AssignColor(renderer.material.color);
                    break;
            }

            /// The inital action of the <see cref="HSVPicker.ColorPicker"/>.
            /// It can be changed by selecting a different ColorKind or the secondary color.
            picker.onValueChanged.AddListener(colorAction = color =>
            {
                GameEdit.ChangePrimaryColor(selectedLine, color);
                lineHolder.PrimaryColor = color;
                new EditLinePrimaryColorNetAction(surface.name, surfaceParentName,
                    selectedLine.name, color).Execute();
            });
        }
        #endregion

        /// <summary>
        /// This method removes the handler of the
        /// line kind selector, the color kind selector, 
        /// the primary and secondary color buttons,
        /// the tiling slider controller,
        /// the thickness slider controller, order in layer slider controller,
        /// the loop switch and the additional color action for the hsv color picker.
        /// </summary>
        private static void RemoveListeners()
        {
            /// Ensures that all menu items are enabled for removing the handlers.
            EnableLineMenuLayers();

            if (lineKindAction != null)
            {
                lineKindSelector.selectorEvent.RemoveListener(lineKindAction);
            }

            if (colorKindAction != null)
            {
                colorKindSelector.selectorEvent.RemoveListener(colorKindAction);
            }

            if (tilingAction != null)
            {
                if (selectedLineKind != LineKind.Dashed)
                {
                    tilingSlider.ResetToMin();
                }
                instance.GetComponentInChildren<FloatValueSliderController>().onValueChanged.RemoveListener(tilingAction);
            }
            primaryColorBMB.clickEvent.RemoveAllListeners();
            secondaryColorBMB.clickEvent.RemoveAllListeners();
            instance.GetComponentInChildren<ThicknessSliderController>().OnValueChanged.RemoveAllListeners();
            instance.GetComponentInChildren<LayerSliderController>().OnValueChanged.RemoveAllListeners();
            loopManager.OffEvents.RemoveAllListeners();
            loopManager.OnEvents.RemoveAllListeners();

            if (colorAction != null)
            {
                instance.GetComponentInChildren<HSVPicker.ColorPicker>().onValueChanged.RemoveListener(colorAction);
            }
        }

        /// <summary>
        /// Assigns an action and a color to the HSV Color Picker.
        /// </summary>
        /// <param name="colorAction">The color action that should be assigned</param>
        /// <param name="color">The color that should be assigned.</param>
        public static void AssignColorArea(UnityAction<Color> newColorAction, Color color)
        {
            if (colorAction != null)
            {
                picker.onValueChanged.RemoveListener(colorAction);
            }
            colorAction = newColorAction;
            picker.AssignColor(color);
            picker.onValueChanged.AddListener(newColorAction);
        }

        #region LineKind
        /// <summary>
        /// Returns the index of the current selected line kind.
        /// </summary>
        /// <returns>Index of selected line kind</returns>
        private static int GetIndexOfSelectedLineKind()
        {
            return GetLineKinds().IndexOf(selectedLineKind);
        }

        /// <summary>
        /// Assigns a line kind to the line kind selection.
        /// </summary>
        /// <param name="kind">The line kind that should be assigned</param>
        public static void AssignLineKind(LineKind kind)
        {
            selectedLineKind = kind;
            MenuHelper.CalculateHeight(instance, true);
        }

        /// <summary>
        /// Assign a line kind and a tiling to the line kind selection and tiling slider controller.
        /// </summary>
        /// <param name="kind">The line kind that should be assigned.</param>
        /// <param name="tiling"></param>
        public static void AssignLineKind(LineKind kind, float tiling)
        {
            selectedLineKind = kind;

            /// Enables the tiling layer if the chosen <see cref="LineKind"/>
            /// is <see cref="LineKind.Dashed"/>.
            if (kind == LineKind.Dashed)
            {
                EnableTilingFromLineMenu();
                tilingSlider.AssignValue(tiling);
            }
            else
            { /// In all other cases the tiling layer will disabled.
                DisableTilingFromLineMenu();
            }
            MenuHelper.CalculateHeight(instance, true);
        }
        #endregion

        #region ColorKind
        /// <summary>
        /// Returns the index of the current selected color kind.
        /// </summary>
        /// <returns>Index of selected color kind</returns>
        private static int GetIndexOfSelectedColorKind()
        {
            return GetColorKinds(true).IndexOf(selectedColorKind);
        }

        /// <summary>
        /// Assigns a color kind to the color kind selection.
        /// </summary>
        /// <param name="kind">The line kind that should be assigned</param>
        public static void AssignColorKind(ColorKind kind)
        {
            selectedColorKind = kind;

            /// Disables the color area (primary / secondary color button) if
            /// the <see cref="ColorKind.Monochrome"/> was chosen.
            if (kind == ColorKind.Monochrome)
            {
                DisableColorAreaFromLineMenu();
            }
            else
            {
                /// For all other <see cref="ColorKind"/> enable the color area.
                EnableColorAreaFromLineMenu();
            }
            MenuHelper.CalculateHeight(instance, true);
        }
        #endregion

        /// <summary>
        /// This method will be used as an action for the Handler of the color buttons (primary/secondary).
        /// This allows only one color to be active at a time.
        /// </summary>
        private static void MutuallyExclusiveColorButtons()
        {
            primaryColorBMB.buttonVar.interactable = !primaryColorBMB.buttonVar.IsInteractable();
            secondaryColorBMB.buttonVar.interactable = !secondaryColorBMB.buttonVar.IsInteractable();
        }

        /// <summary>
        /// Refreshes the horizontal selectors of this.
        /// </summary>
        public static void RefreshHorizontalSelectors()
        {
            lineKindSelector.index = LineMenu.GetIndexOfSelectedLineKind();
            colorKindSelector.index = LineMenu.GetIndexOfSelectedColorKind();
            lineKindSelector.UpdateUI();
            colorKindSelector.UpdateUI();
        }

        #region Enable/Disable Layer
        /// <summary>
        /// Enables all line menu layers that can be hidden.
        /// </summary>
        private static void EnableLineMenuLayers()
        {
            EnableLineKindFromLineMenu();
            EnableTilingFromLineMenu();
            EnableLoopFromLineMenu();
            EnableLayerFromLineMenu();
            EnableThicknessFromLineMenu();
        }

        /// <summary>
        /// Hides the line kind layer
        /// </summary>
        private static void DisableLineKindFromLineMenu()
        {
            content.Find("LineKindSelection").gameObject.SetActive(false);
        }

        /// <summary>
        /// Enables the line kind layer
        /// </summary>
        private static void EnableLineKindFromLineMenu()
        {
            if (selectedLineKind != LineKind.Dashed)
            {
                tilingSlider.ResetToMin();
            }
            content.Find("LineKindSelection").gameObject.SetActive(true);
        }

        /// <summary>
        /// Hides the tiling layer
        /// </summary>
        private static void DisableTilingFromLineMenu()
        {
            tilingSlider.ResetToMin();
            content.Find("Tiling").gameObject.SetActive(false);
        }

        /// <summary>
        /// Enables the tiling layer.
        /// </summary>
        private static void EnableTilingFromLineMenu()
        {
            content.Find("Tiling").gameObject.SetActive(true);
        }

        /// <summary>
        /// Hides the order in layer layer.
        /// </summary>
        private static void DisableLayerFromLineMenu()
        {
            content.Find("Layer").gameObject.SetActive(false);
        }

        /// <summary>
        /// Enables the order in layer layer.
        /// </summary>
        private static void EnableLayerFromLineMenu()
        {
            content.Find("Layer").gameObject.SetActive(true);
            content.Find("Layer").GetComponentInChildren<Slider>().interactable = true;
        }

        /// <summary>
        /// Hides the line thickness layer.
        /// </summary>
        private static void DisableThicknessFromLineMenu()
        {
            content.Find("Thickness").gameObject.SetActive(false);
        }

        /// <summary>
        /// Enables the line thickness layer.
        /// </summary>
        private static void EnableThicknessFromLineMenu()
        {
            content.Find("Thickness").gameObject.SetActive(true);
        }

        /// <summary>
        /// Hides the loop layer.
        /// </summary>
        private static void DisableLoopFromLineMenu()
        {
            content.Find("Loop").gameObject.SetActive(false);
        }

        /// <summary>
        /// Enables the loop layer.
        /// </summary>
        private static void EnableLoopFromLineMenu()
        {
            content.Find("Loop").gameObject.SetActive(true);
        }

        /// <summary>
        /// Refreshes the loop layer.
        /// Will be needed for the switching the editing of lines.
        /// </summary>
        private static void RefreshLoop()
        {
            DisableLoopFromLineMenu();
            EnableLoopFromLineMenu();
        }

        /// <summary>
        /// Hides the color area selector layer.
        /// </summary>
        private static void DisableColorAreaFromLineMenu()
        {
            content.Find("ColorAreaSelector").gameObject.SetActive(false);
        }

        /// <summary>
        /// Enables the color area selector layer.
        /// </summary>
        private static void EnableColorAreaFromLineMenu()
        {
            content.Find("ColorAreaSelector").gameObject.SetActive(true);
        }

        /// <summary>
        /// Hides the return button.
        /// </summary>
        private static void DisableReturn()
        {
            instance.transform.Find("ReturnBtn").gameObject.SetActive(false);
        }

        /// <summary>
        /// Enables the return button
        /// </summary>
        private static void EnableReturn()
        {
            instance.transform.Find("ReturnBtn").gameObject.SetActive(true);
        }
        #endregion
    }
}
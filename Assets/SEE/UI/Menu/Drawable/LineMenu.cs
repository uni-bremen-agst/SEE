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
    /// This class provides a line menu.
    /// </summary>
    public class LineMenu : SingletonMenu
    {
        #region attributes
        /// <summary>
        /// The location where the line menu prefeb is placed.
        /// </summary>
        private const string lineMenuPrefab = "Prefabs/UI/Drawable/LineMenu";

        /// <summary>
        /// We do not want to create an instance of this singleton class outside of this class.
        /// </summary>
        private LineMenu() { }

        /// <summary>
        /// The only instance of this singleton class.
        /// </summary>
        public static LineMenu Instance { get; private set; }

        /// <summary>
        /// Returns the associated game object of the line menu.
        /// </summary>
        public GameObject GameObject => Instance.gameObject;

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
        /// The additionally action for the color-kind selector.
        /// </summary>
        private static UnityAction<int> colorKindAction;

        /// <summary>
        /// The additionally clear fill-out color action.
        /// </summary>
        private static UnityAction clearFillOutColorAction;

        /// <summary>
        /// The transform of the content object.
        /// </summary>
        private static readonly Transform content;

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
        private static readonly FloatValueSliderController tilingSlider;

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
        private static readonly SwitchManager loopManager;

        /// <summary>
        /// Button manager for chosing the primary color.
        /// </summary>
        private static readonly ButtonManagerBasic primaryColorBMB;

        /// <summary>
        /// Button manager for chosing the secondary color.
        /// </summary>
        private static readonly ButtonManagerBasic secondaryColorBMB;

        /// <summary>
        /// The HSVPicker ColorPicker component of the line menu.
        /// </summary>
        private static readonly HSVPicker.ColorPicker picker;

        /// <summary>
        /// Button manager for chosing the color-kind area.
        /// </summary>
        private static readonly ButtonManagerBasic colorKindBMB;

        /// <summary>
        /// Button manager for chosing the fill-out area.
        /// </summary>
        private static readonly ButtonManagerBasic fillOutBMB;

        /// <summary>
        /// The switch manager for the fill-out status.
        /// </summary>
        private static readonly SwitchManager fillOutManager;

        /// <summary>
        /// The mode of manipulating.
        /// </summary>
        private enum Mode
        {
            None,
            Drawing,
            Edit
        }

        /// <summary>
        /// The current mode of the line menu.
        /// </summary>
        private static Mode mode;
        #endregion

        /// <summary>
        /// An enum with the menu points that can be disabled.
        /// </summary>
        private enum MenuLayer
        {
            LineKind,
            Thickness,
            Layer,
            Loop,
            All
        }

        /// <summary>
        /// The constructor. It creates the instance for the line menu and
        /// adds the menu layer to the corresponding game objects.
        /// By default, the menu is hidden.
        /// </summary>
        static LineMenu()
        {
            Instance = new LineMenu();

            /// Instantiates the menu.
            Instance.Instantiate(lineMenuPrefab);

            /// Initialize the content area
            content = Instance.gameObject.transform.Find("Content");

            /// Disables the ability to return to the previous menu.
            /// Intended only for editing MindMap nodes.
            Instance.DisableReturn();

            /// Initialize and sets up the line kind selector.
            Instance.InitLineKindSelectorConstructor();

            /// Initialize and sets up the color-kind selector.
            Instance.InitColorKindSelectorConstructor();

            /// Initialize the remaining GUI elements.
            loopManager = GameFinder.FindChild(Instance.gameObject, "Loop").GetComponentInChildren<SwitchManager>();
            primaryColorBMB = GameFinder.FindChild(Instance.gameObject, "PrimaryColorBtn").GetComponent<ButtonManagerBasic>();
            primaryColorBMB.buttonVar = GameFinder.FindChild(Instance.gameObject, "PrimaryColorBtn").GetComponent<Button>();
            primaryColorBMB.clickEvent.AddListener(MutuallyExclusiveColorButtons);
            primaryColorBMB.buttonVar.interactable = false;
            secondaryColorBMB = GameFinder.FindChild(Instance.gameObject, "SecondaryColorBtn").GetComponent<ButtonManagerBasic>();
            secondaryColorBMB.buttonVar = GameFinder.FindChild(Instance.gameObject, "SecondaryColorBtn").GetComponent<Button>();
            secondaryColorBMB.clickEvent.AddListener(MutuallyExclusiveColorButtons);
            tilingSlider = GameFinder.FindChild(Instance.gameObject, "Tiling").GetComponentInChildren<FloatValueSliderController>();
            picker = Instance.gameObject.GetComponentInChildren<HSVPicker.ColorPicker>();
            colorKindBMB = GameFinder.FindChild(Instance.gameObject, "ColorKindBtn").GetComponent<ButtonManagerBasic>();
            colorKindBMB.buttonVar = GameFinder.FindChild(Instance.gameObject, "ColorKindBtn").GetComponent<Button>();
            colorKindBMB.clickEvent.AddListener(MutuallyExclusiveColorTypeButtons);
            colorKindBMB.buttonVar.interactable = false;
            fillOutBMB = GameFinder.FindChild(Instance.gameObject, "FillOutBtn").GetComponent<ButtonManagerBasic>();
            fillOutBMB.buttonVar = GameFinder.FindChild(Instance.gameObject, "FillOutBtn").GetComponent<Button>();
            fillOutBMB.clickEvent.AddListener(MutuallyExclusiveColorTypeButtons);
            fillOutManager = GameFinder.FindChild(Instance.gameObject, "FillOut").GetComponentInChildren<SwitchManager>();
            mode = Mode.None;
            Instance.Disable();
        }

        /// <summary>
        /// Initializes the default line kind selector for the constructor.
        /// </summary>
        private void InitLineKindSelectorConstructor()
        {
            lineKindSelector = GameFinder.FindChild(Instance.gameObject, "LineKindSelection")
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
        /// Initializes the default color-kind selector for the constructor.
        /// </summary>
        private void InitColorKindSelectorConstructor()
        {
            colorKindSelector = GameFinder.FindChild(Instance.gameObject, "ColorKindSelection")
                .GetComponent<HorizontalSelector>();
            bool isDashed = selectedLineKind != LineKind.Solid;

            /// Creates the items for the color-kind selector.
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

        #region IsOpen
        /// <summary>
        /// True if the menu is already open.
        /// </summary>
        /// <returns>True, if the menu is already open. Otherwise false.</returns>
        public override bool IsOpen()
        {
            return gameObject.activeInHierarchy;
        }

        /// <summary>
        /// Returns true if the line menu is in drawing mode.
        /// </summary>
        /// <returns>true if in drawing mode</returns>
        public bool IsInDrawingMode()
        {
            return gameObject.activeInHierarchy && mode == Mode.Drawing;
        }

        /// <summary>
        /// Returns true if the line menu is in edit mode.
        /// </summary>
        /// <returns>true if in edit mode</returns>
        public bool IsInEditMode()
        {
            return gameObject.activeInHierarchy && mode == Mode.Edit;
        }
        #endregion

        /// <summary>
        /// Enables all line menu layers,
        /// sets the parent back to UI Canvas, enables the window dragger
        /// and then hides the line menu.
        /// The parent of the line menu can be switched through the <see cref="DrawShapesAction"/>
        /// </summary>
        public override void Disable()
        {
            base.Disable();
            EnableLineMenuLayers();
            gameObject.transform.SetParent(UICanvas.Canvas.transform);
            GameFinder.FindChild(gameObject, "Dragger").GetComponent<WindowDragger>().enabled = true;
            DisableReturn();
            mode = Mode.None;
        }

        #region Enable Line Menu
        /// <summary>
        /// Enables the line menu and resets the additional handlers if <paramref name="removeListeners"/> is true.
        /// It can also hide some menu layer.
        /// </summary>
        /// <param name="removeListeners">Whether the handler should be reset.</param>
        /// <param name="withoutMenuLayer">An array of menu layers that should hidden.</param>
        private void EnableLineMenu(bool removeListeners = true, MenuLayer[] withoutMenuLayer = null)
        {
            /// Removes the listeners of the GUI elements if the <paramref name="removeListeners"/> is true.
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

            if (selectedLineKind != LineKind.Dashed)
            {
                DisableTilingFromLineMenu();
            }

            Enable();

            MenuHelper.CalculateHeight(gameObject, true);
        }

        #region Drawing
        /// <summary>
        /// Enables the line menu for drawing.
        /// </summary>
        public void EnableForDrawing()
        {
            EnableLineMenu(withoutMenuLayer: new MenuLayer[] { MenuLayer.Layer, MenuLayer.Loop });
            InitDrawing();
            mode = Mode.Drawing;
            MenuHelper.CalculateHeight(gameObject, true);
        }

        /// <summary>
        /// Initializes the handlers for the drawing.
        /// </summary>
        private void InitDrawing()
        {
            /// Initializes the tiling slider and
            /// saves the changes in the global value for the tiling <see cref="ValueHolder.CurrentTiling"/>.
            tilingSlider.onValueChanged.AddListener(tilingAction = tiling =>
            {
                ValueHolder.CurrentTiling = tiling;
            });

            SetUpLineKindSelectorForDrawing();
            SetUpColorKindSelectorForDrawing();
            SetUpPrimaryColorButtonForDrawing();
            SetUpSecondaryColorButtonForDrawing();
            SetUpOutlineThicknessSliderForDrawing();
            SetUpColorKindTypeButtonForDrawing();
            SetUpFillOutTypeButtonForDrawing();
            SetUpFillOutTypeSwitchForDrawing();

            /// Assigns the current primary color to the <see cref="HSVPicker.ColorPicker"/>.
            picker.AssignColor(ValueHolder.CurrentPrimaryColor);
            picker.onValueChanged.AddListener(colorAction = color => ValueHolder.CurrentPrimaryColor = color);

            /// At last re-calculate the menu height.
            MenuHelper.CalculateHeight(gameObject, true);
        }

        /// <summary>
        /// Sets up the line kind selector with the currently selected <see cref="LineKind"/> and
        /// saves the changes in the global value for it. <see cref="ValueHolder.CurrentLineKind"/>.
        /// </summary>
        private void SetUpLineKindSelectorForDrawing()
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
                if (ValueHolder.CurrentLineKind == LineKind.Solid
                    && ValueHolder.CurrentColorKind == ColorKind.TwoDashed)
                {
                    ValueHolder.CurrentColorKind = ColorKind.Monochrome;
                }
            };

            /// Add the action to the selector.
            lineKindSelector.selectorEvent.AddListener(lineKindAction);
        }

        /// <summary>
        /// Sets up the color-kind selector with the currently selected <see cref="ColorKind"/> and
        /// saves the changes global value for it. <see cref="ValueHolder.CurrentColorKind"/>.
        /// </summary>
        private void SetUpColorKindSelectorForDrawing()
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

                /// Sets the secondary color if it is transparent.
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
        /// Sets up the primary color button for drawing mode.
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
        /// Sets up the secondary color button for the drawing mode.
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
                /// the secondary color is cleared.
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
        /// Sets up the outline thickness slider for drawing mode.
        /// The changes will be saved in the global value <see cref="ValueHolder.CurrentThickness"/>.
        /// </summary>
        private void SetUpOutlineThicknessSliderForDrawing()
        {
            ThicknessSliderController thicknessSlider = gameObject.GetComponentInChildren<ThicknessSliderController>();
            /// Assigns the current value to the slider.
            thicknessSlider.AssignValue(ValueHolder.CurrentThickness);
            /// Add the handler.
            thicknessSlider.OnValueChanged.AddListener(thickness =>
            {
                ValueHolder.CurrentThickness = thickness;
            });
        }

        /// <summary>
        /// Sets up the color-kind type area for the drawing mode.
        /// </summary>
        private static void SetUpColorKindTypeButtonForDrawing()
        {
            colorKindBMB.clickEvent.RemoveAllListeners();
            colorKindBMB.clickEvent.AddListener(MutuallyExclusiveColorTypeButtons);
            colorKindBMB.clickEvent.AddListener(() =>
            {
                DisableFillOut();
                EnableColorKind();
                MenuHelper.CalculateHeight(Instance.gameObject, true);
                if (!primaryColorBMB.buttonVar.interactable)
                {
                    AssignColorArea(color => { ValueHolder.CurrentPrimaryColor = color; }, ValueHolder.CurrentPrimaryColor);
                }
                else
                {
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
                }
            });
            colorKindBMB.buttonVar.interactable = false;
        }

        /// <summary>
        /// Sets up the fill-out type area for the drawing mode.
        /// </summary>
        private static void SetUpFillOutTypeButtonForDrawing()
        {
            fillOutBMB.clickEvent.RemoveAllListeners();
            fillOutBMB.clickEvent.AddListener(MutuallyExclusiveColorTypeButtons);
            fillOutBMB.clickEvent.AddListener(() =>
            {
                DisableColorKind();
                EnableFillOut();
                MenuHelper.CalculateHeight(Instance.gameObject, true);
                if (ValueHolder.CurrentTertiaryColor == Color.clear)
                {
                    ValueHolder.CurrentTertiaryColor = ValueHolder.CurrentPrimaryColor;
                }
                AssignColorArea(color => { ValueHolder.CurrentTertiaryColor = color; }, ValueHolder.CurrentTertiaryColor);
            });
            fillOutBMB.buttonVar.interactable = true;
        }

        /// <summary>
        /// Sets up the fill-out type switch for the drawing mode.
        /// </summary>
        private static void SetUpFillOutTypeSwitchForDrawing()
        {
            fillOutManager.OnEvents.RemoveAllListeners();
            fillOutManager.OnEvents.AddListener(() => ValueHolder.CurrentFillOutStatus = true);
            fillOutManager.OffEvents.RemoveAllListeners();
            fillOutManager.OffEvents.AddListener(() => ValueHolder.CurrentFillOutStatus = true);
            fillOutManager.isOn = ValueHolder.CurrentFillOutStatus;
            RefreshFillOut();
        }

        /// <summary>
        /// Gets the fill-out color for the drawing mode.
        /// </summary>
        /// <returns>null or the currently selected fill-out color.</returns>
        public static Color? GetFillOutColorForDrawing()
        {
            if (fillOutManager.isOn)
            {
                return ValueHolder.CurrentTertiaryColor;
            }
            else
            {
                return null;
            }
        }
        #endregion

        #region Editing
        /// <summary>
        /// Provides the line menu for editing, adding the necessary handlers to the respective components.
        /// </summary>
        /// <param name="selectedLine">The selected line object for editing.</param>
        public void EnableForEditing(GameObject selectedLine, DrawableType newValueHolder, UnityAction returnCall = null)
        {
            if (newValueHolder is LineConf lineHolder)
            {
                EnableLineMenu();
                SetUpReturnButtonForEditing(returnCall);

                LineRenderer renderer = selectedLine.GetComponent<LineRenderer>();
                GameObject surface = GameFinder.GetDrawableSurface(selectedLine);
                string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);

                /// Sets up the line kind selector.
                SetUpLineKindSelectorForEditing(selectedLine, renderer, lineHolder, surface, surfaceParentName);

                /// Sets up the color-kind selector.
                SetUpColorKindSelectorForEditing(selectedLine, renderer, lineHolder, surface, surfaceParentName);

                /// Adds the action that should be executed if the tiling silder changed.
                /// It is only is available for <see cref="LineKind.Dashed"/>.
                tilingSlider.onValueChanged.AddListener(tilingAction = tiling =>
                {
                    ChangeLineKind(selectedLine, LineKind.Dashed, tiling);
                    lineHolder.LineKind = LineKind.Dashed;
                    lineHolder.Tiling = tiling;
                    new ChangeLineKindNetAction(surface.name, surfaceParentName, selectedLine.name,
                            LineKind.Dashed, tiling).Execute();
                });

                /// Sets up the components.
                SetUpPrimaryColorButtonForEditing(selectedLine, lineHolder, surface, surfaceParentName);
                SetUpSecondaryColorButtonForEditing(selectedLine, lineHolder, surface, surfaceParentName);
                SetUpOutlineThicknessSliderForEditing(selectedLine, renderer, lineHolder, surface, surfaceParentName);
                SetUpOrderInLayerSliderForEditing(selectedLine, lineHolder, surface, surfaceParentName);
                SetUpLoopSwitchForEditing(selectedLine, lineHolder, surface, surfaceParentName);
                SetUpColorPickerForEditing(selectedLine, renderer, lineHolder, surface, surfaceParentName);
                SetUpColorKindTypeButtonForEditing(selectedLine, lineHolder, surface, surfaceParentName);
                SetUpFillOutTypeButtonForEditing(selectedLine, lineHolder, surface, surfaceParentName);
                SetUpFillOutSwitchForEditing(selectedLine, lineHolder, surface, surfaceParentName);

                mode = Mode.Edit;

                /// Re-calculates the menu height.
                MenuHelper.CalculateHeight(gameObject, true);
            }
        }

        /// <summary>
        /// Sets up the return button.
        /// If the return call action is not null, activates the return button and
        /// adds the action to it. This occurs only if editing mind map nodes.
        /// In this case, the layer slider is set to inactive, as the order of a mind map node
        /// must be changed directly on the node.
        /// </summary>
        /// <param name="returnCall">The return call back to the parent menu.</param>
        private void SetUpReturnButtonForEditing(UnityAction returnCall)
        {
            if (returnCall != null)
            {
                EnableReturn();
                ButtonManagerBasic returnBtn = GameFinder.FindChild(gameObject, "ReturnBtn").GetComponent<ButtonManagerBasic>();
                returnBtn.clickEvent.RemoveAllListeners();
                returnBtn.clickEvent.AddListener(returnCall);
                GameFinder.FindChild(gameObject, "Layer").GetComponentInChildren<Slider>().interactable = false;
            }
        }

        /// <summary>
        /// Sets up the line kind selector for editing mode.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="renderer">The line renderer of the selected line.</param>
        /// <param name="lineHolder">The configuration which holds the changes.</param>
        /// <param name="surface">The drawable surface on which the line is displayed.</param>
        /// <param name="surfaceParentName">The parent id of the drawable surface.</param>
        private void SetUpLineKindSelectorForEditing(GameObject selectedLine, LineRenderer renderer,
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
        /// Sets up the color-kind selector for editing mode.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="renderer">The line renderer of the selected line.</param>
        /// <param name="lineHolder">The configuration which holds the changes.</param>
        /// <param name="surface">The drawable surface on which the line is displayed.</param>
        /// <param name="surfaceParentName">The parent id of the drawable surface.</param>
        private void SetUpColorKindSelectorForEditing(GameObject selectedLine, LineRenderer renderer,
            LineConf lineHolder, GameObject surface, string surfaceParentName)
        {
            /// Assigns the current <see cref="ColorKind"/> of the selected line to the menu variable.
            AssignColorKind(lineHolder.ColorKind);
            /// Gets and sets the current selected color-kind index.
            colorKindSelector.index = GetIndexOfSelectedColorKind();
            /// Updates the selector.
            colorKindSelector.UpdateUI();

            /// Removes the current color-kind action of the color-kind selector.
            if (colorKindAction != null)
            {
                colorKindSelector.selectorEvent.RemoveListener(colorKindAction);
            }

            /// Creates a new color-kind selector action
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

            /// Adds the color-kind selector action.
            colorKindSelector.selectorEvent.AddListener(colorKindAction);
        }

        /// <summary>
        /// Sets up the primary color button for editing mode.
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
        /// Sets up the secondary color button for editing mode.
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
        /// Sets up the outline thickness slider for editing mode
        /// with the current thickness of the selected line.
        /// Furthermore, the action that should be executed on a thickness change is added.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="renderer">The line renderer of the selected line.</param>
        /// <param name="lineHolder">The configuration which holds the changes.</param>
        /// <param name="surface">The drawable surface on which the line is displayed.</param>
        /// <param name="surfaceParentName">The parent id of the drawable surface.</param>
        private void SetUpOutlineThicknessSliderForEditing(GameObject selectedLine, LineRenderer renderer,
            LineConf lineHolder, GameObject surface, string surfaceParentName)
        {
            ThicknessSliderController thicknessSlider = gameObject.GetComponentInChildren<ThicknessSliderController>();
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
        /// Furthermore, the action that should be executed on an order change is added.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="lineHolder">The configuration which holds the changes.</param>
        /// <param name="surface">The drawable surface on which the line is displayed.</param>
        /// <param name="surfaceParentName">The parent id of the drawable surface.</param>
        private void SetUpOrderInLayerSliderForEditing(GameObject selectedLine, LineConf lineHolder,
            GameObject surface, string surfaceParentName)
        {
            LayerSliderController layerSlider = gameObject.GetComponentInChildren<LayerSliderController>();
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
        /// Sets up the color picker for editing mode.
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

        /// <summary>
        /// Sets up the color-kind type button for editing.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="lineHolder">The configuration which holds the changes.</param>
        /// <param name="surface">The drawable surface on which the line is displayed.</param>
        /// <param name="surfaceParentName">The parent id of the drawable surface.</param>
        private static void SetUpColorKindTypeButtonForEditing(GameObject selectedLine, LineConf lineHolder,
            GameObject surface, string surfaceParentName)
        {
            colorKindBMB.clickEvent.RemoveAllListeners();
            colorKindBMB.clickEvent.AddListener(MutuallyExclusiveColorTypeButtons);
            colorKindBMB.clickEvent.AddListener(() =>
            {
                DisableFillOut();
                EnableColorKind();
                MenuHelper.CalculateHeight(Instance.gameObject, true);
                if (!primaryColorBMB.buttonVar.interactable)
                {
                    AssignColorArea(color =>
                    {
                        GameEdit.ChangePrimaryColor(selectedLine, color);
                        lineHolder.PrimaryColor = color;
                        new EditLinePrimaryColorNetAction(surface.name, surfaceParentName, selectedLine.name, color).Execute();
                    }, lineHolder.PrimaryColor);
                }
                else
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
                }
            });
            colorKindBMB.buttonVar.interactable = false;
        }

        /// <summary>
        /// Sets up the fill-out type button for editing.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="lineHolder">The configuration which holds the changes.</param>
        /// <param name="surface">The drawable surface on which the line is displayed.</param>
        /// <param name="surfaceParentName">The parent id of the drawable surface.</param>
        private static void SetUpFillOutTypeButtonForEditing(GameObject selectedLine, LineConf lineHolder,
            GameObject surface, string surfaceParentName)
        {
            fillOutBMB.clickEvent.RemoveAllListeners();
            fillOutBMB.clickEvent.AddListener(MutuallyExclusiveColorTypeButtons);
            fillOutBMB.clickEvent.AddListener(() =>
            {
                DisableColorKind();
                EnableFillOut();

                if (lineHolder.FillOutStatus && !GameFinder.FindChild(selectedLine, ValueHolder.FillOut))
                {
                    if (FillOut(selectedLine, lineHolder.FillOutColor))
                    {
                        new DrawingFillOutNetAction(surface.name, surfaceParentName, selectedLine.name, lineHolder.FillOutColor).Execute();
                        BlinkEffect.AddFillOutToEffect(selectedLine);
                    }
                }
                AssignColorArea(color =>
                {
                    GameEdit.ChangeFillOutColor(selectedLine, color);
                    lineHolder.FillOutColor = color;
                    new EditLineFillOutColorNetAction(surface.name, surfaceParentName,
                        selectedLine.name, color).Execute();
                }, lineHolder.FillOutColor);

                MenuHelper.CalculateHeight(Instance.gameObject, true);
            });
            fillOutBMB.buttonVar.interactable = true;
        }

        /// <summary>
        /// Sets up the fill-out switch for editing.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="lineHolder">The configuration which holds the changes.</param>
        /// <param name="surface">The drawable surface on which the line is displayed.</param>
        /// <param name="surfaceParentName">The parent id of the drawable surface.</param>
        private static void SetUpFillOutSwitchForEditing(GameObject selectedLine, LineConf lineHolder,
            GameObject surface, string surfaceParentName)
        {
            fillOutManager.OnEvents.RemoveAllListeners();
            fillOutManager.OnEvents.AddListener(() =>
            {
                lineHolder.FillOutStatus = true;
                if (lineHolder.FillOutColor == Color.clear)
                {
                    lineHolder.FillOutColor = lineHolder.PrimaryColor;
                    picker.AssignColor(lineHolder.FillOutColor);
                }
                else if (lineHolder.FillOutColor != picker.CurrentColor)
                {
                    lineHolder.FillOutColor = picker.CurrentColor.WithAlpha(255);
                }

                if (FillOut(selectedLine, lineHolder.FillOutColor))
                {
                    new DrawingFillOutNetAction(surface.name, surfaceParentName, selectedLine.name,
                        lineHolder.FillOutColor).Execute();
                    if (BlinkEffect.CanFillOutBeAdded(selectedLine))
                    {
                        BlinkEffect.AddFillOutToEffect(selectedLine);
                    }
                }

                AssignColorArea(color =>
                {
                    GameEdit.ChangeFillOutColor(selectedLine, color);
                    lineHolder.FillOutColor = color;
                    new EditLineFillOutColorNetAction(surface.name, surfaceParentName,
                        selectedLine.name, color).Execute();
                }, lineHolder.FillOutColor);
            });

            fillOutManager.OffEvents.RemoveAllListeners();
            fillOutManager.OffEvents.AddListener(() =>
            {
                lineHolder.FillOutStatus = false;
                if (colorAction != null)
                {
                    picker.onValueChanged.RemoveListener(colorAction);
                }
                if (clearFillOutColorAction != null)
                {
                    clearFillOutColorAction.Invoke();
                }
                BlinkEffect.RemoveFillOutFromEffect(selectedLine);
                GameObject.DestroyImmediate(GameFinder.FindChild(selectedLine, ValueHolder.FillOut));
                new DeleteFillOutNetAction(surface.name, surfaceParentName, selectedLine.name).Execute();
            });

            fillOutManager.isOn = lineHolder.FillOutStatus;
            RefreshFillOut();
        }

        /// <summary>
        /// Assigns a fill-out status and color to the edit mode.
        /// </summary>
        /// <param name="fillOut">The status and color.</param>
        /// <param name="setFillOutAction">fill-out color change action.</param>
        /// <param name="clearFillOutAction">Action to clear the value.</param>
        public static void AssignFillOutForEditing(Color? fillOut, UnityAction<Color> setFillOutAction, UnityAction clearFillOutAction)
        {
            if (Instance.IsInEditMode() && !fillOutBMB.buttonVar.interactable)
            {
                if (fillOut != null && setFillOutAction != null)
                {
                    fillOutManager.isOn = true;
                    if (FillOut(DrawShapesAction.currentShape, fillOut))
                    {
                        GameObject surface = GameFinder.GetDrawableSurface(DrawShapesAction.currentShape);
                        new DrawingFillOutNetAction(surface.name, GameFinder.GetDrawableSurfaceParentName(surface),
                            DrawShapesAction.currentShape.name, LineConf.GetLine(DrawShapesAction.currentShape).FillOutColor).Execute();
                        if (BlinkEffect.CanFillOutBeAdded(DrawShapesAction.currentShape))
                        {
                            BlinkEffect.AddFillOutToEffect(DrawShapesAction.currentShape);
                        }
                    }
                    if (colorAction != setFillOutAction)
                    {
                        AssignColorArea(setFillOutAction, fillOut.Value);
                    }
                    clearFillOutColorAction = clearFillOutAction;
                }
                else
                {
                    fillOutManager.isOn = false;
                    fillOutManager.OffEvents.Invoke();
                    RefreshFillOut();
                }
            }
        }
        #endregion
        #endregion

        /// <summary>
        /// Removes the handler of the line kind selector, the color-kind selector,
        /// the primary and secondary color buttons, the tiling slider controller,
        /// the thickness slider controller, order-in-layer slider controller,
        /// the loop switch and the additional color action for the HSV color picker.
        /// </summary>
        private void RemoveListeners()
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
                gameObject.GetComponentInChildren<FloatValueSliderController>().onValueChanged.RemoveListener(tilingAction);
            }
            primaryColorBMB.clickEvent.RemoveAllListeners();
            secondaryColorBMB.clickEvent.RemoveAllListeners();
            gameObject.GetComponentInChildren<ThicknessSliderController>().OnValueChanged.RemoveAllListeners();
            gameObject.GetComponentInChildren<LayerSliderController>().OnValueChanged.RemoveAllListeners();
            loopManager.OffEvents.RemoveAllListeners();
            loopManager.OnEvents.RemoveAllListeners();
            fillOutManager.OffEvents.RemoveAllListeners();
            fillOutManager.OnEvents.RemoveAllListeners();
            colorKindBMB.clickEvent.RemoveAllListeners();
            fillOutBMB.clickEvent.RemoveAllListeners();

            if (colorAction != null)
            {
                gameObject.GetComponentInChildren<HSVPicker.ColorPicker>().onValueChanged.RemoveListener(colorAction);
            }
        }

        /// <summary>
        /// Assigns an action and a color to the HSV Color Picker.
        /// </summary>
        /// <param name="newColorAction">The color action that should be assigned</param>
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
        public void AssignLineKind(LineKind kind)
        {
            selectedLineKind = kind;
            MenuHelper.CalculateHeight(gameObject, true);
        }

        /// <summary>
        /// Assigns a line kind and a tiling to the line kind selection and tiling slider controller.
        /// </summary>
        /// <param name="kind">The line kind that should be assigned.</param>
        /// <param name="tiling">The tiling to be assigned</param>
        public void AssignLineKind(LineKind kind, float tiling)
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
            MenuHelper.CalculateHeight(gameObject, true);
        }
        #endregion

        #region ColorKind
        /// <summary>
        /// Returns the index of the currently selected color kind.
        /// </summary>
        /// <returns>Index of selected color kind</returns>
        private static int GetIndexOfSelectedColorKind()
        {
            return GetColorKinds(true).IndexOf(selectedColorKind);
        }

        /// <summary>
        /// Assigns a color kind to the color-kind selection.
        /// </summary>
        /// <param name="kind">The line kind that should be assigned</param>
        public void AssignColorKind(ColorKind kind)
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
            MenuHelper.CalculateHeight(gameObject, true);
        }
        #endregion

        #region Button Mutally Exclusive
        /// <summary>
        /// This method will be used as an action for the handler of the color buttons (primary/secondary).
        /// This allows only one color to be active at a time.
        /// </summary>
        private static void MutuallyExclusiveColorButtons()
        {
            primaryColorBMB.buttonVar.interactable = !primaryColorBMB.buttonVar.IsInteractable();
            secondaryColorBMB.buttonVar.interactable = !secondaryColorBMB.buttonVar.IsInteractable();
        }


        /// <summary>
        /// This method will be used as an action for the handler of the color type buttons (color kind/fill out).
        /// This allows only one color type to be active at a time.
        /// </summary>
        private static void MutuallyExclusiveColorTypeButtons()
        {
            colorKindBMB.buttonVar.interactable = !colorKindBMB.buttonVar.IsInteractable();
            fillOutBMB.buttonVar.interactable = !fillOutBMB.buttonVar.IsInteractable();
        }
        #endregion

        /// <summary>
        ///Refrehes the horizontal selectors <see cref="lineKindSelector"/> and <see cref="colorKindSelector"/>".
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
            EnableColorKind();
            DisableFillOut();
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
        /// Refreshes the fill-out switch layer.
        /// Will be needed for the switchting the editing of lines.
        /// </summary>
        private static void RefreshFillOut()
        {
            GameObject fillout = content.Find("FillOut").gameObject;
            fillout.SetActive(!fillout.activeInHierarchy);
            fillout.SetActive(!fillout.activeInHierarchy);
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
        private void DisableReturn()
        {
            gameObject.transform.Find("ReturnBtn").gameObject.SetActive(false);
        }

        /// <summary>
        /// Enables the return button
        /// </summary>
        private void EnableReturn()
        {
            gameObject.transform.Find("ReturnBtn").gameObject.SetActive(true);
        }

        /// <summary>
        /// Enables the fill-out area.
        /// </summary>
        private static void EnableFillOut()
        {
            content.transform.Find("FillOut").gameObject.SetActive(true);
        }

        /// <summary>
        /// Hides the fill-out area.
        /// </summary>
        private static void DisableFillOut()
        {
            content.transform.Find("FillOut").gameObject.SetActive(false);
        }

        /// <summary>
        /// Enables the color-kind area.
        /// </summary>
        private static void EnableColorKind()
        {
            content.transform.Find("ColorKindSelection").gameObject.SetActive(true);
            if (selectedColorKind != ColorKind.Monochrome)
            {
                EnableColorAreaFromLineMenu();
            }
        }

        /// <summary>
        /// Hides the color-kind area.
        /// </summary>
        private static void DisableColorKind()
        {
            content.transform.Find("ColorKindSelection").gameObject.SetActive(false);
            DisableColorAreaFromLineMenu();
        }
        #endregion
    }
}

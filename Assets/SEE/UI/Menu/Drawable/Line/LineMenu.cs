using Michsky.UI.ModernUIPack;
using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.Net.Actions.Drawable;
using SEE.UI.Drawable;
using UnityEngine;
using UnityEngine.Events;
using static SEE.Game.Drawable.ActionHelpers.LineCapPointsCalculator;
using static SEE.Game.Drawable.GameDrawer;
using Random = UnityEngine.Random;

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
        /// The additional color-picker action used while editing.
        /// </summary>
        private static UnityAction<Color> editingColorAction;

        /// <summary>
        /// The additional tiling-slider action used while editing.
        /// </summary>
        private static UnityAction<float> editingTilingAction;

        /// <summary>
        /// The additional line-kind selector action used while editing.
        /// </summary>
        private static UnityAction<int> editingLineKindAction;

        /// <summary>
        /// The additional color-kind selector action used while editing.
        /// </summary>
        private static UnityAction<int> editingColorKindAction;

        /// <summary>
        /// The additionally clear fill-out color action.
        /// </summary>
        private static UnityAction clearFillOutColorAction;

        /// <summary>
        /// Holds the current selected line kind.
        /// </summary>
        private static LineKind selectedLineKind;

        /// <summary>
        /// Holds the current selected color kind.
        /// </summary>
        private static ColorKind selectedColorKind;

        /// <summary>
        /// Holds the shared UI references of the line menu.
        /// </summary>
        private static readonly LineMenuControls controls;

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

        /// <summary>
        /// True while the editing UI is updated programmatically.
        /// During this time, UI callbacks must not apply changes.
        /// </summary>
        private static bool isRefreshingEditingUI;

        /// <summary>
        /// Manages the drawing-specific behavior of the line menu.
        /// </summary>
        private DrawLineMenu drawLineMenu;

        /// <summary>
        /// Manages the line-cap and segment selection of this menu.
        /// </summary>
        private LineCapMenu lineCapMenu;

        /// <summary>
        /// Whether the main line segment is currently selected.
        /// </summary>
        private static bool IsMainSegment => Instance.lineCapMenu.IsMainSelected;
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
            Segment,
            All
        }

        /// <summary>
        /// The constructor. It creates the instance for the line menu,
        /// initializes its UI components and hides the menu by default.
        /// </summary>
        static LineMenu()
        {
            Instance = new LineMenu();

            /// Instantiates the menu.
            Instance.Instantiate(lineMenuPrefab);

            /// Resolves all shared UI references once.
            controls = new LineMenuControls(Instance.gameObject);

            /// Initializes the drawing-specific line-menu component.
            Instance.drawLineMenu = new DrawLineMenu(
                Instance.gameObject,
                controls,
                Instance.AssignLineKind,
                Instance.AssignColorKind,
                EnsureValidSecondaryColor);

            /// Initializes the line-cap menu component.
            Instance.lineCapMenu = new LineCapMenu(
                Instance.gameObject,
                () => Instance.IsInEditMode());

            /// Disables the ability to return to the previous menu.
            /// Intended only for editing MindMap nodes.
            DisableReturn();

            /// Initializes and sets up the line-kind selector.
            Instance.InitLineKindSelectorConstructor();

            /// Initializes and sets up the color-kind selector.
            Instance.InitColorKindSelectorConstructor();

            controls.PrimaryColorButtonManager.clickEvent.AddListener(
                MutuallyExclusiveColorButtons);
            controls.PrimaryColorButtonManager.buttonVar.interactable = false;

            controls.SecondaryColorButtonManager.clickEvent.AddListener(
                MutuallyExclusiveColorButtons);

            controls.ColorKindButtonManager.clickEvent.AddListener(
                MutuallyExclusiveColorTypeButtons);
            controls.ColorKindButtonManager.buttonVar.interactable = false;

            controls.FillOutButtonManager.clickEvent.AddListener(
                MutuallyExclusiveColorTypeButtons);

            mode = Mode.None;
            Instance.Disable();
        }

        #region Initialization helpers
        /// <summary>
        /// Initializes the default line-kind selector for the line menu.
        /// </summary>
        private void InitLineKindSelectorConstructor()
        {
            HorizontalSelector selector = controls.LineKindSelector;

            foreach (LineKind kind in GetLineKinds())
            {
                selector.CreateNewItem(kind.ToString());
            }

            selector.selectorEvent.AddListener(index =>
            {
                if (GetLineKinds()[index] == LineKind.Dashed)
                {
                    EnableTilingFromLineMenu();
                }
                else
                {
                    DisableTilingFromLineMenu();
                }

                if (GetLineKinds()[index] == LineKind.Solid
                    && selectedColorKind == ColorKind.TwoDashed)
                {
                    AssignColorKind(ColorKind.Monochrome);

                    controls.ColorKindSelector.label.text =
                        ColorKind.Monochrome.ToString();
                    controls.ColorKindSelector.index = 0;
                    controls.ColorKindSelector.UpdateUI();
                }

                AssignLineKind(GetLineKinds()[index]);
            });

            selector.defaultIndex = 0;
        }

        /// <summary>
        /// Initializes the default color-kind selector for the line menu.
        /// </summary>
        private void InitColorKindSelectorConstructor()
        {
            HorizontalSelector selector = controls.ColorKindSelector;

            foreach (ColorKind kind in GetColorKinds(true))
            {
                selector.CreateNewItem(kind.ToString());
            }

            selector.selectorEvent.AddListener(index =>
            {
                bool isDashed = selectedLineKind != LineKind.Solid;
                ColorKind newColorKind = GetColorKinds(true)[index];

                if (!isDashed && newColorKind == ColorKind.TwoDashed)
                {
                    if (selectedColorKind == ColorKind.Monochrome)
                    {
                        newColorKind = ColorKind.Gradient;
                        selector.label.text = ColorKind.Gradient.ToString();
                    }
                    else
                    {
                        newColorKind = ColorKind.Monochrome;
                        selector.label.text = ColorKind.Monochrome.ToString();
                    }
                }

                AssignColorKind(newColorKind);
                selector.index = GetIndexOfSelectedColorKind();
            });

            selector.defaultIndex = 0;
        }
        #endregion
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
        /// <returns>True if in drawing mode.</returns>
        public bool IsInDrawingMode()
        {
            return gameObject.activeInHierarchy && mode == Mode.Drawing;
        }

        /// <summary>
        /// Returns true if the line menu is in edit mode.
        /// </summary>
        /// <returns>True if in edit mode.</returns>
        public bool IsInEditMode()
        {
            return gameObject.activeInHierarchy && mode == Mode.Edit;
        }
        #endregion

        /// <summary>
        /// Enables all line-menu layers, restores the UI Canvas as parent,
        /// enables dragging and hides the line menu.
        /// The parent of the line menu can temporarily be changed by
        /// <see cref="DrawShapesAction"/>.
        /// </summary>
        public override void Disable()
        {
            base.Disable();
            EnableLineMenuLayers();
            DisableLineCap();

            gameObject.transform.SetParent(UICanvas.Canvas.transform);
            controls.WindowDragger.enabled = true;

            DisableReturn();
            mode = Mode.None;
            lineCapMenu.Reset();
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
                        case MenuLayer.Segment:
                            DisableSegment();
                            DisableLineCap();
                            break;
                        case MenuLayer.All:
                            DisableLineKindFromLineMenu();
                            DisableSegment();
                            DisableLineCap();
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
            EnableLineMenu(withoutMenuLayer: new MenuLayer[]
                {
                    MenuLayer.Layer,
                    MenuLayer.Loop,
                    MenuLayer.Segment
                });

            drawLineMenu.Enable();

            mode = Mode.Drawing;
            MenuHelper.CalculateHeight(gameObject, true);
        }

        /// <summary>
        /// Gets the fill-out color for the drawing mode.
        /// </summary>
        /// <returns>
        /// The selected fill-out color if filling is enabled; otherwise, null.
        /// </returns>
        public static Color? GetFillOutColorForDrawing()
        {
            return Instance.drawLineMenu.GetFillOutColor();
        }
        #endregion

        #region Editing
        /// <summary>
        /// Enables the line menu for editing the given line and configures all controls
        /// with the values of its drawable configuration.
        /// Depending on the selected segment, changes are applied either to the main line
        /// or to its start or end cap.
        /// </summary>
        /// <param name="selectedLine">The line object to edit.</param>
        /// <param name="newValueHolder">
        /// The drawable configuration containing the current values of the selected line.
        /// The menu is initialized only if this configuration is a <see cref="LineConf"/>.
        /// </param>
        /// <param name="returnCall">
        /// An optional callback that returns to the parent menu.
        /// This is used when the line menu is opened from another menu, for example while
        /// editing a mind-map element.
        /// </param>
        public void EnableForEditing(GameObject selectedLine, DrawableType newValueHolder,
            UnityAction returnCall = null)
        {
            if (newValueHolder is LineConf lineHolder)
            {
                lineCapMenu.BeginEditing(lineHolder);

                bool isFreehandLine = IsFreehandLine(selectedLine);

                if (returnCall == null)
                {
                    EnableLineMenu(
                        withoutMenuLayer: isFreehandLine
                            ? new MenuLayer[] { MenuLayer.Segment }
                            : null);
                }
                else
                {
                    EnableLineMenu(withoutMenuLayer: new MenuLayer[] { MenuLayer.Segment });
                    SetUpReturnButtonForEditing(returnCall);
                }

                LineRenderer renderer = selectedLine.GetComponent<LineRenderer>();
                GameObject surface = GameFinder.GetDrawableSurface(selectedLine);
                string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);

                /// Sets up the line kind selector.
                SetUpLineKindSelectorForEditing(
                    selectedLine, renderer, lineHolder, surface, surfaceParentName);

                /// Sets up the color-kind selector.
                SetUpColorKindSelectorForEditing(
                    selectedLine, renderer, lineHolder, surface, surfaceParentName);

                if (!isFreehandLine)
                {
                    UnityAction refreshEditingUI = () =>
                        RefreshEditingUIForCurrentSegment(
                            selectedLine,
                            lineHolder,
                            surface,
                            surfaceParentName);

                    lineCapMenu.SetUpSegmentEditing(
                        lineHolder,
                        DisableLineCap,
                        UpdateLineOptions,
                        refreshEditingUI,
                        ResetColorTypeSelectionToDefault);

                    lineCapMenu.SetUpLineCapEditing(
                        selectedLine,
                        lineHolder,
                        surface,
                        surfaceParentName,
                        UpdateLineOptions,
                        refreshEditingUI);
                }
                else
                {
                    lineCapMenu.SelectMain();
                    DisableSegment();
                    DisableLineCap();
                }

                /// Adds the action that should be executed if the tiling slider changed.
                /// It is only available for <see cref="LineKind.Dashed"/>.
                controls.TilingSlider.onValueChanged.AddListener(editingTilingAction = tiling =>
                {
                    if (isRefreshingEditingUI)
                    {
                        return;
                    }

                    if (IsMainSegment)
                    {
                        lineHolder.LineKind = LineKind.Dashed;
                        lineHolder.Tiling = tiling;

                        ChangeLineKind(selectedLine, LineKind.Dashed, tiling);

                        new ChangeLineKindNetAction(
                            surface.name,
                            surfaceParentName,
                            selectedLine.name,
                            LineKind.Dashed,
                            tiling).Execute();
                    }
                    else
                    {
                        LineCapConf capConf = GetSelectedCapConf(lineHolder);
                        if (capConf == null)
                        {
                            return;
                        }

                        capConf.LineKind = LineKind.Dashed;
                        capConf.Tiling = tiling;

                        lineCapMenu.ApplySelectedCapStyle(
                            selectedLine, lineHolder, surface);
                    }
                });

                /// Sets up the components.
                SetUpPrimaryColorButtonForEditing(
                    selectedLine, lineHolder, surface, surfaceParentName);

                SetUpSecondaryColorButtonForEditing(
                    selectedLine, lineHolder, surface, surfaceParentName);

                SetUpOutlineThicknessSliderForEditing(
                    selectedLine, renderer, lineHolder, surface, surfaceParentName);

                SetUpOrderInLayerSliderForEditing(
                    selectedLine, lineHolder, surface, surfaceParentName);

                SetUpLoopSwitchForEditing(
                    selectedLine, lineHolder, surface, surfaceParentName);

                SetUpColorPickerForEditing(
                    selectedLine, lineHolder, surface, surfaceParentName);

                SetUpColorKindTypeButtonForEditing(
                    selectedLine, lineHolder, surface, surfaceParentName);

                SetUpFillOutTypeButtonForEditing(
                    selectedLine, lineHolder, surface, surfaceParentName);

                SetUpFillOutSwitchForEditing(
                    selectedLine, lineHolder, surface, surfaceParentName);

                mode = Mode.Edit;

                /// Re-calculates the menu height.
                MenuHelper.CalculateHeight(gameObject, true);
            }
        }

        /// <summary>
        /// Sets up the return button for editing.
        /// If the callback is available, the button is shown and receives the callback.
        /// The order-in-layer slider is disabled because this mode is used while editing
        /// a line from a parent menu such as the mind-map menu.
        /// </summary>
        /// <param name="returnCall">The callback returning to the parent menu.</param>
        private static void SetUpReturnButtonForEditing(UnityAction returnCall)
        {
            if (returnCall == null)
            {
                return;
            }

            EnableReturn();

            controls.ReturnButtonManager.clickEvent.RemoveAllListeners();
            controls.ReturnButtonManager.clickEvent.AddListener(returnCall);

            controls.LayerSlider.interactable = false;
        }

        /// <summary>
        /// Gets the configuration of the currently selected line cap segment.
        /// </summary>
        /// <param name="lineHolder">The line configuration.</param>
        /// <returns>
        /// The start or end line cap configuration depending on the selected segment;
        /// otherwise, null.
        /// </returns>
        private static LineCapConf GetSelectedCapConf(LineConf lineHolder)
        {
            return Instance.lineCapMenu.GetSelectedCapConf(lineHolder);
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
            controls.LineKindSelector.index = GetIndexOfSelectedLineKind();

            /// Updates the selector.
            controls.LineKindSelector.UpdateUI();

            /// Removes the current line kind action of the line kind selector.
            if (editingLineKindAction != null)
            {
                controls.LineKindSelector.selectorEvent.RemoveListener(editingLineKindAction);
            }

            /// Creates a new line kind selector action
            editingLineKindAction = index =>
            {
                if (isRefreshingEditingUI)
                {
                    return;
                }

                LineKind newKind = GetLineKinds()[index];

                if (newKind == LineKind.Dashed)
                {
                    return;
                }

                if (IsMainSegment)
                {
                    lineHolder.LineKind = newKind;

                    if (lineHolder.LineKind == LineKind.Solid &&
                        lineHolder.ColorKind == ColorKind.TwoDashed)
                    {
                        lineHolder.ColorKind = ColorKind.Monochrome;

                        ChangeColorKind(selectedLine, lineHolder.ColorKind, lineHolder);

                        new ChangeColorKindNetAction(surface.name, surfaceParentName,
                            LineConf.GetLineWithoutRenderPos(selectedLine),
                            lineHolder.ColorKind).Execute();
                    }

                    ChangeLineKind(selectedLine, lineHolder.LineKind, lineHolder.Tiling);

                    new ChangeLineKindNetAction(surface.name, surfaceParentName,
                        selectedLine.name, lineHolder.LineKind, lineHolder.Tiling).Execute();
                }
                else
                {
                    LineCapConf capConf = GetSelectedCapConf(lineHolder);
                    if (capConf == null)
                    {
                        return;
                    }

                    capConf.LineKind = newKind;

                    if (capConf.LineKind == LineKind.Solid &&
                        capConf.ColorKind == ColorKind.TwoDashed)
                    {
                        capConf.ColorKind = ColorKind.Monochrome;
                    }

                    Instance.lineCapMenu.ApplySelectedCapStyle(selectedLine, lineHolder, surface);
                }
            };

            /// Adds the line kind selector action.
            controls.LineKindSelector.selectorEvent.AddListener(editingLineKindAction);
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
            controls.ColorKindSelector.index = GetIndexOfSelectedColorKind();
            /// Updates the selector.
            controls.ColorKindSelector.UpdateUI();

            /// Removes the current color-kind action of the color-kind selector.
            if (editingColorKindAction != null)
            {
                controls.ColorKindSelector.selectorEvent.RemoveListener(editingColorKindAction);
            }

            /// Creates a new color-kind selector action
            editingColorKindAction = index =>
            {
                if (isRefreshingEditingUI)
                {
                    return;
                }

                ColorKind newKind = GetColorKinds(true)[index];

                if (IsMainSegment)
                {
                    lineHolder.ColorKind = newKind;

                    if (lineHolder.ColorKind != ColorKind.Monochrome)
                    {
                        lineHolder.SecondaryColor =
                            EnsureValidSecondaryColor(lineHolder.SecondaryColor);
                    }

                    ChangeColorKind(selectedLine, lineHolder.ColorKind, lineHolder);

                    new ChangeColorKindNetAction(
                        surface.name,
                        surfaceParentName,
                        LineConf.GetLineWithoutRenderPos(selectedLine),
                        lineHolder.ColorKind).Execute();
                }
                else
                {
                    LineCapConf capConf = GetSelectedCapConf(lineHolder);
                    if (capConf == null)
                    {
                        return;
                    }

                    capConf.ColorKind = newKind;

                    if (capConf.ColorKind != ColorKind.Monochrome)
                    {
                        capConf.SecondaryColor =
                            EnsureValidSecondaryColor(capConf.SecondaryColor);
                    }

                    Instance.lineCapMenu.ApplySelectedCapStyle(selectedLine, lineHolder, surface);
                }
            };

            /// Adds the color-kind selector action.
            controls.ColorKindSelector.selectorEvent.AddListener(editingColorKindAction);
        }

        /// <summary>
        /// Updates the visibility of the line options depending on the given line cap.
        /// </summary>
        /// <param name="lineCap">
        /// The line cap whose value determines whether the line options are shown.
        /// </param>
        private static void UpdateLineOptions(LineCap lineCap)
        {
            if (lineCap != LineCap.None)
            {
                EnableLineOptions();
            }
            else
            {
                DisableLineOptions();
            }
        }

        /// <summary>
        /// Sets up the primary color button for editing mode.
        /// The primary and secondary color buttons mutually exclude each other,
        /// so that only one can be active at a time.
        /// Depending on the selected segment, the color change is applied either
        /// to the main line or to the selected line cap.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="lineHolder">The configuration which holds the changes.</param>
        /// <param name="surface">The drawable surface on which the line is displayed.</param>
        /// <param name="surfaceParentName">The parent id of the drawable surface.</param>
        private static void SetUpPrimaryColorButtonForEditing(GameObject selectedLine, LineConf lineHolder,
            GameObject surface, string surfaceParentName)
        {
            /// Removes the old handler
            controls.PrimaryColorButtonManager.clickEvent.RemoveAllListeners();
            /// Add mutually exclusive mode.
            controls.PrimaryColorButtonManager.clickEvent.AddListener(MutuallyExclusiveColorButtons);
            /// Add new handler for <see cref="HSVPicker.ColorPicker"/>
            controls.PrimaryColorButtonManager.clickEvent.AddListener(() =>
            {
                if (IsMainSegment)
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
                    LineCapConf capConf = GetSelectedCapConf(lineHolder);
                    if (capConf == null)
                    {
                        return;
                    }

                    AssignColorArea(color =>
                    {
                        capConf.PrimaryColor = color;
                        Instance.lineCapMenu.ApplySelectedCapStyle(selectedLine, lineHolder, surface);
                    }, capConf.PrimaryColor);
                }

            });
            /// Makes the button unclickable.
            controls.PrimaryColorButtonManager.buttonVar.interactable = false;
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
            controls.SecondaryColorButtonManager.clickEvent.RemoveAllListeners();
            /// Add mutually exclusive mode.
            controls.SecondaryColorButtonManager.clickEvent.AddListener(MutuallyExclusiveColorButtons);

            /// Add new handler for <see cref="HSVPicker.ColorPicker"/>
            controls.SecondaryColorButtonManager.clickEvent.AddListener(() =>
            {
                if (IsMainSegment)
                {
                    lineHolder.SecondaryColor = EnsureValidSecondaryColor(lineHolder.SecondaryColor);
                    AssignColorArea(color =>
                    {
                        GameEdit.ChangeSecondaryColor(selectedLine, color);
                        lineHolder.SecondaryColor = color;
                        new EditLineSecondaryColorNetAction(surface.name, surfaceParentName,
                            selectedLine.name, color).Execute();
                    }, lineHolder.SecondaryColor);
                }
                else
                {
                    LineCapConf capConf = GetSelectedCapConf(lineHolder);
                    if (capConf == null)
                    {
                        return;
                    }

                    capConf.SecondaryColor = EnsureValidSecondaryColor(capConf.SecondaryColor);

                    AssignColorArea(color =>
                    {
                        capConf.SecondaryColor = color;
                        Instance.lineCapMenu.ApplySelectedCapStyle(selectedLine, lineHolder, surface);
                    }, capConf.SecondaryColor);
                }
            });
            /// Makes the button unclickable.
            controls.SecondaryColorButtonManager.buttonVar.interactable = true;
        }

        /// <summary>
        /// Sets up the thickness slider for editing mode.
        /// Depending on the selected segment, the thickness of the main line
        /// or the selected line cap is changed.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="renderer">The renderer of the selected line.</param>
        /// <param name="lineHolder">The edited line configuration.</param>
        /// <param name="surface">The drawable surface.</param>
        /// <param name="surfaceParentName">The parent ID of the drawable surface.</param>
        private void SetUpOutlineThicknessSliderForEditing(GameObject selectedLine, LineRenderer renderer,
            LineConf lineHolder, GameObject surface, string surfaceParentName)
        {
            controls.ThicknessSlider.AssignValue(renderer.startWidth);

            controls.ThicknessSlider.OnValueChanged.AddListener(thickness =>
            {
                if (isRefreshingEditingUI)
                {
                    return;
                }

                if (thickness <= 0.0f)
                {
                    return;
                }

                if (IsMainSegment)
                {
                    GameEdit.ChangeThickness(selectedLine, thickness);
                    lineHolder.Thickness = thickness;

                    new EditLineThicknessNetAction(surface.name, surfaceParentName,
                        selectedLine.name, thickness).Execute();
                }
                else
                {
                    LineCapConf capConf = GetSelectedCapConf(lineHolder);
                    if (capConf == null)
                    {
                        return;
                    }

                    capConf.Thickness = thickness;
                    Instance.lineCapMenu.ApplySelectedCapStyle(selectedLine, lineHolder, surface);
                }
            });
        }

        /// <summary>
        /// Sets up the order-in-layer slider for editing mode with the current
        /// order of the selected line and registers its change handler.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="lineHolder">The configuration storing the changes.</param>
        /// <param name="surface">The drawable surface containing the line.</param>
        /// <param name="surfaceParentName">The parent ID of the drawable surface.</param>
        private static void SetUpOrderInLayerSliderForEditing(GameObject selectedLine,
            LineConf lineHolder, GameObject surface, string surfaceParentName)
        {
            controls.LayerSliderController.AssignMaxOrder(
                surface.GetComponent<DrawableHolder>().OrderInLayer);

            controls.LayerSliderController.AssignValue(lineHolder.OrderInLayer);

            controls.LayerSliderController.OnValueChanged.AddListener(layerOrder =>
            {
                GameEdit.ChangeLayer(selectedLine, layerOrder);
                lineHolder.OrderInLayer = layerOrder;

                new EditLayerNetAction(
                    surface.name,
                    surfaceParentName,
                    selectedLine.name,
                    layerOrder).Execute();
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
            controls.LoopManager.OnEvents.RemoveAllListeners();

            /// Add the handler for turning on the switch.
            /// It enables the loop.
            controls.LoopManager.OnEvents.AddListener(() =>
            {
                GameEdit.ChangeLoop(selectedLine, true);
                lineHolder.Loop = true;
                new EditLineLoopNetAction(surface.name, surfaceParentName, selectedLine.name, true).Execute();
            });
            /// Removes the old off handler.
            controls.LoopManager.OffEvents.RemoveAllListeners();
            /// Adds the handler for turning off the switch.
            controls.LoopManager.OffEvents.AddListener(() =>
            {
                GameEdit.ChangeLoop(selectedLine, false);
                lineHolder.Loop = false;
                new EditLineLoopNetAction(surface.name, surfaceParentName, selectedLine.name, false).Execute();
            });

            /// Update the switch to the current value.
            controls.LoopManager.isOn = lineHolder.Loop;
            /// Updates the switch.
            RefreshLoop();
        }

        /// <summary>
        /// Sets up the color picker for editing mode.
        /// It assigns the currently relevant color depending on the selected segment
        /// and registers the corresponding color change action.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="lineHolder">The edited line configuration.</param>
        /// <param name="surface">The drawable surface on which the line is displayed.</param>
        /// <param name="surfaceParentName">The parent ID of the drawable surface.</param>
        private static void SetUpColorPickerForEditing(GameObject selectedLine,
            LineConf lineHolder, GameObject surface, string surfaceParentName)
        {
            if (editingColorAction != null)
            {
                controls.ColorPicker.onValueChanged.RemoveListener(editingColorAction);
            }

            if (IsMainSegment)
            {
                LineRenderer renderer = selectedLine.GetComponent<LineRenderer>();

                switch (lineHolder.ColorKind)
                {
                    case ColorKind.Monochrome:
                        controls.ColorPicker.AssignColor(renderer.material.color);
                        break;
                    case ColorKind.Gradient:
                        controls.ColorPicker.AssignColor(renderer.startColor);
                        break;
                    case ColorKind.TwoDashed:
                        controls.ColorPicker.AssignColor(renderer.material.color);
                        break;
                }

                editingColorAction = color =>
                {
                    GameEdit.ChangePrimaryColor(selectedLine, color);
                    lineHolder.PrimaryColor = color;
                    new EditLinePrimaryColorNetAction(surface.name, surfaceParentName,
                        selectedLine.name, color).Execute();
                };
            }
            else
            {
                LineCapConf capConf = GetSelectedCapConf(lineHolder);
                if (capConf == null || capConf.CapKind == LineCap.None)
                {
                    return;
                }

                controls.ColorPicker.AssignColor(capConf.PrimaryColor);

                editingColorAction = color =>
                {
                    LineCapConf currentCapConf = GetSelectedCapConf(lineHolder);
                    if (currentCapConf == null || currentCapConf.CapKind == LineCap.None)
                    {
                        return;
                    }

                    currentCapConf.PrimaryColor = color;
                    Instance.lineCapMenu.ApplySelectedCapStyle(selectedLine, lineHolder, surface);
                };
            }

            controls.ColorPicker.onValueChanged.AddListener(editingColorAction);
        }

        /// <summary>
        /// Sets up the color-kind type button for editing.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="lineHolder">The configuration which holds the changes.</param>
        /// <param name="surface">The drawable surface on which the line is displayed.</param>
        /// <param name="surfaceParentName">The parent id of the drawable surface.</param>
        private static void SetUpColorKindTypeButtonForEditing(GameObject selectedLine,
            LineConf lineHolder, GameObject surface, string surfaceParentName)
        {
            controls.ColorKindButtonManager.clickEvent.RemoveAllListeners();
            controls.ColorKindButtonManager.clickEvent.AddListener(MutuallyExclusiveColorTypeButtons);

            controls.ColorKindButtonManager.clickEvent.AddListener(() =>
            {
                DisableFillOut();
                EnableColorKind();

                if (IsMainSegment)
                {
                    if (!controls.PrimaryColorButtonManager.buttonVar.interactable)
                    {
                        AssignColorArea(color =>
                        {
                            GameEdit.ChangePrimaryColor(selectedLine, color);
                            lineHolder.PrimaryColor = color;

                            new EditLinePrimaryColorNetAction(
                                surface.name,
                                surfaceParentName,
                                selectedLine.name,
                                color).Execute();

                        }, lineHolder.PrimaryColor);
                    }
                    else
                    {
                        lineHolder.SecondaryColor =
                            EnsureValidSecondaryColor(lineHolder.SecondaryColor);

                        AssignColorArea(color =>
                        {
                            GameEdit.ChangeSecondaryColor(selectedLine, color);
                            lineHolder.SecondaryColor = color;

                            new EditLineSecondaryColorNetAction(
                                surface.name,
                                surfaceParentName,
                                selectedLine.name,
                                color).Execute();

                        }, lineHolder.SecondaryColor);
                    }
                }
                else
                {
                    LineCapConf capConf = GetSelectedCapConf(lineHolder);
                    if (capConf == null)
                    {
                        return;
                    }

                    if (!controls.PrimaryColorButtonManager.buttonVar.interactable)
                    {
                        AssignColorArea(color =>
                        {
                            capConf.PrimaryColor = color;
                            Instance.lineCapMenu.ApplySelectedCapStyle(selectedLine, lineHolder, surface);

                        }, capConf.PrimaryColor);
                    }
                    else
                    {
                        capConf.SecondaryColor =
                            EnsureValidSecondaryColor(capConf.SecondaryColor);

                        AssignColorArea(color =>
                        {
                            capConf.SecondaryColor = color;
                            Instance.lineCapMenu.ApplySelectedCapStyle(selectedLine, lineHolder, surface);

                        }, capConf.SecondaryColor);
                    }
                }

                MenuHelper.CalculateHeight(Instance.gameObject, true);
            });

            controls.ColorKindButtonManager.buttonVar.interactable = false;
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
            controls.FillOutButtonManager.clickEvent.RemoveAllListeners();
            controls.FillOutButtonManager.clickEvent.AddListener(MutuallyExclusiveColorTypeButtons);

            controls.FillOutButtonManager.clickEvent.AddListener(() =>
            {
                DisableColorKind();
                EnableFillOut();

                if (IsMainSegment)
                {
                    if (lineHolder.FillOutStatus &&
                        GameDrawer.GetOwnFillOutObject(selectedLine) == null)
                    {
                        if (FillOut(selectedLine, lineHolder.FillOutColor))
                        {
                            new DrawingFillOutNetAction(
                                surface.name,
                                surfaceParentName,
                                selectedLine.name,
                                lineHolder.FillOutColor).Execute();
                        }
                    }

                    AssignColorArea(color =>
                    {
                        GameEdit.ChangeFillOutColor(selectedLine, color);
                        lineHolder.FillOutColor = color;

                        new EditLineFillOutColorNetAction(
                            surface.name,
                            surfaceParentName,
                            selectedLine.name,
                            color).Execute();

                    }, lineHolder.FillOutColor);
                }
                else
                {
                    LineCapConf capConf = GetSelectedCapConf(lineHolder);
                    if (capConf == null)
                    {
                        return;
                    }

                    AssignColorArea(color =>
                    {
                        capConf.FillOutColor = color;
                        Instance.lineCapMenu.ApplySelectedCapStyle(selectedLine, lineHolder, surface);

                    }, capConf.FillOutColor);
                }

                MenuHelper.CalculateHeight(Instance.gameObject, true);
            });

            controls.FillOutButtonManager.buttonVar.interactable = true;
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
            controls.FillOutManager.OnEvents.RemoveAllListeners();
            controls.FillOutManager.OffEvents.RemoveAllListeners();

            controls.FillOutManager.OnEvents.AddListener(() =>
            {
                if (isRefreshingEditingUI)
                {
                    return;
                }

                if (IsMainSegment)
                {
                    lineHolder.FillOutStatus = true;

                    if (lineHolder.FillOutColor == Color.clear)
                    {
                        lineHolder.FillOutColor = lineHolder.PrimaryColor;
                    }

                    if (FillOut(selectedLine, lineHolder.FillOutColor))
                    {
                        new DrawingFillOutNetAction(
                            surface.name,
                            surfaceParentName,
                            selectedLine.name,
                            lineHolder.FillOutColor).Execute();
                    }
                }
                else
                {
                    LineCapConf capConf = GetSelectedCapConf(lineHolder);
                    if (capConf == null)
                    {
                        return;
                    }

                    capConf.FillOutStatus = true;
                    Instance.lineCapMenu.UpdateFillOutChangedByUser(capConf);

                    if (capConf.FillOutColor == Color.clear)
                    {
                        capConf.FillOutColor = capConf.PrimaryColor;
                    }

                    Instance.lineCapMenu.ApplySelectedCapStyle(selectedLine, lineHolder, surface);
                }
            });

            controls.FillOutManager.OffEvents.AddListener(() =>
            {
                if (isRefreshingEditingUI)
                {
                    return;
                }

                if (IsMainSegment)
                {
                    lineHolder.FillOutStatus = false;

                    GameObject mainFillOut = GameDrawer.GetOwnFillOutObject(selectedLine);
                    if (mainFillOut != null)
                    {
                        GameObject.DestroyImmediate(mainFillOut);
                    }

                    new DeleteFillOutNetAction(
                        surface.name,
                        surfaceParentName,
                        selectedLine.name).Execute();
                }
                else
                {
                    LineCapConf capConf = GetSelectedCapConf(lineHolder);
                    if (capConf == null)
                    {
                        return;
                    }

                    capConf.FillOutStatus = false;
                    Instance.lineCapMenu.UpdateFillOutChangedByUser(capConf);
                    Instance.lineCapMenu.ApplySelectedCapStyle(selectedLine, lineHolder, surface);
                }
            });

            controls.FillOutManager.isOn = IsMainSegment
                ? lineHolder.FillOutStatus
                : GetSelectedCapConf(lineHolder)?.FillOutStatus ?? false;

            RefreshFillOut();
        }

        /// <summary>
        /// Assigns a fill-out status and color to the edit mode.
        /// </summary>
        /// <param name="fillOut">The status and color.</param>
        /// <param name="setFillOutAction">Fill-out color change action.</param>
        /// <param name="clearFillOutAction">Action to clear the value.</param>
        public static void AssignFillOutForEditing(Color? fillOut, UnityAction<Color> setFillOutAction, UnityAction clearFillOutAction)
        {
            if (Instance.IsInEditMode() && !controls.FillOutButtonManager.buttonVar.interactable)
            {
                if (fillOut != null && setFillOutAction != null)
                {
                    controls.FillOutManager.isOn = true;
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
                    if (editingColorAction != setFillOutAction)
                    {
                        AssignColorArea(setFillOutAction, fillOut.Value);
                    }
                    clearFillOutColorAction = clearFillOutAction;
                }
                else
                {
                    controls.FillOutManager.isOn = false;
                    controls.FillOutManager.OffEvents.Invoke();
                    RefreshFillOut();
                }
            }
        }

        /// <summary>
        /// Refreshes the editing UI so that all controls display the values of the
        /// currently selected segment.
        /// If the main segment is selected, the values of the line are shown.
        /// If a line cap is selected, the values of the corresponding cap are shown.
        /// </summary>
        /// <param name="selectedLine">The selected line.</param>
        /// <param name="lineHolder">The edited line configuration.</param>
        /// <param name="surface">The drawable surface.</param>
        /// <param name="surfaceParentName">The parent ID of the drawable surface.</param>
        private void RefreshEditingUIForCurrentSegment(GameObject selectedLine,
            LineConf lineHolder, GameObject surface, string surfaceParentName)
        {
            isRefreshingEditingUI = true;
            try
            {
                if (editingColorAction != null)
                {
                    controls.ColorPicker.onValueChanged.RemoveListener(editingColorAction);
                }

                if (IsMainSegment)
                {
                    AssignLineKind(lineHolder.LineKind, lineHolder.Tiling);
                    RefreshLineKindSelectorUI();
                    AssignColorKind(lineHolder.ColorKind);
                    RefreshColorKindSelectorUI();

                    controls.ColorPicker.AssignColor(lineHolder.PrimaryColor);

                    controls.ThicknessSlider.AssignValue(lineHolder.Thickness);

                    controls.FillOutManager.isOn = lineHolder.FillOutStatus;
                }
                else
                {
                    LineCapConf capConf = GetSelectedCapConf(lineHolder);
                    if (capConf == null)
                    {
                        return;
                    }

                    if (capConf.CapKind == LineCap.None)
                    {
                        controls.FillOutManager.isOn = false;
                    }
                    else
                    {
                        AssignLineKind(capConf.LineKind, capConf.Tiling);
                        RefreshLineKindSelectorUI();
                        AssignColorKind(capConf.ColorKind);
                        RefreshColorKindSelectorUI();

                        controls.ColorPicker.AssignColor(capConf.PrimaryColor);

                        controls.ThicknessSlider.AssignValue(capConf.Thickness);

                        controls.FillOutManager.isOn = capConf.FillOutStatus;
                    }
                }

                RefreshFillOut();
            }
            finally
            {
                isRefreshingEditingUI = false;
            }

            SetUpColorPickerForEditing(selectedLine, lineHolder, surface, surfaceParentName);
            MenuHelper.CalculateHeight(gameObject, true);
        }

        /// <summary>
        /// Returns whether the given line was created by freehand drawing.
        /// Freehand lines do not support line caps and therefore do not provide segment selection.
        /// </summary>
        /// <param name="line">The line to check.</param>
        /// <returns>True if the line is a freehand line.</returns>
        private static bool IsFreehandLine(GameObject line)
        {
            return line.TryGetComponent(out LineValueHolder holder) && holder.FreehandLine;
        }

        /// <summary>
        /// Refreshes the color-kind selector UI so that it displays the currently assigned color kind.
        /// </summary>
        private static void RefreshColorKindSelectorUI()
        {
            controls.ColorKindSelector.index = GetIndexOfSelectedColorKind();
            controls.ColorKindSelector.UpdateUI();
        }

        /// <summary>
        /// Refreshes the line-kind selector UI so that it displays the currently assigned line kind.
        /// </summary>
        private static void RefreshLineKindSelectorUI()
        {
            controls.LineKindSelector.index = GetIndexOfSelectedLineKind();
            controls.LineKindSelector.UpdateUI();
        }
        #endregion

        /// <summary>
        /// Ensures that the given secondary color is visible and usable.
        /// If the color is clear, a random color is assigned.
        /// If the alpha channel is zero, it is set to fully opaque.
        /// </summary>
        /// <param name="color">The secondary color to validate.</param>
        /// <returns>A visible secondary color.</returns>
        private static Color EnsureValidSecondaryColor(Color color)
        {
            if (color == Color.clear)
            {
                color = Random.ColorHSV();
            }

            if (color.a == 0)
            {
                color = new Color(color.r, color.g, color.b, 1f);
            }

            return color;
        }
        #endregion

        /// <summary>
        /// Removes the drawing-specific and editing-specific handlers registered
        /// at the shared line-menu controls.
        /// </summary>
        private void RemoveListeners()
        {
            EnableLineMenuLayers();

            drawLineMenu.RemoveListeners();

            if (editingLineKindAction != null)
            {
                controls.LineKindSelector.selectorEvent.RemoveListener(editingLineKindAction);
                editingLineKindAction = null;
            }

            if (editingColorKindAction != null)
            {
                controls.ColorKindSelector.selectorEvent.RemoveListener(editingColorKindAction);
                editingColorKindAction = null;
            }

            lineCapMenu.RemoveListeners();

            if (editingTilingAction != null)
            {
                if (selectedLineKind != LineKind.Dashed)
                {
                    controls.TilingSlider.ResetToMin();
                }

                controls.TilingSlider.onValueChanged.RemoveListener(editingTilingAction);
                editingTilingAction = null;
            }

            controls.PrimaryColorButtonManager.clickEvent.RemoveAllListeners();
            controls.SecondaryColorButtonManager.clickEvent.RemoveAllListeners();

            controls.ThicknessSlider.OnValueChanged.RemoveAllListeners();
            controls.LayerSliderController.OnValueChanged.RemoveAllListeners();

            controls.LoopManager.OffEvents.RemoveAllListeners();
            controls.LoopManager.OnEvents.RemoveAllListeners();

            controls.FillOutManager.OffEvents.RemoveAllListeners();
            controls.FillOutManager.OnEvents.RemoveAllListeners();

            controls.ColorKindButtonManager.clickEvent.RemoveAllListeners();
            controls.FillOutButtonManager.clickEvent.RemoveAllListeners();

            if (editingColorAction != null)
            {
                controls.ColorPicker.onValueChanged.RemoveListener(editingColorAction);
                editingColorAction = null;
            }

            clearFillOutColorAction = null;
        }

        /// <summary>
        /// Assigns an action and a color to the HSV color picker while editing.
        /// The previously assigned editing action is removed first.
        /// </summary>
        /// <param name="newColorAction">
        /// The color action that should be assigned.
        /// </param>
        /// <param name="color">The color that should be assigned.</param>
        public static void AssignColorArea(UnityAction<Color> newColorAction, Color color)
        {
            if (editingColorAction != null)
            {
                controls.ColorPicker.onValueChanged.RemoveListener(editingColorAction);
            }

            editingColorAction = newColorAction;

            controls.ColorPicker.AssignColor(color);
            controls.ColorPicker.onValueChanged.AddListener(newColorAction);
        }

        #region LineKind

        /// <summary>
        /// Returns the index of the current selected line kind.
        /// </summary>
        /// <returns>Index of selected line kind.</returns>
        private static int GetIndexOfSelectedLineKind()
        {
            return GetLineKinds().IndexOf(selectedLineKind);
        }

        /// <summary>
        /// Assigns a line kind to the line kind selection.
        /// </summary>
        /// <param name="kind">The line kind that should be assigned.</param>
        public void AssignLineKind(LineKind kind)
        {
            selectedLineKind = kind;
            if (!isRefreshingEditingUI)
            {
                MenuHelper.CalculateHeight(gameObject, true);
            }
        }

        /// <summary>
        /// Assigns a line kind and a tiling to the line kind selection and tiling slider controller.
        /// </summary>
        /// <param name="kind">The line kind that should be assigned.</param>
        /// <param name="tiling">The tiling to be assigned.</param>
        public void AssignLineKind(LineKind kind, float tiling)
        {
            selectedLineKind = kind;

            /// Enables the tiling layer if the chosen <see cref="LineKind"/>
            /// is <see cref="LineKind.Dashed"/>.
            if (kind == LineKind.Dashed)
            {
                EnableTilingFromLineMenu();
                controls.TilingSlider.AssignValue(tiling);
            }
            else
            { /// In all other cases the tiling layer will disabled.
                DisableTilingFromLineMenu();
            }

            if (!isRefreshingEditingUI)
            {
                MenuHelper.CalculateHeight(gameObject, true);
            }
        }
        #endregion

        #region ColorKind
        /// <summary>
        /// Returns the index of the currently selected color kind.
        /// </summary>
        /// <returns>Index of selected color kind.</returns>
        private static int GetIndexOfSelectedColorKind()
        {
            return GetColorKinds(true).IndexOf(selectedColorKind);
        }

        /// <summary>
        /// Assigns a color kind to the color-kind selection.
        /// </summary>
        /// <param name="kind">The line kind that should be assigned.</param>
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

            if (!isRefreshingEditingUI)
            {
                MenuHelper.CalculateHeight(gameObject, true);
            }
        }
        #endregion

        #region Button Mutally Exclusive
        /// <summary>
        /// This method will be used as an action for the handler of the color buttons (primary/secondary).
        /// This allows only one color to be active at a time.
        /// </summary>
        private static void MutuallyExclusiveColorButtons()
        {
            controls.PrimaryColorButtonManager.buttonVar.interactable = !controls.PrimaryColorButtonManager.buttonVar.IsInteractable();
            controls.SecondaryColorButtonManager.buttonVar.interactable = !controls.SecondaryColorButtonManager.buttonVar.IsInteractable();
        }


        /// <summary>
        /// This method will be used as an action for the handler of the color type buttons (color kind/fill out).
        /// This allows only one color type to be active at a time.
        /// </summary>
        private static void MutuallyExclusiveColorTypeButtons()
        {
            controls.ColorKindButtonManager.buttonVar.interactable = !controls.ColorKindButtonManager.buttonVar.IsInteractable();
            controls.FillOutButtonManager.buttonVar.interactable = !controls.FillOutButtonManager.buttonVar.IsInteractable();
        }

        /// <summary>
        /// Resets the color-type selection of the line menu to its default state.
        /// The default state is the color-kind mode, while the fill-out mode is deactivated.
        /// This is used when switching between segments so that no stale segment-specific
        /// UI mode remains active.
        /// </summary>
        private static void ResetColorTypeSelectionToDefault()
        {
            controls.ColorKindButtonManager.buttonVar.interactable = false;
            controls.FillOutButtonManager.buttonVar.interactable = true;
        }
        #endregion

        /// <summary>
        /// Refreshes the line-kind and color-kind selectors so that they display
        /// the currently selected values.
        /// </summary>
        public static void RefreshHorizontalSelectors()
        {
            controls.LineKindSelector.index = GetIndexOfSelectedLineKind();
            controls.ColorKindSelector.index = GetIndexOfSelectedColorKind();
            controls.LineKindSelector.UpdateUI();
            controls.ColorKindSelector.UpdateUI();
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
            EnableSegment();
        }

        /// <summary>
        /// Enables all UI elements related to line configuration options.
        /// </summary>
        private static void EnableLineOptions()
        {
            EnableLineKindFromLineMenu();
            EnableThicknessFromLineMenu();

            if (IsMainSegment)
            {
                EnableLayerFromLineMenu();
                EnableLoopFromLineMenu();
            }
            else
            {
                DisableLayerFromLineMenu();
                DisableLoopFromLineMenu();
            }

            EnableColorType();
            EnableColorPicker();

            if (selectedLineKind == LineKind.Dashed)
            {
                EnableTilingFromLineMenu();
            }
            else
            {
                DisableTilingFromLineMenu();
            }

            DisableFillOut();
            EnableColorKind();

            if (selectedColorKind != ColorKind.Monochrome)
            {
                EnableColorAreaFromLineMenu();
            }
            else
            {
                DisableColorAreaFromLineMenu();
            }
        }

        /// <summary>
        /// Disables all UI elements related to line configuration options.
        /// </summary>
        private static void DisableLineOptions()
        {
            DisableLineKindFromLineMenu();
            DisableThicknessFromLineMenu();
            DisableColorAreaFromLineMenu();
            DisableColorKind();
            DisableLayerFromLineMenu();
            DisableLoopFromLineMenu();
            DisableColorType();
            DisableColorPicker();
            DisableTilingFromLineMenu();
            DisableFillOut();
        }

        /// <summary>
        /// Hides the line-kind controls.
        /// </summary>
        private static void DisableLineKindFromLineMenu()
        {
            controls.LineKindSelectionObject.SetActive(false);
            controls.LineKindTextObject.SetActive(false);
        }

        /// <summary>
        /// Shows the line-kind controls.
        /// </summary>
        private static void EnableLineKindFromLineMenu()
        {
            if (selectedLineKind != LineKind.Dashed)
            {
                controls.TilingSlider.ResetToMin();
            }

            controls.LineKindSelectionObject.SetActive(true);
            controls.LineKindTextObject.SetActive(true);
        }

        /// <summary>
        /// Hides the tiling controls.
        /// </summary>
        private static void DisableTilingFromLineMenu()
        {
            controls.TilingObject.SetActive(false);
        }

        /// <summary>
        /// Shows the tiling controls.
        /// </summary>
        private static void EnableTilingFromLineMenu()
        {
            controls.TilingObject.SetActive(true);
        }

        /// <summary>
        /// Hides the order-in-layer controls.
        /// </summary>
        private static void DisableLayerFromLineMenu()
        {
            controls.LayerObject.SetActive(false);
        }

        /// <summary>
        /// Shows the order-in-layer controls and enables their slider.
        /// </summary>
        private static void EnableLayerFromLineMenu()
        {
            controls.LayerObject.SetActive(true);
            controls.LayerSlider.interactable = true;
        }

        /// <summary>
        /// Hides the thickness controls.
        /// </summary>
        private static void DisableThicknessFromLineMenu()
        {
            controls.ThicknessObject.SetActive(false);
        }

        /// <summary>
        /// Shows the thickness controls.
        /// </summary>
        private static void EnableThicknessFromLineMenu()
        {
            controls.ThicknessObject.SetActive(true);
        }

        /// <summary>
        /// Hides the loop controls.
        /// </summary>
        private static void DisableLoopFromLineMenu()
        {
            controls.LoopObject.SetActive(false);
        }

        /// <summary>
        /// Shows the loop controls.
        /// </summary>
        private static void EnableLoopFromLineMenu()
        {
            controls.LoopObject.SetActive(true);
        }

        /// <summary>
        /// Refreshes the loop controls.
        /// </summary>
        private static void RefreshLoop()
        {
            DisableLoopFromLineMenu();
            EnableLoopFromLineMenu();
        }

        /// <summary>
        /// Refreshes the fill-out switch controls.
        /// </summary>
        private static void RefreshFillOut()
        {
            controls.FillOutObject.SetActive(!controls.FillOutObject.activeInHierarchy);
            controls.FillOutObject.SetActive(!controls.FillOutObject.activeInHierarchy);
        }

        /// <summary>
        /// Hides the color-area selector.
        /// </summary>
        private static void DisableColorAreaFromLineMenu()
        {
            controls.ColorAreaSelectorObject.SetActive(false);
        }

        /// <summary>
        /// Shows the color-area selector.
        /// </summary>
        private static void EnableColorAreaFromLineMenu()
        {
            controls.ColorAreaSelectorObject.SetActive(true);
        }

        /// <summary>
        /// Hides the return button.
        /// </summary>
        private static void DisableReturn()
        {
            controls.ReturnButtonObject.SetActive(false);
        }

        /// <summary>
        /// Shows the return button.
        /// </summary>
        private static void EnableReturn()
        {
            controls.ReturnButtonObject.SetActive(true);
        }

        /// <summary>
        /// Shows the fill-out controls.
        /// </summary>
        private static void EnableFillOut()
        {
            controls.FillOutObject.SetActive(true);
        }

        /// <summary>
        /// Hides the fill-out controls.
        /// </summary>
        private static void DisableFillOut()
        {
            controls.FillOutObject.SetActive(false);
        }

        /// <summary>
        /// Shows the color-kind controls.
        /// </summary>
        private static void EnableColorKind()
        {
            controls.ColorKindSelectionObject.SetActive(true);

            if (selectedColorKind != ColorKind.Monochrome)
            {
                EnableColorAreaFromLineMenu();
            }
        }

        /// <summary>
        /// Hides the color-kind controls.
        /// </summary>
        private static void DisableColorKind()
        {
            controls.ColorKindSelectionObject.SetActive(false);
            DisableColorAreaFromLineMenu();
        }

        /// <summary>
        /// Shows the color-type selector.
        /// </summary>
        private static void EnableColorType()
        {
            controls.ColorTypeSelectorObject.SetActive(true);
        }

        /// <summary>
        /// Hides the color-type selector.
        /// </summary>
        private static void DisableColorType()
        {
            controls.ColorTypeSelectorObject.SetActive(false);
        }

        /// <summary>
        /// Shows the color picker.
        /// </summary>
        private static void EnableColorPicker()
        {
            controls.ColorPickerObject.SetActive(true);
        }

        /// <summary>
        /// Hides the color picker.
        /// </summary>
        private static void DisableColorPicker()
        {
            controls.ColorPickerObject.SetActive(false);
        }

        /// <summary>
        /// Enables the segment area.
        /// </summary>
        private static void EnableSegment()
        {
            Instance.lineCapMenu.EnableSegment();
        }

        /// <summary>
        /// Hides the segment area.
        /// </summary>
        private static void DisableSegment()
        {
            Instance.lineCapMenu.DisableSegment();
        }

        /// <summary>
        /// Enables the line cap area.
        /// </summary>
        private static void EnableLineCap()
        {
            Instance.lineCapMenu.EnableLineCap();
        }

        /// <summary>
        /// Hides the line cap area.
        /// </summary>
        private static void DisableLineCap()
        {
            Instance.lineCapMenu.DisableLineCap();

            if (mode == Mode.Edit)
            {
                EnableLineOptions();
            }
        }
        #endregion
    }
}

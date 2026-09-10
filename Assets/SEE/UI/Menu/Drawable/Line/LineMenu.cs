using Michsky.UI.ModernUIPack;
using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.UI.Drawable;
using UnityEngine;
using UnityEngine.Events;
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
        /// Holds the current selected line kind.
        /// </summary>
        private LineKind selectedLineKind;

        /// <summary>
        /// Holds the current selected color kind.
        /// </summary>
        private ColorKind selectedColorKind;

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
        private Mode mode;

        /// <summary>
        /// Manages the drawing-specific behavior of the line menu.
        /// </summary>
        private DrawLineMenu drawLineMenu;

        /// <summary>
        /// Manages the editing-specific behavior of the line menu.
        /// </summary>
        private EditLineMenu editLineMenu;

        /// <summary>
        /// Manages the line-cap and segment selection of this menu.
        /// </summary>
        private LineCapMenu lineCapMenu;
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

            /// Initializes the editing-specific line-menu component.
            Instance.editLineMenu = new EditLineMenu(
                Instance.gameObject,
                controls,
                Instance.lineCapMenu,
                Instance.AssignLineKind,
                Instance.AssignColorKind,
                () => Instance.selectedLineKind,
                () => Instance.selectedColorKind,
                EnsureValidSecondaryColor);

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

            Instance.mode = Mode.None;
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
        /// </summary>
        /// <param name="selectedLine">The line object to edit.</param>
        /// <param name="newValueHolder">
        /// The drawable configuration containing the current values of the selected line.
        /// The menu is initialized only if this configuration is a <see cref="LineConf"/>.
        /// </param>
        /// <param name="returnCall">
        /// An optional callback that returns to the parent menu.
        /// </param>
        public void EnableForEditing(
            GameObject selectedLine,
            DrawableType newValueHolder,
            UnityAction returnCall = null)
        {
            if (newValueHolder is LineConf lineHolder)
            {
                bool isFreehandLine =
                    EditLineMenu.IsFreehandLine(selectedLine);

                if (returnCall == null)
                {
                    EnableLineMenu(
                        withoutMenuLayer: isFreehandLine
                            ? new MenuLayer[] { MenuLayer.Segment }
                            : null);
                }
                else
                {
                    EnableLineMenu(
                        withoutMenuLayer:
                            new MenuLayer[] { MenuLayer.Segment });
                }

                editLineMenu.Enable(
                    selectedLine,
                    lineHolder,
                    isFreehandLine,
                    returnCall);

                mode = Mode.Edit;

                MenuHelper.CalculateHeight(gameObject, true);
            }
        }

        /// <summary>
        /// Assigns a fill-out status and color to the edit mode.
        /// </summary>
        /// <param name="fillOut">The status and color.</param>
        /// <param name="setFillOutAction">Fill-out color change action.</param>
        /// <param name="clearFillOutAction">Action to clear the value.</param>
        public static void AssignFillOutForEditing(
            Color? fillOut,
            UnityAction<Color> setFillOutAction,
            UnityAction clearFillOutAction)
        {
            if (Instance.IsInEditMode())
            {
                Instance.editLineMenu.AssignFillOut(
                    fillOut,
                    setFillOutAction,
                    clearFillOutAction);
            }
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
        /// Removes the drawing-specific, editing-specific and line-cap handlers
        /// registered at the shared line-menu controls.
        /// </summary>
        private void RemoveListeners()
        {
            EnableLineMenuLayers();

            drawLineMenu.RemoveListeners();
            editLineMenu.RemoveListeners();
            lineCapMenu.RemoveListeners();
        }

        /// <summary>
        /// Assigns an action and a color to the HSV color picker while editing.
        /// </summary>
        /// <param name="newColorAction">
        /// The color action that should be assigned.
        /// </param>
        /// <param name="color">The color that should be assigned.</param>
        public static void AssignColorArea(
            UnityAction<Color> newColorAction,
            Color color)
        {
            Instance.editLineMenu.AssignColorArea(
                newColorAction,
                color);
        }

        #region LineKind

        /// <summary>
        /// Returns the index of the currently selected line kind.
        /// </summary>
        /// <returns>The index of the selected line kind.</returns>
        private int GetIndexOfSelectedLineKind()
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

            if (!editLineMenu.IsRefreshingUI)
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

            if (kind == LineKind.Dashed)
            {
                EnableTilingFromLineMenu();
                controls.TilingSlider.AssignValue(tiling);
            }
            else
            {
                DisableTilingFromLineMenu();
            }

            if (!editLineMenu.IsRefreshingUI)
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
        private int GetIndexOfSelectedColorKind()
        {
            return GetColorKinds(true).IndexOf(selectedColorKind);
        }

        /// <summary>
        /// Assigns a color kind to the color-kind selection.
        /// </summary>
        /// <param name="kind">The color kind that should be assigned.</param>
        public void AssignColorKind(ColorKind kind)
        {
            selectedColorKind = kind;

            if (kind == ColorKind.Monochrome)
            {
                DisableColorAreaFromLineMenu();
            }
            else
            {
                EnableColorAreaFromLineMenu();
            }

            if (!editLineMenu.IsRefreshingUI)
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
        #endregion

        /// <summary>
        /// Refreshes the line-kind and color-kind selectors so that they display
        /// the currently selected values.
        /// </summary>
        public static void RefreshHorizontalSelectors()
        {
            controls.LineKindSelector.index = Instance.GetIndexOfSelectedLineKind();
            controls.ColorKindSelector.index = Instance.GetIndexOfSelectedColorKind();
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
            if (Instance.selectedLineKind != LineKind.Dashed)
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

            if (Instance.selectedColorKind != ColorKind.Monochrome)
            {
                EnableColorAreaFromLineMenu();
            }
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
        /// Hides the line-cap selection.
        /// </summary>
        private static void DisableLineCap()
        {
            Instance.lineCapMenu.DisableLineCap();

            if (Instance.mode == Mode.Edit)
            {
                Instance.editLineMenu.EnableLineOptions();
            }
        }
        #endregion
    }
}

using Assets.SEE.Game.Drawable;
using Michsky.UI.ModernUIPack;
using SEE.Controls.Actions;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using SEE.Controls.Actions.Drawable;
using static Assets.SEE.Game.Drawable.GameDrawer;

namespace Assets.SEE.Game.UI.Drawable
{
    /// <summary>
    /// This class holds the instance for the line menu.
    /// </summary>
    public static class LineMenu
    {
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
        /// The transform of the dragger object.
        /// </summary>
        private static Transform dragger;

        /// <summary>
        /// Holds the current selected line kind.
        /// </summary>
        private static GameDrawer.LineKind selectedLineKind;

        /// <summary>
        /// Holds the current selected color kind.
        /// </summary>
        private static GameDrawer.ColorKind selectedColorKind;

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

            instance = PrefabInstantiator.InstantiatePrefab(lineMenuPrefab,
                GameObject.Find("UI Canvas").transform, false);
            content = instance.transform.Find("Content");
            dragger = instance.transform.Find("Dragger");
            disableReturn();

            lineKindSelector = GameFinder.FindChild(instance, "LineKindSelection").GetComponent<HorizontalSelector>();
            foreach (LineKind kind in GameDrawer.GetLineKinds())
            {
                lineKindSelector.CreateNewItem(kind.ToString());
            }
            lineKindSelector.selectorEvent.AddListener(index =>
            {
                if (GameDrawer.GetLineKinds()[index] == GameDrawer.LineKind.Dashed)
                {
                    enableTilingFromLineMenu();
                }
                else
                {
                    disableTilingFromLineMenu();
                }
                if (GameDrawer.GetLineKinds()[index] == GameDrawer.LineKind.Solid &&
                    selectedColorKind == GameDrawer.ColorKind.TwoDashed)
                {
                    AssignColorKind(GameDrawer.ColorKind.Monochrome);
                    colorKindSelector.label.text = ColorKind.Monochrome.ToString();
                    colorKindSelector.index = 0;
                    colorKindSelector.UpdateUI();
                }
                AssignLineKind(GameDrawer.GetLineKinds()[index]);
            });
            lineKindSelector.defaultIndex = 0;

            colorKindSelector = GameFinder.FindChild(instance, "ColorKindSelection").GetComponent<HorizontalSelector>();
            bool isDashed = selectedLineKind != GameDrawer.LineKind.Solid;
            foreach (ColorKind kind in GameDrawer.GetColorKinds(true))
            {
                colorKindSelector.CreateNewItem(kind.ToString());
            }
            colorKindSelector.selectorEvent.AddListener(index =>
            {
                bool isDashed = selectedLineKind != GameDrawer.LineKind.Solid;
                ColorKind newColorKind = GameDrawer.GetColorKinds(true)[index];
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
            colorKindSelector.defaultIndex = 0;

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
            instance.SetActive(false);
        }

        /// <summary>
        /// Enables all line menu layers,
        /// sets the parent back to UI Canvas, enables the window dragger 
        /// and then hides the line menu.
        /// The parent of the line menu can be switched through the <see cref="DrawShapesAction"/>
        /// </summary>
        public static void disableLineMenu()
        {
            enableLineMenuLayers();
            instance.transform.SetParent(GameObject.Find("UI Canvas").transform);
            GameFinder.FindChild(instance, "Dragger").GetComponent<WindowDragger>().enabled = true;
            disableReturn();
            instance.SetActive(false);
        }

        #region Enable Line Menu
        /// <summary>
        /// Enables the line menu. And resets the additional Handler if the parameter for that is true.
        /// Also it can hide some menu layer.
        /// </summary>
        /// <param name="removeListeners">The bool, if the Handler should be reset.</param>
        /// <param name="withoutMenuLayer">An array of menu layers that should hide.</param>
        public static void enableLineMenu(bool removeListeners = true, MenuLayer[] withoutMenuLayer = null)
        {
            if (removeListeners)
            {
                RemoveListeners();
            }
            if (withoutMenuLayer != null)
            {
                foreach (MenuLayer menuPoint in withoutMenuLayer)
                {
                    switch (menuPoint)
                    {
                        case MenuLayer.LineKind:
                            disableLineKindFromLineMenu();
                            break;
                        case MenuLayer.Thickness:
                            disableThicknessFromLineMenu();
                            break;
                        case MenuLayer.Layer:
                            disableLayerFromLineMenu();
                            break;
                        case MenuLayer.Loop:
                            disableLoopFromLineMenu();
                            break;
                        case MenuLayer.All:
                            disableLineKindFromLineMenu();
                            disableThicknessFromLineMenu();
                            disableLayerFromLineMenu();
                            disableLoopFromLineMenu();
                            break;
                    }
                }
            }
            if (selectedLineKind != GameDrawer.LineKind.Dashed)
            {
                disableTilingFromLineMenu();
            }
            instance.SetActive(true);
            MenuHelper.CalculateHeight(instance);
        }

        /// <summary>
        /// Enables the line menu for drawing.
        /// </summary>
        public static void EnableForDrawing()
        {
            enableLineMenu(withoutMenuLayer: new MenuLayer[] { MenuLayer.Layer, MenuLayer.Loop });
            InitDrawing();
            MenuHelper.CalculateHeight(instance);
        }

        /// <summary>
        /// Init the Handlers for the Drawing.
        /// It's needed to be outsourced because it will needed in shape menu.
        /// </summary>
        public static void InitDrawing()
        {
            tilingSlider.onValueChanged.AddListener(tilingAction = tiling =>
            {
                ValueHolder.currentTiling = tiling;
            });

            AssignLineKind(ValueHolder.currentLineKind);
            lineKindSelector.index = GetIndexOfSelectedLineKind();
            lineKindSelector.UpdateUI();
            if (lineKindAction != null)
            {
                lineKindSelector.selectorEvent.RemoveListener(lineKindAction);
            }
            lineKindAction = index =>
            {
                ValueHolder.currentLineKind = GameDrawer.GetLineKinds()[index];
                if (ValueHolder.currentLineKind == GameDrawer.LineKind.Solid &&
                    ValueHolder.currentColorKind == GameDrawer.ColorKind.TwoDashed)
                {
                    ValueHolder.currentColorKind = ColorKind.Monochrome;
                }
            };
            lineKindSelector.selectorEvent.AddListener(lineKindAction);

            AssignColorKind(ValueHolder.currentColorKind);
            colorKindSelector.index = GetIndexOfSelectedColorKind();
            colorKindSelector.UpdateUI();
            if (colorKindAction != null)
            {
                colorKindSelector.selectorEvent.RemoveListener(colorKindAction);
            }
            colorKindAction = index => { ValueHolder.currentColorKind = GameDrawer.GetColorKinds(true)[index]; };
            colorKindSelector.selectorEvent.AddListener(colorKindAction);

            primaryColorBMB.clickEvent.RemoveAllListeners();
            primaryColorBMB.clickEvent.AddListener(MutuallyExclusiveColorButtons);
            primaryColorBMB.clickEvent.AddListener(() =>
            {
                AssignColorArea((color => ValueHolder.currentPrimaryColor = color), ValueHolder.currentPrimaryColor);
            });
            primaryColorBMB.buttonVar.interactable = false;

            secondaryColorBMB.clickEvent.RemoveAllListeners();
            secondaryColorBMB.clickEvent.AddListener(MutuallyExclusiveColorButtons);
            secondaryColorBMB.clickEvent.AddListener(() =>
            {
                if (ValueHolder.currentSecondaryColor == Color.clear)
                {
                    ValueHolder.currentSecondaryColor = Random.ColorHSV();
                }
                if (ValueHolder.currentSecondaryColor.a == 0)
                {
                    ValueHolder.currentSecondaryColor = new Color(ValueHolder.currentSecondaryColor.r, ValueHolder.currentSecondaryColor.g, ValueHolder.currentSecondaryColor.b, 255);
                }
                AssignColorArea((color => { ValueHolder.currentSecondaryColor = color; }), ValueHolder.currentSecondaryColor);
            });
            secondaryColorBMB.buttonVar.interactable = true;

            ThicknessSliderController thicknessSlider = instance.GetComponentInChildren<ThicknessSliderController>();
            thicknessSlider.AssignValue(ValueHolder.currentThickness);
            thicknessSlider.onValueChanged.AddListener(thickness =>
            {
                ValueHolder.currentThickness = thickness;
            });

            picker.AssignColor(ValueHolder.currentPrimaryColor);
            picker.onValueChanged.AddListener((colorAction = color => ValueHolder.currentPrimaryColor = color));
            MenuHelper.CalculateHeight(instance);
        }

        /// <summary>
        /// This method provides the line menu for editing, adding the necessary Handler to the respective components.
        /// </summary>
        /// <param name="selectedLine">The selected line object for editing.</param>
        public static void EnableForEditing(GameObject selectedLine, DrawableType newValueHolder, UnityAction returnCall = null)
        {
            if (newValueHolder is LineConf lineHolder)
            {
                enableLineMenu();
                if (returnCall != null)
                {
                    enableReturn();
                    ButtonManagerBasic returnBtn = GameFinder.FindChild(instance, "ReturnBtn").GetComponent<ButtonManagerBasic>();
                    returnBtn.clickEvent.RemoveAllListeners();
                    returnBtn.clickEvent.AddListener(returnCall);
                    GameFinder.FindChild(instance, "Layer").GetComponentInChildren<Slider>().interactable = false;
                }
                LineRenderer renderer = selectedLine.GetComponent<LineRenderer>();
                GameObject drawable = GameFinder.GetDrawable(selectedLine);
                string drawableParentName = GameFinder.GetDrawableParentName(drawable);

                AssignLineKind(selectedLine.GetComponent<LineValueHolder>().GetLineKind(), renderer.textureScale.x);
                lineKindSelector.index = GetIndexOfSelectedLineKind();
                lineKindSelector.UpdateUI();
                if (lineKindAction != null)
                {
                    lineKindSelector.selectorEvent.RemoveListener(lineKindAction);
                }
                lineKindAction = index =>
                {
                    if (GameDrawer.GetLineKinds()[index] != LineKind.Dashed)
                    {
                        lineHolder.lineKind = GameDrawer.GetLineKinds()[index];
                        if (lineHolder.lineKind == GameDrawer.LineKind.Solid &&
                            lineHolder.colorKind == GameDrawer.ColorKind.TwoDashed)
                        {
                            lineHolder.colorKind = ColorKind.Monochrome;
                            ChangeColorKind(selectedLine, lineHolder.colorKind, lineHolder);
                            new ChangeColorKindNetAction(drawable.name, drawableParentName, LineConf.GetLine(selectedLine), lineHolder.colorKind).Execute();
                        }
                        ChangeLineKind(selectedLine, lineHolder.lineKind, lineHolder.tiling);
                        new ChangeLineKindNetAction(drawable.name, drawableParentName, selectedLine.name,
                                lineHolder.lineKind, lineHolder.tiling).Execute();
                    }
                };
                lineKindSelector.selectorEvent.AddListener(lineKindAction);

                AssignColorKind(lineHolder.colorKind);
                colorKindSelector.index = GetIndexOfSelectedColorKind();
                colorKindSelector.UpdateUI();
                if (colorKindAction != null)
                {
                    colorKindSelector.selectorEvent.RemoveListener(colorKindAction);
                }
                colorKindAction = index =>
                {
                    lineHolder.colorKind = GameDrawer.GetColorKinds(true)[index];
                    ChangeColorKind(selectedLine, lineHolder.colorKind, lineHolder);
                    new ChangeColorKindNetAction(drawable.name, drawableParentName, LineConf.GetLine(selectedLine), lineHolder.colorKind).Execute();
                };
                colorKindSelector.selectorEvent.AddListener(colorKindAction);

                tilingSlider.onValueChanged.AddListener(tilingAction = tiling =>
                {
                    ChangeLineKind(selectedLine, LineKind.Dashed, tiling);
                    lineHolder.lineKind = LineKind.Dashed;
                    lineHolder.tiling = tiling;
                    new ChangeLineKindNetAction(drawable.name, drawableParentName, selectedLine.name,
                            LineKind.Dashed, tiling).Execute();
                });

                primaryColorBMB.clickEvent.RemoveAllListeners();
                primaryColorBMB.clickEvent.AddListener(MutuallyExclusiveColorButtons);
                primaryColorBMB.clickEvent.AddListener(() =>
                {
                    AssignColorArea((color =>
                    {
                        GameEdit.ChangePrimaryColor(selectedLine, color);
                        lineHolder.primaryColor = color;
                        new EditLinePrimaryColorNetAction(drawable.name, drawableParentName, selectedLine.name, color).Execute();
                    }), lineHolder.primaryColor);
                });
                primaryColorBMB.buttonVar.interactable = false;

                secondaryColorBMB.clickEvent.RemoveAllListeners();
                secondaryColorBMB.clickEvent.AddListener(MutuallyExclusiveColorButtons);
                secondaryColorBMB.clickEvent.AddListener(() =>
                {
                    if (lineHolder.secondaryColor == Color.clear)
                    {
                        lineHolder.secondaryColor = Random.ColorHSV();
                    }
                    if (lineHolder.secondaryColor.a == 0)
                    {
                        lineHolder.secondaryColor = new Color(lineHolder.secondaryColor.r, lineHolder.secondaryColor.g, lineHolder.secondaryColor.b, 255);
                    }
                    AssignColorArea((color =>
                    {
                        GameEdit.ChangeSecondaryColor(selectedLine, color);
                        lineHolder.secondaryColor = color;
                        new EditLineSecondaryColorNetAction(drawable.name, drawableParentName, selectedLine.name, color).Execute();
                    }), lineHolder.secondaryColor);
                });
                secondaryColorBMB.buttonVar.interactable = true;

                ThicknessSliderController thicknessSlider = instance.GetComponentInChildren<ThicknessSliderController>();
                thicknessSlider.AssignValue(renderer.startWidth);
                thicknessSlider.onValueChanged.AddListener(thickness =>
                {
                    if (thickness > 0.0f)
                    {
                        GameEdit.ChangeThickness(selectedLine, thickness);
                        lineHolder.thickness = thickness;
                        new EditLineThicknessNetAction(drawable.name, drawableParentName, selectedLine.name, thickness).Execute();
                    }
                });

                LayerSliderController layerSlider = instance.GetComponentInChildren<LayerSliderController>();
                layerSlider.AssignValue(lineHolder.orderInLayer);
                layerSlider.onValueChanged.AddListener(layerOrder =>
                {
                    GameEdit.ChangeLayer(selectedLine, layerOrder);
                    lineHolder.orderInLayer = layerOrder;
                    new EditLayerNetAction(drawable.name, drawableParentName, selectedLine.name, layerOrder).Execute();
                });

                loopManager.isOn = lineHolder.loop;
                loopManager.OnEvents.RemoveAllListeners();
                loopManager.OnEvents.AddListener(() => 
                {
                    GameEdit.ChangeLoop(selectedLine, true);
                    lineHolder.loop = true;
                    new EditLineLoopNetAction(drawable.name, drawableParentName, selectedLine.name, true).Execute();
                });
                loopManager.OffEvents.RemoveAllListeners();
                loopManager.OffEvents.AddListener(() =>
                {
                    GameEdit.ChangeLoop(selectedLine, false);
                    lineHolder.loop = false;
                    new EditLineLoopNetAction(drawable.name, drawableParentName, selectedLine.name, false).Execute();
                });

                switch (lineHolder.colorKind)
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
                picker.onValueChanged.AddListener(colorAction = color =>
                {
                    GameEdit.ChangePrimaryColor(selectedLine, color);
                    lineHolder.primaryColor = color;
                    new EditLinePrimaryColorNetAction(drawable.name, drawableParentName, selectedLine.name, color).Execute();
                });
                MenuHelper.CalculateHeight(instance);
            }
        }
        #endregion

        /// <summary>
        /// This method removes the handler of the
        /// line kind buttons (previous/next), the tiling slider controller,
        /// the thickness slider controller, order in layer slider controller,
        /// toggle and the additional color action for the hsv color picker.
        /// </summary>
        private static void RemoveListeners()
        {
            enableLineMenuLayers();
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
                if (selectedLineKind != GameDrawer.LineKind.Dashed)
                {
                    tilingSlider.ResetToMin();
                }
                instance.GetComponentInChildren<FloatValueSliderController>().onValueChanged.RemoveListener(tilingAction);
            }
            primaryColorBMB.clickEvent.RemoveAllListeners();
            secondaryColorBMB.clickEvent.RemoveAllListeners();
            instance.GetComponentInChildren<ThicknessSliderController>().onValueChanged.RemoveAllListeners();
            instance.GetComponentInChildren<LayerSliderController>().onValueChanged.RemoveAllListeners();
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
            return GameDrawer.GetLineKinds().IndexOf(selectedLineKind);
        }

        /// <summary>
        /// Assigns a line kind to the line kind selection.
        /// </summary>
        /// <param name="kind">The line kind that should be assigned</param>
        public static void AssignLineKind(GameDrawer.LineKind kind)
        {
            selectedLineKind = kind;
            MenuHelper.CalculateHeight(instance);
        }

        /// <summary>
        /// Assign a line kind and a tiling to the line kind selection and tiling slider controller.
        /// </summary>
        /// <param name="kind">The line kind that should be assigned.</param>
        /// <param name="tiling"></param>
        public static void AssignLineKind(GameDrawer.LineKind kind, float tiling)
        {
            selectedLineKind = kind;
            if (kind == GameDrawer.LineKind.Dashed)
            {
                enableTilingFromLineMenu();
                tilingSlider.AssignValue(tiling);
            }
            else
            {
                disableTilingFromLineMenu();
            }
            MenuHelper.CalculateHeight(instance);
        }
        #endregion

        #region ColorKind
        /// <summary>
        /// Returns the index of the current selected color kind.
        /// </summary>
        /// <returns>Index of selected color kind</returns>
        private static int GetIndexOfSelectedColorKind()
        {
            return GameDrawer.GetColorKinds(true).IndexOf(selectedColorKind);
        }

        /// <summary>
        /// Assigns a color kind to the color kind selection.
        /// </summary>
        /// <param name="kind">The line kind that should be assigned</param>
        public static void AssignColorKind(GameDrawer.ColorKind kind)
        {
            selectedColorKind = kind;
            if (kind == GameDrawer.ColorKind.Monochrome)
            {
                disableColorAreaFromLineMenu();
            }
            else
            {
                enableColorAreaFromLineMenu();
            }
            MenuHelper.CalculateHeight(instance);
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

        #region Enable/Disable Layer
        /// <summary>
        /// Enables all line menu layers that can be hidden.
        /// </summary>
        private static void enableLineMenuLayers()
        {
            enableLineKindFromLineMenu();
            enableTilingFromLineMenu();
            enableLoopFromLineMenu();
            enableLayerFromLineMenu();
            enableThicknessFromLineMenu();
        }

        /// <summary>
        /// Hides the line kind layer
        /// </summary>
        private static void disableLineKindFromLineMenu()
        {
            content.Find("LineKindSelection").gameObject.SetActive(false);
        }

        /// <summary>
        /// Enables the line kind layer
        /// </summary>
        private static void enableLineKindFromLineMenu()
        {
            if (selectedLineKind != GameDrawer.LineKind.Dashed)
            {
                tilingSlider.ResetToMin();
            }
            content.Find("LineKindSelection").gameObject.SetActive(true);
        }

        /// <summary>
        /// Hides the tiling layer
        /// </summary>
        private static void disableTilingFromLineMenu()
        {
            tilingSlider.ResetToMin();
            content.Find("Tiling").gameObject.SetActive(false);
        }

        /// <summary>
        /// Enables the tiling layer.
        /// </summary>
        private static void enableTilingFromLineMenu()
        {
            content.Find("Tiling").gameObject.SetActive(true);
        }

        /// <summary>
        /// Hides the order in layer layer.
        /// </summary>
        private static void disableLayerFromLineMenu()
        {
            content.Find("Layer").gameObject.SetActive(false);
        }

        /// <summary>
        /// Enables the order in layer layer.
        /// </summary>
        private static void enableLayerFromLineMenu()
        {
            content.Find("Layer").gameObject.SetActive(true);
            content.Find("Layer").GetComponentInChildren<Slider>().interactable = true;
        }

        /// <summary>
        /// Hides the line thickness layer.
        /// </summary>
        private static void disableThicknessFromLineMenu()
        {
            content.Find("Thickness").gameObject.SetActive(false);
        }

        /// <summary>
        /// Enables the line thickness layer.
        /// </summary>
        private static void enableThicknessFromLineMenu()
        {
            content.Find("Thickness").gameObject.SetActive(true);
        }

        /// <summary>
        /// Hides the loop layer.
        /// </summary>
        private static void disableLoopFromLineMenu()
        {
            content.Find("Loop").gameObject.SetActive(false);
        }

        /// <summary>
        /// Enables the loop layer.
        /// </summary>
        private static void enableLoopFromLineMenu()
        {
            content.Find("Loop").gameObject.SetActive(true);
        }

        /// <summary>
        /// Hides the color area selector layer.
        /// </summary>
        private static void disableColorAreaFromLineMenu()
        {
            content.Find("ColorAreaSelector").gameObject.SetActive(false);
        }

        /// <summary>
        /// Enables the color area selector layer.
        /// </summary>
        private static void enableColorAreaFromLineMenu()
        {
            content.Find("ColorAreaSelector").gameObject.SetActive(true);
        }

        /// <summary>
        /// Hides the return button.
        /// </summary>
        private static void disableReturn()
        {
            instance.transform.Find("ReturnBtn").gameObject.SetActive(false);
        }

        /// <summary>
        /// Enables the return button
        /// </summary>
        private static void enableReturn()
        {
            instance.transform.Find("ReturnBtn").gameObject.SetActive(true);
        }
        #endregion
    }
}
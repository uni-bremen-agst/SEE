using Assets.SEE.Game.Drawable;
using SEE.Controls.Actions;
using SEE.Game;
using SEE.Game.Drawable.Configurations;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static SEE.Game.GameDrawer;

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
        /// The text component for the displayed line kind text
        /// </summary>
        private static TMP_Text lineKindText;

        /// <summary>
        /// The text component for the displayed color kind text
        /// </summary>
        private static TMP_Text colorKindText;

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
        /// The next button for the line kind selection.
        /// </summary>
        private static Button nextLineKindBtn;

        /// <summary>
        /// The previous button for the line kind selection.
        /// </summary>
        private static Button previousLineKindBtn;

        /// <summary>
        /// The next button for the color kind selection.
        /// </summary>
        private static Button nextColorKindBtn;

        /// <summary>
        /// The previous button for the color kind selection.
        /// </summary>
        private static Button previousColorKindBtn;

        /// <summary>
        /// Button for chose primary color.
        /// </summary>
        private static Button primaryColorBtn;

        /// <summary>
        /// Button for chose secondary color.
        /// </summary>
        private static Button secondaryColorBtn;

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
            lineKindText = instance.transform.Find("LineKindSelection").Find("KindSelection").GetComponent<TMP_Text>();
            colorKindText = instance.transform.Find("ColorKindSelection").Find("KindSelection").GetComponent<TMP_Text>();
            AssignLineKind(ValueHolder.currentLineKind);
            AssignColorKind(ValueHolder.currentColorKind);
            previousLineKindBtn = instance.transform.Find("LineKindSelection").Find("PreviousBtn").GetComponent<Button>();
            nextLineKindBtn = instance.transform.Find("LineKindSelection").Find("NextBtn").GetComponent<Button>();
            previousColorKindBtn = instance.transform.Find("ColorKindSelection").Find("PreviousBtn").GetComponent<Button>();
            nextColorKindBtn = instance.transform.Find("ColorKindSelection").Find("NextBtn").GetComponent<Button>();
            primaryColorBtn = instance.transform.Find("ColorAreaSelector").Find("PrimaryColorBtn").GetComponent <Button>();
            primaryColorBtn.onClick.AddListener(MutuallyExclusiveColorButtons);
            primaryColorBtn.interactable = false;
            secondaryColorBtn = instance.transform.Find("ColorAreaSelector").Find("SecondaryColorBtn").GetComponent <Button>();
            secondaryColorBtn.onClick.AddListener(MutuallyExclusiveColorButtons);
            tilingSlider = instance.transform.Find("Tiling").GetComponentInChildren<FloatValueSliderController>();
            picker = instance.GetComponent<HSVPicker.ColorPicker>();
            instance.SetActive(false);
        }

        /// <summary>
        /// Enables all line menu layers and then
        /// hides the line menu.
        /// </summary>
        public static void disableLineMenu()
        {
            enableLineMenuLayers();
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
        }

        /// <summary>
        /// Enables the line menu for drawing.
        /// </summary>
        public static void EnableForDrawing()
        {
            enableLineMenu(withoutMenuLayer: new MenuLayer[] { MenuLayer.Layer, MenuLayer.Loop });
            InitDrawing();
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

            nextLineKindBtn.onClick.RemoveAllListeners();
            nextLineKindBtn.onClick.AddListener(() => ValueHolder.currentLineKind = NextLineKind());

            previousLineKindBtn.onClick.RemoveAllListeners();
            previousLineKindBtn.onClick.AddListener(() => ValueHolder.currentLineKind = PreviousLineKind());

            nextColorKindBtn.onClick.RemoveAllListeners();
            nextColorKindBtn.onClick.AddListener(() => ValueHolder.currentColorKind = NextColorKind());

            previousColorKindBtn.onClick.RemoveAllListeners();
            previousColorKindBtn.onClick.AddListener(() => ValueHolder.currentColorKind = PreviousColorKind());

            primaryColorBtn.onClick.RemoveAllListeners();
            primaryColorBtn.onClick.AddListener(MutuallyExclusiveColorButtons);
            primaryColorBtn.onClick.AddListener(() =>
            {
                AssignColorArea((color => ValueHolder.currentPrimaryColor = color), ValueHolder.currentPrimaryColor);
            });
            primaryColorBtn.interactable = false;

            secondaryColorBtn.onClick.RemoveAllListeners();
            secondaryColorBtn.onClick.AddListener(MutuallyExclusiveColorButtons);
            secondaryColorBtn.onClick.AddListener(() =>
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
            secondaryColorBtn.interactable = true;

            ThicknessSliderController thicknessSlider = instance.GetComponentInChildren<ThicknessSliderController>();
            thicknessSlider.AssignValue(ValueHolder.currentThickness);
            thicknessSlider.onValueChanged.AddListener(thickness =>
            {
                ValueHolder.currentThickness = thickness;
            });

            picker.AssignColor(ValueHolder.currentPrimaryColor);
            picker.onValueChanged.AddListener((colorAction = color => ValueHolder.currentPrimaryColor = color));
        }

        /// <summary>
        /// This method provides the line menu for editing, adding the necessary Handler to the respective components.
        /// </summary>
        /// <param name="selectedLine">The selected line object for editing.</param>
        public static void EnableForEditing(GameObject selectedLine, DrawableType newValueHolder)
        {
            if (newValueHolder is LineConf lineHolder)
            {
                enableLineMenu();
                LineRenderer renderer = selectedLine.GetComponent<LineRenderer>();
                GameObject drawable = GameDrawableFinder.FindDrawable(selectedLine);
                string drawableParentName = GameDrawableFinder.GetDrawableParentName(drawable);

                AssignLineKind(selectedLine.GetComponent<LineValueHolder>().GetLineKind(), renderer.textureScale.x);

                tilingSlider.onValueChanged.AddListener(tilingAction = tiling =>
                {
                    ChangeLineKind(selectedLine, LineKind.Dashed, tiling);
                    lineHolder.lineKind = LineKind.Dashed;
                    lineHolder.tiling = tiling;
                    new ChangeLineKindNetAction(drawable.name, drawableParentName, selectedLine.name,
                            LineKind.Dashed, tiling).Execute();
                });

                nextLineKindBtn.onClick.RemoveAllListeners();
                nextLineKindBtn.onClick.AddListener(() =>
                {
                    LineKind kind = NextLineKind();
                    if (kind != LineKind.Dashed)
                    {
                        ChangeLineKind(selectedLine, kind, lineHolder.tiling);
                        lineHolder.lineKind = kind;
                        new ChangeLineKindNetAction(drawable.name, drawableParentName, selectedLine.name,
                            kind, lineHolder.tiling).Execute();
                    }
                });
                previousLineKindBtn.onClick.RemoveAllListeners();
                previousLineKindBtn.onClick.AddListener(() =>
                {
                    LineKind kind = PreviousLineKind();
                    if (kind != LineKind.Dashed)
                    {
                        ChangeLineKind(selectedLine, kind, lineHolder.tiling);
                        lineHolder.lineKind = kind;
                        new ChangeLineKindNetAction(drawable.name, drawableParentName, selectedLine.name,
                            kind, lineHolder.tiling).Execute();
                    }
                });

                AssignColorKind(lineHolder.colorKind);

                nextColorKindBtn.onClick.RemoveAllListeners();
                nextColorKindBtn.onClick.AddListener(() =>
                {
                    ColorKind kind = NextColorKind();
                    lineHolder.colorKind = kind;
                    ChangeColorKind(selectedLine, kind, lineHolder);
                    new ChangeColorKindNetAction(drawable.name, drawableParentName, LineConf.GetLine(selectedLine), kind).Execute();
                });

                previousColorKindBtn.onClick.RemoveAllListeners();
                previousColorKindBtn.onClick.AddListener(() => 
                {
                    ColorKind kind = PreviousColorKind();
                    lineHolder.colorKind = kind;
                    ChangeColorKind(selectedLine, kind, lineHolder);
                    new ChangeColorKindNetAction(drawable.name, drawableParentName, LineConf.GetLine(selectedLine), kind).Execute();
                });

                primaryColorBtn.onClick.RemoveAllListeners();
                primaryColorBtn.onClick.AddListener(MutuallyExclusiveColorButtons);
                primaryColorBtn.onClick.AddListener(() =>
                {
                    AssignColorArea((color =>
                    {
                        GameEdit.ChangePrimaryColor(selectedLine, color);
                        lineHolder.primaryColor = color;
                        new EditLinePrimaryColorNetAction(drawable.name, drawableParentName, selectedLine.name, color).Execute();
                    }), lineHolder.primaryColor);
                });
                primaryColorBtn.interactable = false;

                secondaryColorBtn.onClick.RemoveAllListeners();
                secondaryColorBtn.onClick.AddListener(MutuallyExclusiveColorButtons);
                secondaryColorBtn.onClick.AddListener(() =>
                {
                    if (lineHolder.secondaryColor == Color.clear)
                    {
                        lineHolder.secondaryColor = Random.ColorHSV();
                    }
                    if (lineHolder.secondaryColor.a == 0)
                    {
                        lineHolder.secondaryColor = new Color(lineHolder.secondaryColor.r, lineHolder.secondaryColor.g, lineHolder.secondaryColor.b, 255);
                    }
                    AssignColorArea((color => {
                        GameEdit.ChangeSecondaryColor(selectedLine, color);
                        lineHolder.secondaryColor = color;
                        new EditLineSecondaryColorNetAction(drawable.name, drawableParentName, selectedLine.name, color).Execute();
                    }), lineHolder.secondaryColor);
                });
                secondaryColorBtn.interactable = true;

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

                Toggle toggle = instance.GetComponentInChildren<Toggle>();
                toggle.isOn = lineHolder.loop;
                toggle.onValueChanged.AddListener(loop =>
                {
                    GameEdit.ChangeLoop(selectedLine, loop);
                    lineHolder.loop = loop;
                    new EditLineLoopNetAction(drawable.name, drawableParentName, selectedLine.name, loop).Execute();
                });

                switch(lineHolder.colorKind)
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
            previousLineKindBtn.onClick.RemoveAllListeners();
            nextLineKindBtn.onClick.RemoveAllListeners();
            if (tilingAction != null)
            {
                if (selectedLineKind != GameDrawer.LineKind.Dashed)
                {
                    tilingSlider.ResetToMin();
                }
                instance.GetComponentInChildren<FloatValueSliderController>().onValueChanged.RemoveListener(tilingAction);
            }
            previousColorKindBtn.onClick.RemoveAllListeners();
            nextColorKindBtn.onClick.RemoveAllListeners();
            primaryColorBtn.onClick.RemoveAllListeners();
            secondaryColorBtn.onClick.RemoveAllListeners();
            instance.GetComponentInChildren<ThicknessSliderController>().onValueChanged.RemoveAllListeners();
            instance.GetComponentInChildren<LayerSliderController>().onValueChanged.RemoveAllListeners();
            instance.GetComponentInChildren<Toggle>().onValueChanged.RemoveAllListeners();
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
            lineKindText.text = kind.ToString();
            selectedLineKind = kind;
        }

        /// <summary>
        /// Assign a line kind and a tiling to the line kind selection and tiling slider controller.
        /// </summary>
        /// <param name="kind">The line kind that should be assigned.</param>
        /// <param name="tiling"></param>
        public static void AssignLineKind(GameDrawer.LineKind kind, float tiling)
        {
            lineKindText.text = kind.ToString();
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
        }

        /// <summary>
        /// Returns the next line kind.
        /// Used for the line kind selection.
        /// </summary>
        /// <returns>The next line kind of the list</returns>
        public static GameDrawer.LineKind NextLineKind()
        {
            int index = GetIndexOfSelectedLineKind() + 1;
            if (index >= GameDrawer.GetLineKinds().Count)
            {
                index = 0;
            }
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
            }
            AssignLineKind(GameDrawer.GetLineKinds()[index]);
            return GameDrawer.GetLineKinds()[index];
        }

        /// <summary>
        /// Returns the previous line kind.
        /// Used for the line kind selection.
        /// </summary>
        /// <returns>The previous line kind of the list</returns>
        public static GameDrawer.LineKind PreviousLineKind()
        {
            int index = GetIndexOfSelectedLineKind() - 1;
            if (index < 0)
            {
                index = GameDrawer.GetLineKinds().Count - 1;
            }
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
            }
            AssignLineKind(GameDrawer.GetLineKinds()[index]);
            return GameDrawer.GetLineKinds()[index];
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
            colorKindText.text = kind.ToString();
            selectedColorKind = kind;
            if (kind == GameDrawer.ColorKind.Monochrome)
            {
                disableColorAreaFromLineMenu();
            } else
            {
                enableColorAreaFromLineMenu();
            }
        }

        /// <summary>
        /// Returns the next color kind.
        /// Used for the color kind selection.
        /// </summary>
        /// <returns>the selected color kind</returns>
        public static ColorKind NextColorKind()
        {
            int index = GetIndexOfSelectedColorKind() + 1;
            bool isDashed = selectedLineKind != GameDrawer.LineKind.Solid;
            if (index >= GameDrawer.GetColorKinds(isDashed).Count)
            {
                index = 0;
            }
            AssignColorKind(GameDrawer.GetColorKinds(isDashed)[index]);
            return GetColorKinds(isDashed)[index];
        }

        /// <summary>
        /// Returns the previous color kind.
        /// Used for the color kind selection.
        /// </summary>
        /// <returns>the selected color kind</returns>
        public static ColorKind PreviousColorKind()
        {
            int index = GetIndexOfSelectedColorKind() - 1;
            bool isDashed = selectedLineKind != GameDrawer.LineKind.Solid;
            if (index < 0)
            {
                index = GameDrawer.GetColorKinds(isDashed).Count - 1;
            }
            AssignColorKind(GameDrawer.GetColorKinds(isDashed)[index]);
            return GetColorKinds(isDashed)[index];
        }
        #endregion

        /// <summary>
        /// This method will be used as an action for the Handler of the color buttons (primary/secondary).
        /// This allows only one color to be active at a time.
        /// </summary>
        private static void MutuallyExclusiveColorButtons()
        {
            primaryColorBtn.interactable = !primaryColorBtn.IsInteractable();
            secondaryColorBtn.interactable = !secondaryColorBtn.IsInteractable();
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
            instance.transform.Find("LineKindSelection").gameObject.SetActive(false);
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
            instance.transform.Find("LineKindSelection").gameObject.SetActive(true);
        }

        /// <summary>
        /// Hides the tiling layer
        /// </summary>
        private static void disableTilingFromLineMenu()
        {
            tilingSlider.ResetToMin();
            instance.transform.Find("Tiling").gameObject.SetActive(false);
        }

        /// <summary>
        /// Enables the tiling layer.
        /// </summary>
        private static void enableTilingFromLineMenu()
        {
            instance.transform.Find("Tiling").gameObject.SetActive(true);
        }

        /// <summary>
        /// Hides the order in layer layer.
        /// </summary>
        private static void disableLayerFromLineMenu()
        {
            instance.transform.Find("Layer").gameObject.SetActive(false);
        }

        /// <summary>
        /// Enables the order in layer layer.
        /// </summary>
        private static void enableLayerFromLineMenu()
        {
            instance.transform.Find("Layer").gameObject.SetActive(true);
        }

        /// <summary>
        /// Hides the line thickness layer.
        /// </summary>
        private static void disableThicknessFromLineMenu()
        {
            instance.transform.Find("Thickness").gameObject.SetActive(false);
        }

        /// <summary>
        /// Enables the line thickness layer.
        /// </summary>
        private static void enableThicknessFromLineMenu()
        {
            instance.transform.Find("Thickness").gameObject.SetActive(true);
        }

        /// <summary>
        /// Hides the loop layer.
        /// </summary>
        private static void disableLoopFromLineMenu()
        {
            instance.transform.Find("Loop").gameObject.SetActive(false);
        }

        /// <summary>
        /// Enables the loop layer.
        /// </summary>
        private static void enableLoopFromLineMenu()
        {
            instance.transform.Find("Loop").gameObject.SetActive(true);
        }

        /// <summary>
        /// Hides the color area selector layer.
        /// </summary>
        private static void disableColorAreaFromLineMenu()
        {
            instance.transform.Find("ColorAreaSelector").gameObject.SetActive(false);
        }

        /// <summary>
        /// Enables the color area selector layer.
        /// </summary>
        private static void enableColorAreaFromLineMenu()
        {
            instance.transform.Find("ColorAreaSelector").gameObject.SetActive(true);
        }
        #endregion
    }
}
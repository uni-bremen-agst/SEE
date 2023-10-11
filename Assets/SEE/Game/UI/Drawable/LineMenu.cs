using Assets.SEE.Game.Drawable;
using SEE.Controls.Actions;
using SEE.Game;
using SEE.Utils;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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
        /// Holds the current selected line kind.
        /// </summary>
        private static GameDrawer.LineKind selectedKind;

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
        /// A list of actions where the line menu is already displayed at awake.
        /// </summary>
        public static List<ActionStateType> usedIn = new() { ActionStateTypes.DrawOn, ActionStateTypes.ColorPicker };

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
            AssignLineKind(ValueHolder.currentLineKind);
            previousLineKindBtn = instance.transform.Find("LineKindSelection").Find("PreviousBtn").GetComponent<Button>();
            nextLineKindBtn = instance.transform.Find("LineKindSelection").Find("NextBtn").GetComponent<Button>();
            tilingSlider = instance.transform.Find("Tiling").GetComponentInChildren<FloatValueSliderController>();
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

        /// <summary>
        /// Returns the previous line kind button
        /// </summary>
        /// <returns>The previous line kind button</returns>
        public static Button GetPreviousLineKindBtn()
        {
            return previousLineKindBtn;
        }

        /// <summary>
        /// Returns the next line kind button
        /// </summary>
        /// <returns>The next line kind button</returns>
        public static Button GetNextLineKindBtn()
        {
            return nextLineKindBtn;
        }

        /// <summary>
        /// Returns the tiling slider controller.
        /// </summary>
        /// <returns>The controller for the tiling slider.</returns>
        public static FloatValueSliderController GetTilingSliderController()
        {
            return tilingSlider;
        }

        /// <summary>
        /// Enables the line menu. And resets the additional listeners if the parameter for that is true.
        /// Also it can hide some menu layer.
        /// </summary>
        /// <param name="removeListeners">The bool, if the listeners should be reset.</param>
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
            if (selectedKind != GameDrawer.LineKind.Dashed)
            {
                disableTilingFromLineMenu();
            }
            instance.SetActive(true);
        }

        /// <summary>
        /// This method removes the listeners of the
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
                if (selectedKind != GameDrawer.LineKind.Dashed)
                {
                    tilingSlider.ResetToMin();
                }
                instance.GetComponentInChildren<FloatValueSliderController>().onValueChanged.RemoveListener(tilingAction);
            }
            instance.GetComponentInChildren<ThicknessSliderController>().onValueChanged.RemoveAllListeners();
            instance.GetComponentInChildren<LayerSliderController>().onValueChanged.RemoveAllListeners();
            instance.GetComponentInChildren<Toggle>().onValueChanged.RemoveAllListeners();
            if (colorAction != null)
            {
                instance.GetComponentInChildren<HSVPicker.ColorPicker>().onValueChanged.RemoveListener(colorAction);
            }
        }

        /// <summary>
        /// Returns the index of the current selected line kind.
        /// </summary>
        /// <returns>Index of selected line kind</returns>
        private static int GetIndexOfSelectedLineKind()
        {
            return GameDrawer.GetLineKinds().IndexOf(selectedKind);
        }

        /// <summary>
        /// Assigns a line kind to the line kind selection.
        /// </summary>
        /// <param name="kind">The line kind that should be assigned</param>
        public static void AssignLineKind(GameDrawer.LineKind kind)
        {
            lineKindText.text = kind.ToString();
            selectedKind = kind;
        }

        /// <summary>
        /// Assign a line kind and a tiling to the line kind selection and tiling slider controller.
        /// </summary>
        /// <param name="kind">The line kind that should be assigned.</param>
        /// <param name="tiling"></param>
        public static void AssignLineKind(GameDrawer.LineKind kind, float tiling)
        {
            lineKindText.text = kind.ToString();
            selectedKind = kind;
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
            AssignLineKind(GameDrawer.GetLineKinds()[index]);
            return GameDrawer.GetLineKinds()[index];
        }

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
            if (selectedKind != GameDrawer.LineKind.Dashed)
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
    }
}
using SEE.Game;
using SEE.GO;
using SEE.Utils;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.SEE.Game.Drawable
{
    public static class DrawableConfigurator
    {
        /// <summary>
        /// The current choosen color for drawing.
        /// </summary>
        public static Color currentColor { get; set; }

        /// <summary>
        /// The current choosen thickness for drawing.
        /// </summary>
        public static float currentThickness { get; set; }

        public static int orderInLayer { get; set; }

        public readonly static Vector3 distanceZ = new(0, 0, 0.002f);

        public readonly static Vector3 distanceX = new(0.002f, 0, 0);

        public readonly static Vector3 distanceZ2 = new(0, 0, 0.004f);

        private const string drawableMenuPrefab = "Prefabs/UI/DrawableLineMenu";
        public static GameObject drawableMenu;
        public static UnityAction<Color> colorAction;

        static DrawableConfigurator() {
            currentColor = UnityEngine.Random.ColorHSV();
            currentThickness = 0.01f;
            orderInLayer = 0;

            drawableMenu = PrefabInstantiator.InstantiatePrefab(drawableMenuPrefab,
                            GameObject.Find("UI Canvas").transform, false);
            drawableMenu.SetActive(false);
        }

        public static void disableDrawableMenu()
        {
            drawableMenu.SetActive(false);
        }

        public static void enableDrawableMenu()
        {
            drawableMenu.GetComponentInChildren<ThicknessSliderController>().onValueChanged.RemoveAllListeners();
            drawableMenu.GetComponentInChildren<LayerSliderController>().onValueChanged.RemoveAllListeners();
            if (colorAction != null)
            {
                drawableMenu.GetComponentInChildren<HSVPicker.ColorPicker>().onValueChanged.RemoveListener(colorAction);
            }
            drawableMenu.SetActive(true);
        }

        public static void disableLayerFromDrawableMenu()
        {
            drawableMenu.transform.Find("Layer").gameObject.SetActive(false);
        }

        public static void enableLayerFromDrawableMenu()
        {
            drawableMenu.transform.Find("Layer").gameObject.SetActive(true);
        }

        public static void disableThicknessFromDrawableMenu()
        {
            drawableMenu.transform.Find("Thickness").gameObject.SetActive(false);
        }

        public static void enableThicknessFromDrawableMenu()
        {
            drawableMenu.transform.Find("Thickness").gameObject.SetActive(true);
        }
    }
}
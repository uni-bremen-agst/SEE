using SEE.Game;
using SEE.GO;
using SEE.Utils;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.SEE.Game.Drawable
{
    public static class DrawableHelper
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

        public readonly static Vector3 distanceY = new(0, 0.002f, 0);

        private const string drawableMenuPrefab = "Prefabs/UI/DrawableLineMenu";
        public static GameObject drawableMenu;
        public static UnityAction<Color> colorAction;

        static DrawableHelper() {
            currentColor = UnityEngine.Random.ColorHSV();
            currentThickness = 0.01f;
            orderInLayer = 0;

            drawableMenu = PrefabInstantiator.InstantiatePrefab(drawableMenuPrefab,
                            GameObject.Find("UI Canvas").transform, false);
            drawableMenu.SetActive(false);
        }
        #region DrawableMenu
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
        #endregion

        #region Direction
        public enum Direction
        {
            Front,
            Back,
            Left,
            Right,
            Below,
            Above,
            None
        }

        public static Direction checkDirection(GameObject parent)
        {
            Direction direction = Direction.None;
            float x = Mathf.Round(parent.transform.rotation.eulerAngles.x);
            float y = Mathf.Round(parent.transform.rotation.eulerAngles.y);
            if (x == 0 && y == 180)
            {
                direction = Direction.Back;
            }
            if (x == 0 && y == 0)
            {
                direction = Direction.Front;
            }
            if (x == 0 && y == 270)
            {
                direction = Direction.Left;
            }
            if (x == 0 && y == 90)
            {
                direction = Direction.Right;
            }
            if (x == 90)
            {
                direction = Direction.Below;
            }
            if (x == 270)
            {
                direction = Direction.Above;
            }
            return direction;
        }
        #endregion
    }
}
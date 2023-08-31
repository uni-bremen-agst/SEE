using SEE.Game;
using SEE.GO;
using SEE.Utils;
using System.Collections;
using System.Collections.Generic;
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

        public static readonly string LinePrefix = "Line";
        public static readonly string LineHolderPrefix = "LineHolder";

        public readonly static Vector3 distanceToBoard = new(0, 0, 0.02f);


        public readonly static Vector3 distanceZ = new(0, 0, 0.002f);
        public readonly static Vector3 distanceX = new(0.002f, 0, 0);
        public readonly static Vector3 distanceY = new(0, 0.002f, 0);

        private const string drawableMenuPrefab = "Prefabs/UI/DrawableLineMenu";
        public static GameObject drawableMenu;
        public static UnityAction<Color> colorAction;

        static DrawableHelper() {
            currentColor = UnityEngine.Random.ColorHSV();
            currentThickness = 0.01f;
            orderInLayer = 1;

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
        public static Vector2[] castToVector2Array(Vector3[] vector3)
        {
            Vector2[] vector2 = new Vector2[vector3.Length];
            for (int i = 0; i < vector3.Length; i++)
            {
                vector2[i] = new Vector2(vector3[i].x, vector3[i].y);
            }
            return vector2;
        }

        public static List<int> GetNearestIndexes(Vector3[] positions, Vector3 hitPoint)
        {
            List<int> matchedIndexes = new();
            Vector2[] vector2 = castToVector2Array(positions);
            Vector2 hitPoint2D = new Vector2(hitPoint.x, hitPoint.y);
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < vector2.Length; i++)
            {
                if (Vector2.Distance(vector2[i], hitPoint2D) < nearestDistance)
                {
                    nearestDistance = Vector2.Distance(vector2[i], hitPoint2D);
                    matchedIndexes = new List<int>();
                    matchedIndexes.Add(i);
                }
                else if (Vector2.Distance(vector2[i], hitPoint2D) == nearestDistance)
                {
                    matchedIndexes.Add(i);
                }
            }
            return matchedIndexes;
        }

        public static int GetNearestIndex(Vector3[] positions, Vector3 hitPoint)
        {
            int index = -1;
            Vector2[] vector2 = castToVector2Array(positions);
            Vector2 hitPoint2D = new Vector2(hitPoint.x, hitPoint.y);
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < vector2.Length; i++)
            {
                if (Vector2.Distance(vector2[i], hitPoint2D) < nearestDistance)
                {
                    nearestDistance = Vector2.Distance(vector2[i], hitPoint2D);
                    index = i;
                }
            }
            return index;
        }
        #endregion
    }
}
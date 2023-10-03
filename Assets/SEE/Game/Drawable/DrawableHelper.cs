using SEE.Controls.Actions;
using SEE.Game;
using SEE.GO;
using SEE.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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

        public static GameDrawer.LineKind currentLineKind { get; set; }

        public static float currentTiling { get; set; }

        public static int orderInLayer { get; set; }

        public static readonly string LinePrefix = "Line";
        public static readonly string LineHolderPrefix = "LineHolder";
        public static readonly string DrawableHolderPrefix = "DrawableHolder";
        public static readonly string AttachedObject = "AttachedObjects";

        public readonly static Vector3 distanceToBoard = new(0, 0, 0.0001f);

        private const string drawableMenuPrefab = "Prefabs/UI/Drawable/DrawableLineMenu";
        public static GameObject drawableMenu;
        public static UnityAction<Color> colorAction;
        private static TMP_Text lineKindText;
        private static GameDrawer.LineKind selectedKind;
        private static FloatValueSliderController tilingSlider;
        public static UnityAction<float> tilingAction;
        private static Button nextBtn;
        private static Button previousBtn;

        static DrawableHelper()
        {
            currentColor = UnityEngine.Random.ColorHSV();
            currentThickness = 0.01f;
            currentLineKind = GameDrawer.LineKind.Solid;
            currentTiling = 1f;
            orderInLayer = 1;

            drawableMenu = PrefabInstantiator.InstantiatePrefab(drawableMenuPrefab,
                            GameObject.Find("UI Canvas").transform, false);
            lineKindText = drawableMenu.transform.Find("LineKindSelection").GetComponentsInChildren<TMP_Text>()[1];
            AssignLineKind(currentLineKind);
            previousBtn = drawableMenu.transform.Find("LineKindSelection").GetComponentsInChildren<Button>()[0];
            //previousBtn.onClick.AddListener(PreviousLineKind);
            nextBtn = drawableMenu.transform.Find("LineKindSelection").GetComponentsInChildren<Button>()[1];
            //nextBtn.onClick.AddListener(NextLineKind);
            tilingSlider = drawableMenu.transform.Find("Tiling").GetComponentInChildren<FloatValueSliderController>();
            drawableMenu.SetActive(false);
        }

        #region DrawableHolder
        private const string letters = "abcdefghijklmnopqrstuvwxyz";
        private const string numbers = "0123456789";
        private const string specialCharacters = "!?§$%&.,_-#+*@";
        private static readonly string characters = letters + letters.ToUpper() + numbers + specialCharacters;
        public static string GetRandomString(int size)
        {
            string randomString = "";
            for (int i = 0; i < size; i++)
            {
                randomString += characters[UnityEngine.Random.Range(0, characters.Length)];
            }
            return randomString;
        }

        public static void SetupDrawableHolder(GameObject drawable, out GameObject highestParent, out GameObject attachedObjects)
        {
            if (GameDrawableFinder.hasAParent(drawable))
            {
                GameObject parent = GameDrawableFinder.GetHighestParent(drawable);
                if (!parent.name.StartsWith(DrawableHelper.DrawableHolderPrefix))
                {
                    highestParent = new GameObject(DrawableHelper.DrawableHolderPrefix + "-" + parent.name);//drawable.GetInstanceID());
                    highestParent.transform.position = parent.transform.position;
                    highestParent.transform.rotation = parent.transform.rotation;

                    attachedObjects = new GameObject(DrawableHelper.AttachedObject);
                    attachedObjects.tag = Tags.AttachedObjects;
                    attachedObjects.transform.position = highestParent.transform.position;
                    attachedObjects.transform.rotation = highestParent.transform.rotation;
                    attachedObjects.transform.SetParent(highestParent.transform);
                    parent.transform.SetParent(highestParent.transform);
                }
                else
                {
                    highestParent = parent;
                    attachedObjects = GameDrawableFinder.FindChildWithTag(highestParent, Tags.AttachedObjects);
                }
            }
            else
            {
                highestParent = new GameObject(DrawableHelper.DrawableHolderPrefix + drawable.GetInstanceID());
                highestParent.transform.position = drawable.transform.position;
                highestParent.transform.rotation = drawable.transform.rotation;

                attachedObjects = new GameObject(DrawableHelper.AttachedObject);
                attachedObjects.tag = Tags.AttachedObjects;
                attachedObjects.transform.position = highestParent.transform.position;
                attachedObjects.transform.rotation = highestParent.transform.rotation;
                attachedObjects.transform.SetParent(highestParent.transform);

                drawable.transform.SetParent(highestParent.transform);
            }
        }
        #endregion

        #region DrawableMenu
        public enum MenuPoint
        {
            LineKind,
            Thickness,
            Layer,
            Loop,
            All
        }

        public static List<ActionStateType> usedIn = new() { ActionStateTypes.DrawOn, ActionStateTypes.ColorPicker };
        public static void disableDrawableMenu()
        {
            enableDrawableMenuPoints();
            drawableMenu.SetActive(false);
        }

        public static Button GetPreviousBtn()
        {
            return previousBtn;
        }

        public static Button GetNextBtn()
        {
            return nextBtn;
        }

        public static FloatValueSliderController GetTilingSlider()
        {
            return tilingSlider;
        }

        public static void enableDrawableMenu(bool removeListeners = true, MenuPoint[] withoutMenuPoints = null)
        {
            if (removeListeners)
            {
                RemoveListeners();
            }
            if (withoutMenuPoints != null)
            {
                foreach (MenuPoint menuPoint in withoutMenuPoints)
                {
                    switch (menuPoint)
                    {
                        case MenuPoint.LineKind:
                            disableLineKindFromDrawableMenu();
                            break;
                        case MenuPoint.Thickness:
                            disableThicknessFromDrawableMenu();
                            break;
                        case MenuPoint.Layer:
                            disableLayerFromDrawableMenu();
                            break;
                        case MenuPoint.Loop:
                            disableLoopFromDrawableMenu();
                            break;
                        case MenuPoint.All:
                            disableLineKindFromDrawableMenu();
                            disableThicknessFromDrawableMenu();
                            disableLayerFromDrawableMenu();
                            disableLoopFromDrawableMenu();
                            break;
                    }
                }
            }
            if (selectedKind != GameDrawer.LineKind.Dashed)
            {
                disableTilingFromDrawableMenu();
            }
            drawableMenu.SetActive(true);
        }

        private static void RemoveListeners()
        {
            enableDrawableMenuPoints();
            previousBtn.onClick.RemoveAllListeners();
            previousBtn.onClick.RemoveAllListeners();
            if (tilingAction != null)
            {
                if (selectedKind != GameDrawer.LineKind.Dashed)
                {
                    tilingSlider.ResetToMin();
                }
                drawableMenu.GetComponentInChildren<FloatValueSliderController>().onValueChanged.RemoveListener(tilingAction);
            }
            drawableMenu.GetComponentInChildren<ThicknessSliderController>().onValueChanged.RemoveAllListeners();
            drawableMenu.GetComponentInChildren<LayerSliderController>().onValueChanged.RemoveAllListeners();
            drawableMenu.GetComponentInChildren<Toggle>().onValueChanged.RemoveAllListeners();
            if (colorAction != null)
            {
                drawableMenu.GetComponentInChildren<HSVPicker.ColorPicker>().onValueChanged.RemoveListener(colorAction);
            }
        }

        public static int GetIndexOfSelectedLineKind()
        {
            return GameDrawer.GetLineKinds().IndexOf(selectedKind);
        }

        public static void AssignLineKind(GameDrawer.LineKind kind)
        {
            lineKindText.text = kind.ToString();
            selectedKind = kind;
        }
        /*
        public static void NextLineKind()
        {
            int index = GetIndexOfSelectedLineKind() + 1;
            if (index >= GameDrawer.GetLineKinds().Count)
            {
                index = 0;
            }
            if (GameDrawer.GetLineKinds()[index] == GameDrawer.LineKind.Dashed)
            {
                enableTilingFromDrawableMenu();
            } else
            {
                disableTilingFromDrawableMenu();
            }
            AssignLineKind(GameDrawer.GetLineKinds()[index]);
        }
        */
        public static GameDrawer.LineKind NextLineKind()
        {
            int index = GetIndexOfSelectedLineKind() + 1;
            if (index >= GameDrawer.GetLineKinds().Count)
            {
                index = 0;
            }
            if (GameDrawer.GetLineKinds()[index] == GameDrawer.LineKind.Dashed)
            {
                enableTilingFromDrawableMenu();
            }
            else
            {
                disableTilingFromDrawableMenu();
            }
            AssignLineKind(GameDrawer.GetLineKinds()[index]);
            return GameDrawer.GetLineKinds()[index];
        }
        /*
        public static void PreviousLineKind()
        {
            int index = GetIndexOfSelectedLineKind() - 1;
            if (index < 0)
            {
                index = GameDrawer.GetLineKinds().Count - 1;
            }
            if (GameDrawer.GetLineKinds()[index] == GameDrawer.LineKind.Dashed)
            {
                enableTilingFromDrawableMenu();
            }
            else
            {
                disableTilingFromDrawableMenu();
            }
            AssignLineKind(GameDrawer.GetLineKinds()[index]);
            previousAction.Invoke(GameDrawer.GetLineKinds()[index].ToString());
        }
        */
        public static GameDrawer.LineKind PreviousLineKind()
        {
            int index = GetIndexOfSelectedLineKind() - 1;
            if (index < 0)
            {
                index = GameDrawer.GetLineKinds().Count - 1;
            }
            if (GameDrawer.GetLineKinds()[index] == GameDrawer.LineKind.Dashed)
            {
                enableTilingFromDrawableMenu();
            }
            else
            {
                disableTilingFromDrawableMenu();
            }
            AssignLineKind(GameDrawer.GetLineKinds()[index]);
            return GameDrawer.GetLineKinds()[index];
        }

        private static void enableDrawableMenuPoints()
        {
            enableLineKindFromDrawableMenu();
            enableTilingFromDrawableMenu();
            enableLoopFromDrawableMenu();
            enableLayerFromDrawableMenu();
            enableThicknessFromDrawableMenu();
        }

        private static void disableLineKindFromDrawableMenu()
        {
            drawableMenu.transform.Find("LineKindSelection").gameObject.SetActive(false);
        }

        private static void enableLineKindFromDrawableMenu()
        {
            if (selectedKind != GameDrawer.LineKind.Dashed)
            {
                tilingSlider.ResetToMin();
            }
            drawableMenu.transform.Find("LineKindSelection").gameObject.SetActive(true);
        }
        private static void disableTilingFromDrawableMenu()
        {
            tilingSlider.ResetToMin();
            drawableMenu.transform.Find("Tiling").gameObject.SetActive(false);
        }

        private static void enableTilingFromDrawableMenu()
        {
            drawableMenu.transform.Find("Tiling").gameObject.SetActive(true);
        }
        private static void disableLayerFromDrawableMenu()
        {
            drawableMenu.transform.Find("Layer").gameObject.SetActive(false);
        }

        private static void enableLayerFromDrawableMenu()
        {
            drawableMenu.transform.Find("Layer").gameObject.SetActive(true);
        }

        private static void disableThicknessFromDrawableMenu()
        {
            drawableMenu.transform.Find("Thickness").gameObject.SetActive(false);
        }

        private static void enableThicknessFromDrawableMenu()
        {
            drawableMenu.transform.Find("Thickness").gameObject.SetActive(true);
        }

        private static void disableLoopFromDrawableMenu()
        {
            drawableMenu.transform.Find("Loop").gameObject.SetActive(false);
        }

        private static void enableLoopFromDrawableMenu()
        {
            drawableMenu.transform.Find("Loop").gameObject.SetActive(true);
        }
        #endregion

        #region Nearest Point
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
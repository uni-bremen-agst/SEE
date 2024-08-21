using Michsky.UI.ModernUIPack;
using SEE.Controls.Actions;
using SEE.Controls.Actions.Drawable;
using SEE.Game;
using SEE.Game.Drawable;
using SEE.GO.Menu;
using SEE.Utils;
using UnityEngine;

namespace SEE.UI.Drawable
{
    /// <summary>
    /// Creates the drawable action bar if the user opens a drawable action.
    /// </summary>
    public class DrawableActionBar : MonoBehaviour
    {
        /// <summary>
        /// Prefab of the drawable action bar with toggler.
        /// </summary>
        private const string actionBarWithTogglerPrefab = "Prefabs/UI/Drawable/DrawableActionBarWithToggler";

        /// <summary>
        /// The instance of the action bar with toggle button.
        /// </summary>
        private GameObject actionBar;

        /// <summary>
        /// The instance of the action bar.
        /// </summary>
        private GameObject barInstance;

        /// <summary>
        /// The instance of the toggler.
        /// </summary>
        private GameObject togglerInstance;

        /// <summary>
        /// Whether the user enables the bar with the toggler.
        /// </summary>
        private bool toggles = false;

        /// <summary>
        /// The original position of the toggler.
        /// </summary>
        private Vector3 originTogglePos;

        /// <summary>
        /// Instantiates the action bar.
        /// </summary>
        void Start()
        {
            actionBar = PrefabInstantiator.InstantiatePrefab(actionBarWithTogglerPrefab,
                                                             UICanvas.Canvas.transform, false);
            barInstance = actionBar.transform.Find("DrawableActionBar").gameObject;
            togglerInstance = actionBar.transform.Find("DrawableActionBarToggler").gameObject;
            Init();
            barInstance.SetActive(false);
            InitToggler();
            originTogglePos = togglerInstance.transform.position;
        }

        /// <summary>
        /// Displays the action bar if a drawable action will be used.
        /// Otherwise it will be disabled.
        /// </summary>
        void Update()
        {
            if (GlobalActionHistory.Current() != null
                && GlobalActionHistory.Current().Parent == ActionStateTypes.Drawable)
            {
                barInstance.SetActive(true);
                togglerInstance.SetActive(false);
            }
            else
            {
                if (!toggles)
                {
                    barInstance.SetActive(false);
                }
                togglerInstance.SetActive(true);
            }
            if (toggles)
            {
                barInstance.SetActive(true);
            }
        }

        /// <summary>
        /// Destroys the action bar.
        /// </summary>
        private void OnDestroy()
        {
            Destroyer.Destroy(barInstance);
        }

        /// <summary>
        /// Configurates the toggler for the action bar.
        /// </summary>
        private void InitToggler()
        {
            GameObject toggle = GameFinder.FindChild(togglerInstance, "Toggle");
            ButtonManagerBasic toggleBMB = toggle.GetComponent<ButtonManagerBasic>();
            toggleBMB.clickEvent.AddListener(() =>
            {
                if (!toggles)
                {
                    toggles = true;
                    toggle.transform.eulerAngles = new Vector3(0, 0, 180);
                    RectTransform barRect = (RectTransform)barInstance.transform;
                    RectTransform togglerRect = (RectTransform)togglerInstance.transform;
                    togglerRect.localPosition = new Vector3(togglerRect.localPosition.x,
                        togglerRect.localPosition.y + barRect.rect.height,
                        togglerRect.localPosition.z);
                }
                else
                {
                    toggles = false;
                    toggle.transform.eulerAngles = new Vector3(0, 0, 0);
                    togglerInstance.transform.position = originTogglePos;
                }

            });
        }

        /// <summary>
        /// Configurates the action bar.
        /// </summary>
        private void Init()
        {
            LocalPlayer.TryGetPlayerMenu(out PlayerMenu menu);
            GameObject drawFreehand = GameFinder.FindChild(barInstance, "DrawFreehand");
            drawFreehand.AddComponent<ButtonHoverTooltip>().SetMessage("Draw Freehand");
            drawFreehand.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.DrawFreehand);
                menu.UpdateActiveEntry(ActionStateTypes.Drawable.Name);
            });

            GameObject drawShape = GameFinder.FindChild(barInstance, "DrawShape");
            drawShape.AddComponent<ButtonHoverTooltip>().SetMessage("Draw Shape");
            drawShape.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.DrawShapes);
                menu.UpdateActiveEntry(ActionStateTypes.Drawable.Name);
            });

            GameObject writeText = GameFinder.FindChild(barInstance, "WriteText");
            writeText.AddComponent<ButtonHoverTooltip>().SetMessage("Write Text");
            writeText.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.WriteText);
                menu.UpdateActiveEntry(ActionStateTypes.Drawable.Name);
            });

            GameObject addImage = GameFinder.FindChild(barInstance, "AddImage");
            addImage.AddComponent<ButtonHoverTooltip>().SetMessage("Add An Image");
            addImage.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.AddImage);
                menu.UpdateActiveEntry(ActionStateTypes.Drawable.Name);
            });

            GameObject mindMap = GameFinder.FindChild(barInstance, "MindMap");
            mindMap.AddComponent<ButtonHoverTooltip>().SetMessage("Create Mind Map");
            mindMap.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.MindMap);
                menu.UpdateActiveEntry(ActionStateTypes.Drawable.Name);
            });

            GameObject colorPicker = GameFinder.FindChild(barInstance, "ColorPicker");
            colorPicker.AddComponent<ButtonHoverTooltip>().SetMessage("Color Picker");
            colorPicker.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.ColorPicker);
                menu.UpdateActiveEntry(ActionStateTypes.Drawable.Name);
            });

            GameObject edit = GameFinder.FindChild(barInstance, "Edit");
            edit.AddComponent<ButtonHoverTooltip>().SetMessage("Edit");
            edit.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.Edit);
                menu.UpdateActiveEntry(ActionStateTypes.Drawable.Name);
            });

            GameObject moveRotator = GameFinder.FindChild(barInstance, "MoveRotator");
            moveRotator.AddComponent<ButtonHoverTooltip>().SetMessage("Move Or Rotate");
            moveRotator.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.MoveRotator);
                menu.UpdateActiveEntry(ActionStateTypes.Drawable.Name);
            });

            GameObject scale = GameFinder.FindChild(barInstance, "Scale");
            scale.AddComponent<ButtonHoverTooltip>().SetMessage("Scale");
            scale.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.Scale);
                menu.UpdateActiveEntry(ActionStateTypes.Drawable.Name);
            });

            GameObject layerChanger = GameFinder.FindChild(barInstance, "LayerChanger");
            layerChanger.AddComponent<ButtonHoverTooltip>().SetMessage("Change The Sorting Layer.");
            layerChanger.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.LayerChanger);
                menu.UpdateActiveEntry(ActionStateTypes.Drawable.Name);
            });

            GameObject cutCopyPaste = GameFinder.FindChild(barInstance, "CutCopyPaste");
            cutCopyPaste.AddComponent<ButtonHoverTooltip>().SetMessage("Cut, Copy, Paste");
            cutCopyPaste.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.CutCopyPaste);
                menu.UpdateActiveEntry(ActionStateTypes.Drawable.Name);
            });

            GameObject movePoint = GameFinder.FindChild(barInstance, "MovePoint");
            movePoint.AddComponent<ButtonHoverTooltip>().SetMessage("Move a Point");
            movePoint.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.MovePoint);
                menu.UpdateActiveEntry(ActionStateTypes.Drawable.Name);
            });

            GameObject lineSplit = GameFinder.FindChild(barInstance, "LineSplit");
            lineSplit.AddComponent<ButtonHoverTooltip>().SetMessage("Line Split");
            lineSplit.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.LineSplit);
                menu.UpdateActiveEntry(ActionStateTypes.Drawable.Name);
            });

            GameObject save = GameFinder.FindChild(barInstance, "Save");
            save.AddComponent<ButtonHoverTooltip>().SetMessage("Save");
            save.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.Save);
                menu.UpdateActiveEntry(ActionStateTypes.Drawable.Name);
            });

            GameObject load = GameFinder.FindChild(barInstance, "Load");
            load.AddComponent<ButtonHoverTooltip>().SetMessage("Load");
            load.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.Load);
                menu.UpdateActiveEntry(ActionStateTypes.Drawable.Name);
            });

            GameObject linePointErase = GameFinder.FindChild(barInstance, "LinePointErase");
            linePointErase.AddComponent<ButtonHoverTooltip>().SetMessage("Line Point Erase");
            linePointErase.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.LinePointErase);
                menu.UpdateActiveEntry(ActionStateTypes.Drawable.Name);
            });

            GameObject lineConnectionErase = GameFinder.FindChild(barInstance, "LineConnectionErase");
            lineConnectionErase.AddComponent<ButtonHoverTooltip>().SetMessage("Line Connection Erase");
            lineConnectionErase.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.LineConnectionErase);
                menu.UpdateActiveEntry(ActionStateTypes.Drawable.Name);
            });

            GameObject erase = GameFinder.FindChild(barInstance, "Erase");
            erase.AddComponent<ButtonHoverTooltip>().SetMessage("Erase");
            erase.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.Erase);
                menu.UpdateActiveEntry(ActionStateTypes.Drawable.Name);
            });

            GameObject cleaner = GameFinder.FindChild(barInstance, "Clear");
            cleaner.AddComponent<ButtonHoverTooltip>().SetMessage("Clear");
            cleaner.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.Clear);
                menu.UpdateActiveEntry(ActionStateTypes.Drawable.Name);
            });

            GameObject stickyNote = GameFinder.FindChild(barInstance, "StickyNote");
            stickyNote.AddComponent<ButtonHoverTooltip>().SetMessage("Sticky Note");
            stickyNote.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.StickyNote);
                menu.UpdateActiveEntry(ActionStateTypes.Drawable.Name);
            });

            GameObject manager = GameFinder.FindChild(barInstance, "Manager");
            manager.AddComponent<ButtonHoverTooltip>().SetMessage("Drawable Surface Manager");
            manager.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                LocalPlayer.Instance.GetComponentInChildren<ShowDrawableManager>().Toggle();
            }
            );
        }
    }
}
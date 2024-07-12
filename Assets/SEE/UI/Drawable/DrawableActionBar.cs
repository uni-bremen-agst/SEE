using Michsky.UI.ModernUIPack;
using SEE.Controls.Actions;
using SEE.Game;
using SEE.GO.Menu;
using SEE.Utils;
using SEE.Game.Drawable;
using UnityEngine;

namespace SEE.UI.Drawable
{
    /// <summary>
    /// Creates the drawable action bar if the user opens a drawable action. 
    /// </summary>
    public class DrawableActionBar : MonoBehaviour
    {
        /// <summary>
        /// The action bar.
        /// </summary>
        private const string actionBarPrefab = "Prefabs/UI/Drawable/DrawableActionBar";

        /// <summary>
        /// The instance of the action bar.
        /// </summary>
        private GameObject instance;

        /// <summary>
        /// Instantiates the action bar.
        /// </summary>
        void Start()
        {
            instance = PrefabInstantiator.InstantiatePrefab(actionBarPrefab,
                    GameObject.Find("UI Canvas").transform, false);
            Init();
            instance.SetActive(false);
        }

        /// <summary>
        /// Displays the action bar if a drawable action will be used.
        /// Otherwise it will be disabled.
        /// </summary>
        void Update()
        {
            if (GlobalActionHistory.Current().Parent == ActionStateTypes.Drawable)
            {
                instance.SetActive(true);
            }
            else
            {
                instance.SetActive(false);
            }
        }

        /// <summary>
        /// Destroys the action bar.
        /// </summary>
        private void OnDestroy()
        {
            Destroyer.Destroy(instance);
        }

        /// <summary>
        /// Configurates the action bar.
        /// </summary>
        private void Init()
        {
            LocalPlayer.TryGetPlayerMenu(out PlayerMenu menu);
            GameObject drawFreehand = GameFinder.FindChild(instance, "DrawFreehand");
            drawFreehand.AddComponent<ButtonHoverTooltip>().SetMessage("Draw Freehand");
            drawFreehand.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.DrawFreehand);
                menu.UpdateActiveEntry();
            });

            GameObject drawShape = GameFinder.FindChild(instance, "DrawShape");
            drawShape.AddComponent<ButtonHoverTooltip>().SetMessage("Draw Shape");
            drawShape.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.DrawShapes);
                menu.UpdateActiveEntry();
            });

            GameObject writeText = GameFinder.FindChild(instance, "WriteText");
            writeText.AddComponent<ButtonHoverTooltip>().SetMessage("Write Text");
            writeText.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.WriteText);
                menu.UpdateActiveEntry();
            });

            GameObject addImage = GameFinder.FindChild(instance, "AddImage");
            addImage.AddComponent<ButtonHoverTooltip>().SetMessage("Add An Image");
            addImage.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.AddImage);
                menu.UpdateActiveEntry();
            });

            GameObject mindMap = GameFinder.FindChild(instance, "MindMap");
            mindMap.AddComponent<ButtonHoverTooltip>().SetMessage("Create Mind Map");
            mindMap.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.MindMap);
                menu.UpdateActiveEntry();
            });

            GameObject colorPicker = GameFinder.FindChild(instance, "ColorPicker");
            colorPicker.AddComponent<ButtonHoverTooltip>().SetMessage("Color Picker");
            colorPicker.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.ColorPicker);
                menu.UpdateActiveEntry();
            });

            GameObject edit = GameFinder.FindChild(instance, "Edit");
            edit.AddComponent<ButtonHoverTooltip>().SetMessage("Edit");
            edit.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.Edit);
                menu.UpdateActiveEntry();
            });

            GameObject moveRotator = GameFinder.FindChild(instance, "MoveRotator");
            moveRotator.AddComponent<ButtonHoverTooltip>().SetMessage("Move Or Rotate");
            moveRotator.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.MoveRotator);
                menu.UpdateActiveEntry();
            });

            GameObject scale = GameFinder.FindChild(instance, "Scale");
            scale.AddComponent<ButtonHoverTooltip>().SetMessage("Scale");
            scale.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.Scale);
                menu.UpdateActiveEntry();
            });

            GameObject layerChanger = GameFinder.FindChild(instance, "LayerChanger");
            layerChanger.AddComponent<ButtonHoverTooltip>().SetMessage("Change The Sorting Layer.");
            layerChanger.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.MindMap);
                menu.UpdateActiveEntry();
            });

            GameObject cutCopyPaste = GameFinder.FindChild(instance, "CutCopyPaste");
            cutCopyPaste.AddComponent<ButtonHoverTooltip>().SetMessage("Cut, Copy, Paste");
            cutCopyPaste.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.CutCopyPaste);
                menu.UpdateActiveEntry();
            });

            GameObject movePoint = GameFinder.FindChild(instance, "MovePoint");
            movePoint.AddComponent<ButtonHoverTooltip>().SetMessage("Move a Point");
            movePoint.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.MovePoint);
                menu.UpdateActiveEntry();
            });

            GameObject lineSplit = GameFinder.FindChild(instance, "LineSplit");
            lineSplit.AddComponent<ButtonHoverTooltip>().SetMessage("Line Split");
            lineSplit.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.LineSplit);
                menu.UpdateActiveEntry();
            });

            GameObject save = GameFinder.FindChild(instance, "Save");
            save.AddComponent<ButtonHoverTooltip>().SetMessage("Save");
            save.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.Save);
                menu.UpdateActiveEntry();
            });

            GameObject load = GameFinder.FindChild(instance, "Load");
            load.AddComponent<ButtonHoverTooltip>().SetMessage("Load");
            load.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.Load);
                menu.UpdateActiveEntry();
            });

            GameObject linePointErase = GameFinder.FindChild(instance, "LinePointErase");
            linePointErase.AddComponent<ButtonHoverTooltip>().SetMessage("Line Point Erase");
            linePointErase.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.LinePointErase);
                menu.UpdateActiveEntry();
            });

            GameObject lineConnectionErase = GameFinder.FindChild(instance, "LineConnectionErase");
            lineConnectionErase.AddComponent<ButtonHoverTooltip>().SetMessage("Line Connection Erase");
            lineConnectionErase.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.LineConnectionErase);
                menu.UpdateActiveEntry();
            });

            GameObject erase = GameFinder.FindChild(instance, "Erase");
            erase.AddComponent<ButtonHoverTooltip>().SetMessage("Erase");
            erase.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.Erase);
                menu.UpdateActiveEntry();
            });

            GameObject cleaner = GameFinder.FindChild(instance, "Clear");
            cleaner.AddComponent<ButtonHoverTooltip>().SetMessage("Clear");
            cleaner.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.Clear);
                menu.UpdateActiveEntry();
            });

            GameObject stickyNote = GameFinder.FindChild(instance, "StickyNote");
            stickyNote.AddComponent<ButtonHoverTooltip>().SetMessage("Sticky Note");
            stickyNote.GetComponent<ButtonManagerBasic>().clickEvent.AddListener(() =>
            {
                GlobalActionHistory.Execute(ActionStateTypes.StickyNote);
                menu.UpdateActiveEntry();
            });
        }
    }
}
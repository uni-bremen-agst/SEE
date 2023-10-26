using SEE.Controls;
using SEE.Controls.Actions.Drawable;
using SEE.GO.Menu;
using SEE.Utils;
using SimpleFileBrowser;
using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using static SEE.Controls.Actions.Drawable.LoadAction;
using static SEE.Controls.Actions.Drawable.SaveAction;

namespace Assets.SEE.Game.Drawable
{
    public class DrawableFileBrowser : MonoBehaviour
    {
        private string initPath;

        private void Awake()
        {
            DrawableConfigManager.EnsureDrawableDirectoryExists(DrawableConfigManager.configurationPath);
            initPath = DrawableConfigManager.configurationPath;
        }

        public void LoadDrawableConfiguration(LoadState loadState)
        {
            SEEInput.KeyboardShortcutsEnabled = false;
            StartCoroutine(ShowLoadDialogCoroutine(loadState));
        }

        private IEnumerator ShowLoadDialogCoroutine(LoadState loadState)
        {
            string title = "";
            switch (loadState) {
                case LoadState.Specific:
                    DrawableConfigManager.EnsureDrawableDirectoryExists(DrawableConfigManager.configurationPath);
                    initPath = DrawableConfigManager.configurationPath;
                    title = "Load on specific Drawable";
                    break;
                case LoadState.Regular:
                    DrawableConfigManager.EnsureDrawableDirectoryExists(DrawableConfigManager.configurationPath);
                    initPath = DrawableConfigManager.configurationPath;
                    title = "Load Drawable(s)";
                    break;
            }

            FileBrowser.SetFilters(false, Filenames.ConfigExtension);
            yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, initPath, null, title, "Load");

            SEEInput.KeyboardShortcutsEnabled = true;
            if (FileBrowser.Success)
            {
                LoadAction.filePath = FileBrowser.Result[0];
            }
            GameObject UICanvas = GameObject.Find("UI Canvas");
            UICanvas.SetActive(false);
            UICanvas.SetActive(true);
            Destroy(this);
        }

        public void SaveDrawableConfiguration(SaveState saveState)
        {
            SEEInput.KeyboardShortcutsEnabled = false;
            StartCoroutine(ShowSaveDialogCoroutine(saveState));
        }

        private IEnumerator ShowSaveDialogCoroutine(SaveState saveState)
        {
            string title = "";
            switch (saveState)
            {
                case SaveState.One:
                    DrawableConfigManager.EnsureDrawableDirectoryExists(DrawableConfigManager.singleConfPath);
                    initPath = DrawableConfigManager.singleConfPath;
                    title = "Save Drawable";
                    break;
                case SaveState.More:
                    DrawableConfigManager.EnsureDrawableDirectoryExists(DrawableConfigManager.multipleConfPath);
                    initPath = DrawableConfigManager.multipleConfPath;
                    title = "Save specific Drawables";
                    break;
                case SaveState.All:
                    DrawableConfigManager.EnsureDrawableDirectoryExists(DrawableConfigManager.multipleConfPath);
                    initPath = DrawableConfigManager.multipleConfPath;
                    title = "Save all Drawables";
                    break;
            }
            SaveAction.filePath = "";
            FileBrowser.SetFilters(false, Filenames.ConfigExtension);
            yield return FileBrowser.WaitForSaveDialog(FileBrowser.PickMode.Files, false, initPath, null, title, "Save");
            SEEInput.KeyboardShortcutsEnabled = true;

            if (FileBrowser.Success)
            {
                SaveAction.filePath = FileBrowser.Result[0];
            }
            GameObject UICanvas = GameObject.Find("UI Canvas");
            UICanvas.SetActive(false);
            UICanvas.SetActive(true);
            Destroy(this);
        }

        public void LoadImage()
        {
            SEEInput.KeyboardShortcutsEnabled = false;
            StartCoroutine(ShowLoadImageDialogCoroutine());
        }

        private IEnumerator ShowLoadImageDialogCoroutine()
        {
            DrawableConfigManager.EnsureDrawableDirectoryExists(ValueHolder.imagePath);
            initPath = ValueHolder.imagePath;
            string title = "Load an image";
            
            AddImageAction.filePath = "";
            FileBrowser.SetFilters(true, Filenames.PNGExtension, Filenames.JPGExtension);
            yield return FileBrowser.WaitForSaveDialog(FileBrowser.PickMode.Files, false, initPath, null, title, "Load");
            SEEInput.KeyboardShortcutsEnabled = true;

            if (FileBrowser.Success)
            {
                AddImageAction.filePath = FileBrowser.Result[0];
            }
            GameObject UICanvas = GameObject.Find("UI Canvas");
            UICanvas.SetActive(false);
            UICanvas.SetActive(true);
            Destroy(this);
        }

        public bool IsOpen()
        {
            return FileBrowser.IsOpen;
        } 
    }
}
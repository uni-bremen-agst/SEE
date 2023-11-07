using SEE.Controls;
using SEE.Utils;
using SimpleFileBrowser;
using System.Collections;
using UnityEngine;
using static SEE.Controls.Actions.Drawable.LoadAction;
using static SEE.Controls.Actions.Drawable.SaveAction;

namespace Assets.SEE.Game.Drawable
{
    /// <summary>
    /// The file browser which will used in different drawable actions.
    /// </summary>
    public class DrawableFileBrowser : MonoBehaviour
    {
        /// <summary>
        /// Whether this class has a file path in store that wasn't yet fetched.
        /// </summary>
        private static bool gotFilePath;

        /// <summary>
        /// If <see cref="gotFilePath"/> is true, this contains the file path which the player selected.
        /// </summary>
        private static string path;

        /// <summary>
        /// The init file path for the file browser
        /// </summary>
        private string initPath;

        /// <summary>
        /// Ensure that the configuration path exists and set the init path to the configuration path.
        /// </summary>
        private void Awake()
        {
            DrawableConfigManager.EnsureDrawableDirectoryExists(DrawableConfigManager.configurationPath);
            initPath = DrawableConfigManager.configurationPath;
        }

        /// <summary>
        /// Method to load a drawable configuration.
        /// </summary>
        /// <param name="loadState">The chosen load state (regular/specific)</param>
        public void LoadDrawableConfiguration(LoadState loadState)
        {
            SEEInput.KeyboardShortcutsEnabled = false;
            StartCoroutine(ShowLoadDialogCoroutine(loadState));
        }

        /// <summary>
        /// Coroutine that enables the file browser to chose a file.
        /// </summary>
        /// <param name="loadState">The chosen load state (regular/specific)</param>
        /// <returns>nothing</returns>
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
                SetPath(FileBrowser.Result[0]);
            }
            GameObject UICanvas = GameObject.Find("UI Canvas");
            UICanvas.SetActive(false);
            UICanvas.SetActive(true);
        }

        /// <summary>
        /// Method to save a drawable configuration
        /// </summary>
        /// <param name="saveState">The chosen save state (one/more/all)</param>
        public void SaveDrawableConfiguration(SaveState saveState)
        {
            SEEInput.KeyboardShortcutsEnabled = false;
            StartCoroutine(ShowSaveDialogCoroutine(saveState));
        }

        /// <summary>
        /// Coroutine that enables the file browser to chose a file.
        /// </summary>
        /// <param name="saveState">The chosen save state (one/more/all)</param>
        /// <returns>nothing</returns>
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
            FileBrowser.SetFilters(false, Filenames.ConfigExtension);
            yield return FileBrowser.WaitForSaveDialog(FileBrowser.PickMode.Files, false, initPath, null, title, "Save");
            SEEInput.KeyboardShortcutsEnabled = true;

            if (FileBrowser.Success)
            {
                SetPath(FileBrowser.Result[0]);
            }
            GameObject UICanvas = GameObject.Find("UI Canvas");
            UICanvas.SetActive(false);
            UICanvas.SetActive(true);
        }

        /// <summary>
        /// Method to load an image
        /// </summary>
        public void LoadImage()
        {
            SEEInput.KeyboardShortcutsEnabled = false;
            StartCoroutine(ShowLoadImageDialogCoroutine());
        }

        /// <summary>
        /// Coroutine that enables the file browser to chose a file.
        /// </summary>
        /// <returns>nothing</returns>
        private IEnumerator ShowLoadImageDialogCoroutine()
        {
            DrawableConfigManager.EnsureDrawableDirectoryExists(ValueHolder.imagePath);
            initPath = ValueHolder.imagePath;
            string title = "Load an image";
            
            FileBrowser.SetFilters(true, Filenames.PNGExtension, Filenames.JPGExtension);
            yield return FileBrowser.WaitForSaveDialog(FileBrowser.PickMode.Files, false, initPath, null, title, "Load");
            SEEInput.KeyboardShortcutsEnabled = true;

            if (FileBrowser.Success)
            {
                SetPath(FileBrowser.Result[0]);
            }
            GameObject UICanvas = GameObject.Find("UI Canvas");
            UICanvas.SetActive(false);
            UICanvas.SetActive(true);
        }

        /// <summary>
        /// Gets the value if the file browser is open.
        /// </summary>
        /// <returns>The is open state.</returns>
        public bool IsOpen()
        {
            return FileBrowser.IsOpen;
        }

        /// <summary>
        /// Sets the chosen path.
        /// </summary>
        /// <param name="newPath">The chosen path</param>
        private void SetPath(string newPath)
        {
            path = newPath;
            gotFilePath = true;
        }

        /// <summary>
        /// If <see cref="gotFilePath"/> is true, the <paramref name="filePath"/> will be the file path chosen by the
        /// player. Otherwise it will be some dummy value.
        /// </summary>
        /// <param name="filePath">The file path the player confirmed, if that doesn't exist, some dummy value</param>
        /// <returns><see cref="gotFilePath"/></returns>
        public bool TryGetFilePath(out string filePath)
        {
            if (gotFilePath)
            {
                filePath = path;
                gotFilePath = false;
                Destroy(this);
                return true;
            }

            filePath = "";
            return false;
        }
    }
}
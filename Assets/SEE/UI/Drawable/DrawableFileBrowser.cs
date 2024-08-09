using Cysharp.Threading.Tasks;
using SEE.Controls;
using SEE.Game.Drawable;
using SEE.Utils;
using SimpleFileBrowser;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using static SEE.Controls.Actions.Drawable.LoadAction;
using static SEE.Controls.Actions.Drawable.SaveAction;

namespace SEE.UI.Drawable
{
    /// <summary>
    /// The file browser which is used in different drawable actions.
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
        /// Ensures that the configuration path exists and sets the init path to the configuration path.
        /// </summary>
        private void Awake()
        {
            DrawableConfigManager.EnsureDrawableDirectoryExists(DrawableConfigManager.ConfigurationPath);
            initPath = DrawableConfigManager.ConfigurationPath;
        }

        /// <summary>
        /// Loads a drawable configuration. Asks the user for a filename
        /// using the <see cref="FileBrowser"/>.
        /// </summary>
        /// <param name="loadState">The chosen load state (regular/specific)</param>
        public async Task LoadDrawableConfigurationAsync(LoadState loadState)
        {
            string title = "";
            switch (loadState)
            {
                /// Block for loading on a specific drawable.
                case LoadState.Specific:
                    DrawableConfigManager.EnsureDrawableDirectoryExists(DrawableConfigManager.ConfigurationPath);
                    initPath = DrawableConfigManager.ConfigurationPath;
                    title = "Load on specific Drawable";
                    break;
                /// Block for loading on the regular drawable.
                case LoadState.Regular:
                    DrawableConfigManager.EnsureDrawableDirectoryExists(DrawableConfigManager.ConfigurationPath);
                    initPath = DrawableConfigManager.ConfigurationPath;
                    title = "Load Drawable(s)";
                    break;
            }

            string result = await GetPathAsync(title, "Load", initPath, Filenames.DrawableConfigExtension);

            if (!string.IsNullOrWhiteSpace(result))
            {
                SetPath(result);
            }

            /// Refreshes the UI canvas to prevent display issues.
            UICanvas.Refresh();
        }

        /// <summary>
        /// Opens a <see cref="FileBrowser"/> and ask the user to pick a single file.
        /// The picked file is returned when the user closes the <see cref="FileBrowser"/>.
        /// If no file has been picked, the empty string is returned.
        /// </summary>
        /// <param name="title">title of the dialog</param>
        /// ´<param name="buttonText">the text of the button to confirm the selection</param>
        /// <param name="initialPath">the initial path where the <see cref="FileBrowser"/> allows
        /// selection/param>
        /// <param name="extensions">the file extensions; only files with one of these extensions
        /// can be picked</param>
        /// <returns>the name of the picked file or the empty if none was picked</returns>
        private async UniTask<string> GetPathAsync(string title, string buttonText, string initialPath, params string[] extensions)
        {
            SEEInput.KeyboardShortcutsEnabled = false;

            FileBrowser.SetFilters(false, extensions);

            await FileBrowser.WaitForLoadDialog(pickMode: FileBrowser.PickMode.Files, allowMultiSelection: false,
                initialPath: initialPath, initialFilename: null, title: title, loadButtonText: buttonText);

            SEEInput.KeyboardShortcutsEnabled = true;

            return FileBrowser.Success ? FileBrowser.Result[0] : string.Empty;
        }

        /// <summary>
        /// Saves a drawable configuration. Asks the user for a filename using the <see cref="FileBrowser"/>.
        /// </summary>
        /// <param name="saveState">The chosen save state (one/more/all)</param>
        public async Task SaveDrawableConfigurationAsync(SaveState saveState)
        {
            string title = "";
            switch (saveState)
            {
                /// Block for save only one drawable.
                case SaveState.One:
                    DrawableConfigManager.EnsureDrawableDirectoryExists(DrawableConfigManager.SingleConfPath);
                    initPath = DrawableConfigManager.SingleConfPath;
                    title = "Save Drawable";
                    break;

                /// Block for save more drawables.
                case SaveState.Multiple:
                    DrawableConfigManager.EnsureDrawableDirectoryExists(DrawableConfigManager.MultipleConfPath);
                    initPath = DrawableConfigManager.MultipleConfPath;
                    title = "Save specific Drawables";
                    break;

                /// Block for save all drawables of the game world.
                case SaveState.All:
                    DrawableConfigManager.EnsureDrawableDirectoryExists(DrawableConfigManager.MultipleConfPath);
                    initPath = DrawableConfigManager.MultipleConfPath;
                    title = "Save all Drawables";
                    break;
            }

            string result = await GetPathAsync(title, "Save", initPath, Filenames.DrawableConfigExtension);

            if (!string.IsNullOrWhiteSpace(result))
            {
                SetPath(result);
            }

            /// Refreshes the UI canvas to prevent display issues.
            UICanvas.Refresh();
        }

        /// <summary>
        /// Method to load an image
        /// </summary>
        public void LoadImage()
        {
            StartCoroutine(ShowLoadImageDialogCoroutine());
        }

        /// <summary>
        /// Coroutine that enables the file browser to chose a file.
        /// </summary>
        /// <returns>nothing</returns>
        private IEnumerator ShowLoadImageDialogCoroutine()
        {
            DrawableConfigManager.EnsureDrawableDirectoryExists(ValueHolder.ImagePath);
            initPath = ValueHolder.ImagePath;
            string title = "Load an image";

            /// Ensures that only PNG or JPG files can be selected.
            string[] extensions = new string[] { Filenames.PNGExtension, Filenames.JPGExtension };

            SEEInput.KeyboardShortcutsEnabled = false;

            FileBrowser.SetFilters(true, extensions);

            /// Opens the file browser.
            yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, initPath, null, title, "Load");

            /// Enables the short cuts again.
            SEEInput.KeyboardShortcutsEnabled = true;

            if (FileBrowser.Success)
            {
                /// Sets the file path upon success.
                SetPath(FileBrowser.Result[0]);
            }

            /// Refreshes the UI canvas to prevent display issues.
            UICanvas.Refresh();
        }

        /// <summary>
        /// Returns true if the file browser is open.
        /// </summary>
        /// <returns>Whether file browser is open.</returns>
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
using SEE.Game.Drawable;
using SEE.UI.FilePicker;
using SEE.Utils;
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
        /// using a <see cref="FileBrowser"/>.
        /// </summary>
        /// <param name="loadState">The chosen load state (regular/specific).</param>
        public void LoadDrawableConfiguration(LoadState loadState)
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

            PathPicker.GetPath(title, true, initPath, HandleFileBrowserSuccess, () => { }, Filenames.DrawableConfigExtension);
        }

        /// <summary>
        /// Saves a drawable configuration. Asks the user for a filename using the <see cref="FileBrowser"/>.
        /// </summary>
        /// <param name="saveState">The chosen save state (one/more/all).</param>
        public void SaveDrawableConfiguration(SaveState saveState)
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

            PathPicker.GetPath(title, false, initPath, HandleFileBrowserSuccess, () => { }, Filenames.DrawableConfigExtension);
        }

        /// <summary>
        /// Called when the file browser has successfully chosen a file.
        /// Sets the path to the chosen file and refreshes the UI canvas.
        /// </summary>
        /// <param name="path">The path the user selected.</param>
        private void HandleFileBrowserSuccess(string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                SetPath(path);
            }

            /// Refreshes the UI canvas to prevent display issues.
            UICanvas.Refresh();
        }

        /// <summary>
        /// Method to load an image. The file is selected by the user by way
        /// of the <see cref="FileBrowser"/>.
        /// </summary>
        public void LoadImage()
        {
            DrawableConfigManager.EnsureDrawableDirectoryExists(ValueHolder.ImagePath);
            initPath = ValueHolder.ImagePath;

            /// Ensures that only PNG or JPG files can be selected.
            PathPicker.GetPath("Load an image", false, initPath, HandleFileBrowserSuccess, () => { },
                                new string[] { Filenames.PNGExtension, Filenames.JPGExtension, Filenames.JPEGExtension });
        }

        /// <summary>
        /// Returns true if the file browser is open.
        /// </summary>
        /// <returns>Whether file browser is open.</returns>
        public bool IsOpen()
        {
            return PathPicker.IsOpen();
        }

        /// <summary>
        /// Sets the chosen path.
        /// </summary>
        /// <param name="newPath">The chosen path.</param>
        private void SetPath(string newPath)
        {
            path = newPath;
            gotFilePath = true;
        }

        /// <summary>
        /// If <see cref="gotFilePath"/> is true, the <paramref name="filePath"/> will be the file path chosen by the
        /// player. Otherwise it will be some dummy value.
        /// </summary>
        /// <param name="filePath">The file path the player confirmed, if that doesn't exist, some dummy value.</param>
        /// <returns><see cref="gotFilePath"/>.</returns>
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

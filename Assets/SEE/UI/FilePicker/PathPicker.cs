using SEE.Controls;
using SimpleFileBrowser;
using System;

namespace SEE.UI.FilePicker
{
    /// <summary>
    /// Allows a user to pick a file path using a <see cref="FileBrowser"/>.
    /// </summary>
    internal static class PathPicker
    {
        /// <summary>
        /// Opens a <see cref="FileBrowser"/> and asks the user to pick a single file.
        /// The picked file is returned via the callback <paramref name="onSuccess"/>
        /// when the user closes the <see cref="FileBrowser"/>.
        /// If no file has been picked, the callback <paramref name="onCancel"/> is called.
        /// </summary>
        /// <param name="title">Title of the dialog.</param>
        /// <param name="load">Whether the dialog is for loading a file; if false, it is
        /// for saving a file.</param>
        /// <param name="initialPath">The initial path where the <see cref="FileBrowser"/> allows
        /// selection.</param>
        /// <param name="onSuccess">The callback that is called when the user closes
        /// the <see cref="FileBrowser"/> confirming the selection.</param>
        /// <param name="onCancel">The callback that is called when the user closes the
        /// dialog using the cancel button.</param>
        /// <param name="extensions">The file extensions; only files with one of these extensions
        /// can be picked.</param>
        public static void GetPath(string title, bool load, string initialPath,
                                   Action<string> onSuccess, Action onCancel,
                                   params string[] extensions)
        {
            SEEInput.KeyboardShortcutsEnabled = false;

            FileBrowser.SetFilters(false, extensions);

            if (load)
            {
                FileBrowser.ShowLoadDialog(HandleFileBrowserSuccess,
                                           HandleFileBrowserCancel,
                                           allowMultiSelection: false,
                                           pickMode: FileBrowser.PickMode.Files,
                                           title: title,
                                           initialPath: initialPath);
            }
            else
            {
                FileBrowser.ShowSaveDialog(HandleFileBrowserSuccess,
                                           HandleFileBrowserCancel,
                                           pickMode: FileBrowser.PickMode.Files,
                                           title: title,
                                           initialPath: initialPath);
            }

            void HandleFileBrowserSuccess(string[] paths)
            {
                SEEInput.KeyboardShortcutsEnabled = true;
                if (paths.Length != 1)
                {
                    throw new ArgumentException($"Expected exactly one file path, but got {paths.Length}.");
                }
                onSuccess(paths[0]);
            }

            void HandleFileBrowserCancel()
            {
                SEEInput.KeyboardShortcutsEnabled = true;
                onCancel();
            }
        }

        /// <summary>
        /// Returns true if the <see cref="FileBrowser"/> is open.
        /// </summary>
        /// <returns>Whether the <see cref="FileBrowser"/> is open.</returns>
        public static bool IsOpen()
        {
            return FileBrowser.IsOpen;
        }
    }
}



using System;
using System.Collections.Generic;
using System.IO;
using SEE.Net.Util;
namespace SEE.Utils
{

    public static class FileWatcher
    {
        /// <summary>
        /// A list of files with their last update date.
        /// </summary>
        private static Dictionary<string, DateTime> lastWriteDate = new();

        /// <summary>
        /// A list of file which should be ignored the next time a change is registered.
        /// This is necessary, because a file update from another participant would trigger an update event loop.
        /// </summary>
        private static List<string> ignoreSyncedFiles = new();


        /// <summary>
        /// Will ignore the file at <paramref name="filePath"/> for one time at the next change event.
        ///
        /// This is used to not create an infinite loop when syncing the files.
        /// </summary>
        /// <param name="filePath">File path relative to the multiplayer directory.</param>
        public static void IgnoreFileOneTime(string filePath)
        {
            ignoreSyncedFiles.Add(filePath);
        }


        /// <summary>
        /// Starts watching for file changes at the passed path.
        /// </summary>
        /// <param name="path">The path to watch for changes.</param>
        /// <param name="OnChanged">Will be fired, when a file has changed.</param>
        /// <param name="OnRenamed">Will be fired, when a file has changed.</param>
        public static void Watch(string path, FileSystemEventHandler OnChanged, RenamedEventHandler OnRenamed, FileSystemEventHandler OnDelete)
        {

            Logger.Log($"Start watching {path} for changes");


            FileSystemWatcher watcher = new(path)
            {
                Filter = "*.*",
                //NotifyFilter = NotifyFilters.LastWrite
            };
            watcher.Changed += OnDirectoryChanged;
            watcher.Renamed += OnRenamed;
            watcher.Deleted += OnDelete;
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;

            void OnDirectoryChanged(object sender, FileSystemEventArgs e)
            {
                string filePath = e.FullPath;
                DateTime writeDate = File.GetLastWriteTime(filePath);

                if (lastWriteDate.ContainsKey(filePath))
                {
                    // Check if the file was actually written.
                    // This will prevent multiple invocations of the event.
                    if (lastWriteDate[filePath] == writeDate)
                    {
                        return;
                    }
                    lastWriteDate[filePath] = writeDate;
                }
                else
                {
                    lastWriteDate.Add(filePath, writeDate);
                }

                if (ignoreSyncedFiles.Contains(e.FullPath))
                {
                    ignoreSyncedFiles.Remove(e.FullPath);
                    Logger.Log($"Ignore file {e.FullPath}");
                    return;
                }
                OnChanged.Invoke(sender, e);

            }
        }
    }
}

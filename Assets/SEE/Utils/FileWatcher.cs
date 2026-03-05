

using System;
using System.Collections.Generic;
using System.IO;
using SEE.Net.Util;
using UnityEngine.XR.Interaction.Toolkit.Attachment;

namespace SEE.Utils
{

    public static class FileWatcher
    {
        static Dictionary<string, DateTime> lastWriteDate = new(); //fileName - Last write date time

        public static void Watch(string filePath, FileSystemEventHandler OnChanged, RenamedEventHandler OnRenamed)
        {

            Logger.Log($"Start watching {filePath} for changes");


            FileSystemWatcher watcher = new(filePath)
            {
                Filter = "*.*"
            };

            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Changed += OnDirectoryChanged;
            watcher.Renamed += OnRenamed;
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;

            void OnDirectoryChanged(object sender, FileSystemEventArgs e)
            {
                string filePath = e.FullPath;
                DateTime writeDate = File.GetLastWriteTime(filePath);

                if (lastWriteDate.ContainsKey(filePath))
                {
                    // Check if the file was actually written.
                    // This will prevent multiple invocations of this event method.
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

                OnChanged.Invoke(sender, e);

            }
        }


    }
}



using System.IO;
using SEE.Net.Util;
using UnityEngine.XR.Interaction.Toolkit.Attachment;

namespace SEE.Utils
{

    public static class FileWatcher
    {

        public static void Watch(string filePath, FileSystemEventHandler OnChanged, RenamedEventHandler OnRenamed)
        {

            Logger.Log($"Start watching {filePath} for changes");
            FileSystemWatcher watcher = new(filePath)
            {
                Filter = "*.*"
            };


            watcher.Changed += OnChanged;
            watcher.Renamed += OnRenamed;
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;
        }
    }
}

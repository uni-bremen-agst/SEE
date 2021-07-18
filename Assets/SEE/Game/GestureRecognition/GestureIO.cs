using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SEE.Game.GestureRecognition
{
    /// <summary>
    /// Methods for storing and loading gesture sets from disk.
    /// </summary>
    public class GestureIO
    {

        /// <summary>
        /// The gesture data set.
        /// </summary>
        public static Gesture[] DataSet => LoadDataSetFromDisk();


        /// <summary>
        /// Serializable Wrapper class for <see cref="Gesture"/> to store rawpoints and gesture name as json
        /// </summary>
        [Serializable]
        private class GestureWrapper
        {
            /// <summary>
            /// The raw points of the gesture. No Processing was applied.
            /// </summary>
            public GesturePoint[] Points;

            /// <summary>
            /// The name of this gesture.
            /// </summary>
            public string Name;


            /// <summary>
            /// Creates a new GestureWrapper
            /// </summary>
            /// <param name="points">The gesture path</param>
            /// <param name="name">The gesture name</param>
            public GestureWrapper(GesturePoint[] points, string name)
            {
                this.Points = points;
                this.Name = name;
            }
        }

        /// <summary>
        /// Writes the given gesture path identified by the given name to disk.
        /// </summary>
        /// <param name="gesture">The gesture path</param>
        /// <param name="name">The gesture name</param>
        public static void SaveDataSetToDisk(GesturePoint[] gesture, string name)
        {
            if (!Directory.Exists(Application.streamingAssetsPath + "\\Architecture\\GestureSet\\NewGestures"))
                Directory.CreateDirectory(Application.streamingAssetsPath + "\\Architecture\\GestureSet\\NewGestures");
            GestureWrapper wrapper = new GestureWrapper(gesture, name);
            string json = JsonUtility.ToJson(wrapper);
            string filepath =
                $"{Application.streamingAssetsPath}\\Architecture\\GestureSet\\NewGestures\\{name}_{DateTime.Now.ToFileTime()}.json";
            File.WriteAllText(filepath, json);
        }


        /// <summary>
        /// Loads all gesture data from disk.
        /// </summary>
        /// <returns></returns>
        private static Gesture[] LoadDataSetFromDisk()
        {
            List<Gesture> gestures = new List<Gesture>();
            string[] gestureFolders =
                Directory.GetDirectories(Application.streamingAssetsPath + "\\Architecture\\GestureSet");
            foreach (string folder in gestureFolders)
            {
                var gestureFiles = Directory.GetFiles(folder, "*.json").Where(s => s.EndsWith(".json")).ToList();
                foreach (string file in gestureFiles)
                {
                    string json = File.ReadAllText(file);
                    GestureWrapper wrapper = JsonUtility.FromJson<GestureWrapper>(json);
                    gestures.Add(new Gesture(wrapper.Points, name: wrapper.Name));
                }
            }

            var folderEntries = Directory
                .GetFiles(Application.streamingAssetsPath + "\\Architecture\\GestureSet", "*.json")
                .Where(s => s.EndsWith(".json")).ToList();
            foreach (string entry in folderEntries)
            {
                string json = File.ReadAllText(entry);
                GestureWrapper wrapper = JsonUtility.FromJson<GestureWrapper>(json);
                gestures.Add(new Gesture(wrapper.Points, name: wrapper.Name));
            }
            return gestures.ToArray();
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using SEE.Game;
using SEE.Controls;
using UnityEngine;

namespace SEE.DataModel.IO
{
    /// <summary>
    /// Serialises and deserialises the current city layout using the binaryformater.
    /// </summary>
    public static class LayoutSaveSystem
    {
        /// <summary>
        /// Serialises the current city layout <paramref name="annotatableObjects"/> into <paramref name="path"/>.
        /// </summary>
        /// <param name="annotatableObjects">the objects to be saved</param>
        /// <param name="path">the path which the file is going to be saved in</param>
        public static void SaveAnnotatableObjects(List<AnnotatableObjectData> annotatableObjects, string path)
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Create);

            try
            {
                binaryFormatter.Serialize(stream, annotatableObjects);
                Debug.Log("Saving complete " + path.Substring(path.LastIndexOf('\\') + 1));
            }
            finally
            {
                stream.Close();
            }
        }

        /// <summary>
        /// Deserialases the a city layout from <paramref name="path"/>.
        /// </summary>
        /// <param name="path">the path which the layout is going to be loaded from</param>
        public static List<AnnotatableObjectData> LoadAnnotatableObjects(string path)
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);
            List<AnnotatableObjectData> annotatableObjects = new List<AnnotatableObjectData>();

            try
            {
                annotatableObjects = binaryFormatter.Deserialize(stream) as List<AnnotatableObjectData>;
            }
            finally
            {
                stream.Close();
            }

            return annotatableObjects;
        }

    }
}

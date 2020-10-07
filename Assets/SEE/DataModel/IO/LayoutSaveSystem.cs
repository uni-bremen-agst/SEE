using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using SEE.Game;
using SEE.Controls;
using UnityEngine;

namespace SEE.DataModel.IO
{
    public static class LayoutSaveSystem
    {
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

﻿using System.Collections.Generic;
using SEE.GO;
using UnityEngine;

namespace SEE.Layout.IO
{
    /// <summary>
    /// Allows to save layout information in SLD (SEE Layout Data) format.
    /// SLD captures the complete transform of a game object.
    ///
    /// The syntax of SLD is a list of lines each containing comma-separated values (CSV).
    /// Every line represents: id, position, rotation, and scale in exactly this order:
    /// id: unique name the node whose layout is represented
    /// position: world-space position as 3D vector in Unity's co-ordinate system
    /// rotation: rotation of the node in Euler angles as a 3D vector in Unity's co-ordinate system
    /// scale: world-space scale as 3D vector in Unity's co-ordinate system
    ///
    /// Regarding the id: If the game object has a component <see cref="NodeRef"/> referring
    /// to a valid graph node, that graph node's ID is used; otherwise the name of the game
    /// object is used.
    ///
    /// The values are separated by <see cref="Delimiter"/>.
    ///
    /// <seealso cref="SLDReader"/>.
    /// </summary>
    public class SLDWriter
    {
        /// <summary>
        /// The delimiter to separate data points within the same line in the output file.
        /// </summary>
        public const char Delimiter = ';';

        /// <summary>
        /// Writes the complete Transform data of all game objects in <paramref name="gameObjects"/>
        /// in the SLD format.
        /// </summary>
        /// <param name="filename">name of file where the data are stored</param>
        /// <param name="gameObjects">the objects whose Transform is to be written</param>
        public static void Save(string filename, ICollection<GameObject> gameObjects)
        {
            List<string> outputs = new();

            foreach (GameObject go in gameObjects)
            {
                string name = ID(go);
                Vector3 position = go.transform.position;
                Vector3 rotation = go.transform.eulerAngles;
                Vector3 scale = go.transform.lossyScale;

                string output = name
                              + Delimiter + ToColumns(position)
                              + Delimiter + ToColumns(rotation)
                              + Delimiter + ToColumns(scale);
                outputs.Add(output);
            }

            // WriteAllLines creates a file, writes a collection of strings to the file,
            // and then closes the file.  You do NOT need to call Flush() or Close().
            System.IO.File.WriteAllLines(filename, outputs);
        }

        /// <summary>
        /// Returns the ID to be used for the given <paramref name="gameObject"/>.
        /// If the game object has component  <see cref="NodeRef"/> referring to a valid
        /// graph node, that graph node's ID is returned; otherwise the name of the game
        /// object is returned.
        /// </summary>
        /// <param name="gameObject">object whose ID is to be retrieved</param>
        /// <returns>the ID of the node</returns>
        private static string ID(GameObject gameObject)
        {
            if (gameObject.TryGetComponent<NodeRef>(out NodeRef nodeRef))
            {
                if (nodeRef.Value != null)
                {
                    return nodeRef.Value.ID;
                }
                else
                {
                    return gameObject.name;
                }
            }
            else
            {
                return gameObject.name;
            }
        }

        /// <summary>
        /// Returns a string will all three vector components of <paramref name="value"/>
        /// separated by <see cref="Delimiter"/>.
        /// </summary>
        /// <param name="value">vector to be converted to string</param>
        /// <returns>vector components as string</returns>
        private static string ToColumns(Vector3 value)
        {
            return FloatToString(value.x)
                + Delimiter + FloatToString(value.y)
                + Delimiter + FloatToString(value.z);
        }

        /// <summary>
        /// Converts a float value to a string with a period as a decimal separator.
        /// </summary>
        /// <param name="value">value to be converted</param>
        /// <returns>the float as a string in the requested format</returns>
        private static string FloatToString(float value)
        {
            return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
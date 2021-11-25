using System.Collections.Generic;
using TinySpline;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Utility functions for interoperability between TinySpline and Unity.
    /// </summary>
    class TinySplineInterop
    {
        /// <summary>
        /// Converts the given list of Unity Vector3 to a list of doubles
        /// (TinySpline's representation of points).
        /// </summary>
        /// <param name="vectors">Vectors to be converted</param>
        /// <returns>List of doubles where the values `i' to `i+2' correspond
        /// to the the Vector3 `i' in <paramref name="vectors"/></returns>
        public static IList<double> VectorsToList(IList<Vector3> vectors)
        {
            List<double> list = new List<double>();
            foreach (Vector3 vector in vectors)
            {
                list.Add(vector.x);
                list.Add(vector.y);
                list.Add(vector.z);
            }
            return list;
        }

        /// <summary>
        /// Converts the given Unity Vector3s to a list of doubles
        /// (TinySpline's representation of points).
        /// </summary>
        /// <param name="vectors">Vectors to be converted</param>
        /// <returns>List of doubles where the values `i' to `i+2' correspond
        /// to the the Vector3 `i' in <paramref name="vectors"/></returns>
        public static IList<double> VectorsToList(params Vector3[] vectors)
        {
            return VectorsToList(new List<Vector3>(vectors));
        }

        /// <summary>
        /// Converts the given list of doubles (TinySpline's representation of
        /// points) to an array of Unity Vector3. It is assumed that the
        /// length of <paramref name="values"/> can be completely divided by 3
        /// (i.e., <paramref name="values"/> contains three-dimensional
        /// points).
        /// </summary>
        /// <param name="values">Values to be converted</param>
        /// <returns><paramref name="values"/> as an array of Unity Vector3
        /// </returns>
        public static Vector3[] ListToVectors(IList<double> values)
        {
            Debug.Assert(values.Count % 3 == 0,
                    "Expecting three-dimensional points");
            Vector3[] vectors = new Vector3[values.Count / 3];
            for (int i = 0; i < vectors.Length; i++)
            {
                int idx = i * 3;
                vectors[i] = new Vector3(
                    (float)values[idx],
                    (float)values[idx + 1],
                    (float)values[idx + 2]);
            }
            return vectors;
        }

        /// <summary>
        /// Converts the given list of doubles to a float array.
        /// </summary>
        /// <param name="values">Values to be converted</param>
        /// <returns><paramref name="values"/> as float array</returns>
        public static float[] ListToArray(IList<double> values)
        {
            float[] array = new float[values.Count];
            for (int i = 0; i < values.Count; i++)
            {
                array[i] = (float)values[i];
            }
            return array;
        }

        /// <summary>
        /// Converts the given float array to a list of doubles.
        /// </summary>
        /// <param name="values">Values to be converted</param>
        /// <returns><paramref name="values"/> as list of doubles</returns>
        public static IList<double> ArrayToList(float[] values)
        {
            IList<double> list = new List<double>(values.Length);
            foreach (float val in values)
            {
                list.Add(val);
            }
            return list;
        }

        /// <summary>
        /// Converts TinySpline's Vec3 to Unity's Vector3.
        /// </summary>
        /// <param name="vec3">Vector to be converted</param>
        /// <returns>A Unity Vector3</returns>
        public static Vector3 VectorToVector(Vec3 vec3)
        {
            return new Vector3(
                (float)vec3.X,
                (float)vec3.Y,
                (float)vec3.Z);
        }
    }
}
using OdinSerializer;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.SEE.GameObjects
{
    /// <summary>
    /// This class serves as a bridge between TinySpline's representation of
    /// B-Splines and a serializable version that can be used in subclasses of
    /// <see cref="MonoBehaviour"/>. Note that the attributes related to Unity
    /// (e.g., <see cref="ControlPoints"/>) must not be set directly. Instead,
    /// they must be updated via setting <see cref="Spline"/>.
    /// </summary>
    public class SEESpline : SerializedMonoBehaviour
    {
        /// <summary>
        /// Degree of the piecewise polynomials.
        /// </summary>
        public uint Degree;

        /// <summary>
        /// The control points of a spline are decisive for its shape.
        /// </summary>
        public Vector3[] ControlPoints;

        /// <summary>
        /// Weighting factors of the <see cref="ControlPoints"/>. Can also be
        /// used change the shape of a spline, but is less intuitive.
        /// </summary>
        public float[] Knots;

        public TinySpline.BSpline Spline
        {
            get
            {
                return new TinySpline.BSpline((uint)ControlPoints.Length, 3, Degree)
                {
                    ControlPoints = TinySplineInterop.VectorsToList(ControlPoints),
                    Knots = TinySplineInterop.ArrayToList(Knots)
                };
            }
            set
            {
                Degree = (uint)value.Degree;
                ControlPoints = TinySplineInterop.ListToVectors(value.ControlPoints);
                Knots = TinySplineInterop.ListToArray(value.Knots);
            }
        }
    }

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
    }
}
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout.Utils
{
    /// <summary>
    /// Creates points of straight lines, simple splines, and B-splines.
    /// </summary>
    public static class LinePoints
    {
        /// <summary>
        /// Number of dimensions. Here: 3D.
        /// </summary>
        private const int dimensions = 3;

        /// <summary>
        /// Determines the strength of the tension for bundling edges. This value may
        /// range from 0.0 (straight lines) to 1.0 (maximal bundling along the spline).
        /// </summary>
        public const float tensionDefault = 0.85f; // 0.85 is the value recommended by Holten

        /// <summary>
        /// Returns the points of the line along the B-spline constrained by the given <paramref name="controlPoints"/>.
        /// </summary>
        /// <param name="controlPoints">control points of the B-spline</param>
        /// <param name="tension">tension of the control points onto the spline points; must be in
        /// the range [0, 1]</param>
        /// <returns>points of the line along the B-spline</returns>
        public static Vector3[] BSplineLinePoints(Vector3[] controlPoints, float tension = tensionDefault)
        {
            Debug.Assert(controlPoints.Length > 3);
            Debug.Assert(0.0f <= tension && tension <= 1.0f);

            // Create a cubic spline with control points in 3D using a clamped knot vector.
            TinySpline.BSpline spline = new TinySpline.BSpline((uint)controlPoints.Length, dimensions)
            {
                // Setup control points.
                ControlPoints = VectorsToList(controlPoints)
            };

            IList<double> list = spline.Tension(tension).Sample();
            return ListToVectors(list);
        }


        /// <summary>
        /// Returns the points of the line along the B-spline constrained by the given <paramref name="controlPoints"/> and <paramref name="sampleRate"/>.
        /// </summary>
        /// <param name="controlPoints">control points of the B-spline</param>
        /// <param name="tension">tension of the control points onto the spline points; must be in
        /// the range [0, 1]</param>
        /// <param name="sampleRate">Number of points on the line
        /// <returns>points (depending on the sampleRate) of the line along the B-spline</returns>
        public static Vector3[] BSplineLinePointsSampleRate(Vector3[] controlPoints, uint sampleRate = 100, float tension = tensionDefault)
        {
            Debug.Assert(controlPoints.Length > 3);
            Debug.Assert(0.0f <= tension && tension <= 1.0f);

            // Create a cubic spline with control points in 3D using a clamped knot vector.
            TinySpline.BSpline spline = new TinySpline.BSpline((uint)controlPoints.Length, dimensions)
            {
                // Setup control points.
                ControlPoints = VectorsToList(controlPoints)
            };

            IList<double> list = spline.Tension(tension).Sample(sampleRate);
            return ListToVectors(list);
        }


        /// <summary>
        /// Serializes the co-ordinates of all given vectors as a list.
        /// E.g., The list {(1.0, 2.0, 3.0), (4.0, 5.0, 6.0)} is serialized
        /// into {1.0, 2.0, 3.0, 4.0, 5.0, 6.0}.
        /// </summary>
        /// <param name="vectors">vectors to be serialized</param>
        /// <returns>serialized coordindates of given vectors</returns>
        private static IList<double> VectorsToList(IList<Vector3> vectors)
        {
            List<double> result = new List<double>();
            foreach (Vector3 vector in vectors)
            {
                result.Add(vector.x);
                result.Add(vector.y);
                result.Add(vector.z);
            }
            return result;
        }

        /// <summary>
        /// Deserializes the given co-oordindates back into 3D vectors.
        /// E.g., The list [1.0, 2.0, 3.0, 4.0, 5.0, 6.0] is deserialized
        /// into [(1.0, 2.0, 3.0), (4.0, 5.0, 6.0)].
        /// </summary>
        /// <param name="values">co-ordinates to be deserialized</param>
        /// <returns>Deserialized vectors having the given co-ordinates</returns>
        private static Vector3[] ListToVectors(IList<double> values)
        {
            Vector3[] result = new Vector3[values.Count / dimensions];

            int i = 0;
            int next = 0;
            // Random value; this value will not never be added based on the
            // logic of the loop, but the compiler forces us to initialize v.
            Vector3 v = Vector3.zero;
            foreach (double value in values)
            {
                switch (i % dimensions)
                {
                    case 0:
                        v = new Vector3();
                        v.x = (float)value;
                        break;
                    case 1:
                        v.y = (float)value;
                        break;
                    case 2:
                        v.z = (float)value;
                        result[next] = v;
                        next++;
                        break;
                }
                i++;
            }
            return result;
        }
    }
}

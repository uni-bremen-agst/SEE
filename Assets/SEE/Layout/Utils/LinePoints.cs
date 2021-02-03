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

        /// <summary>
        /// Returns the points of a direct spline from <paramref name="start"/> to <paramref name="end"/>.
        /// The y co-ordinate of the middle point of the spline is X above (if <paramref name="above"/>
        /// is true) or below (if <paramref name="above"/> is false), respectively, the higher
        /// (or lower if <paramref name="above"/> is false) of the two (<paramref name="start"/>
        /// and <paramref name="end"/>), where X is half the distance between <paramref name="start"/>
        /// and <paramref name="end"/>.
        ///
        /// The y offset of the middle point is chosen relative to the distance between the two points.
        /// We are using a value relative to the distance so that splines connecting close points do
        /// not shoot into the sky. Otherwise they would be difficult to read. Likewise, splines
        /// connecting two points farther away should go higher (or deeper, respectively) so that we
        /// avoid crossings with things that may be in between. This heuristic may help to better read
        /// the splines.
        /// </summary>
        /// <param name="start">starting point</param>
        /// <param name="end">ending point</param>
        /// <param name="above">whether middle point of the spline should be above <paramref name="start"/>
        /// <param name="minOffset">the minimal y offset for the point in between <paramref name="start"/>
        /// and <paramref name="end"/> through which the spline should pass</param>
        /// <returns>points of the spline</returns>
        public static Vector3[] SplineLinePoints(Vector3 start, Vector3 end, bool above, float minOffset)
        {
            // This offset is used to draw the line somewhat below
            // or above the house (depending on the orientation).
            float offset = Mathf.Max(minOffset, 0.5f * Vector3.Distance(start, end)); // must be positive
            // The level at which edges are drawn.
            float edgeLevel = above ? Mathf.Max(start.y, end.y) + offset
                                    : Mathf.Min(start.y, end.y) - offset;

            Vector3 middle = Vector3.Lerp(start, end, 0.5f);
            middle.y = edgeLevel;
            return SplineLinePoints(start, middle, end);
        }

        /// <summary>
        /// Returns the points of a spline from <paramref name="start"/> over <paramref name="middle"/>
        /// to <paramref name="end"/>.
        ///
        /// Note: The resultant spline actually goes through <paramref name="middle"/>.
        /// </summary>
        /// <param name="start">start of the spline</param>
        /// <param name="middle">middle of the spline</param>
        /// <param name="end">end of the spline</param>
        /// <returns>points of a spline</returns>
        public static Vector3[] SplineLinePoints(Vector3 start, Vector3 middle, Vector3 end)
        {
            List<double> path = new List<double>()
               { start.x,  start.y,  start.z,
                 middle.x, middle.y, middle.z,
                 end.x,    end.y,    end.z
               };
            return ListToVectors(TinySpline.BSpline.InterpolateCubicNatural(path, dimensions).Sample());
        }

        /// <summary>
        /// Returns the points from <paramref name="start"/> to <paramref name="end"/>
        /// on an offset straight line led on the given <paramref name="yLevel"/>. The first
        /// point is <paramref name="start"/>. The second point has the same x and z
        /// co-ordinate as <paramref name="start"/> but its y co-ordinate is <paramref name="yLevel"/>.
        /// The third point has the same x and z co-ordinate as <paramref name="end"/> but again
        /// its y co-ordinate is <paramref name="yLevel"/>. The last point is <paramref name="end"/>.
        /// </summary>
        /// <param name="start">start of the line</param>
        /// <param name="end">end of the line</param>
        /// <param name="yLevel">the y co-ordinate of the two other points in between <paramref name="start"/>
        /// and <paramref name="end"/></param>
        /// <returns>the four points of the offset straight line</returns>
        public static Vector3[] StraightLinePoints(Vector3 start, Vector3 end, float yLevel)
        {
            Vector3[] points = new Vector3[4];
            points[0] = start;
            points[1] = points[0]; // we are maintaining the x and z co-ordinates,
            points[1].y = yLevel;   // but adjust the y co-ordinate
            points[2] = end;
            points[2].y = yLevel;
            points[3] = end;
            return points;
        }
    }
}

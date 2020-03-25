using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
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
        /// Returns the B-spline for the given <paramref name="controlPoints"/>.
        /// </summary>
        /// <param name="controlPoints">control points of the B-spline</param>
        /// <returns>B-spline constrained by the given <paramref name="controlPoints"/></returns>
        private static TinySpline.BSpline Spline(IList<Vector3> controlPoints)
        {
            // Create a cubic spline with 7 control points in 3D using
            // a clamped knot vector. This call is equivalent to:
            // BSpline spline = new BSpline(7, 2, 3, BSplineType.CLAMPED);
            TinySpline.BSpline spline = new TinySpline.BSpline(7, dimensions);

            // Setup control points. Note: This looks like a superflous assignment,
            // but in fact is a call to the setter of the property with a side effect
            // on spline.
            IList<double> ctrlp = spline.controlPoints;
            return spline;
        }

        /// <summary>
        /// Determines the strength of the tension for bundling edges. This value may
        /// range from 0.0 (straight lines) to 1.0 (maximal bundling along the spline).
        /// </summary>
        public static float tension = 0.85f; // 0.85 is the value recommended by Holten

        /// <summary>
        /// Returns the points of the line along the B-spline constrained by the given <paramref name="controlPoints"/>.
        /// </summary>
        /// <param name="controlPoints">control points of the B-spline</param>
        /// <returns>points of the line along the B-spline</returns>
        public static Vector3[] BSplineLinePoints(Vector3[] controlPoints)
        {
            // Create a cubic spline with control points in 3D using a clamped knot vector.
            TinySpline.BSpline spline = new TinySpline.BSpline((uint)controlPoints.Length, dimensions)
            {
                // Setup control points.
                controlPoints = VectorsToList(controlPoints)
            };

            IList<double> list = spline.buckle(tension).sample();
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

        public static Vector3[] SplineLinePoints(Vector3 start, Vector3 end, bool above)
        {
            // The offset of the edges above or below the ground chosen relative 
            // to the distance between the two blocks.
            // We are using a value relative to the distance so that edges 
            // connecting close blocks do not shoot into the sky. Otherwise they
            // would be difficult to read. Likewise, edges connecting blocks farther
            // away should go higher so that we avoid edge and node crossings.
            // This heuristic may help to better read the edges.

            // This offset is used to draw the line somewhat below
            // or above the house (depending on the orientation).
            float offset = 1.5f * Vector3.Distance(start, end); // must be positive
            // The level at which edges are drawn.
            float edgeLevel = above ? Mathf.Max(start.y, end.y) + offset
                                    : Mathf.Min(start.y, end.y) - offset;

            Vector3[] controlPoints = new Vector3[4];
            controlPoints[0] = start;
            controlPoints[1] = Vector3.Lerp(start, end, 0.333333f);
            controlPoints[1].y = edgeLevel;
            controlPoints[2] = Vector3.Lerp(start, end, 0.666666f);
            controlPoints[3].y = edgeLevel;
            controlPoints[3] = end;
            return BSplineLinePoints(controlPoints);
        }

        /// <summary>
        /// Returns the points from <paramref name="start"/> to <paramref name="end"/>
        /// on an offset straight line led on the given <paramref name="yLevel"/>. The first 
        /// point is <paramref name="start"/>. The second point has the same x and z 
        /// co-ordiante as <paramref name="start"/> but its y co-ordinate is <paramref name="yLevel"/>.
        /// The third point has the same x and z  co-ordiante as <paramref name="end"/> but again
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
            points[0]   = start;
            points[1]   = points[0]; // we are maintaining the x and z co-ordinates,
            points[1].y = yLevel;   // but adjust the y co-ordinate
            points[2]   = end;
            points[2].y = yLevel;
            points[3]   = end;
            return points;
        }
    }
}
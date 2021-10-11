using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// Layout information about an edge.
    /// </summary>
    public abstract class ILayoutEdge
    {
        /// <summary>
        /// Source of the edge.
        /// </summary>
        public ILayoutNode Source;

        /// <summary>
        /// Target of the edge.
        /// </summary>
        public ILayoutNode Target;

        /// <summary>
        /// The shaping spline of the edge. The default spline is a line
        /// (i.e., a spline of degree 1 with 2 control points) connecting the
        /// points (0, 0, 0) and (1, 1, 1).
        /// </summary>
        public TinySpline.BSpline Spline { get; set; } =
            new TinySpline.BSpline(2, 3, 1)
            {
                ControlPoints = {0, 0, 0, 1, 1, 1}
            };

        public Vector3[] ControlPoints
        {
            get { return ListToVectors( Spline.ControlPoints ); }
        }

        public Vector3[] Points
        {
            // Todo RDP
            get { return ListToVectors( Spline.Sample(100) ); }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="source">Source of the edge.</param>
        /// <param name="target">Target of the edge.</param>
        public ILayoutEdge(ILayoutNode source, ILayoutNode target)
        {
            Source = source;
            Target = target;
        }

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

        public static Vector3[] ListToVectors(IList<double> values)
        {
            Debug.Assert(values.Count % 3 == 0,
                    "Expecting three-dimensional points");
            Vector3[] vectors = new Vector3[values.Count / 3];
            for (int i = 0; i < vectors.Length; i++)
            {
                int idx = i * 3;
                vectors[i] = new Vector3(
                    (float) values[idx],
                    (float) values[idx + 1],
                    (float) values[idx + 2]);
            }
            return vectors;
        }
    }
}

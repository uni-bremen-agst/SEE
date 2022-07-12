using UnityEngine;
using System.Collections.Generic;

namespace SEE.GO.NodeFactories
{
    /// <summary>
    /// A utility class to generate a set of triangles from a list of points
    /// of a closed polygon, P, covering the area of P completely.
    /// It can be used to generate meshes, which consist of triangles.
    /// </summary>
    internal static class Triangulator
    {
        /// <summary>
        /// Given the <paramref name="points"/> of a closed polygon P. This method
        /// returns a set of triangles covering the area of P completely.
        /// The resulting array is a list of triangles that contains indices into
        /// the <paramref name="points"/> array. The size of the resulting triangle
        /// array will be 3 * |points|.
        /// </summary>
        /// <param name="points">the set of points of a closed polygon for
        /// which triangles are to be created completely covering the area
        /// of the polygon</param>
        /// <returns>list of triangles (indices into <paramref name="points"/>></returns>
        public static int[] Triangulate(Vector2[] points)
        {
            List<int> indices = new List<int>();

            int numberOfPoints = points.Length;
            if (numberOfPoints < 3)
            {
                return indices.ToArray();
            }

            int[] vertices = new int[numberOfPoints];
            if (Area(points) > 0)
            {
                for (int v = 0; v < numberOfPoints; v++)
                {
                    vertices[v] = v;
                }
            }
            else
            {
                for (int v = 0; v < numberOfPoints; v++)
                {
                    vertices[v] = (numberOfPoints - 1) - v;
                }
            }

            int nv = numberOfPoints;
            int count = 2 * nv;
            for (int m = 0, v = nv - 1; nv > 2;)
            {
                if ((count--) <= 0)
                {
                    return indices.ToArray();
                }

                int u = v;
                if (nv <= u)
                {
                    u = 0;
                }
                v = u + 1;
                if (nv <= v)
                {
                    v = 0;
                }
                int w = v + 1;
                if (nv <= w)
                {
                    w = 0;
                }

                if (Snip(points, u, v, w, nv, vertices))
                {
                    indices.Add(vertices[u]);
                    indices.Add(vertices[v]);
                    indices.Add(vertices[w]);
                    m++;
                    int s, t;
                    for (s = v, t = v + 1; t < nv; s++, t++)
                    {
                        vertices[s] = vertices[t];
                    }
                    nv--;
                    count = 2 * nv;
                }
            }

            indices.Reverse();
            return indices.ToArray();
        }

        /// <summary>
        /// Returns the area enclosed by <paramref name="points"/>.
        /// </summary>
        /// <param name="points">points of a closed polygon</param>
        /// <returns>area of polygon defined by <paramref name="points"/></returns>
        private static float Area(Vector2[] points)
        {
            int n = points.Length;
            float area = 0.0f;
            for (int p = n - 1, q = 0; q < n; p = q++)
            {
                Vector2 pval = points[p];
                Vector2 qval = points[q];
                area += pval.x * qval.y - qval.x * pval.y;
            }
            return (area * 0.5f);
        }

        /// <summary>
        /// Return whether the triangle defined by the given vertex indices needs to be snipped.
        /// </summary>
        /// <param name="points">the set of points of a closed polygon for
        /// which triangles are to be created completely covering the area
        /// of the polygon</param>
        /// <param name="u">first vertex index of triangle (index for <paramref name="points"/>)</param>
        /// <param name="v">second vertex index of triangle (index for <paramref name="points"/>)</param>
        /// <param name="w">third vertex index of triangle (index for <paramref name="points"/>)</param>
        /// <param name="n">number of points</param>
        /// <param name="vertices">vertex indices of triangls into <paramref name="points"/></param>
        /// <returns>true if the triangle defined by the given vertex indices needs to be snipped</returns>
        private static bool Snip(Vector2[] points, int u, int v, int w, int n, int[] vertices)
        {
            int p;
            Vector2 a = points[vertices[u]];
            Vector2 b = points[vertices[v]];
            Vector2 c = points[vertices[w]];
            if (Mathf.Epsilon > (((b.x - a.x) * (c.y - a.y)) - ((b.y - a.y) * (c.x - a.x))))
            {
                return false;
            }
            for (p = 0; p < n; p++)
            {
                if ((p == u) || (p == v) || (p == w))
                {
                    continue;
                }
                if (InsideTriangle(a, b, c, points[vertices[p]]))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns whether <paramref name="point"/> is enclosed in the triangle formed by
        /// <paramref name="a"/>, <paramref name="b"/>, and <paramref name="c"/>.
        /// </summary>
        /// <param name="a">first point of a triangle</param>
        /// <param name="b">second point of a triangle</param>
        /// <param name="c">third point of a triangle</param>
        /// <param name="point">a point to be checked</param>
        /// <returns>true if <paramref name="point"/> is enclosed in the triangle</returns>
        private static bool InsideTriangle(Vector2 a, Vector2 b, Vector2 c, Vector2 point)
        {
            float ax = c.x - b.x;
            float ay = c.y - b.y;
            float bx = a.x - c.x;
            float by = a.y - c.y;
            float cx = b.x - a.x;
            float cy = b.y - a.y;
            float apx = point.x - a.x;
            float apy = point.y - a.y;
            float bpx = point.x - b.x;
            float bpy = point.y - b.y;
            float cpx = point.x - c.x;
            float cpy = point.y - c.y;

            float aCROSSbp = ax * bpy - ay * bpx;
            float cCROSSap = cx * apy - cy * apx;
            float bCROSScp = bx * cpy - by * cpx;

            return ((aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f));
        }
    }
}

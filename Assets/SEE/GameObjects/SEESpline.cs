using OdinSerializer;
using System;
using System.Collections.Generic;
using TinySpline;
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
        /// What the name says.
        /// </summary>
        [NonSerialized]
        const float PI2 = Mathf.PI * 2f;

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
        /// used to shape a spline, but is less intuitive. We usually don't
        /// care about the values.
        /// </summary>
        public float[] Knots;

        /// <summary>
        /// Internal cache for <see cref="Spline"/>.
        /// </summary>
        [NonSerialized]
        private BSpline Cache;

        /// <summary>
        /// TinySpline's representation of a B-Spline. Note that this property
        /// is cached. It is therefore necessary to propagate back any changes
        /// applied to the returned instance (e.g., when changing the knot
        /// vector of a spline) by calling this property with the updated
        /// spline. If changes are not propagated back, the serialization
        /// attributes (e.g., <see cref="Knots"/>) might be out of sync with
        /// the internal state of <see cref="Spline"/>.
        /// </summary>
        public BSpline Spline
        {
            get
            {
                if (Cache is null)
                {
                    Cache = new BSpline((uint)ControlPoints.Length, 3, Degree)
                    {
                        ControlPoints = TinySplineInterop.VectorsToList(ControlPoints),
                        Knots = TinySplineInterop.ArrayToList(Knots)
                    };
                }
                return Cache;
            }
            set
            {
                Degree = (uint)value.Degree;
                ControlPoints = TinySplineInterop.ListToVectors(value.ControlPoints);
                Knots = TinySplineInterop.ListToArray(value.Knots);
                Cache = Spline;
            }
        }

        /// <summary>
        /// Approximates <see cref="Spline"/> as poly line. The greater
        /// <paramref name="num"/>, the more accurate the approximation.
        /// The poly line can be visualized with <see cref="LineRenderer"/>.
        /// </summary>
        /// <param name="num">Number of vertecies in the poly line</param>
        /// <returns>A poly line approximating <see cref="Spline"/></returns>
        public Vector3[] PolyLine(int num = 100)
        {
            return TinySplineInterop.ListToVectors(Spline.Sample((uint)num));
        }

        /// <summary>
        /// Approximates <see cref="Spline"/> as tubular mesh.
        /// </summary>
        /// <param name="radius">Radius of tube</param>
        /// <param name="markDynamic">If true, the created mesh is marked
        /// dynamic (<see cref="Mesh.MarkDynamic"/>)</param>
        /// <param name="tubularSegments">Number of "rings" along the tube</param>
        /// <param name="radialSegments">Number of vertecies per "ring"</param>
        /// <returns>A tubular mesh approximating <see cref="Spline"/></returns>
        public Mesh Mesh(float radius,
            bool markDynamic = true,
            int tubularSegments = 50,
            int radialSegments = 8)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var tangents = new List<Vector4>();
            var uvs = new List<Vector2>();
            var indices = new List<int>();

            // TODO: See todos below
            var rv = Spline.UniformKnotSeq((uint) tubularSegments + 1);
            var frames = Spline.ComputeRMF(rv);

            void GenerateSegment(int i)
            {
                var fr = frames.At((uint)i);

                var p = TinySplineInterop.VectorToVector(fr.Position());
                var N = TinySplineInterop.VectorToVector(fr.Normal());
                var B = TinySplineInterop.VectorToVector(fr.Binormal());

                for (int j = 0; j <= radialSegments; j++)
                {
                    float v = 1f * j / radialSegments * PI2;
                    var sin = Mathf.Sin(v);
                    var cos = Mathf.Cos(v);

                    Vector3 normal = (cos * N + sin * B).normalized;
                    vertices.Add(p + radius * normal);
                    normals.Add(normal);

                    var tangent = TinySplineInterop.VectorToVector(fr.Tangent());
                    tangents.Add(new Vector4(tangent.x, tangent.y, tangent.z, 0f));
                }
            }

            for (int i = 0; i < tubularSegments; i++)
            {
                GenerateSegment(i);
            }
            // TODO: Isn't this one too many?
            GenerateSegment(tubularSegments);

            // TODO: Isn't this one too many?
            for (int i = 0; i <= tubularSegments; i++)
            {
                for (int j = 0; j <= radialSegments; j++)
                {
                    float u = 1f * j / radialSegments;
                    float v = 1f * i / tubularSegments;
                    uvs.Add(new Vector2(u, v));
                }
            }

            for (int j = 1; j <= tubularSegments; j++)
            {
                for (int i = 1; i <= radialSegments; i++)
                {
                    int a = (radialSegments + 1) * (j - 1) + (i - 1);
                    int b = (radialSegments + 1) * j + (i - 1);
                    int c = (radialSegments + 1) * j + i;
                    int d = (radialSegments + 1) * (j - 1) + i;

                    // faces
                    indices.Add(a); indices.Add(d); indices.Add(b);
                    indices.Add(b); indices.Add(d); indices.Add(c);
                }
            }

            var mesh = new Mesh();
            if (markDynamic)
            {
                mesh.MarkDynamic();
            }
            mesh.vertices = vertices.ToArray();
            mesh.normals = normals.ToArray();
            mesh.tangents = tangents.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
            return mesh;
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

        /// <summary>
        /// Converts TinySpline's Vec3 to Unity's Vector3.
        /// </summary>
        /// <param name="vec3">Vector to be converted</param>
        /// <returns>A Unity Vector3</returns>
        public static Vector3 VectorToVector(Vec3 vec3)
        {
            return new Vector3(
                (float)vec3.X(),
                (float)vec3.Y(),
                (float)vec3.Z());
        }
    }
}
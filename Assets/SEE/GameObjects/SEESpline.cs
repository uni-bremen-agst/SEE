using OdinSerializer;
using SEE.Utils;
using System;
using System.Collections.Generic;
using TinySpline;
using UnityEngine;
using UnityEngine.Assertions;

namespace Assets.SEE.GameObjects
{
    /// <summary>
    /// This class serves as a bridge between TinySpline's representation of
    /// B-Splines and a serializable B-Spline representation that can be
    /// attached to <see cref="GameObject"/>. Note that the attributes related
    /// to Unity (e.g., <see cref="ControlPoints"/>) must not be set directly.
    /// Instead, they must be updated via setting <see cref="Spline"/>.
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
        private BSpline cache;

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
                if (cache is null)
                {
                    cache = TinySplineInterop.Deserialize(
                        Degree, ControlPoints, Knots);
                }
                return cache;
            }
            set
            {
                TinySplineInterop.Serialize(value,
                    out Degree, out ControlPoints, out Knots);
                cache = value;
            }
        }

        /// <summary>
        /// Approximates <see cref="Spline"/> as poly line. The greater
        /// <paramref name="num"/>, the more accurate the approximation.
        /// The poly line can be visualized with a <see cref="LineRenderer"/>.
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

                var p = TinySplineInterop.VectorToVector(fr.Position);
                var N = TinySplineInterop.VectorToVector(fr.Normal);
                var B = TinySplineInterop.VectorToVector(fr.Binormal);

                for (int j = 0; j <= radialSegments; j++)
                {
                    float v = 1f * j / radialSegments * PI2;
                    var sin = Mathf.Sin(v);
                    var cos = Mathf.Cos(v);

                    Vector3 normal = (cos * N + sin * B).normalized;
                    vertices.Add(p + radius * normal);
                    normals.Add(normal);

                    var tangent = TinySplineInterop.VectorToVector(fr.Tangent);
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
    /// This class can be used to morph the <see cref="SEESpline"/> component
    /// of a <see cref="GameObject"/>. The morphism is initialized with
    /// <see cref="InitMorph(BSpline, BSpline)"/> and evaluated with
    /// <see cref="Eval(double)"/>.
    /// </summary>
    public class SplineMorphism : SerializedMonoBehaviour
    {
        /*
         * Attributes of the source spline.
         * (see SEESpline for more details)
         */
        public uint SourceDegree;
        public Vector3[] SourceControlPoints;
        public float[] SourceKnots;
        [NonSerialized]
        private BSpline sourceCache;

        /*
         * Attributes of the target spline.
         * (see SEESpline for more details)
         */
        public uint TargetDegree;
        public Vector3[] TargetControlPoints;
        public float[] TargetKnots;
        [NonSerialized]
        private BSpline targetCache;

        /// <summary>
        /// TinySpline's spline morphism.
        /// </summary>
        [NonSerialized]
        private Morphism morphism;

        /// <summary>
        /// Stores the last time parameter passed to
        /// <see cref="Eval(double)"/>. The default value is 0.
        /// </summary>
        public double Time = 0;

        /// <summary>
        /// Initializes the spline morphism. Asserts that
        /// <paramref name="source"/> and <paramref name="target"/> are not
        /// null.
        /// </summary>
        /// <param name="source">Origin of the spline morphsim</param>
        /// <param name="target">Target of the spline morphism</param>
        public void InitMorph(BSpline source, BSpline target)
        {
            TinySplineInterop.Serialize(source,
                out SourceDegree, out SourceControlPoints, out SourceKnots);
            sourceCache = source;
            TinySplineInterop.Serialize(target,
                out TargetDegree, out TargetControlPoints, out TargetKnots);
            targetCache = target;
            morphism = source.MorphTo(target);
            Time = 0;
        }

        /// <summary>
        /// Evaluates the morphism at the time parameter <see cref="t"/>
        /// (domain [0, 1]; clamped if necessary) and updates the
        /// <see cref="SEESpline"/>, <see cref="LineRenderer"/>, and
        /// <see cref="MeshFilter"/> component of the <see cref="GameObject"/>
        /// this morphism is attached to. Does not fail if any of these
        /// components is missing. The returned <see cref="BSpline"/> instance
        /// is a deep copy of the morphism result and can be used by the
        /// caller for further calculations.
        /// </summary>
        /// <param name="t">The time parameter. Clamped to domain [0, 1]</param>
        /// <returns>Linear interpolation of source and target at t</returns>
        public BSpline Eval(double t)
        {
            if (morphism == null)
            { // morphism cannot be serialized
                // sourceCache and targetCache should also be null
                sourceCache = TinySplineInterop.Deserialize(
                    SourceDegree, SourceControlPoints, SourceKnots);
                targetCache = TinySplineInterop.Deserialize(
                    TargetDegree, TargetControlPoints, TargetKnots);
                morphism = sourceCache.MorphTo(targetCache);
            }

            Time = t;
            BSpline interpolated = morphism.Eval(t);
            if (gameObject.TryGetComponent<SEESpline>(out SEESpline spline))
            {
                spline.Spline = interpolated;
                // Update line renderer.
                if (gameObject.TryGetComponent<LineRenderer>(out LineRenderer lineRenderer))
                {
                    Vector3[] polyLine = spline.PolyLine();
                    lineRenderer.positionCount = polyLine.Length;
                    lineRenderer.SetPositions(polyLine);
                }
                // Update mesh.
                if (gameObject.TryGetComponent<MeshFilter>(out MeshFilter meshFilter))
                {
                    // TODO
                }
            }
            else
            {
                Debug.LogWarning("gameObject without SEESpline component");
            }
            // Protect internal state of `spline'.
            return new BSpline(spline.Spline);
        }
    }
}
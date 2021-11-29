using Assets.SEE.Game.Evolution.Animators;
using OdinSerializer;
using SEE.Utils;
using System;
using System.Collections.Generic;
using TinySpline;
using UnityEngine;

namespace Assets.SEE.GameObjects
{

    /// <summary>
    /// This class serves as a bridge between TinySpline's representation of
    /// B-Splines and a serializable B-Spline representation that can be
    /// attached to <see cref="GameObject"/>.
    /// </summary>
    public class SEESpline : SerializedMonoBehaviour
    {
        /// <summary>
        /// What the name says.
        /// </summary>
        [NonSerialized]
        private const float PI2 = Mathf.PI * 2f;

        /// <summary>
        /// The shaping spline.
        /// </summary>
        [NonSerialized]
        private BSpline spline;

        /// <summary>
        /// Property of <see cref="spline"/>. If set, the
        /// <see cref="LineRenderer"/> and <see cref="MeshFilter"/> component
        /// of the <see cref="GameObject"/> this spline is attached to is
        /// updated. Does not fail if any of these components is missing. Note
        /// that the returned <see cref="BSpline"/> instance is NOT a copy of
        /// <see cref="spline"/>. Hence, treat it well and don't forget to set
        /// this property after applying changes.
        /// </summary>
        public BSpline Spline
        {
            get { return spline; }
            set
            {
                spline = value;
                // Update line renderer (if any).
                if (gameObject.TryGetComponent<LineRenderer>(out LineRenderer lineRenderer))
                {
                    Vector3[] polyLine = PolyLine();
                    lineRenderer.positionCount = polyLine.Length;
                    lineRenderer.SetPositions(polyLine);
                }
                // Update mesh (if any).
                if (gameObject.TryGetComponent<MeshFilter>(out MeshFilter meshFilter))
                {
                    // TODO
                }
            }
        }

        /// <summary>
        /// Serializable representation of <see cref="Spline"/>.
        /// </summary>
        [SerializeField]
        private SerializableSpline serializableSpline;

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

        protected override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            serializableSpline = TinySplineInterop.Serialize(Spline);
        }

        protected override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            Spline = TinySplineInterop.Deserialize(serializableSpline);
        }
    }

    /// <summary>
    /// This class can be used to morph the <see cref="SEESpline"/> component
    /// of a <see cref="GameObject"/>. A spline morphism can be thought of as
    /// a linear interpolation between a `source' and a `target' spline (where
    /// `source' and `target' can have any structure). To initialize the
    /// morphism, call <see cref="Init(BSpline, BSpline)"/> with desired
    /// source and target. To evaluate the morphism at a certain point, call
    /// <see cref="Morph(double)"/> with corresponding time parameter. Note
    /// that this class implements the <see cref="EdgeAnimator.IEvaluator"/>
    /// interface and therefore is well suited to realize the edge animation
    /// of <see cref="SEE.Game.EvolutionRenderer"/>.
    /// </summary>
    public class SplineMorphism :
        SerializedMonoBehaviour, EdgeAnimator.IEvaluator
    {
        [NonSerialized]
        private BSpline source;

        [SerializeField]
        private SerializableSpline serializableSource;

        [NonSerialized]
        private BSpline target;

        [SerializeField]
        private SerializableSpline serializableTarget;

        /// <summary>
        /// TinySpline's spline morphism.
        /// </summary>
        [NonSerialized]
        private Morphism morphism;

        /// <summary>
        /// Initializes the spline morphism. Asserts that
        /// <paramref name="source"/> and <paramref name="target"/> are not
        /// null.
        /// </summary>
        /// <param name="source">Origin of the spline morphsim</param>
        /// <param name="target">Target of the spline morphism</param>
        public void Init(BSpline source, BSpline target)
        {
            this.source = source;
            this.target = target;
            morphism = source.MorphTo(target);
        }

        /// <summary>
        /// Evaluates the morphism at the time parameter <see cref="t"/>
        /// (domain [0, 1]; clamped if necessary) and updates the
        /// <see cref="SEESpline"/> component of the <see cref="GameObject"/>
        /// this morphism is attached to. Does not fail if the
        /// <see cref="GameObject"/> has no <see cref="SEESpline"/> component.
        /// The returned <see cref="BSpline"/> instance is a deep copy of the
        /// morphism result and can be used by the caller for further
        /// calculations.
        /// </summary>
        /// <param name="t">Time parameter; clamped to domain [0, 1]</param>
        /// <returns>Linear interpolation of source and target at t</returns>
        public BSpline Morph(double t)
        {
            if (gameObject.TryGetComponent<SEESpline>(out SEESpline spline))
            {
                spline.Spline = morphism.Eval(t);
            }
            else
            {
                Debug.LogWarning("gameObject without SEESpline component");
            }
            // Protect internal state of `spline'.
            return new BSpline(spline.Spline);
        }

        // Implementation of IEvaluator interface.
        public void Eval(float t)
        {
            Morph(t);
        }

        protected override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            serializableSource = TinySplineInterop.Serialize(source);
            serializableTarget = TinySplineInterop.Serialize(target);
        }

        protected override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            source = TinySplineInterop.Deserialize(serializableSource);
            target = TinySplineInterop.Deserialize(serializableTarget);
            morphism = source.MorphTo(target);
        }
    }
}
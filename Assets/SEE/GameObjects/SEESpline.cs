using OdinSerializer;
using SEE.GO;
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
    /// attached to instances of <see cref="GameObject"/> (usually objects
    /// representing an edge). It also provides functions for rendering
    /// splines and keeps the rendering in sync with the internal state. That
    /// is, whenever one of the public properties of this class is set (e.g.,
    /// <see cref="Spline"/>), the internal state is marked dirty and the
    /// rendering is updated in the next frame (via <see cref="Update"/>).
    /// There are two rendering methods:
    ///
    /// 1. <see cref="LineRenderer"/>: The spline is rendered as polyline.
    /// This method is comparatively fast, but lacks more advanced features
    /// such as collision detection. It serves as a placeholder until the
    /// runtime environment found the time to create a <see cref="Mesh"/>
    /// (along with <see cref="MeshRenderer"/>, <see cref="MeshFilter"/>,
    /// <see cref="MeshCollider"/> etc.). This class doesn't create
    /// <see cref="LineRenderer"/> instances on its own, but rather updates
    /// them if they are present.
    ///
    /// 2. <see cref="Mesh"/>: The spline is rendered as tubular mesh. This
    /// method is a lot slower than <see cref="LineRenderer"/>, but in
    /// contrast creates "real" 3D objects with collision detection. Because
    /// the creation of a larger amount of meshes is quite slow, it is up to
    /// an external client to replace any <see cref="LineRenderer"/> with a
    /// <see cref="MeshRenderer"/>. For this purpose there is the method
    /// <see cref="CreateMesh"/>.
    ///
    /// The geometric characteristics of the generated mesh, e.g., the radius
    /// of the tube, can be set via properties. By setting a property, the
    /// rendering of the spline is updated in the next frame. If an update
    /// needs to be applied immediatly, call <see cref="UpdateMesh"/> after
    /// setting one or more properties.
    /// </summary>
    public class SEESpline : SerializedMonoBehaviour
    {
        /// <summary>
        /// What the name says.
        /// </summary>
        [NonSerialized]
        private const float PI2 = Mathf.PI * 2f;

        /// <summary>
        /// Indicates whether the rendering of <see cref="spline"/> must be
        /// updated (as a result of setting one of the public properties).
        /// </summary>
        private bool needsUpdate = false;

        /// <summary>
        /// The shaping spline.
        /// </summary>
        [NonSerialized]
        private BSpline spline;

        /// <summary>
        /// Property of <see cref="spline"/>. The returned instance is NOT a
        /// copy of <see cref="spline"/>. Hence, treat it well and don't
        /// forget to set this property after modifying the returned instance.
        /// </summary>
        public BSpline Spline
        {
            get { return spline; }
            set
            {
                spline = value;
                needsUpdate = true;
            }
        }

        /// <summary>
        /// Serializable representation of <see cref="Spline"/>.
        /// </summary>
        [SerializeField]
        private SerializableSpline serializableSpline;

        /// <summary>
        /// Radius of the mesh to be created (<see cref="CreateMesh"/>) or
        /// updated <see cref="UpdateMesh"/>).
        /// </summary>
        [SerializeField, Min(0.0001f)]
        private float radius = 0.01f; // default value

        /// <summary>
        /// Property of <see cref="radius"/>. Domain: [0.0001f, inf]
        /// </summary>
        public float Radius
        {
            get { return radius; }
            set
            {
                radius = Math.Max(0.0001f, value);
                needsUpdate = true;
            }
        }

        /// <summary>
        /// Number of tubular segments (number of radial polygons along the
        /// spline) of the mesh to be created (<see cref="CreateMesh"/>) or
        /// updated (<see cref="UpdateMesh"/>).
        /// </summary>
        [SerializeField, Min(5)]
        public int tubularSegments = 50; // default value; based on Holten

        /// <summary>
        /// Property of <see cref="tubularSegments"/>. Domain [5, inf]
        /// </summary>
        public int TubularSegments
        {
            get { return tubularSegments; }
            set
            {
                var tmp = Math.Max(5, value);
                if (tubularSegments != tmp)
                {
                    tubularSegments = tmp;
                    needsUpdate = true;
                }
            }
        }

        /// <summary>
        /// Number of radial segments (number of vertices around the spline)
        /// of the mesh to be created (<see cref="CreateMesh"/>) or updated
        /// (<see cref="UpdateMesh"/>).
        /// </summary>
        [SerializeField, Min(3)]
        public int radialSegments = 8; // default value; octagon

        /// <summary>
        /// Property of <see cref="radialSegments"/>. Domain: [3, inf]
        /// </summary>
        public int RadialSegments
        {
            get { return radialSegments; }
            set
            {
                var tmp = Math.Max(3, value);
                if (radialSegments != tmp)
                {
                    radialSegments = tmp;
                    needsUpdate = true;
                }
            }
        }

        /// <summary>
        /// The default material to be used for splines whose
        /// <see cref="Mesh"/> has just been created (used by
        /// <see cref="UpdateMaterial"/>).
        /// </summary>
        [SerializeField]
        private Material defaultMaterial;

        /// <summary>
        /// Called by Unity when an instance of this class is being loaded.
        /// </summary>
        private void Awake()
        {
            // Corresponds to the material of the LineRenderer.
            defaultMaterial = Materials.New(
                Materials.ShaderType.TransparentLine, Color.white);
        }

        /// <summary>
        /// Called by Unity after one of the serializable fields (e.g.,
        /// <see cref="radius"/>) has been updated in the editor. Marks the
        /// internal state as dirty, forcing an update in the next frame.
        /// </summary>
        private void OnValidate()
        {
            needsUpdate = true;
        }

        /// <summary>
        /// Updates the rendering of the spline if the internal state is
        /// marked dirty (i.e., <see cref="needsUpdate"/> is true).
        /// </summary>
        private void Update()
        {
            if (needsUpdate)
            {
                UpdateLineRenderer();
                UpdateMesh();
                needsUpdate = false;
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
        /// Updates the <see cref="LineRenderer"/> of the
        /// <see cref="GameObject"/> this component is attached to
        /// (<see cref="Component.gameObject"/>) and marks the internal state
        /// as clean (i.e., <see cref="needsUpdate"/> is set to false) so that
        /// <see cref="Update"/> doesn't update the renderer again in the next
        /// frame. Calling this method doesn't fail if
        /// <see cref="Component.gameObject"/> has no
        /// <see cref="LineRenderer"/> attached to it.
        /// </summary>
        private void UpdateLineRenderer()
        {
            if (gameObject.TryGetComponent<LineRenderer>(out var lr))
            {
                var polyLine = PolyLine(lr.positionCount);
                lr.positionCount = polyLine.Length;
                lr.SetPositions(polyLine);
            }
            needsUpdate = false;
        }

        /// <summary>
        /// Create or update the spline mesh (a tube) and replace any
        /// <see cref="LineRenderer"/> with the necessary mesh components
        /// (<see cref="MeshFilter"/>, <see cref="MeshCollider"/> etc.).
        /// </summary>
        /// <returns>The created or updated mesh</returns>
        private Mesh CreateOrUpdateMesh()
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var tangents = new List<Vector4>();
            var uvs = new List<Vector2>();
            var indices = new List<int>();

            // TODO: See todos below
            // It is much more efficient to generate uniform knots than
            // equidistant knots. Besides, you can't see the difference
            // anyway. For the curious among you: With uniform knots, the
            // distance between neighboring frames along the spline is not
            // equal.
            var rv = Spline.UniformKnotSeq((uint)tubularSegments + 1);
            var frames = Spline.ComputeRMF(rv);

            // Helper function. Creates a radial polygon for frame `i'.
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

            // Radial polygons
            // TODO: Isn't this one too many?
            for (int i = 0; i < tubularSegments; i++)
            {
                GenerateSegment(i);
            }
            GenerateSegment(tubularSegments);

            // U-v-vectors
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

            // Indices (faces)
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

            // Set up the mesh components.
            Mesh mesh; // The mesh to work on.
            bool updateMaterial = false; // Whether to call `UpdateMaterial'.
            if (!gameObject.TryGetComponent(out MeshFilter filter))
            {
                filter = gameObject.AddComponent<MeshFilter>();
                if (!gameObject.TryGetComponent(out MeshCollider collider))
                {
                    collider = gameObject.AddComponent<MeshCollider>();
                }
                mesh = new Mesh();
                mesh.MarkDynamic(); // May improve performance.
                filter.sharedMesh = mesh;
                collider.sharedMesh = mesh;
                updateMaterial = true;
            }
            mesh = filter.mesh;
            updateMaterial = updateMaterial // Implies new mesh.
                || // Or the geometrics of the mesh have changed.
                (mesh.vertices.Length != vertices.Count ||
                 mesh.normals.Length  != normals.Count  ||
                 mesh.tangents.Length != tangents.Count ||
                 mesh.uv.Length != uvs.Count);
            if (updateMaterial)
            {
                mesh.Clear();
            }
            mesh.vertices = vertices.ToArray();
            mesh.normals  = normals.ToArray();
            mesh.tangents = tangents.ToArray();
            mesh.uv       = uvs.ToArray();
            mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
            if (!gameObject.TryGetComponent(out MeshRenderer _))
            {
                gameObject.AddComponent<MeshRenderer>();
            }
            if (updateMaterial)
            {
                UpdateMaterial();
            }

            // Remove line renderer.
            if (gameObject.TryGetComponent(out LineRenderer renderer))
            {
                Destroy(renderer);
            }

            return mesh;
        }

        /// <summary>
        /// Update the material of the <see cref="MeshRenderer"/> created by
        /// <see cref="CreateOrUpdateMesh"/>.
        /// </summary>
        protected virtual void UpdateMaterial()
        {
            if (!gameObject.TryGetComponent(out MeshFilter filter) ||
                !gameObject.TryGetComponent(out MeshRenderer renderer))
            {
                return;
            }

            if (renderer.sharedMaterial == null)
            {
                renderer.sharedMaterial = defaultMaterial;
            }
            if (renderer.sharedMaterial.shader == defaultMaterial.shader)
            { // Don't re-color non-default material.
                Mesh mesh = filter.mesh;
                Vector2[] uv = mesh.uv;
                Color[] colors = new Color[uv.Length];
                for (var i = 0; i < uv.Length; i++)
                {
                    colors[i] = Color.Lerp(Color.red, Color.green, uv[i].y);
                }
                mesh.colors = colors;
            }
        }

        /// <summary>
        /// Enables mesh rendering and removes any <see cref="LineRenderer"/>
        /// attached to the <see cref="GameObject"/> this component is
        /// attached to (<see cref="Component.gameObject"/>). Returns the mesh
        /// of <see cref="Component.gameObject"/> (without any updates applied
        /// to it) if mesh rendering is already enabled (i.e.,
        /// <see cref="Component.gameObject"/> has a <see cref="MeshFilter"/>
        /// attached to it). In order to update a mesh, set any of the public
        /// properties of this class, e.g., <see cref="Radius"/>. The update
        /// is then applied in the next frame (via <see cref="Update"/>). Or
        /// use <see cref="UpdateMesh"/> to update the mesh immediately.
        /// </summary>
        /// <returns>A mesh approximating <see cref="Spline"/></returns>
        public Mesh CreateMesh()
        {
            if (gameObject.TryGetComponent<MeshFilter>(out var filter))
            {
                return filter.mesh;
            }
            var mesh = CreateOrUpdateMesh();
            needsUpdate = false; // apparently
            return mesh;
        }

        /// <summary>
        /// Updates the mesh rendering and marks the internal state as clean
        /// (i.e., <see cref="needsUpdate"/> is set to false) so that
        /// <see cref="Update"/> doesn't update the mesh again in the next
        /// frame. Calling this method doesn't fail if mesh rendering has not
        /// been enabled yet (i.e., there is no <see cref="MeshFilter"/>
        /// attached to <see cref="Component.gameObject"/>; see
        /// <see cref="CreateMesh"/> for more details).
        /// </summary>
        public void UpdateMesh()
        {
            if (gameObject.TryGetComponent<MeshFilter>(out var _))
            {
                CreateOrUpdateMesh();
            }
            needsUpdate = false;
        }

        protected override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            serializableSpline = TinySplineInterop.Serialize(Spline);
        }

        protected override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            spline = TinySplineInterop.Deserialize(serializableSpline);
        }
    }

    /// <summary>
    /// This class can be used to morph the <see cref="SEESpline"/> component
    /// of a <see cref="GameObject"/>. A spline morphism can be thought of as
    /// a linear interpolation between a `source' and a `target' spline (where
    /// `source' and `target' may have any structure with regard to degree,
    /// control points, and knots). To initialize the morphism, call
    /// <see cref="Init(BSpline, BSpline)"/> with desired source and target
    /// spline. To evaluate the morphism at a certain point, call
    /// <see cref="Morph(double)"/> with corresponding time parameter.
    ///
    /// By implementing the <see cref="EdgeAnimator.IEvaluator"/> interface,
    /// spline morphisms can used by <see cref="EdgeAnimator"/> to run linear
    /// edge animations.
    /// </summary>
    public class SplineMorphism :
        SerializedMonoBehaviour, EdgeAnimator.IEvaluator
    {
        /// <summary>
        /// Origin of the spline morphism.
        /// </summary>
        [NonSerialized]
        private BSpline source;

        /// <summary>
        /// Serializable representation of <see cref="source"/>.
        /// </summary>
        [SerializeField]
        private SerializableSpline serializableSource;

        /// <summary>
        /// Target of the spline morphism.
        /// </summary>
        [NonSerialized]
        private BSpline target;

        /// <summary>
        /// Serializable representation of <see cref="target"/>.
        /// </summary>
        [SerializeField]
        private SerializableSpline serializableTarget;

        /// <summary>
        /// TinySpline's spline morphism.
        /// </summary>
        [NonSerialized]
        private Morphism morphism;

        /// <summary>
        /// Initializes the spline morphism.
        ///
        /// Postcondition: <see cref="SEESpline"/> is morphed to
        /// <paramref name="source"/>.
        /// </summary>
        /// <param name="source">Origin of the spline morphsim</param>
        /// <param name="target">Target of the spline morphism</param>
        public void Init(BSpline source, BSpline target)
        {
            this.source = source;
            this.target = target;
            morphism = source.MorphTo(target);
            Morph(0); // Morph to source.
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
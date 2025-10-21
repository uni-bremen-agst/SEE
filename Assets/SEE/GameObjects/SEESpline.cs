using SEE.Utils;
using System;
using System.Collections.Generic;
using DG.Tweening;
using SEE.Game;
using SEE.Game.Operator;
using TinySpline;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Rendering;
using Frame = TinySpline.Frame;

namespace SEE.GO
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
    /// <list type="number"><item>
    /// <see cref="LineRenderer"/>: The spline is rendered as polyline.
    /// This method is comparatively fast, but lacks more advanced features
    /// such as collision detection. It serves as a placeholder until the
    /// runtime environment found the time to create a <see cref="Mesh"/>
    /// (along with <see cref="MeshRenderer"/>, <see cref="MeshFilter"/>,
    /// <see cref="MeshCollider"/> etc.). This class doesn't create
    /// <see cref="LineRenderer"/> instances on its own, but rather updates
    /// them if they are present.
    /// </item><item>
    /// <see cref="Mesh"/>: The spline is rendered as tubular mesh. This
    /// method is a lot slower than <see cref="LineRenderer"/>, but in
    /// contrast creates "real" 3D objects with collision detection. Because
    /// the creation of a larger amount of meshes is quite slow, it is up to
    /// an external client to replace any <see cref="LineRenderer"/> with a
    /// <see cref="MeshRenderer"/>. For this purpose there is the method
    /// <see cref="CreateMesh"/>.
    /// </item></list>
    /// The geometric characteristics of the generated mesh, e.g., the radius
    /// of the tube, can be set via properties. By setting a property, the
    /// rendering of the spline is updated in the next frame. If an update
    /// needs to be applied immediately, call <see cref="UpdateMesh"/> after
    /// setting one or more properties.
    /// </summary>
    public class SEESpline : SerializedMonoBehaviour
    {
        /// <summary>
        /// What the name says.
        /// </summary>
        [NonSerialized]
        private const float doublePi = Mathf.PI * 2f;

        /// <summary>
        /// Indicates whether the rendering of <see cref="spline"/> must be
        /// updated (as a result of setting one of the public properties).
        /// </summary>
        private bool needsCompleteUpdate;

        /// <summary>
        /// Indicates whether the color of <see cref="spline"/> must be updated.
        /// </summary>
        private bool needsColorUpdate;

        /// <summary>
        /// Indicates whether the collider of <see cref="spline"/> must be updated.
        /// <para>
        /// If <see cref="needsCompleteUpdate"/> is not set, <see cref="UpdateCollider"/> will be called for a
        /// more lightweight update compared to a full regeneration of the procedurally generated mesh:
        /// </para><para>
        /// If <see cref="IsSelectable"/> was set to <c>false</c>, this will inactivate any existing collider
        /// instead of regenerating the spline.
        /// If <see cref="IsSelectable"/> was set to <c>true</c>, this will activate the collider if available
        /// or create one using the existing shared mesh without regenerating it.
        /// </para>
        /// </summary>
        private bool needsSelectableUpdate;

        /// <summary>
        /// Indicates whether the visible segment of the <see cref="spline"/> must be updated.
        /// </summary>
        private bool needsVisibleSegmentUpdate;

        /// <summary>
        /// The shaping spline.
        /// </summary>
        [NonSerialized]
        private BSpline spline;

        /// <summary>
        /// Backing field for the <see cref="VisibleSegmentStart"/> property.
        /// </summary>
        [SerializeField]
        private float visibleSegmentStart;

        /// <summary>
        /// The start position of the visible subspline, e.g., for a build-up animation (element of [0..1]).
        /// </summary>
        public float VisibleSegmentStart
        {
            get => visibleSegmentStart;
            set
            {
                if (value < 0f || value > 1f)
                {
                    throw new ArgumentOutOfRangeException(nameof(VisibleSegmentStart), value, "Value must be between 0 and 1.");
                }

                visibleSegmentStart = value;
                needsVisibleSegmentUpdate = true;
            }
        }

        /// <summary>
        /// Backing field for the <see cref="visibleSegmentEnd"/> property.
        /// </summary>
        [SerializeField]
        private float visibleSegmentEnd = 1.0f;

        /// <summary>
        /// The end position of the visible subspline, e.g., for a build-up animation (element of [0..1]).
        /// </summary>
        public float VisibleSegmentEnd
        {
            get => visibleSegmentEnd;
            set
            {
                if (value < 0f || value > 1f)
                {
                    throw new ArgumentOutOfRangeException(nameof(VisibleSegmentEnd), value, "Value must be between 0 and 1.");
                }

                visibleSegmentEnd = value;
                needsVisibleSegmentUpdate = true;
            }
        }

        /// <summary>
        /// The event is emitted each time the renderer is updated (see <see cref="needsCompleteUpdate"/>).
        /// </summary>
        public event Action OnRendererChanged;

        /// <summary>
        /// Property of <see cref="spline"/>. The returned instance is NOT a
        /// copy of <see cref="spline"/>. Hence, treat it well and don't
        /// forget to set this property after modifying the returned instance.
        /// </summary>
        public BSpline Spline
        {
            get => spline;
            set
            {
                spline = value;
                needsCompleteUpdate = true;
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
            get => radius;
            set
            {
                radius = Math.Max(0.0001f, value);
                needsCompleteUpdate = true;
            }
        }

        /// <summary>
        /// Number of tubular segments (number of radial polygons along the
        /// spline) of the mesh to be created (<see cref="CreateMesh"/>) or
        /// updated (<see cref="UpdateMesh"/>).
        /// </summary>
        [SerializeField, Min(5)]
        private int tubularSegments = 50; // default value; based on Holten

        /// <summary>
        /// Property of <see cref="tubularSegments"/>. Domain [5, inf]
        /// </summary>
        public int TubularSegments
        {
            get => tubularSegments;
            set
            {
                int max = Math.Max(5, value);
                if (tubularSegments != max)
                {
                    tubularSegments = max;
                    needsCompleteUpdate = true;
                }
            }
        }

        /// <summary>
        /// Number of radial segments (number of vertices around the spline)
        /// of the mesh to be created (<see cref="CreateMesh"/>) or updated
        /// (<see cref="UpdateMesh"/>).
        /// </summary>
        [SerializeField, Min(3)]
        private int radialSegments = 8; // default value; octagon

        /// <summary>
        /// Property of <see cref="radialSegments"/>. Domain: [3, inf]
        /// </summary>
        public int RadialSegments
        {
            get => radialSegments;
            set
            {
                int max = Math.Max(3, value);
                if (radialSegments != max)
                {
                    radialSegments = max;
                    needsCompleteUpdate = true;
                }
            }
        }

        /// <summary>
        /// Whether the spline shall be selectable, that is, whether a <see cref="MeshCollider"/> shall be added to it.
        /// </summary>
        [SerializeField]
        private bool isSelectable = true;

        /// <summary>
        /// Whether the spline shall be selectable, that is, whether a <see cref="MeshCollider"/> shall be added to it.
        /// </summary>
        public bool IsSelectable
        {
            get => isSelectable;
            set
            {
                isSelectable = value;
                needsSelectableUpdate = true;
            }
        }

        /// <summary>
        /// Tuple of the start color of the gradient and the end color of it.
        /// Should only be changed via <see cref="GradientColors"/>.
        /// </summary>
        [SerializeField]
        private (Color start, Color end) gradientColors = (Color.red, Color.green);

        /// <summary>
        /// Tuple of the start color of the gradient and the end color of it.
        /// </summary>
        public (Color start, Color end) GradientColors
        {
            get => gradientColors;
            set
            {
                gradientColors = value;
                needsColorUpdate = true;
            }
        }

        /// <summary>
        /// The mesh renderer behind this spline. May be null if this spline uses a line renderer.
        /// </summary>
        private MeshRenderer meshRenderer;

        /// <summary>
        /// The line renderer behind this spline. May be null if this spline uses a mesh renderer.
        /// </summary>
        private LineRenderer lineRenderer;

        /// <summary>
        /// The default material to be used for splines whose
        /// <see cref="Mesh"/> has just been created (used by
        /// <see cref="UpdateMaterial"/>).
        /// </summary>
        [SerializeField]
        private Material defaultMaterial;

        /// <summary>
        /// The material this edge uses, if it uses a mesh renderer.
        /// If it does not, using this property causes a mesh to be created.
        /// </summary>
        public Material MeshMaterial
        {
            get
            {
                if (meshRenderer == null)
                {
                    CreateMesh();
                }

                return meshRenderer.sharedMaterial;
            }
            set
            {
                if (MeshMaterial != value)
                {
                    meshRenderer.sharedMaterial = value;
                    UpdateMaterial();
                }
            }
        }

        /// <summary>
        /// Shader property that defines the (start) color.
        /// </summary>
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");

        /// <summary>
        /// Shader property that defines the end color of the color gradient.
        /// </summary>
        private static readonly int EndColorProperty = Shader.PropertyToID("_EndColor");

        /// <summary>
        /// Shader property that enables or disables the color gradient.
        /// </summary>
        private static readonly int ColorGradientEnabledProperty = Shader.PropertyToID("_ColorGradientEnabled");

        /// <summary>
        /// Shader property that defines the start of the visible segment.
        /// </summary>
        private static readonly int VisibleStartProperty = Shader.PropertyToID("_VisibleStart");

        /// <summary>
        /// Shader property that defines the end of the visible segment.
        /// </summary>
        private static readonly int VisibleEndProperty = Shader.PropertyToID("_VisibleEnd");

        /// <summary>
        /// Called by Unity when an instance of this class is being loaded.
        /// </summary>
        private void Awake()
        {
            // Corresponds to the material of the LineRenderer.
            defaultMaterial = Materials.New(Materials.ShaderType.TransparentEdge, Color.white);
        }

        /// <summary>
        /// Called by Unity after one of the serializable fields (e.g.,
        /// <see cref="radius"/>) has been updated in the editor. Marks the
        /// internal state as dirty, forcing an update in the next frame.
        /// </summary>
        private void OnValidate()
        {
            needsCompleteUpdate = true;
        }

        /// <summary>
        /// Updates the rendering of the spline if the internal state is
        /// marked dirty (i.e., <see cref="needsCompleteUpdate"/> is true).
        /// </summary>
        private void Update()
        {
            if (needsCompleteUpdate)
            {
                UpdateLineRenderer();
                UpdateMesh();
                needsCompleteUpdate = needsColorUpdate = needsSelectableUpdate = false;
                OnRendererChanged?.Invoke();
            }
            else if (needsColorUpdate)
            {
                UpdateColor();
                needsColorUpdate = false;
            }
            else if (needsSelectableUpdate)
            {
                UpdateCollider();
                needsSelectableUpdate = false;
            }
            else if (needsVisibleSegmentUpdate)
            {
                UpdateVisibleSegment();
            }
        }

        /// <summary>
        /// Changes the last control point of the spline represented by this object to <paramref name="newPosition"/>.
        /// </summary>
        /// <param name="newPosition">The new position the last control point of this spline should have</param>
        public void UpdateEndPosition(Vector3 newPosition) => UpdateControlPoint(spline.NumControlPoints - 1, newPosition);

        /// <summary>
        /// Changes the first control point of the spline represented by this object to <paramref name="newPosition"/>.
        /// </summary>
        /// <param name="newPosition">The new position the first control point of this spline should have</param>
        public void UpdateStartPosition(Vector3 newPosition) => UpdateControlPoint(0, newPosition);

        /// <summary>
        /// Changes the control point at <paramref name="index"/> to the given <paramref name="newControlPoint"/>.
        /// </summary>
        /// <param name="index">Index of the control point which is to be changed</param>
        /// <param name="newControlPoint">New value for the control point at <paramref name="index"/></param>
        private void UpdateControlPoint(uint index, Vector3 newControlPoint)
        {
            spline.SetControlPointVec3At(index, new Vec3(newControlPoint.x, newControlPoint.y, newControlPoint.z));
            needsCompleteUpdate = true;
        }

        /// <summary>
        /// Returns the control point at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">Index of the control point to be returned</param>
        /// <returns>The control point at <paramref name="index"/></returns>
        private Vector3 GetControlPoint(uint index) => TinySplineInterop.VectorToVector(spline.ControlPointVec3At(index));

        /// <summary>
        /// Returns the control point in the middle of the spline.
        /// If the number of control points is even, the control point will not be exactly in the middle.
        /// </summary>
        /// <returns>The control point in the middle of the spline</returns>
        public Vector3 GetMiddleControlPoint() => GetControlPoint(spline.NumControlPoints / 2);

        /// <summary>
        /// Updates the <see cref="LineRenderer"/> of the
        /// <see cref="GameObject"/> this component is attached to
        /// (<see cref="Component.gameObject"/>) and marks the internal state
        /// as clean (i.e., <see cref="needsCompleteUpdate"/> is set to false) so that
        /// <see cref="Update"/> doesn't update the meshRenderer again in the next
        /// frame. Calling this method doesn't fail if
        /// <see cref="Component.gameObject"/> has no
        /// <see cref="LineRenderer"/> attached to it.
        /// </summary>
        private void UpdateLineRenderer()
        {
            if (gameObject.TryGetComponent(out LineRenderer lr))
            {
                lineRenderer = lr;
            }
            if (lineRenderer != null)
            {
                Vector3[] polyLine = GenerateVertices();

                lineRenderer.positionCount = polyLine.Length;
                lineRenderer.SetPositions(polyLine);
                lineRenderer.startColor = gradientColors.start;
                lineRenderer.endColor = gradientColors.end;
            }
            UpdateVisibleSegment();
            needsCompleteUpdate = false;
        }

        /// <summary>
        /// Generates the vertices that represent this spline.
        /// </summary>
        /// <returns>The vertices that make up this spline.</returns>
        public Vector3[] GenerateVertices()
        {
            return TinySplineInterop.ListToVectors(spline.Sample());
        }

        /// <summary>
        /// Updates the start and end color of the line renderer attached
        /// to the gameObject using the values of <see cref="gradientColors"/>
        /// in case there is a line renderer. Otherwise (if <see cref="meshRenderer"/>
        /// is different from <c>null</c>, updates the material via
        /// <see cref="UpdateMaterial"/>.
        /// </summary>
        private void UpdateColor()
        {
            if (lineRenderer != null)
            {
                lineRenderer.startColor = gradientColors.start;
                lineRenderer.endColor = gradientColors.end;
            }
            else if (meshRenderer != null)
            {
                UpdateMaterial();
            }
        }

        /// <summary>
        /// Update the collider to reflect <see cref="IsSelectable"/> state.
        /// <list type="bullet">
        /// <item><description>
        /// If available, the collider is either enabled or disabled.
        /// </description></item><item><description>
        /// If <see cref="IsSelectable"/> is <c>true</c> and no collider is available and a <see cref="MeshFilter"/> is available,
        /// a <see cref="MeshCollider"/> will be created using the available shared mesh.
        /// </description></item>
        /// </list>
        /// </summary>
        private void UpdateCollider()
        {
            if (gameObject.TryGetComponent(out Collider collider))
            {
                collider.enabled = IsSelectable;
                return;
            }

            if (IsSelectable)
            {
                if (!gameObject.TryGetComponent(out MeshFilter filter))
                {
                    Debug.LogWarning("Trying to update selectability without generating a mesh first!");
                    return;
                }
                Mesh mesh = filter.sharedMesh;

                MeshCollider meshCollider = gameObject.AddOrGetComponent<MeshCollider>();
                // IMPORTANT: Null the shared mesh of the collider before assigning the updated mesh.
                // https://forum.unity.com/threads/how-to-update-a-mesh-collider.32467/
                meshCollider.sharedMesh = null; // Do we still need this workaround?
                meshCollider.sharedMesh = mesh;
            }
        }

        /// <summary>
        /// Update the material properties to reflect the visible segment configuration
        /// defined by <see cref="visibleSegmentStart"/> and <see cref="visibleSegmentEnd"/>.
        /// </summary>
        private void UpdateVisibleSegment()
        {
            Material material;
            if (meshRenderer != null)
            {
                material = meshRenderer.sharedMaterial;
            }
            else if (lineRenderer != null)
            {
                material = lineRenderer.sharedMaterial;
            }
            else
            {
                return;
            }

            material.SetFloat(VisibleStartProperty, visibleSegmentStart);
            material.SetFloat(VisibleEndProperty, visibleSegmentEnd);
            needsVisibleSegmentUpdate = false;
        }

        /// <summary>
        /// Create or update the spline mesh (a tube) and replace any
        /// <see cref="LineRenderer"/> with the necessary mesh components
        /// (<see cref="MeshFilter"/>, <see cref="MeshCollider"/> etc.).
        /// </summary>
        /// <returns>The created or updated mesh</returns>
        private Mesh CreateOrUpdateMesh()
        {
            int totalVertices = (tubularSegments + 1) * (radialSegments + 1);
            int totalIndices = tubularSegments * radialSegments * 6;
            Vector3[] vertices = new Vector3[totalVertices];
            Vector3[] normals = new Vector3[totalVertices];
            Vector4[] tangents = new Vector4[totalVertices];
            Vector2[] uvs = new Vector2[totalVertices];
            int[] indices = new int[totalIndices];

            // It is much more efficient to generate uniform knots than
            // equidistant knots. Besides, you can't see the difference
            // anyway. For the curious among you: With uniform knots, the
            // distance between neighboring frames along the spline is not
            // equal.
            IList<double> rv = spline.UniformKnotSeq((uint)tubularSegments + 1);
            FrameSeq frames = spline.ComputeRMF(rv);
            // Precalculated values for the loops later on.
            float radialSegmentsInv = 1f / radialSegments;
            float tubularSegmentsInv = 1f / tubularSegments;
            int segmentPlusOne = radialSegments + 1;

            // The index of the current index in the index array.
            // This is less confusing (except, admittedly, for the name)
            // than calculating this index on-the-fly.
            uint indexIndex = 0;
            // Additionally, the index of the current vertex in the vertex-based arrays
            int index = 0;
            // Pre-calculate sin and cos values.
            float[] sinValues = new float[radialSegments + 1];
            float[] cosValues = new float[radialSegments + 1];

            for (int j = 0; j <= radialSegments; j++)
            {
                float v = j * radialSegmentsInv * doublePi;
                sinValues[j] = Mathf.Sin(v);
                cosValues[j] = Mathf.Cos(v);
            }
            // TODO: This loop is the main culprit for the performance issues when splines are modified.
            //       I have already tried optimizing it (see PR #622), which resulted in promising improvements, more
            //       than doubling FPS from 5 to 13, but we should strive for at least 30. Looking at the profiler,
            //       I see at least two possible avenues for further optimization:
            //       1. The biggest performance hit comes from the fact that we have to interop with the native C code,
            //          specifically that we have to convert a lot of objects back-and-forth between C# and C data
            //          datastructures, and that we have to do so within the unoptimized interop code.
            //          ==> One solution may be to move this part into C code, as it uses no Unity-specific code.
            //          We would thus avoid those interop performance problems.
            //       2. The other big performance hit comes from the many vector operations we have to do,
            //          such as the normalization in the innermost loop. While this is unavoidable, C# does not provide
            //          the most efficient way to do so. Additionally, note that the loop is heavily parallelizable –
            //          the only dependency between iterations is the `index` variable, which can be easily computed
            //          in each iteration.
            //          ==> One solution may be to parallelize this loop. We could either use
            //          Unity's "Parallel jobs" system (https://docs.unity3d.com/Manual/JobSystemParallelForJobs.html)
            //          or if we want to get especially fancy, we could use the GPU to do the calculations, using
            //          Unity's "Compute Shaders" (https://docs.unity3d.com/Manual/class-ComputeShader.html).
            //          While the latter is most likely much more efficient, it is also likely more complicated.
            //          Another open question is whether we want to parallelize not only across spline segments, i.e.,
            //          the loop below, but also across splines themselves (since quite often,
            //          multiple spline operations are carried out at the same time).
            for (uint i = 0; i <= tubularSegments; i++)
            {
                // Create radial polygons for frame 'i'.
                Frame fr = frames.At(i);

                Vector3 frPosition = TinySplineInterop.VectorToVector(fr.Position);
                Vector3 frNormal = TinySplineInterop.VectorToVector(fr.Normal);
                Vector3 frBinormal = TinySplineInterop.VectorToVector(fr.Binormal);
                Vector4 frTangent = TinySplineInterop.VectorToVector(fr.Tangent); // w = 0f by default.

                // TODO: This was previously (before the optimization) implicit behavior. Is this intentional?
                if (float.IsNaN(frNormal.x))
                {
                    frNormal = Vector3.zero;
                }
                if (float.IsNaN(frBinormal.x))
                {
                    frBinormal = Vector3.zero;
                }
                Vector3 radiusNormal = frNormal * radius;
                Vector3 radiusBinormal = frBinormal * radius;

                for (int j = 0; j <= radialSegments; j++)
                {
                    // Generate radial segment of frame `i` for segment `j`.
                    Vector3 normal = cosValues[j] * radiusNormal + sinValues[j] * radiusBinormal;
                    vertices[index] = frPosition + normal;
                    normals[index] = normal; // TODO: Is it fine not to normalize this?
                    tangents[index] = frTangent;
                    // U-v-vectors
                    uvs[index] = new Vector2(j * radialSegmentsInv, i * tubularSegmentsInv);
                    // Indices (faces)
                    if (i >= 1 && j >= 1)
                    {
                        int a = index - segmentPlusOne - 1;
                        int b = index - 1;
                        int c = index;
                        int d = index - segmentPlusOne;

                        // faces
                        indices[indexIndex++] = a;
                        indices[indexIndex++] = d;
                        indices[indexIndex++] = b;
                        indices[indexIndex++] = b;
                        indices[indexIndex++] = d;
                        indices[indexIndex++] = c;
                    }

                    index++;
                }
            }

            // Set up the mesh components.
            Mesh mesh; // The mesh to work on.

            // Does this game object already have a mesh which we can reuse?
            if (gameObject.TryGetComponent(out MeshFilter filter))
            {
                mesh = filter.mesh;
                mesh.Clear();
            }
            else
            {
                // Create a new mesh for this game object.
                mesh = new Mesh();
                mesh.MarkDynamic(); // May improve performance.
                filter = gameObject.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
            }

            // IMPORTANT: Set mesh vertices, normals, tangents etc. before updating the shared mesh of the collider.
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.tangents = tangents;
            mesh.uv = uvs;
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);

            UpdateCollider();

            meshRenderer = gameObject.AddOrGetComponent<MeshRenderer>();
            UpdateMaterial();

            if (lineRenderer != null)
            {
                Destroyer.Destroy(lineRenderer);
                lineRenderer = null;
            }

            return mesh;
        }

        /// <summary>
        /// Update the material of the <see cref="MeshRenderer"/> created by <see cref="CreateOrUpdateMesh"/>.
        /// </summary>
        protected virtual void UpdateMaterial()
        {
            if (meshRenderer == null)
            {
                Debug.LogWarning("Trying to update MeshRenderer material, but there is none!");
                return;
            }

            if (meshRenderer.sharedMaterial == null)
            {
                meshRenderer.sharedMaterial = defaultMaterial;
                Portal.SetPortal(transform.parent.parent.gameObject, gameObject);
            }

            if (meshRenderer.sharedMaterial.shader != defaultMaterial.shader)
            {
                Debug.LogWarning("Cannot update MeshRenderer because the shader does not match!");
                return;
            }

            meshRenderer.material.SetColor(ColorProperty, gradientColors.start);
            meshRenderer.material.SetColor(EndColorProperty, gradientColors.end);
            meshRenderer.material.SetFloat(ColorGradientEnabledProperty, 1.0f);
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
            if (gameObject.TryGetComponent(out MeshFilter filter))
            {
                return filter.mesh;
            }

            Mesh mesh = CreateOrUpdateMesh();
            if (gameObject.TryGetComponent(out EdgeOperator edgeOperator))
            {
                // Glow effect depends on materials staying the same. We need to fully refresh it.
                edgeOperator.RefreshGlowAsync(true).Forget();
            }
            needsCompleteUpdate = false;
            return mesh;
        }

        /// <summary>
        /// Updates the mesh rendering and marks the internal state as clean
        /// (i.e., <see cref="needsCompleteUpdate"/> is set to false) so that
        /// <see cref="Update"/> doesn't update the mesh again in the next
        /// frame. Calling this method doesn't fail if mesh rendering has not
        /// been enabled yet (i.e., there is no <see cref="MeshFilter"/>
        /// attached to <see cref="Component.gameObject"/>; see
        /// <see cref="CreateMesh"/> for more details).
        /// </summary>
        private void UpdateMesh()
        {
            if (meshRenderer != null)
            {
                CreateOrUpdateMesh();
                UpdateVisibleSegment();
            }
            needsCompleteUpdate = false;
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
    /// <see cref="CreateTween"/> can be used to create a <see cref="DG.Tweening.Tween"/>
    /// object with which the morphism can be played as an animation.
    /// The animation can be controlled using the tween object.
    /// </summary>
    public class SplineMorphism : SerializedMonoBehaviour
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
        /// The tween which can play the spline morphism from <see cref="source"/>
        /// to <see cref="target"/>, created by <see cref="CreateTween"/>.
        /// </summary>
        public Tween Tween;

        /// <summary>
        /// Creates a new <see cref="DG.Tweening.Tween"/> which can play the spline morphism from <paramref name="source"/>
        /// to <see name="target"/>, taking <paramref name="duration"/> seconds.
        /// </summary>
        /// <param name="source">Origin of the spline morphism</param>
        /// <param name="target">Target of the spline morphism</param>
        /// <param name="duration">Duration of the animation; lower bound is clamped to 0.01</param>
        /// <remarks>
        /// Note that the returned tween can be modified (e.g., to apply an ease function)
        /// and that <c>Play()</c> has to be called to actually start the animation.
        /// </remarks>
        public Tween CreateTween(BSpline source, BSpline target, float duration)
        {
            Init(source, target);
            return Tween = DOTween.To(t => Morph(t), 0f, 1f, Math.Max(duration, 0.01f));
        }

        /// <summary>
        /// Changes the target of the morphism to <paramref name="newTarget"/>.
        /// </summary>
        /// <param name="newTarget">The new target of this morphism.</param>
        public void ChangeTarget(BSpline newTarget)
        {
            target = newTarget;
            morphism = source.MorphTo(target);
        }

        /// <summary>
        /// Whether the <paramref name="tween"/> belonging to this morphism is active.
        /// If no tween exists, <c>false</c> will be returned.
        /// </summary>
        public bool IsActive() => Tween?.IsActive() ?? false;

        /// <summary>
        /// Initializes the spline morphism.
        ///
        /// Postcondition: <see cref="SEESpline"/> is morphed to
        /// <paramref name="source"/>.
        /// </summary>
        /// <param name="source">Origin of the spline morphism</param>
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
        /// <param name="time">Time parameter; clamped to domain [0, 1]</param>
        /// <returns>Linear interpolation of source and target at t</returns>
        public BSpline Morph(double time)
        {
            if (gameObject.TryGetComponent(out SEESpline spline))
            {
                spline.Spline = morphism.Eval(time);
            }
            else
            {
                Debug.LogWarning($"GameObject '{gameObject.name}' without SEESpline component.\n");
            }
            // Protect internal state of `spline'.
            return new BSpline(spline.Spline);
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

//
//  Outline.cs
//  QuickOutline
//
//  Created by Chris Nolet on 3/30/18.
//  Copyright © 2018 Chris Nolet. All rights reserved.
//
// https://assetstore.unity.com/packages/tools/particles-effects/quick-outline-115488
//

using System;
using System.Collections.Generic;
using System.Linq;
using SEE.GO;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Controls.Interactables
{
    [DisallowMultipleComponent]
    public class Outline : MonoBehaviour
    {
        private static readonly HashSet<Mesh> registeredMeshes = new HashSet<Mesh>();

        private enum Mode
        {
            OutlineAll,
            OutlineVisible,
            OutlineHidden,
            OutlineAndSilhouette,
            SilhouetteOnly
        }

        private Mode OutlineMode
        {
            set
            {
                outlineMode = value;
                needsUpdate = true;
            }
        }

        public Color OutlineColor
        {
            get => outlineColor;
            set
            {
                outlineColor = value;
                needsUpdate = true;
                UpdateMaterialProperties();
            }
        }

        [SerializeField, Range(0f, 10f)]
        private float outlineWidth = 1f;

        private float OutlineWidth
        {
            set
            {
                outlineWidth = value;
                needsUpdate = true;
            }
        }

        [Serializable]
        private class ListVector3
        {
            public List<Vector3> data;
        }

        [SerializeField] private Mode outlineMode;

        [SerializeField] private Color outlineColor = Color.white;

        [Header("Optional")]
        [SerializeField, Tooltip("Precompute enabled: Per-vertex calculations are performed in the editor and serialized with the object. "
                                 + "Precompute disabled: Per-vertex calculations are performed at runtime in Awake(). "
                                 + "This may cause a pause for large meshes.")]
        private bool precomputeOutline;

        private readonly List<Mesh> bakeKeys = new List<Mesh>();

        private readonly List<ListVector3> bakeValues = new List<ListVector3>();

        private Renderer[] renderers;
        private Material outlineMaterial;

        /// <summary>
        /// Whether the material properties must be updated, i.e., whether
        /// <see cref="UpdateMaterialProperties"/> must be called.
        /// </summary>
        private bool needsUpdate;

        // Cached shader property IDs for improved lookup.
        private static readonly int OutlineColor1 = Shader.PropertyToID("_OutlineColor");
        private static readonly int ZTestMask = Shader.PropertyToID("_ZTestMask");
        private static readonly int ZTestFill = Shader.PropertyToID("_ZTestFill");
        private static readonly int Width = Shader.PropertyToID("_OutlineWidth");

        /// <summary>
        /// The default width of outline.
        /// </summary>
        public const float DefaultWidth = 1.0f;

        public static Outline Create(GameObject go, Color color, float outlineWidth = DefaultWidth)
        {
            Outline result = null;

            if (go)
            {
                result = go.AddComponent<Outline>();
                if (go.HasEdgeRef())
                {
                    result.enabled = false;
                }
                else
                {
                    result.OutlineMode = Mode.OutlineVisible;
                }
                result.OutlineColor = color;
                result.OutlineWidth = outlineWidth;
            }

            return result;
        }

        /// <summary>
        /// Updates the render queue by setting it to the value of the first material's render queue which isn't
        /// an outline material.
        /// </summary>
        /// <param name="fetchMaterial">
        /// If true, we will also fetch the materials again, in case they've been changed.
        /// </param>
        public void UpdateRenderQueue(bool fetchMaterial = false)
        {
            IEnumerable<Material> materials = fetchMaterial
                ? renderers.Select(x => x.materials.First(y => y.shader.name.Contains("Outline")))
                : new[] { outlineMaterial };
            int renderQueue = GetRenderQueue();

            foreach (Material material in materials)
            {
                material.renderQueue = renderQueue;
            }

            // Returns render queue setting of game object's first material
            int GetRenderQueue() => renderers
                                    .Select(x => x.materials.First(y => y != outlineMaterial).renderQueue)
                                    .First();
        }

        private void Awake()
        {
            // Cache renderers
            renderers = GetComponents<Renderer>();

            // Instantiate outline material
            outlineMaterial = Instantiate(Resources.Load<Material>(@"Materials/Outline"));

            outlineMaterial.name = "Outline (Instance)";
            //UpdateRenderQueue();

            // Retrieve or generate smooth normals
            LoadSmoothNormals();

            // Apply material properties immediately
            needsUpdate = true;
        }

        private void OnEnable()
        {
            foreach (Renderer renderer in renderers)
            {
                if (renderer.enabled)
                {
                    // Append outline shaders
                    List<Material> materials = renderer.sharedMaterials.ToList();

                    materials.Add(outlineMaterial);

                    renderer.materials = materials.ToArray();
                }
                else
                {
                    renderer.enabled = true;

                    Material[] materials =
                    {
                        outlineMaterial,
                    };

                    renderer.materials = materials;
                }
            }

            // The portal depends upon the code-city object. This object can be retrieved only
            // if the gameObject is a descendant of it.
            if (gameObject.transform.parent != null)
            {
                gameObject.UpdatePortal(true);
            }
        }

        private void OnValidate()
        {
            // Update material properties
            needsUpdate = true;

            // Clear cache when baking is disabled or corrupted
            if (!precomputeOutline && bakeKeys.Count != 0 || bakeKeys.Count != bakeValues.Count)
            {
                bakeKeys.Clear();
                bakeValues.Clear();
            }

            // Generate smooth normals when baking is enabled
            if (precomputeOutline && bakeKeys.Count == 0)
            {
                Bake();
            }
        }

        private void Update()
        {
            if (needsUpdate)
            {
                needsUpdate = false;

                UpdateMaterialProperties();
            }
        }

        private void OnDisable()
        {
            renderers = renderers.Where(x => x != null).ToArray();
            foreach (Renderer renderer in renderers)
            {
                // Remove outline shaders
                List<Material> materials = renderer.sharedMaterials.ToList();

                materials.Remove(outlineMaterial);

                renderer.materials = materials.ToArray();
            }
        }

        private void OnDestroy()
        {
            // Destroy material instances
            Destroy(outlineMaterial);
        }

        private void Bake()
        {
            // Generate smooth normals for each mesh
            HashSet<Mesh> bakedMeshes = new HashSet<Mesh>();

            foreach (MeshFilter meshFilter in GetComponentsInChildren<MeshFilter>())
            {
                // Skip duplicates
                if (!bakedMeshes.Add(meshFilter.sharedMesh))
                {
                    continue;
                }

                // Serialize smooth normals
                List<Vector3> smoothNormals = SmoothNormals(meshFilter.sharedMesh);

                bakeKeys.Add(meshFilter.sharedMesh);
                bakeValues.Add(new ListVector3 { data = smoothNormals });
            }
        }

        private void LoadSmoothNormals()
        {
            // Retrieve or generate smooth normals
            foreach (MeshFilter meshFilter in GetComponentsInChildren<MeshFilter>())
            {
                // Skip if smooth normals have already been adopted
                if (meshFilter.sharedMesh == null || !registeredMeshes.Add(meshFilter.sharedMesh))
                {
                    continue;
                }

                // Retrieve or generate smooth normals
                int index = bakeKeys.IndexOf(meshFilter.sharedMesh);
                List<Vector3> smoothNormals =
                    (index >= 0) ? bakeValues[index].data : SmoothNormals(meshFilter.sharedMesh);

                // Store smooth normals in UV3
                meshFilter.sharedMesh.SetUVs(3, smoothNormals);

                // Combine submeshes
                Renderer renderer = meshFilter.GetComponent<Renderer>();

                if (renderer != null)
                {
                    CombineSubmeshes(meshFilter.sharedMesh, renderer.sharedMaterials);
                }
            }

            // Clear UV3 on skinned mesh renderers
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                // Skip if UV3 has already been reset
                if (!registeredMeshes.Add(skinnedMeshRenderer.sharedMesh))
                {
                    continue;
                }

                // Clear UV3
                skinnedMeshRenderer.sharedMesh.uv4 = new Vector2[skinnedMeshRenderer.sharedMesh.vertexCount];

                // Combine submeshes
                CombineSubmeshes(skinnedMeshRenderer.sharedMesh, skinnedMeshRenderer.sharedMaterials);
            }
        }

        private static List<Vector3> SmoothNormals(Mesh mesh)
        {
            // Group vertices by location
            Assert.IsNotNull(mesh);
            Assert.IsNotNull(mesh.vertices);
            IEnumerable<IGrouping<Vector3, KeyValuePair<Vector3, int>>> groups
                = mesh.vertices.Select((vertex, index) => new KeyValuePair<Vector3, int>(vertex, index))
                      .GroupBy(pair => pair.Key);

            // Copy normals to a new list
            List<Vector3> smoothNormals = new List<Vector3>(mesh.normals);

            // Average normals for grouped vertices
            foreach (var group in groups)
            {
                // Skip single vertices
                if (group.Count() == 1)
                {
                    continue;
                }

                // Calculate the average normal
                Vector3 smoothNormal = group.Aggregate(Vector3.zero, (current, pair) => current + smoothNormals[pair.Value]);

                smoothNormal.Normalize();
            }

            return smoothNormals;
        }
        
        void CombineSubmeshes(Mesh mesh, IReadOnlyCollection<Material> materials) {

            // Skip meshes with a single submesh
            if (mesh.subMeshCount == 1) {
                return;
            }

            // Skip if submesh count exceeds material count
            if (mesh.subMeshCount > materials.Count) {
                return;
            }

            // Append combined submesh
            mesh.subMeshCount++;
            mesh.SetTriangles(mesh.triangles, mesh.subMeshCount - 1);
        }

        private void UpdateMaterialProperties()
        {
            if (outlineMaterial == null)
            {
                return;
            }

            // Apply properties according to mode
            outlineMaterial.SetColor(OutlineColor1, outlineColor);

            switch (outlineMode)
            {
                case Mode.OutlineAll:
                    outlineMaterial.SetFloat(ZTestMask, (float)UnityEngine.Rendering.CompareFunction.Always);
                    outlineMaterial.SetFloat(ZTestFill, (float)UnityEngine.Rendering.CompareFunction.Always);
                    outlineMaterial.SetFloat(Width, outlineWidth);
                    break;

                case Mode.OutlineVisible:
                    outlineMaterial.SetFloat(ZTestMask, (float)UnityEngine.Rendering.CompareFunction.Always);
                    outlineMaterial.SetFloat(ZTestFill, (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                    outlineMaterial.SetFloat(Width, outlineWidth);
                    break;

                case Mode.OutlineHidden:
                    outlineMaterial.SetFloat(ZTestMask, (float)UnityEngine.Rendering.CompareFunction.Always);
                    outlineMaterial.SetFloat(ZTestFill, (float)UnityEngine.Rendering.CompareFunction.Greater);
                    outlineMaterial.SetFloat(Width, outlineWidth);
                    break;

                case Mode.OutlineAndSilhouette:
                    outlineMaterial.SetFloat(ZTestMask, (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                    outlineMaterial.SetFloat(ZTestFill, (float)UnityEngine.Rendering.CompareFunction.Always);
                    outlineMaterial.SetFloat(Width, outlineWidth);
                    break;

                case Mode.SilhouetteOnly:
                    outlineMaterial.SetFloat(ZTestMask, (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                    outlineMaterial.SetFloat(ZTestFill, (float)UnityEngine.Rendering.CompareFunction.Greater);
                    outlineMaterial.SetFloat(Width, 0);
                    break;
            }
        }
    }
}
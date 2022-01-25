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

        [SerializeField, Range(0f, 10f)] private float outlineWidth = 2f;

        [Header("Optional")]
        [SerializeField, Tooltip(
             "Precompute enabled: Per-vertex calculations are performed in the editor and serialized with the object. "
             + "Precompute disabled: Per-vertex calculations are performed at runtime in Awake(). "
             + "This may cause a pause for large meshes.")]
        private bool precomputeOutline;

        private readonly List<Mesh> bakeKeys = new List<Mesh>();

        private readonly List<ListVector3> bakeValues = new List<ListVector3>();

        private Renderer[] renderers;
        private Material outlineMaskMaterial;
        private Material outlineFillMaterial;

        /// <summary>
        /// Whether the material properties must be updated, i.e., whether
        /// <see cref="UpdateMaterialProperties"/> must be called.
        /// </summary>
        private bool needsUpdate;

        public static Outline Create(GameObject go, Color color)
        {
            Outline result = null;

            if (go)
            {
                result = go.AddComponent<Outline>();
                result.OutlineMode = Mode.OutlineAll;
                result.OutlineColor = color;
                result.OutlineWidth = 4.0f;
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
                ? renderers.SelectMany(x => x.materials.Where(y =>
                    y.shader.name.Contains("Outline Mask") || y.shader.name.Contains("Outline Fill")))
                : new[] {outlineMaskMaterial, outlineFillMaterial};
            int renderQueue = GetRenderQueue();

            foreach (Material material in materials)
            {
                material.renderQueue = renderQueue;
            }

            // Returns render queue setting of game object's first material
            int GetRenderQueue() => renderers
                .Select(x =>
                    x.materials.First(y => y != outlineMaskMaterial && y != outlineFillMaterial).renderQueue)
                .First();
        }

        private void Awake()
        {
            // Cache renderers
            renderers = GetComponents<Renderer>();

            // Instantiate outline materials
            outlineMaskMaterial = Instantiate(Resources.Load<Material>(@"Materials/OutlineMask"));
            outlineFillMaterial = Instantiate(Resources.Load<Material>(@"Materials/OutlineFill"));

            outlineMaskMaterial.name = "OutlineMask (Instance)";
            outlineFillMaterial.name = "OutlineFill (Instance)";
            UpdateRenderQueue();

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

                    materials.Add(outlineMaskMaterial);
                    materials.Add(outlineFillMaterial);

                    renderer.materials = materials.ToArray();
                }
                else
                {
                    renderer.enabled = true;

                    Material[] materials =
                    {
                        outlineMaskMaterial,
                        outlineFillMaterial
                    };

                    renderer.materials = materials;
                }
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

                materials.Remove(outlineMaskMaterial);
                materials.Remove(outlineFillMaterial);

                renderer.materials = materials.ToArray();
            }
        }

        private void OnDestroy()
        {
            // Destroy material instances
            Destroy(outlineMaskMaterial);
            Destroy(outlineFillMaterial);
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
                bakeValues.Add(new ListVector3 {data = smoothNormals});
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
            }

            // Clear UV3 on skinned mesh renderers
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (registeredMeshes.Add(skinnedMeshRenderer.sharedMesh))
                {
                    skinnedMeshRenderer.sharedMesh.uv4 = new Vector2[skinnedMeshRenderer.sharedMesh.vertexCount];
                }
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
            foreach (IGrouping<Vector3, KeyValuePair<Vector3, int>> group in groups)
            {
                // Skip single vertices
                if (group.Count() == 1)
                {
                    continue;
                }

                // Calculate the average normal
                Vector3 smoothNormal =
                    group.Aggregate(Vector3.zero, (current, pair) => current + mesh.normals[pair.Value]);

                smoothNormal.Normalize();

                // Assign smooth normal to each vertex
                foreach (KeyValuePair<Vector3, int> pair in group)
                {
                    smoothNormals[pair.Value] = smoothNormal;
                }
            }

            return smoothNormals;
        }

        private void UpdateMaterialProperties()
        {
            if (outlineFillMaterial == null)
            {
                return;
            }

            // Apply properties according to mode
            outlineFillMaterial.SetColor("_OutlineColor", outlineColor);

            switch (outlineMode)
            {
                case Mode.OutlineAll:
                    outlineMaskMaterial.SetFloat("_ZTest", (float) UnityEngine.Rendering.CompareFunction.Always);
                    outlineFillMaterial.SetFloat("_ZTest", (float) UnityEngine.Rendering.CompareFunction.Always);
                    outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
                    break;

                case Mode.OutlineVisible:
                    outlineMaskMaterial.SetFloat("_ZTest", (float) UnityEngine.Rendering.CompareFunction.Always);
                    outlineFillMaterial.SetFloat("_ZTest", (float) UnityEngine.Rendering.CompareFunction.LessEqual);
                    outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
                    break;

                case Mode.OutlineHidden:
                    outlineMaskMaterial.SetFloat("_ZTest", (float) UnityEngine.Rendering.CompareFunction.Always);
                    outlineFillMaterial.SetFloat("_ZTest", (float) UnityEngine.Rendering.CompareFunction.Greater);
                    outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
                    break;

                case Mode.OutlineAndSilhouette:
                    outlineMaskMaterial.SetFloat("_ZTest", (float) UnityEngine.Rendering.CompareFunction.LessEqual);
                    outlineFillMaterial.SetFloat("_ZTest", (float) UnityEngine.Rendering.CompareFunction.Always);
                    outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
                    break;

                case Mode.SilhouetteOnly:
                    outlineMaskMaterial.SetFloat("_ZTest", (float) UnityEngine.Rendering.CompareFunction.LessEqual);
                    outlineFillMaterial.SetFloat("_ZTest", (float) UnityEngine.Rendering.CompareFunction.Greater);
                    outlineFillMaterial.SetFloat("_OutlineWidth", 0);
                    break;
            }
        }
    }
}

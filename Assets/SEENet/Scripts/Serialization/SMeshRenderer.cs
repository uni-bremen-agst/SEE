using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace SEE.Net.Internal
{

    [Serializable]
    public class SObject : IInitializer<UnityEngine.Object>
    {
        public string name;
        public HideFlags hideFlags;

        public SObject(UnityEngine.Object o)
        {
            Assert.IsNotNull(o);

            name = o.name;
            hideFlags = o.hideFlags;
        }

        public void Initialize(UnityEngine.Object value)
        {
            Assert.IsNotNull(value);

            value.name = name;
            value.hideFlags = hideFlags;
        }
    }

    [Serializable]
    public class SComponent : IInitializer<Component>
    {
        public string tag;

        public SComponent(Component c)
        {
            Assert.IsNotNull(c);

            tag = c.tag;
        }

        public void Initialize(Component value)
        {
            Assert.IsNotNull(value);

            value.tag = tag;
        }
    }

    [Serializable]
    public class SMaterial : IInitializer<Material>
    {
        public SMaterial(Material m)
        {

        }

        public void Initialize(Material value)
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class SMaterialPropertyBlock : IInitializer<MaterialPropertyBlock>
    {
        public SMaterialPropertyBlock(MaterialPropertyBlock mpb)
        {

        }

        public void Initialize(MaterialPropertyBlock value)
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class SRenderer : IInitializer<Renderer>
    {
        public uint renderingLayerMask;
        public int rendererPriority;
        public string sortingLayerName;
        public int sortingLayerID;
        public int sortingOrder;
        public bool allowOcclusionWhenDynamic;
        //public GameObject lightProbeProxyVolumeOverride;
        public Transform probeAnchor;
        public int lightmapIndex;
        public int realtimeLightmapIndex;
        public Vector4 lightmapScaleOffset;
        public Vector4 realtimeLightmapScaleOffset;
        public string[] materialNames;
        public ReflectionProbeUsage reflectionProbeUsage;
        public LightProbeUsage lightProbeUsage;
        public bool receiveShadows;
        public MotionVectorGenerationMode motionVectorGenerationMode;
        public bool motionVectors;
        public Vector4 lightmapTilingOffset;
        public bool enabled;
        public ShadowCastingMode shadowCastingMode;
        public string[] sharedMaterialNames;
        public SMaterialPropertyBlock[] materialPropertyBlocks;

        public SRenderer(Renderer r)
        {
            renderingLayerMask = r.renderingLayerMask;
            rendererPriority = r.rendererPriority;
            sortingLayerName = r.sortingLayerName;
            sortingLayerID = r.sortingLayerID;
            allowOcclusionWhenDynamic = r.allowOcclusionWhenDynamic;
            // lightProbeProxyVolumeOverride = ...
            probeAnchor = r.probeAnchor;
            lightmapIndex = r.lightmapIndex;
            realtimeLightmapIndex = r.realtimeLightmapIndex;
            lightmapScaleOffset = r.lightmapScaleOffset;
            realtimeLightmapScaleOffset = r.realtimeLightmapScaleOffset;
            materialNames = new string[r.materials.Length];
            for (int i = 0; i < r.materials.Length; i++)
            {
                materialNames[i] = r.materials[i].name;
            }
            reflectionProbeUsage = r.reflectionProbeUsage;
            lightProbeUsage = r.lightProbeUsage;
            receiveShadows = r.receiveShadows;
            motionVectorGenerationMode = r.motionVectorGenerationMode;
            enabled = r.enabled;
            shadowCastingMode = r.shadowCastingMode;
            sharedMaterialNames = new string[r.sharedMaterials.Length];
            for (int i = 0; i < r.sharedMaterials.Length; i++)
            {
                sharedMaterialNames[i] = r.sharedMaterials[i].name;
            }
            materialPropertyBlocks = new SMaterialPropertyBlock[r.sharedMaterials.Length];
            for (int i = 0; i < r.sharedMaterials.Length; i++)
            {
                MaterialPropertyBlock p = new MaterialPropertyBlock();
                r.GetPropertyBlock(p, i);
                materialPropertyBlocks[i] = new SMaterialPropertyBlock(p);
            }
        }

        public void Initialize(Renderer value)
        {
            //value.renderingLayerMask = renderingLayerMask;
            //value.rendererPriority = rendererPriority;
            //value.sortingLayerName = sortingLayerName;
            //value.sortingLayerID = sortingLayerID;
            //value.allowOcclusionWhenDynamic = allowOcclusionWhenDynamic;
            //// lightProbeProxyVolumeOverride = ...
            //value.probeAnchor = probeAnchor;
            //value.lightmapIndex = lightmapIndex;
            //value.realtimeLightmapIndex = realtimeLightmapIndex;
            //value.lightmapScaleOffset = lightmapScaleOffset;
            //value.realtimeLightmapScaleOffset = realtimeLightmapScaleOffset;
            //value.materials = new Material[materialNames.Length];
            //for (int i = 0; i < materialNames.Length; i++)
            //{
            //    value.materials[i] = new Material(...);
            //}
            //value.reflectionProbeUsage = reflectionProbeUsage;
            //value.sharedMaterial = new Material(sharedMaterial);
            //value.lightProbeUsage = lightProbeUsage;
            //value.receiveShadows = receiveShadows;
            //value.motionVectorGenerationMode = motionVectorGenerationMode;
            //value.enabled = enabled;
            //value.shadowCastingMode = shadowCastingMode;
            //value.sharedMaterials = new Material[sharedMaterials.Length];
            //for (int i = 0; i < sharedMaterials.Length; i++)
            //{
            //    sharedMaterials[i] = new Material(sharedMaterials[i]);
            //}
            //for (int i = 0; i < sharedMaterials.Length; i++)
            //{
            //    value.SetPropertyBlock(new MaterialPropertyBlock(...), i);
            //}
        }
    }

    [Serializable]
    public class SMeshRenderer : IInitializer<MeshRenderer>
    {
        SObject sObject;
        SComponent sComponent;
        SRenderer sRenderer;

        public void Initialize(MeshRenderer value)
        {
            throw new NotImplementedException();
        }
    }

}

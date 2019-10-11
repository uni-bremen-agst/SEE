using System;
using UnityEngine;

[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]

public class CSDepthToNormal : MonoBehaviour
{

    public Material material;

    void Start()
    {
        if (!SystemInfo.supportsImageEffects || null == material ||
           null == material.shader || !material.shader.isSupported)
        {
            enabled = false;
            return;
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, material);
    }
}

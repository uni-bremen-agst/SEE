using System;
using UnityEngine;

[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]

public class CinematicDof : MonoBehaviour
{

    public Material material;
    public Material material2;
    public RenderTexture screen;

    void Start()
    {
        if (!SystemInfo.supportsImageEffects || null == material || null == material2 ||
           null == material.shader || !material.shader.isSupported)
        {
            enabled = false;
            return;
        }
        screen = new RenderTexture(Screen.width, Screen.height, 0);
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        
        Graphics.Blit(source, screen, material);
        Graphics.Blit(screen, destination, material2);
    }
}

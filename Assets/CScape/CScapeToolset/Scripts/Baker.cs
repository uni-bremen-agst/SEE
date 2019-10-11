using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
//using UnityEditor;

[ExecuteInEditMode]
public class Baker : MonoBehaviour
{
    public int resolution = 1024;
    public Camera cam;

    private RenderTexture unfilteredRt;
    private Texture2D screenShot;
    public string fileName;
    public bool bake = false;
    private RenderTexture sourceTex;
    public bool CScapeCompatible = true;
    public bool bakeNormal = true;
    public bool bakeMask = false;
    public bool bakeDepth = false;
    public bool bakeAO = false;
    public bool bakeNormalCScape = false;
    public Material maskMat;
    public bool linear = true;




    // Update is called once per frame
    void Update()
    {
        if (bake)
        {
            
            Bake();
        }

    }
    public void Bake()
    {

        cam = gameObject.GetComponent<Camera>();
        sourceTex = cam.targetTexture;
        string fileName;
        if (bakeNormal)
        {
            fileName = "Assets/CScapeBaker/Resources/" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "/Normal/";
            System.IO.Directory.CreateDirectory(fileName);
        }
        if (CScapeCompatible) { 
            fileName = "Assets/CScapeCDK/Editor/Resources/" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "/Normal/";
            System.IO.Directory.CreateDirectory(fileName);
            fileName = "Assets/CScapeCDK/Editor/Resources/" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "/Mask/";
            System.IO.Directory.CreateDirectory(fileName);
            fileName = "Assets/CScapeCDK/Editor/Resources/" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "/AO/";
            System.IO.Directory.CreateDirectory(fileName);
            fileName = "Assets/CScapeCDK/Editor/Resources/" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "/Depth/";
            System.IO.Directory.CreateDirectory(fileName);
        }
        CaptureScreenshot();

        // string path = AssetDatabase.GetAssetPath(someTexture);
        //UpdateAssets();

//#if UNITY_EDITOR
//        UnityEditor.AssetDatabase.Refresh();
//#endif

        bake = false;
        cam.targetTexture = sourceTex;


    }

    private void UpdateAssets()
    {
#if UNITY_EDITOR
        UnityEditor.TextureImporter A = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(fileName);
        A.isReadable = true;
        A.sRGBTexture = false;
        A.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
        UnityEditor.AssetDatabase.ImportAsset(fileName, UnityEditor.ImportAssetOptions.ForceUpdate);
#endif
    }

    public Texture2D GetVideoScreenshot()
    {
        screenShot = new Texture2D(resolution, resolution, TextureFormat.ARGB32, true);
        if (linear)
            unfilteredRt = RenderTexture.GetTemporary(resolution, resolution, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        else unfilteredRt = RenderTexture.GetTemporary(resolution, resolution, 0, RenderTextureFormat.Default, RenderTextureReadWrite.sRGB);
        unfilteredRt.useMipMap = true;
        Camera VRCam = cam;

        VRCam.targetTexture = unfilteredRt;
        VRCam.Render();
        RenderTexture.active = unfilteredRt;

        screenShot.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);

        VRCam.targetTexture = null;
        RenderTexture.ReleaseTemporary(unfilteredRt);
        return screenShot;

    }





    public void SaveScreenshot()
    {
        Texture2D screenShot = GetVideoScreenshot();
        byte[] bytes = screenShot.EncodeToPNG();
        File.WriteAllBytes(fileName, bytes);
    }

    public void CaptureScreenshot()
    {
        Texture2D screenShot;
        screenShot = new Texture2D(resolution, resolution, TextureFormat.ARGB32, true);
        if (linear)
            unfilteredRt = RenderTexture.GetTemporary(resolution, resolution, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        else unfilteredRt = RenderTexture.GetTemporary(resolution, resolution, 0, RenderTextureFormat.Default, RenderTextureReadWrite.sRGB);
        unfilteredRt.useMipMap = true;

        Camera VRCam = cam;

        VRCam.targetTexture = unfilteredRt;
        VRCam.Render();
        RenderTexture.active = unfilteredRt;
        if (bakeNormal)
        maskMat.SetTexture("_ShapeTex", unfilteredRt);
        screenShot.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);

       // VRCam.targetTexture = null;
       // RenderTexture.ReleaseTemporary(unfilteredRt);

        Texture2D encodedImage = screenShot;
        byte[] bytes = encodedImage.EncodeToPNG();
        File.WriteAllBytes(fileName, bytes);

    }


}

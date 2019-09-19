#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
[ExecuteInEditMode]
public class DetailMaterialTools : MonoBehaviour
{


    public string TextureFolder;
    public int slices = 40;
    public Texture2DArray surfaceArray;
    public Material material;
    
    public int size;
    public bool generate = false;
    

    // Use this for initialization
    void Start()
    {

    }

    void Update()
    {
        if (generate)
        {
            CreateMaterials();
            CreateNormals();
            generate = false;
        }
    }

    // Update is called once per frame


    // Update is called once per frame



    public void CreateMaterials()
    {
        Texture2DArray textureArray = new Texture2DArray(size, size, slices * 2, TextureFormat.ARGB32, true, false);
        string filePattern = TextureFolder + "/diffuse_{0:00}";
        string filePatternAO = TextureFolder + "/ao_{0:00}";
        int texNumber = 2;



        // CHANGEME: If your files start at 001, use i = 1. Otherwise change to what you got.

        for (int i = 0; i < slices; i++)
        {
            string filename = string.Format(filePattern, i + 1);
            string filenameAO = string.Format(filePatternAO, i + 1);
            Debug.Log("Loading " + filename);
            Texture2D tex = (Texture2D)Resources.Load(filename);
            Texture2D texAO = (Texture2D)Resources.Load(filenameAO);


            Texture2D scaleTex = scaled(tex, size, size, 0);
            Texture2D scaleTexAO = scaled(texAO, size, size, 0);
            Color[] texCol = scaleTex.GetPixels(0);
            Color[] texColAO = scaleTexAO.GetPixels(0);
            Color[] texComposite = texCol;
            for (int j = 0; j < texCol.Length; j++)
            {
                texComposite[j] = new Color(texCol[j].r, texCol[j].g, texCol[j].b, texColAO[j].r);
            }
            textureArray.SetPixels(texCol, i, 0);
        }

        filePattern = TextureFolder + "/specular_{0:00}";
        for (int i = 0; i < slices; i++)
        {
            string filename = string.Format(filePattern, i + 1);
            string filenameAO = string.Format(filePatternAO, i + 1);
            Debug.Log("Loading " + filename);
            Texture2D tex = (Texture2D)Resources.Load(filename);
            Texture2D texAO = (Texture2D)Resources.Load(filenameAO);


            Texture2D scaleTex = scaled(tex, size, size, 0);
            Texture2D scaleTexAO = scaled(texAO, size, size, 0);
            Color[] texCol = scaleTex.GetPixels(0);
            Color[] texColAO = scaleTexAO.GetPixels(0);
            Color[] texComposite = texCol;
            for (int j = 0; j < texCol.Length; j++)
            {
                texComposite[j] = new Color(texCol[j].r, texCol[j].g, texCol[j].b, texColAO[j].r);
            }
            textureArray.SetPixels(texCol, i + slices, 0);
        }
        textureArray.Apply();
        string path = "Assets/CScape/Editor/Resources/TextureArrays/" + TextureFolder + "/" + TextureFolder +  "_maps.asset";
        AssetDatabase.CreateAsset(textureArray, path);
        Debug.Log("Saved asset to " + path);
        surfaceArray = Resources.Load("TextureArrays/" + TextureFolder + "/" + TextureFolder + "_maps" ) as Texture2DArray;
        material.SetTexture("_AlbedoArray", surfaceArray);


    }

    public void CreateNormals()
    {
        Texture2DArray textureArray = new Texture2DArray(size, size, slices, TextureFormat.ARGB32, true, true);
        string filePattern = TextureFolder + "/normal_{0:00}";
        string filePatternAO = TextureFolder + "/emissive_{0:00}";


        for (int i = 0; i < slices; i++)
        {
            string filename = string.Format(filePattern, i + 1);
            string filenameAO = string.Format(filePatternAO, i + 1);
            Debug.Log("Loading " + filename);
            Texture2D tex = (Texture2D)Resources.Load(filename);
            Texture2D texAO = (Texture2D)Resources.Load(filenameAO);


            Texture2D scaleTex = scaled(tex, size, size, 0);
            Texture2D scaleTexAO = scaled(texAO, size, size, 0);
            Color[] texCol = scaleTex.GetPixels(0);
            Color[] texColAO = scaleTexAO.GetPixels(0);
            Color[] texComposite = texCol;
            for (int j = 0; j < texCol.Length; j++)
            {
                texComposite[j] = new Color(texCol[j].r, texCol[j].g, texColAO[j].b, texCol[j].b);
            }
            textureArray.SetPixels(texCol, i, 0);
        }

  
        textureArray.Apply();
        string path = "Assets/CScape/Editor/Resources/TextureArrays/" + TextureFolder + "/" + TextureFolder + "_normal.asset";
        AssetDatabase.CreateAsset(textureArray, path);
        Debug.Log("Saved asset to " + path);
        surfaceArray = Resources.Load("TextureArrays/" + TextureFolder + "/" + TextureFolder + "_normal") as Texture2DArray;
        material.SetTexture("_NormalArray", surfaceArray);


    }

    public static Texture2D scaled(Texture2D src, int width, int height, FilterMode mode = FilterMode.Trilinear)
    {
        Rect texR = new Rect(0, 0, width, height);
        _gpu_scale(src, width, height, mode);

        //Get rendered data back to a new texture
        Texture2D result = new Texture2D(width, height, TextureFormat.ARGB32, true);
        result.Resize(width, height);
        result.ReadPixels(texR, 0, 0, true);
        return result;
    }

    /// <summary>
    /// Scales the texture data of the given texture.
    /// </summary>
    /// <param name="tex">Texure to scale</param>
    /// <param name="width">New width</param>
    /// <param name="height">New height</param>
    /// <param name="mode">Filtering mode</param>
    public static void scale(Texture2D tex, int width, int height, FilterMode mode = FilterMode.Trilinear)
    {
        Rect texR = new Rect(0, 0, width, height);
        _gpu_scale(tex, width, height, mode);

        // Update new texture
        tex.Resize(width, height);
        tex.ReadPixels(texR, 0, 0, true);
        tex.Apply(true);        //Remove this if you hate us applying textures for you :)
    }

    // Internal unility that renders the source texture into the RTT - the scaling method itself.
    static void _gpu_scale(Texture2D src, int width, int height, FilterMode fmode)
    {
        //We need the source texture in VRAM because we render with it
        src.filterMode = fmode;
        src.Apply(true);

        //Using RTT for best quality and performance. Thanks, Unity 5
        RenderTexture rtt = new RenderTexture(width, height, 32);

        //Set the RTT in order to render to it
        Graphics.SetRenderTarget(rtt);

        //Setup 2D matrix in range 0..1, so nobody needs to care about sized
        GL.LoadPixelMatrix(0, 1, 1, 0);

        //Then clear & draw the texture to fill the entire RTT.
        GL.Clear(true, true, new Color(0, 0, 0, 0));
        Graphics.DrawTexture(new Rect(0, 0, 1, 1), src);
    }
}
#endif
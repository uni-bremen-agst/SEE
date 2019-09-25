#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[ExecuteInEditMode]
public class CSMaterialTools : MonoBehaviour
{

    //public Texture2D[] maskTex;
    //public Texture2D[] aoTex;
    public string TextureFolder;
    public int slices = 40;
    public int slicesSurface = 10;
    public int slicesBlinds = 10;
    public int slicesStreets = 2;
    public int slicesEnt = 6;
    public int slicesDirt_Illumination = 10;
    public int slicesShops = 10;
    public int slicesDecalls = 5;
    public Texture2DArray normalArray;
    public Texture2DArray maskArray;
    public Texture2DArray surfaceArray;
    public Texture2DArray surfaceNormalArray;
    public Texture2DArray blindsArray;
    public Texture2DArray streetsArray;
    public Texture2DArray entArray;
    public Texture2DArray Dirt_IlluminationArray;
    public Texture2DArray shopSignsArray;
    public Material cityMaterial;
    public Material streetsMaterial;
    public TextureFormat compressionFormat;
    public TextureFormat normalCompressionFormat;
    
    public int size;
    public int surfaceSize;
    public int blindsSize;
    public int streetsSize;
    public int decallsSize = 256;
    public int shopsSize = 512;
    public int entSize = 512;
    public int Dirt_IlluminationSize;
    public bool generate = false;
    private static Dictionary<int, int> MipCount = new Dictionary<int, int>() { { 32, 6 }, { 64, 7 }, { 128, 8 }, { 256, 9 }, { 512, 10 }, { 1024, 11 }, { 2048, 12 }, { 4096, 13 }, { 8192, 14 } };
    public Texture2D volumeArraySource;
    public bool CScapeToolsetSource = false;



    public void UpdateMe()
    {
        if (generate)
        {


            generate = true;

                CreateStyleShapes();
                CreateMaterialsNew();
                CreateBlinds();
                CreateStreets();
              //  CreateMaterialsNormal();
                CreateDirt();
            CreateInt();
            CreateShops();
            CreateStreetDecalls();


            generate = false;
        }
    }

    //THIS
    public void CreateStyleShapes()
    {
        string fileNormal = TextureFolder + "/Normal/normal_{0:000}";
        string fileAO = TextureFolder + "/AO/ao_{0:000}";
        string fileBlindsMask = TextureFolder + "/BlindsGradient/depth_{0:000}";
        string mask = TextureFolder + "/Mask/diff_{0:000}";
        string depth = TextureFolder + "/Depth/depth_{0:000}";


        Texture2DArray textureArray = new Texture2DArray(size, size, slices * 2, compressionFormat, true, true);

        for (int i = 0; i < slices; i++)
        {
            string filename = string.Format(fileNormal, i + 1);
            string filenameAO = string.Format(fileAO, i + 1);
            string filenameAdd = string.Format(fileBlindsMask, i + 1);
            Debug.Log("Loading " + filename);
            Texture2D tex = (Texture2D)Resources.Load(filename);
            Texture2D texAO = (Texture2D)Resources.Load(filenameAO);
            Texture2D texAdd = (Texture2D)Resources.Load(filenameAdd);

            Texture2D scaleTex = scaled(tex, size, size, 0);
            Texture2D scaleTexAO = scaled(texAO, size, size, 0);
            Texture2D scaleTexAdd = scaled(texAdd, size, size, 0);
            Color[] texCol = scaleTex.GetPixels(0);
            Color[] texColAO = scaleTexAO.GetPixels(0);
            Color[] texColAdd = scaleTexAdd.GetPixels(0);
            Color[] texComposite = texCol;
            for (int j = 0; j < texCol.Length; j++)
            {
                texComposite[j] = new Color(texCol[j].r, texCol[j].g, texColAdd[j].r, texColAO[j].r);
            }
            Texture2D final = new Texture2D(size, size, TextureFormat.RGBA32, true);

            final.SetPixels(texComposite);
            final.Apply(true);
            EditorUtility.CompressTexture(final, compressionFormat, 100);
            final.Apply(true);

            for (int mip = 0; mip < MipCount[size]; mip++)
            {
                Graphics.CopyTexture(final, 0, mip, textureArray, i, mip);
            }

            string filenameMask = string.Format(mask, i + 1);
            string filenameDepth = string.Format(depth, i + 1);
            Debug.Log("Loading " + filenameMask);
            tex = (Texture2D)Resources.Load(filenameMask);
            texAO = (Texture2D)Resources.Load(filenameDepth);

            scaleTex = scaled(tex, size, size, 0);
            scaleTexAO = scaled(texAO, size, size, 0);
            texCol = scaleTex.GetPixels(0);
            texColAO = scaleTexAO.GetPixels(0);
            texComposite = texCol;
            for (int j = 0; j < texCol.Length; j++)
            {
                texComposite[j] = new Color(texColAO[j].b, texCol[j].g, texCol[j].b, texColAO[j].r);
            }

            final = new Texture2D(size, size, TextureFormat.RGBA32, true);

            final.SetPixels(texComposite);
            final.Apply(true);
            EditorUtility.CompressTexture(final, compressionFormat, 100);
            final.Apply(true);

            for (int mip = 0; mip < MipCount[size]; mip++)
            {
                Graphics.CopyTexture(final, 0, mip, textureArray, i + slices, mip);
            }
        }
        textureArray.Apply(false);
        System.IO.Directory.CreateDirectory("Assets/CScape/Editor/Resources/TextureArrays/" + TextureFolder);
        string path = "Assets/CScape/Editor/Resources/TextureArrays/" + TextureFolder + "/NormaltextureArray.asset";
        AssetDatabase.CreateAsset(textureArray, path);
        Debug.Log("Saved asset to " + path);
        normalArray = Resources.Load("TextureArrays/" + TextureFolder + "/NormaltextureArray") as Texture2DArray;
        cityMaterial.SetTexture("_MaskTexArray", normalArray);


    }

    public void CreateMaterialsNew()
    {

        string fileNormal = TextureFolder + "/Surfaces/basecolor_surface_{0:00}";
        string fileAO = TextureFolder + "/Surfaces/roughness_surface_{0:00}";
        string fileBlindsMask = TextureFolder + "/Surfaces/normal_surface_{0:00}";
        string mask = TextureFolder + "/Surfaces/metallic_surface_{0:00}";
        string depth = TextureFolder + "/Depth/depth_{0:000}";


        Texture2DArray textureArray = new Texture2DArray(surfaceSize, surfaceSize, slicesSurface * 2, compressionFormat, true, true);

        for (int i = 0; i < slicesSurface; i++)
        {
            string filename = string.Format(fileNormal, i + 1);
            string filenameAO = string.Format(fileAO, i + 1);
            string filenameAdd = string.Format(fileBlindsMask, i + 1);
            Debug.Log("Loading " + filename);
            Texture2D tex = (Texture2D)Resources.Load(filename);
            Texture2D texAO = (Texture2D)Resources.Load(filenameAO);
            Texture2D texAdd = (Texture2D)Resources.Load(filenameAdd);

            Texture2D scaleTex = scaled(tex, surfaceSize, surfaceSize, 0);
            Texture2D scaleTexAO = scaled(texAO, surfaceSize, surfaceSize, 0);
            Texture2D scaleTexAdd = scaled(texAdd, surfaceSize, surfaceSize, 0);
            Color[] texCol = scaleTex.GetPixels(0);
            Color[] texColAO = scaleTexAO.GetPixels(0);
            Color[] texColAdd = scaleTexAdd.GetPixels(0);
            Color[] texComposite = texCol;
            for (int j = 0; j < texCol.Length; j++)
            {
                texComposite[j] = new Color(texCol[j].r, texCol[j].g, texCol[j].b, texColAO[j].r);
            }
            Texture2D final = new Texture2D(surfaceSize, surfaceSize, TextureFormat.RGBA32, true);

            final.SetPixels(texComposite);
            final.Apply(true);
            EditorUtility.CompressTexture(final, compressionFormat, 100);
            final.Apply(true);

            for (int mip = 0; mip < MipCount[surfaceSize]; mip++)
            {
                Graphics.CopyTexture(final, 0, mip, textureArray, i, mip);
            }

            string filenameMask = string.Format(fileBlindsMask, i + 1);
            string filenameDepth = string.Format(mask, i + 1);
            Debug.Log("Loading " + filenameMask);
            tex = (Texture2D)Resources.Load(filenameMask);
            texAO = (Texture2D)Resources.Load(filenameDepth);

            scaleTex = scaled(tex, surfaceSize, surfaceSize, 0);
            scaleTexAO = scaled(texAO, surfaceSize, surfaceSize, 0);
            texCol = scaleTex.GetPixels(0);
            texColAO = scaleTexAO.GetPixels(0);
            texComposite = texCol;
            for (int j = 0; j < texCol.Length; j++)
            {
                texComposite[j] = new Color(texCol[j].r, texCol[j].g, texCol[j].b, texColAO[j].r);
            }

            final = new Texture2D(surfaceSize, surfaceSize, TextureFormat.RGBA32, true);

            final.SetPixels(texComposite);
            final.Apply(true);
            EditorUtility.CompressTexture(final, compressionFormat, 100);
            final.Apply(true);

            for (int mip = 0; mip < MipCount[surfaceSize]; mip++)
            {
                Graphics.CopyTexture(final, 0, mip, textureArray, i + slicesSurface, mip);
            }
        }
        textureArray.Apply(false);
        System.IO.Directory.CreateDirectory("Assets/CScape/Editor/Resources/TextureArrays/" + TextureFolder);

        string path = "Assets/CScape/Editor/Resources/TextureArrays/" + TextureFolder + "/SurfaceArray.asset";
        AssetDatabase.CreateAsset(textureArray, path);
        Debug.Log("Saved asset to " + path);
        surfaceArray = Resources.Load("TextureArrays/" + TextureFolder + "/SurfaceArray") as Texture2DArray;
        cityMaterial.SetTexture("_SurfaceArray", surfaceArray);
    }

    public void CreateMaterials()
    {

        string filePattern = TextureFolder + "/Surfaces/basecolor_surface_{0:00}";
        string filePatternAO = TextureFolder + "/Surfaces/roughness_surface_{0:00}";
        string fileNormal = TextureFolder + "/Surfaces/normal_surface_{0:00}";
        string fileMetallic = TextureFolder + "/Surfaces/metallic_surface_{0:00}";
        string fileBlindsDepth = TextureFolder + "/BlindsGradient/depth_{0:000}";

        Texture2DArray textureArray = new Texture2DArray(surfaceSize, surfaceSize, slicesSurface, compressionFormat, true, true);

        // CHANGEME: If your files start at 001, use i = 1. Otherwise change to what you got.
        for (int i = 0; i < slicesSurface; i++)
        {
            string filename = string.Format(filePattern, i + 1);
            string filenameAO = string.Format(filePatternAO, i + 1);
            Debug.Log("Loading " + filename);
            Texture2D tex = (Texture2D)Resources.Load(filename);
            Texture2D texAO = (Texture2D)Resources.Load(filenameAO);


            Texture2D scaleTex = scaled(tex, surfaceSize, surfaceSize, 0);
            Texture2D scaleTexAO = scaled(texAO, surfaceSize, surfaceSize, 0);
            Color[] texCol = scaleTex.GetPixels(0);
            Color[] texColAO = scaleTexAO.GetPixels(0);
            Color[] texComposite = texCol;
            for (int j = 0; j < texCol.Length; j++)
            {
                texComposite[j] = new Color(texCol[j].r, texCol[j].g, texCol[j].b, texColAO[j].r);
            }
            Texture2D final = new Texture2D(surfaceSize, surfaceSize, TextureFormat.RGBA32, true);

            final.SetPixels(texComposite);
            final.Apply(true);
            EditorUtility.CompressTexture(final, compressionFormat, 100);
            final.Apply(true);

            for (int mip = 0; mip < MipCount[surfaceSize]; mip++)
            {
                Graphics.CopyTexture(final, 0, mip, textureArray, i, mip);
            }
        }
        textureArray.Apply(false);

        // CHANGEME: Path where you want to save the texture array. It must end in .asset extension for Unity to recognise it.
        string path = "Assets/CScape/Editor/Resources/TextureArrays/" + TextureFolder + "/SurfaceArray.asset";
        AssetDatabase.CreateAsset(textureArray, path);
        Debug.Log("Saved asset to " + path);
        surfaceArray = Resources.Load("TextureArrays/" + TextureFolder + "/SurfaceArray") as Texture2DArray;
        cityMaterial.SetTexture("_SurfaceArray", surfaceArray);
        // cityMaterial.SetTexture("_WallsArray", surfaceArray);


    }

   

    public void CreateMaterialsNormal()
    {

        string fileNormal = TextureFolder + "/Surfaces/normal_surface_{0:00}";
        string fileMetallic = TextureFolder + "/Surfaces/metallic_surface_{0:00}";
        string fileDepth = TextureFolder + "/BlindsGradient/depth_{0:000}";

        Texture2DArray textureArray = new Texture2DArray(surfaceSize, surfaceSize, slicesSurface, normalCompressionFormat, true, true);

        for (int i = 0; i < slicesSurface; i++)
        {
            string filenameNormal = string.Format(fileNormal, i + 1);
            string filenameMetallic = string.Format(fileMetallic, i + 1);
            string filenameBlindsDepth = string.Format(fileDepth, i + 1);
            Debug.Log("Loading " + filenameNormal);
            Texture2D normalTex = (Texture2D)Resources.Load(filenameNormal);
            Texture2D MetallicTex = (Texture2D)Resources.Load(filenameMetallic);
            Texture2D BlindsDepthTex = (Texture2D)Resources.Load(filenameBlindsDepth);

            Texture2D scaleTex = scaled(normalTex, surfaceSize, surfaceSize, 0);
            Texture2D scaleTexAO = scaled(MetallicTex, surfaceSize, surfaceSize, 0);
            Texture2D scaleTexAdd = scaled(BlindsDepthTex, surfaceSize, surfaceSize, 0);
            Color[] texNormal = scaleTex.GetPixels(0);
            Color[] texMetallic = scaleTexAO.GetPixels(0);
            Color[] texBlindsDepth = scaleTexAdd.GetPixels(0);
            Color[] texComposite = texNormal;
            for (int j = 0; j < texNormal.Length; j++)
            {
                texComposite[j] = new Color(texNormal[j].r, texNormal[j].g, texBlindsDepth[j].r, texMetallic[j].r);
            }
            Texture2D final = new Texture2D(surfaceSize, surfaceSize, TextureFormat.RGBA32, true);

            final.SetPixels(texComposite);
            final.Apply(true);
            EditorUtility.CompressTexture(final, compressionFormat, 100);
            final.Apply(true);

            for (int mip = 0; mip < MipCount[surfaceSize]; mip++)
            {
                Graphics.CopyTexture(final, 0, mip, textureArray, i, mip);
            }
        }
        textureArray.Apply(false);

        string path = "Assets/CScape/Editor/Resources/TextureArrays/" + TextureFolder + "/SurfaceNormalArray.asset";
        AssetDatabase.CreateAsset(textureArray, path);
        Debug.Log("Saved asset to " + path);
        surfaceNormalArray = Resources.Load("TextureArrays/" + TextureFolder + "/SurfaceNormalArray") as Texture2DArray;
        cityMaterial.SetTexture("_SurfaceNormalArray", surfaceNormalArray);
        //  cityMaterial.SetTexture("_WallsNormalArray", surfaceArray);


    }

    public void CreateDirt()
    {

        string filePattern = TextureFolder + "/Dirt_Illumination/dirt_{0:00}";
        string filePatternAO = TextureFolder + "/Dirt_Illumination/illum_{0:00}";

        Texture2DArray textureArray = new Texture2DArray(Dirt_IlluminationSize, Dirt_IlluminationSize, slicesDirt_Illumination, compressionFormat, true, true);

        for (int i = 0; i < slicesDirt_Illumination; i++)
        {
            string filename = string.Format(filePattern, i + 1);
            string filenameAO = string.Format(filePatternAO, i + 1);
            Debug.Log("Loading " + filename);
            Texture2D tex = (Texture2D)Resources.Load(filename);
            Texture2D texAO = (Texture2D)Resources.Load(filenameAO);

            Texture2D scaleTex = scaled(tex, Dirt_IlluminationSize, Dirt_IlluminationSize, 0);
            Texture2D scaleTexAO = scaled(texAO, Dirt_IlluminationSize, Dirt_IlluminationSize, 0);
            Color[] texCol = scaleTex.GetPixels(0);
            Color[] texColAO = scaleTexAO.GetPixels(0);
            Color[] texComposite = texCol;
            for (int j = 0; j < texCol.Length; j++)
            {
                texComposite[j] = new Color(texCol[j].r, texColAO[j].r, texCol[j].b, texColAO[j].r);
            }
            Texture2D final = new Texture2D(Dirt_IlluminationSize, Dirt_IlluminationSize, TextureFormat.RGBA32, true);

            final.SetPixels(texComposite);
            final.Apply(true);
            EditorUtility.CompressTexture(final, compressionFormat, 100);
            final.Apply(true);

            for (int mip = 0; mip < MipCount[Dirt_IlluminationSize]; mip++)
            {
                Graphics.CopyTexture(final, 0, mip, textureArray, i, mip);
            }
        }
        textureArray.Apply(false);


        string path = "Assets/CScape/Editor/Resources/TextureArrays/" + TextureFolder + "/Dirt_IlluminationArray.asset";
        AssetDatabase.CreateAsset(textureArray, path);
        Debug.Log("Saved asset to " + path);
        Dirt_IlluminationArray = Resources.Load("TextureArrays/" + TextureFolder + "/Dirt_IlluminationArray") as Texture2DArray;
        cityMaterial.SetTexture("_Dirt", Dirt_IlluminationArray);


    }

    public void CreateBlinds()
    {

        string filePattern = TextureFolder + "/Blinds/Blinds_{0:000}";

        Texture2DArray textureArray = new Texture2DArray(blindsSize, blindsSize, slicesBlinds, compressionFormat, true, true);

        // CHANGEME: If your files start at 001, use i = 1. Otherwise change to what you got.
        for (int i = 0; i < slicesBlinds; i++)
        {
            string filename = string.Format(filePattern, i + 1);
            //     string filenameAO = string.Format(filePatternAO, i + 1);
            Debug.Log("Loading " + filename);
            Texture2D tex = (Texture2D)Resources.Load(filename);
            Texture2D scaleTex = scaled(tex, blindsSize, blindsSize, 0);
            Texture2D scaleTexAO = scaled(tex, blindsSize, blindsSize, 0);
            Color[] texCol = scaleTex.GetPixels(0);
            Color[] texColAO = scaleTexAO.GetPixels(0);
            Color[] texComposite = texCol;
            for (int j = 0; j < texCol.Length; j++)
            {
                texComposite[j] = new Color(texCol[j].r, texCol[j].g, texCol[j].b, texColAO[j].a);
            }
            Texture2D final = new Texture2D(blindsSize, blindsSize, TextureFormat.RGBA32, true);

            final.SetPixels(texComposite);
            final.Apply(true);
            EditorUtility.CompressTexture(final, compressionFormat, 100);
            final.Apply(true);

            for (int mip = 0; mip < MipCount[blindsSize]; mip++)
            {
                Graphics.CopyTexture(final, 0, mip, textureArray, i, mip);
            }
        }
        textureArray.Apply(false);

        string path = "Assets/CScape/Editor/Resources/TextureArrays/" + TextureFolder + "/blindsArray.asset";
        AssetDatabase.CreateAsset(textureArray, path);
        Debug.Log("Saved asset to " + path);
        blindsArray = Resources.Load("TextureArrays/" + TextureFolder + "/blindsArray") as Texture2DArray;
        cityMaterial.SetTexture("_BlindsArray", blindsArray);


    }

    public void CreateInt()
    {

        string filePattern = TextureFolder + "/Interior/ent_{0:000}";

        Texture2DArray textureArray = new Texture2DArray(entSize, entSize, slicesEnt, compressionFormat, true, true);
        for (int i = 0; i < slicesEnt; i++)
        {
            string filename = string.Format(filePattern, i + 1);

            Debug.Log("Loading " + filename);
            Texture2D tex = (Texture2D)Resources.Load(filename);


            Texture2D scaleTex = scaled(tex, entSize, entSize, 0);
            Color[] texCol = scaleTex.GetPixels(0);
            Color[] texComposite = texCol;
            for (int j = 0; j < texCol.Length; j++)
            {
                texComposite[j] = new Color(texCol[j].r, texCol[j].g, texCol[j].b, texCol[j].r);
            }
            Texture2D final = new Texture2D(entSize, entSize, TextureFormat.RGBA32, true);

            final.SetPixels(texComposite);
            final.Apply(true);
            EditorUtility.CompressTexture(final, compressionFormat, 100);
            final.Apply(true);

            for (int mip = 0; mip < MipCount[entSize]; mip++)
            {
                Graphics.CopyTexture(final, 0, mip, textureArray, i, mip);
            }
        }
        textureArray.Apply(false);


        string path = "Assets/CScape/Editor/Resources/TextureArrays/" + TextureFolder + "/intArray.asset";
        AssetDatabase.CreateAsset(textureArray, path);
        Debug.Log("Saved asset to " + path);
        entArray = Resources.Load("TextureArrays/" + TextureFolder + "/intArray") as Texture2DArray;
        cityMaterial.SetTexture("_Interior2", entArray);


    }

    public void CreateShops()
    {

        string filePattern = TextureFolder + "/ShopSigns/shop_{0:000}";

        Texture2DArray textureArray = new Texture2DArray(shopsSize, shopsSize, slicesShops, compressionFormat, true, true);
        for (int i = 0; i < slicesShops; i++)
        {
            string filename = string.Format(filePattern, i + 1);

            Debug.Log("Loading " + filename);
            Texture2D tex = (Texture2D)Resources.Load(filename);


            Texture2D scaleTex = scaled(tex, shopsSize, shopsSize, 0);
            Color[] texCol = scaleTex.GetPixels(0);
            Color[] texComposite = texCol;
            for (int j = 0; j < texCol.Length; j++)
            {
                texComposite[j] = new Color(texCol[j].r, texCol[j].g, texCol[j].b, texCol[j].a);
            }
            Texture2D final = new Texture2D(shopsSize, shopsSize, TextureFormat.RGBA32, true);

            final.SetPixels(texComposite);
            final.Apply(true);
            EditorUtility.CompressTexture(final, compressionFormat, 100);
            final.Apply(true);

            for (int mip = 0; mip < MipCount[shopsSize]; mip++)
            {
                Graphics.CopyTexture(final, 0, mip, textureArray, i, mip);
            }
        }
        textureArray.Apply(false);


        string path = "Assets/CScape/Editor/Resources/TextureArrays/" + TextureFolder + "/shopSignsArray.asset";
        AssetDatabase.CreateAsset(textureArray, path);
        Debug.Log("Saved asset to " + path);
       shopSignsArray = Resources.Load("TextureArrays/" + TextureFolder + "/shopSignsArray") as Texture2DArray;
       cityMaterial.SetTexture("_ShopSigns", shopSignsArray);


    }

    public void CreateVolume()
    {
        int xSize = 32;
        int ySize = 32;
        int zSize = 4;
        Texture3D textureArray = new Texture3D(xSize, ySize, zSize, TextureFormat.RGBA32, true);
        Texture2D scaleTex = scaled(volumeArraySource, xSize, ySize * zSize, 0);
        Color[] texCol = scaleTex.GetPixels(0);
        Color[] texComposite = texCol;
        //for (int j = 0; j < texCol.Length; j++)
        //{
        //    texComposite[j] = new Color(texCol[j].r, texCol[j].g, texCol[j].b, texCol[j].a);
        //}

        Color[] colorArray = new Color[xSize * ySize * zSize];
        for (int z = 0; z < zSize; z++)
        {
            for (int y = 0; y < ySize; y++)
            {
                for (int x = 0; x < xSize; x++)
                {
                    
                    colorArray[x + (y * xSize) + (z * xSize * ySize)] = texComposite[x + (y * xSize) + (z * xSize * ySize)];
                }
            }
        }
        textureArray.SetPixels(colorArray);

        textureArray.Apply(false);


        string path = "Assets/CScape/Editor/Resources/TextureArrays/" + TextureFolder + "/volume.asset";
        AssetDatabase.CreateAsset(textureArray, path);
        Debug.Log("Saved asset to " + path);
        shopSignsArray = Resources.Load("TextureArrays/" + TextureFolder + "/volume") as Texture2DArray;
        cityMaterial.SetTexture("_ShopSigns", shopSignsArray);


    }

    public void CreateStreets()
    {
        // CHANGEME: Filepath must be under "Resources" and named appropriately. Extension is ignored.
        // {0:000} means zero padding of 3 digits, i.e. 001, 002, 003 ... 010, 011, 012, ...
        string filePattern = TextureFolder + "/Street/StreetMap_{0:000}";
        //   string filePatternAO = "CScapeCDK/Street/StreetMap_{0:000}";

        Texture2DArray textureArray = new Texture2DArray(streetsSize, streetsSize, slicesStreets, compressionFormat, true, true);

        // CHANGEME: If your files start at 001, use i = 1. Otherwise change to what you got.
        for (int i = 0; i < slicesStreets; i++)
        {
            string filename = string.Format(filePattern, i + 1);
            //      string filenameAO = string.Format(filePatternAO, i + 1);
            Debug.Log("Loading " + filename);
            Texture2D tex = (Texture2D)Resources.Load(filename);
            //    Texture2D texAO = (Texture2D)Resources.Load(filenameAO);
            //tex.Resize(size, size, TextureFormat.ARGB32, false);
            //texAO.Resize(size, size, TextureFormat.ARGB32, false);
            //tex.Apply();
            //texAO.Apply();

            Texture2D scaleTex = scaled(tex, streetsSize, streetsSize, 0);
            Texture2D scaleTexAO = scaled(tex, streetsSize, streetsSize, 0);
            Color[] texCol = scaleTex.GetPixels(0);
            Color[] texColAO = scaleTexAO.GetPixels(0);
            Color[] texComposite = texCol;
            for (int j = 0; j < texCol.Length; j++)
            {
                texComposite[j] = new Color(texCol[j].r, texCol[j].g, texCol[j].b, texColAO[j].a);
            }
            //    textureArray.SetPixels(texCol, i, 0);
            Texture2D final = new Texture2D(streetsSize, streetsSize, TextureFormat.RGBA32, true);

            final.SetPixels(texComposite);
            final.Apply(true);
            EditorUtility.CompressTexture(final, compressionFormat, 100);
            final.Apply(true);

            for (int mip = 0; mip < MipCount[streetsSize]; mip++)
            {
                Graphics.CopyTexture(final, 0, mip, textureArray, i, mip);
            }
        }
        textureArray.Apply(false);


        string path = "Assets/CScape/Editor/Resources/TextureArrays/" + TextureFolder + "/streetsArray.asset";
        AssetDatabase.CreateAsset(textureArray, path);
        Debug.Log("Saved asset to " + path);
        streetsArray = Resources.Load("TextureArrays/" + TextureFolder + "/streetsArray") as Texture2DArray;
        streetsMaterial.SetTexture("_StreetsArray", streetsArray);


    }

    public void CreateStreetDecalls()
    {
        // CHANGEME: Filepath must be under "Resources" and named appropriately. Extension is ignored.
        // {0:000} means zero padding of 3 digits, i.e. 001, 002, 003 ... 010, 011, 012, ...
        string filePattern = TextureFolder + "/StreetDecalls/StreetDecalls_{0:000}";
        //   string filePatternAO = "CScapeCDK/Street/StreetMap_{0:000}";

        Texture2DArray textureArray = new Texture2DArray(decallsSize, decallsSize, slicesDecalls, compressionFormat, true, true);

        // CHANGEME: If your files start at 001, use i = 1. Otherwise change to what you got.
        for (int i = 0; i < slicesDecalls; i++)
        {
            string filename = string.Format(filePattern, i + 1);
            //      string filenameAO = string.Format(filePatternAO, i + 1);
            Debug.Log("Loading " + filename);
            Texture2D tex = (Texture2D)Resources.Load(filename);
            //    Texture2D texAO = (Texture2D)Resources.Load(filenameAO);
            //tex.Resize(size, size, TextureFormat.ARGB32, false);
            //texAO.Resize(size, size, TextureFormat.ARGB32, false);
            //tex.Apply();
            //texAO.Apply();

            Texture2D scaleTex = scaled(tex, decallsSize, decallsSize, 0);
            Texture2D scaleTexAO = scaled(tex, decallsSize, decallsSize, 0);
            Color[] texCol = scaleTex.GetPixels(0);
            Color[] texColAO = scaleTexAO.GetPixels(0);
            Color[] texComposite = texCol;
            for (int j = 0; j < texCol.Length; j++)
            {
                texComposite[j] = new Color(texCol[j].r, texCol[j].g, texCol[j].b, texColAO[j].a);
            }
            //    textureArray.SetPixels(texCol, i, 0);
            Texture2D final = new Texture2D(decallsSize, decallsSize, TextureFormat.RGBA32, true);

            final.SetPixels(texComposite);
            final.Apply(true);
            EditorUtility.CompressTexture(final, compressionFormat, 100);
            final.Apply(true);

            for (int mip = 0; mip < MipCount[decallsSize]; mip++)
            {
                Graphics.CopyTexture(final, 0, mip, textureArray, i, mip);
            }
        }
        textureArray.Apply(false);


        string path = "Assets/CScape/Editor/Resources/TextureArrays/" + TextureFolder + "/decallsArray.asset";
        AssetDatabase.CreateAsset(textureArray, path);
        Debug.Log("Saved asset to " + path);
        streetsArray = Resources.Load("TextureArrays/" + TextureFolder + "/decallsArray") as Texture2DArray;
        streetsMaterial.SetTexture("_StreetDecalls", streetsArray);


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


    static void _gpu_scale(Texture2D src, int width, int height, FilterMode fmode)
    {
        
        src.filterMode = fmode;
        src.Apply(true);

        
        RenderTexture rtt = new RenderTexture(width, height, 32);

 
        Graphics.SetRenderTarget(rtt);


        GL.LoadPixelMatrix(0, 1, 1, 0);


        GL.Clear(true, true, new Color(0, 0, 0, 0));
        Graphics.DrawTexture(new Rect(0, 0, 1, 1), src);
    }
}
#endif
// Used to generate Texture Array asset
// Menu button is available in GameObject > Create Texture Array
// See CHANGEME in the file
using UnityEngine;
using UnityEditor;

public class TextureArrayNormal : MonoBehaviour
{

    [MenuItem("GameObject/Texture array/Create Normal Array")]
    static void Create()
    {
        // CHANGEME: Filepath must be under "Resources" and named appropriately. Extension is ignored.
        // {0:000} means zero padding of 3 digits, i.e. 001, 002, 003 ... 010, 011, 012, ...
        string filePattern = "normal/depth_{0:000}";
        string filePatternAO = "normal/ao_{0:000}";

        // CHANGEME: Number of textures you want to add in the array
        int slices = 33;

        // CHANGEME: TextureFormat.RGB24 is good for PNG files with no alpha channels. Use TextureFormat.RGB32 with alpha.
        // See Texture2DArray in unity scripting API.
        Texture2DArray textureArray = new Texture2DArray(512, 512, slices, TextureFormat.ARGB32, true);

        // CHANGEME: If your files start at 001, use i = 1. Otherwise change to what you got.
        for (int i = 0; i <= slices; i++)
        {
            string filename = string.Format(filePattern, i + 1);
            string filenameAO = string.Format(filePatternAO, i + 1);
            Debug.Log("Loading " + filename);
            Texture2D tex = (Texture2D)Resources.Load(filename);
            Texture2D texAO = (Texture2D)Resources.Load(filenameAO);
            Color[] texCol = tex.GetPixels(0);
            Color[] texColAO = texAO.GetPixels(0);
            Color[] texComposite = texCol;
            for (int j = 0; j < texCol.Length; j++) {
                texComposite[j] = new Color(texCol[j].r, texCol[j].g, texCol[j].b, texColAO[j].r);
            }
            textureArray.SetPixels(texCol, i, 0);
        }
        textureArray.Apply();

        // CHANGEME: Path where you want to save the texture array. It must end in .asset extension for Unity to recognise it.
        string path = "Assets/Resources/NormaltextureArray.asset";
        AssetDatabase.CreateAsset(textureArray, path);
        Debug.Log("Saved asset to " + path);
    }
}

// After this, you will have a Texture Array asset which you can assign to the shader's Tex attribute!
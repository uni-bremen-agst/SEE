#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using CScape;


namespace CScape
{

    public class TextureArrayTools : MonoBehaviour
    {
        public int slices = 15;
        public bool createArray = false;
        public int resolution;
        public string prefix = "depth";
        public string infile;
        public enum MyArrayType { MASK_MAP, DEPTH, NORMAL, WALLS };
        public MyArrayType arrayType = MyArrayType.MASK_MAP;




        // Update is called once per frame
        void Update()
        {

        }

        public void MakeArray()
        {

            string filePattern = prefix + "_{0:000}";
            Debug.Log(filePattern + ".png");

            // CHANGEME: TextureFormat.RGB24 is good for PNG files with no alpha channels. Use TextureFormat.RGB32 with alpha.
            // See Texture2DArray in unity scripting API.
            Texture2DArray textureArray = new Texture2DArray(resolution, resolution, slices, TextureFormat.ARGB32, true);

            // CHANGEME: If your files start at 001, use i = 1. Otherwise change to what you got.
            for (int i = 1; i <= slices; i++)
            {
                string filename = string.Format(filePattern, i);
                Debug.Log("Loading " + filename + ".png");
                // Texture2D tex = (Texture2D)Resources.Load(filename);
                Texture2D tex = LoadPNG(filename + ".png", 512);
                Debug.Log(filePattern + ".png");
                textureArray.SetPixels(tex.GetPixels(0), i - 1, 0);
            }
            textureArray.Apply();

            // CHANGEME: Path where you want to save the texture array. It must end in .asset extension for Unity to recognise it.
            string path = "Assets/Resources/texArray.asset";
            UnityEditor.AssetDatabase.CreateAsset(textureArray, path);
            Debug.Log("Saved asset to " + path);
        }
        public static Texture2D LoadPNG(string filePath, int res)
        {

            Texture2D tex = null;
            byte[] fileData;

            if (File.Exists(filePath))
            {
                fileData = File.ReadAllBytes(filePath);
                tex = new Texture2D(2, 2);
                tex.LoadImage(fileData);
                Debug.Log("Found");
            }
            return tex;
        }
    }
}
#endif
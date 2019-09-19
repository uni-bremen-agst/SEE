using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using CScape;
//using UnityEditor;

namespace CScape
{

    [CustomEditor(typeof(TextureArrayTools))]

    public class TextureArrayToolsEditor : Editor
    {

        public bool configurePrefab = false;
        public string directory = "";


        void OnEnable()
        {



        }

        public override void OnInspectorGUI()
        {

            TextureArrayTools ta = (TextureArrayTools)target;
            GUILayout.Label("Create Texture Array", EditorStyles.boldLabel);
            ta.resolution = EditorGUILayout.IntField("Resolution", ta.resolution);
            ta.slices = EditorGUILayout.IntField("Slices", ta.slices);
            ta.infile = OpenFileField(ta.infile, "Input files", "Open file");
            ta.arrayType = (TextureArrayTools.MyArrayType)EditorGUILayout.EnumPopup("Out File Type", ta.arrayType);



            if (GUILayout.Button("Generate 3d Array"))
            {
                directory = GetDirectoryName(ta.infile);
                ta.prefix = ta.infile.Remove(ta.infile.Length - 8);
                ta.MakeArray();


            }


            if (GUI.changed)
            {
                EditorUtility.SetDirty(ta);
#if UNITY_5_4_OR_NEWER
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
#endif
            }

            // Update is called once per frame
            //void Update()
            //{
            //    if (createArray)
            //    {
            //        MakeArray();
            //        createArray = false;
            //    }
        }

        public static string OpenFileField(string path, string label, string dialogueTitle, string extension = "")
        {
            GUILayout.BeginHorizontal();
            path = EditorGUILayout.TextField(label, path);
            if (GUILayout.Button(new GUIContent("...", dialogueTitle), GUILayout.Width(22), GUILayout.Height(13)))
            {
                string newFileName = EditorUtility.OpenFilePanel(dialogueTitle, GetDirectoryName(path), extension);
                if (!string.IsNullOrEmpty(newFileName))
                    path = newFileName;
            }
            GUILayout.EndHorizontal();
            return path;
        }

        private static string GetDirectoryName(string fileNameWithPath)
        {
            try
            {
                return System.IO.Path.GetDirectoryName(fileNameWithPath);
            }
            catch (Exception)
            {
            }
            return null;
        }


    }
}

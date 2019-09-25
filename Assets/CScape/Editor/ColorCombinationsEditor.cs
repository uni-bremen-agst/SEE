using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(ColorCombinations))]
public class ColorCombinationsEditor : Editor
{

    public bool borderCol = false;
    public bool nightLightsCol = false;
    public bool glassColors = false;
    public bool concreteCol = false;
    public bool shopCol = false;
    public bool buildingCol = false;
    public bool texScales = false;



    void OnEnable()
    {

        ColorCombinations ba = (ColorCombinations)target;


    }

    public override void OnInspectorGUI()
    {
        ColorCombinations ba = (ColorCombinations)target;
        buildingCol = EditorGUILayout.Foldout(buildingCol, new GUIContent("Faccade Color combinations", "Here you can set faccade color combinations"), true);
        if (buildingCol)
            DropdowDoubleColorArray(ba.buildingColorPairs);

        borderCol = EditorGUILayout.Foldout(borderCol, new GUIContent("WindowsBorderColors", "Here you can set windows Border Colors RGB+a"), true);
        if (borderCol)
            DropdowColorArray(ba.colorBorderArray);
        nightLightsCol = EditorGUILayout.Foldout(nightLightsCol, new GUIContent("Night Lights Color", "Here you can set faccade lights Colors"), true);
        if (nightLightsCol)
            DropdowColorArray(ba.buildingLightsColors);
        texScales = EditorGUILayout.Foldout(texScales, new GUIContent("Surface Texture Scales", "Here you can set individual surface texture Scale values"), true);
        if (texScales)
            DropdowFloatArray(ba.textureScales);

        concreteCol = EditorGUILayout.Foldout(concreteCol, new GUIContent("Concrete Color", "Here you can set concrete colors"), true);
        if (concreteCol)
            DropdowColorArray(ba.concreteColors);
        glassColors = EditorGUILayout.Foldout(glassColors, new GUIContent("Glass colors", "Here you can set windows glass colors"), true);
        if (glassColors)
            DropdowColorArray(ba.glassColors);

        shopCol = EditorGUILayout.Foldout(shopCol, new GUIContent("Shop Color combinations", "Here you can set shop color combinations"), true);
        if (shopCol)
            DropdowDoubleColorArray(ba.advertisingPairs);

        if (GUI.changed)
        {
            EditorUtility.SetDirty(ba);
#if UNITY_5_4_OR_NEWER
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
           
#endif
        }

    }

    private static void DropdowColorArray(Vector4[] col)
    {
        GUILayout.BeginVertical("Box");

        for (int i = 0; i < col.Length; i++)
        {
            col[i] = EditorGUILayout.ColorField("Col " + i, col[i]);
        }
        GUILayout.BeginHorizontal("Box");
        if (GUILayout.Button("-", "Label", GUILayout.Width(20), GUILayout.Height(15)))
            System.Array.Resize(ref col, col.Length - 1);
        if (GUILayout.Button("+", "Label", GUILayout.Width(20), GUILayout.Height(15)))
            System.Array.Resize(ref col, col.Length + 1);
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private static void DropdowFloatArray(float[] floatValue)
    {
        GUILayout.BeginVertical("Box");
        for (int i = 0; i < floatValue.Length; i++)
        {
            floatValue[i] = EditorGUILayout.FloatField("Col " + i, floatValue[i]);
        }
        GUILayout.BeginHorizontal("Box");
        if (GUILayout.Button("-", "Label", GUILayout.Width(20), GUILayout.Height(15)))
            System.Array.Resize(ref floatValue, floatValue.Length - 1);
        if (GUILayout.Button("+", "Label", GUILayout.Width(20), GUILayout.Height(15)))
            System.Array.Resize(ref floatValue, floatValue.Length + 1);
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private static void DropdowDoubleColorArray(Vector4[] col)
    {

        GUILayout.BeginVertical("Box");

        for (int i = 0; i < col.Length / 2; i++)
        {
            GUILayout.BeginHorizontal("Box");
            col[i * 2] = EditorGUILayout.ColorField("Col1 ", col[i * 2]);
            col[(i * 2) + 1] = EditorGUILayout.ColorField("Col2 ", col[(i * 2) + 1]);
            GUILayout.EndHorizontal();

        }
        GUILayout.BeginHorizontal("Box");
        if (GUILayout.Button("-", "Label", GUILayout.Width(20), GUILayout.Height(15)))
            System.Array.Resize(ref col, col.Length - 2);
        if (GUILayout.Button("+", "Label", GUILayout.Width(20), GUILayout.Height(15)))
            System.Array.Resize(ref col, col.Length + 2);
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();


    }


}


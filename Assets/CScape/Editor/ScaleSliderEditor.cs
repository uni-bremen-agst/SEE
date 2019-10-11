// Name this script "ScaleSliderEditor"
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ScaleSlider))]
[CanEditMultipleObjects]
public class ScaleSliderEditor : Editor
{
    public void OnSceneGUI()
    {
        ScaleSlider t = (target as ScaleSlider);

        EditorGUI.BeginChangeCheck();
        float scale = Handles.ScaleSlider(t.scale, Vector3.zero, Vector3.right, Quaternion.identity, 3, 0.5f);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, "Scale Slider");
            t.scale = scale;
            t.Update();
        }
    }
}
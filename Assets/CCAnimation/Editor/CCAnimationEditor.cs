using UnityEngine;
using UnityEditor;
using System.Collections;
using SEE;
using System.IO;
using System.Collections.Generic;
using SEE.DataModel;
using System.Linq;

public class CCAnimationEditor : EditorWindow
{
    [MenuItem("Window/CCAnimationEditor")]

    public static void Init()
    {
        // We try to open the window by docking it next to the Inspector if possible.
        System.Type desiredDockNextTo = System.Type.GetType("UnityEditor.InspectorWindow,UnityEditor.dll");
        CCAnimationEditor window;
        if (desiredDockNextTo == null)
        {
            window = (CCAnimationEditor)EditorWindow.GetWindow(typeof(CCAnimationEditor));
        }
        else
        {
            window = EditorWindow.GetWindow<CCAnimationEditor>(new System.Type[] { desiredDockNextTo });
        }
        window.Show();
    }

    private GameObject accAnimation;

    void OnGUI()
    {

    }
}

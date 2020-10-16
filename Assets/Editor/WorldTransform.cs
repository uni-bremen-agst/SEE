#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace SEEEditor
{
    /// <summary>
    /// Add a menu item "Debug/Print World-Space Transform" that prints the
    /// transform of the selected game object in world space co-ordinates 
    /// (that is, not relative to its parent) to the console of the Unity
    /// editor.
    /// </summary>
    public static class WorldTransform
    {
        [MenuItem("Debug/Print World-Space Transform")]
        public static void PrintWorldSpaceTransform()
        {
            if (Selection.activeGameObject != null)
            {
                GameObject go = Selection.activeGameObject;
                Debug.LogFormat("{0} world position = {1} world scale = {2}\n",
                                go.name, go.transform.position.ToString("F4"), go.transform.lossyScale.ToString("F4"));
            }
        }
    }
}

#endif

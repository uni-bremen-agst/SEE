﻿#if UNITY_EDITOR

using SEE.GO;
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
                Debug.Log($"{go.FullName()} world position = {go.transform.position.ToString("F6")} "
                    + $"world scale = {go.transform.lossyScale.ToString("F6")} "
                    + $"local scale = {go.transform.localScale.ToString("F6")} "
                    + $"world rotation = {go.transform.rotation.eulerAngles.ToString("F6")}\n");
            }
        }
    }
}

#endif

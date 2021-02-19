using System;
using SEE.GO;
using UnityEditor;
using UnityEngine;

namespace SEE.Game.UI.ConfigMenu
{
    public class DynamicUIBehaviour : MonoBehaviour
    {
        protected void MustGetChild(string path, out GameObject target)
        {
            target = gameObject.transform.Find(path)?.gameObject;
            if (!target)
            {
                throw new ArgumentException($"Child not found at path: {path}");
            }
        }

        protected void MustGetComponentInChild<T>(string pathToChild, out T component)
        {
            MustGetChild(pathToChild, out GameObject child);
            child.MustGetComponent(out component);
        }

        protected GameObject MustLoadPrefabAtPath(string path)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(path) ??
                   throw new ArgumentException("Prefab not found at path: {path}");
        }
    }
}
using System;
using SEE.GO;
using UnityEditor;
using UnityEngine;

namespace SEE.Game.UI.ConfigMenu
{
    /// <summary>
    /// The base script that powers all of the input elements of the config menu.
    /// It offers utilities to ease interaction with children or to load prefabs at runtime.
    ///
    /// This implementation makes heavy use of the 'Must'-notation inspired by Golang. 'Must' means
    /// that a function is expected to succeed and otherwise should throw an exception
    /// (that is somewhat equal to an illegal state exception).
    /// </summary>
    public class DynamicUIBehaviour : MonoBehaviour
    {
        /// <summary>
        /// Tries to get a child of the GameObject this script is attached to.
        /// </summary>
        /// <param name="path">The path to the child (as described by Transform#Find).</param>
        /// <param name="target">The target to which the GameObject should be attached.</param>
        /// <exception cref="ArgumentException">Gets thrown if the child couldn't be found.</exception>
        protected void MustGetChild(string path, out GameObject target)
        {
            target = gameObject.transform.Find(path)?.gameObject;
            if (!target)
            {
                throw new ArgumentException($"Child not found at path: {path}");
            }
        }

        /// <summary>
        /// Tries to get a component in a child of the GameObject this script is attached to.
        /// </summary>
        /// <param name="pathToChild">The path to the child (as described by Transform#Find).</param>
        /// <param name="component">The target to which the found component should be attached.</param>
        /// <typeparam name="T">The type of the component to search for.</typeparam>
        protected void MustGetComponentInChild<T>(string pathToChild, out T component)
        {
            MustGetChild(pathToChild, out GameObject child);
            child.MustGetComponent(out component);
        }

        /// <summary>
        /// Wraps the call to the MustGetComponent method of the GameObject this script is attached to.
        /// </summary>
        /// <param name="component">The target to which the found component should be attached.</param>
        /// <typeparam name="T">The type of the component to search for.</typeparam>
        protected void MustGetComponent<T>(out T component)
        {
            gameObject.MustGetComponent(out component);
        }

        /// <summary>
        /// Tries to load a prefab at a given path.
        /// </summary>
        /// <param name="path">The path to look for.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Gets thrown if the path doesn't resolve to a file.</exception>
        protected GameObject MustLoadPrefabAtPath(string path)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(path) ??
                   throw new ArgumentException($"Prefab not found at path: {path}");
        }
    }
}

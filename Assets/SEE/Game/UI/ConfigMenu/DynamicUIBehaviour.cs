// Copyright 2021 Ruben Smidt
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
// IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using SEE.GO;
using System;
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
        // FIXME:
        // It may be more practical to implement these methods as extension methods instead.
        // This way, components for which these methods would be useful weren't forced to inherit
        // from this class.

        /// <summary>
        /// Tries to get a child of the GameObject this script is attached to.
        /// </summary>
        /// <param name="path">The path to the child (as described by Transform#Find).</param>
        /// <param name="target">The target to which the GameObject should be attached.</param>
        /// <exception cref="ArgumentException">Gets thrown if the child couldn't be found.</exception>
        protected void MustGetChild(string path, out GameObject target)
        {
            Transform foundObject = gameObject.transform.Find(path);
            if (foundObject != null)
            {
                target = foundObject.gameObject;
            }
            else
            {
                target = null;
                throw new ArgumentException($"Child not found at path: {path}.");
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
            // FIXME: 
            // It seems like the PrefabInstantiator class does essentially the same thing as
            // this method, with the difference that the PrefabInstantiator will also work in
            // the built game, while this will not(due to using the UnityEditor namespace for
            // LoadAssetAtPath instead of using Resources.Load).
            // This means that any calls to this method should probably be replaced by a call
            // to the prefab instantiator.
            // Note that another important difference is that LoadAssetAtPath requires
            // the .prefab extension to be present, while the PrefabInstantiator's Resources.Load
            // approach requires it not to be present. To fix this, either all paths to the prefabs
            // need to be updated, or the PrefabInstantiator should be updated to automatically
            // remove this suffix. In addition, prefabs to be instantiated at runtime must be
            // in the Resources folder.
            return AssetDatabase.LoadAssetAtPath<GameObject>(path) ??
                   throw new ArgumentException($"Prefab not found at path: {path}.");
        }

        /// <summary>
        /// Returns the transform of the object holding the <see cref="Canvas"/> component
        /// and having the given <paramref name="gameObject"/> as an descendant.
        ///
        /// Assumption: The root (note: root, not just parent!) of <paramref name="gameObject"/>
        /// has an immediate child with the requested <see cref="Canvas"/> component. If that is not
        /// the case, an exception will be thrown.
        /// </summary>
        /// <param name="gameObject">the object from which to start the search</param>
        /// <returns>transform of the object holding the <see cref="Canvas"/> component</returns>
        /// <exception cref="InvalidOperationException">thrown if <paramref name="gameObject"/> has
        /// no child with a <see cref="Canvas"/> object</exception>
        protected static Transform FindCanvas(GameObject gameObject)
        {
            Transform configMenu = gameObject.transform.root;
            foreach (Transform child in configMenu.transform)
            {
                if (child.TryGetComponent(out Canvas canvas))
                {
                    return canvas.transform;
                }
            }
            throw new InvalidOperationException($"Root game object {configMenu.name} has no child with a {nameof(Canvas)} component.");
        }
    }
}

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
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SEE.Game.UI.ConfigMenu
{
    /// <summary>
    /// The base builder for all UI components that are required to be instantiated by themselves
    /// as opposed to be dragged to a GameObject via the editor.
    ///
    /// By providing this fluent interface you can easily add new inputs to a page without ever
    /// needing to touch the prefab of a page.
    /// </summary>
    /// <typeparam name="T">The wrapper component that gets attached to the instantiated prefab.</typeparam>
    public abstract class UIBuilder<T> where T : Component
    {
        /// <summary>
        /// Concrete builders need to specify the path where the prefab is located.
        /// </summary>
        protected abstract string PrefabPath { get; }

        /// <summary>
        /// The prefab from which to instantiate the UI elements.
        /// </summary>
        private GameObject prefab;

        /// <summary>
        /// The UI element instantiated from the prefab.
        /// </summary>
        protected T Instance;

        /// <summary>
        /// Instantiates a new builder and also instantiates the associated prefab.
        /// </summary>
        /// <param name="parent">The parent to which the newly created GameObject should be attached to.</param>
        protected UIBuilder(Transform parent)
        {
            GameObject instanceHost = Object.Instantiate(GetPrefab(), parent);
            Instance = instanceHost.AddComponent<T>();
        }

        /// <summary>
        /// Builds and returns the component.
        /// </summary>
        /// <returns>the UI element instantiated from the prefab</returns>
        public T Build() => Instance;

        /// <summary>
        /// Returns the prefab loaded from <see cref="PrefabPath"/>.
        /// </summary>
        /// <returns>prefab loaded from <see cref="PrefabPath"/></returns>
        private GameObject GetPrefab()
        {
            if (prefab == null)
            {
                // FIXME: This shouldn't use LoadAssetAtPath, because it won't work with the built game
                // (due to using the UnityEditor namespace). Instead, the PrefabInstantiator utility
                // class can be used.
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            }
            return prefab;
        }
    }
}

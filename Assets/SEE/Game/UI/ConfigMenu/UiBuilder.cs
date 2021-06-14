using SEE.GO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
namespace SEE.Game.UI.ConfigMenu
{
    /// <summary>
    /// The base builder for all ui components that are required to be instantiated by themselves
    /// as opposed to be dragged to a GameObject via the editor.
    ///
    /// By providing this fluent interface you can easily add new inputs to a page without never
    /// needing to touch the prefab of a page.
    /// </summary>
    /// <typeparam name="T">The wrapper component that gets attached to the instantiated prefab.</typeparam>
    public abstract class UiBuilder<T> where T : Component
    {
        /// <summary>
        /// Concrete builders need to specify the path where the prefab is located.
        /// </summary>
        protected abstract string PrefabPath { get; }

        private GameObject _prefab;
        protected T Instance;

        /// <summary>
        /// Instantiates a new builder and also instantiates the associated prefab.
        /// </summary>
        /// <param name="parent">The parent to which the newly created GameObject should be attached to.</param>
        protected UiBuilder(Transform parent)
        {
            GameObject instanceHost =
                Object.Instantiate(GetPrefab(), parent);
            instanceHost.AddComponent<T>();
            instanceHost.MustGetComponent(out Instance);
        }

        /// <summary>
        /// Builds an returns the component.
        /// </summary>
        /// <returns></returns>
        public T Build() => Instance;

        private GameObject GetPrefab()
        {
            if (_prefab == null)
            {
                _prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            }
            return _prefab;
        }
    }
}

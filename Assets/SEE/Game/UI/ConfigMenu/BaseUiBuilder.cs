using System;
using SEE.GO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
namespace SEE.Game.UI.ConfigMenu
{
    public abstract class BaseUiBuilder<T> where T : Component
    {
        protected abstract String PrefabPath { get; }

        private GameObject _prefab;
        protected T Instance;

        protected BaseUiBuilder(Transform parent)
        {
            GameObject instanceHost =
                Object.Instantiate(GetPrefab(), parent);
            instanceHost.AddComponent<T>();
            instanceHost.MustGetComponent(out Instance);
        }

        public T Build() => Instance;

        protected GameObject GetPrefab()
        {
            if (_prefab == null)
            {
                _prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            }
            return _prefab;
        }
    }
}

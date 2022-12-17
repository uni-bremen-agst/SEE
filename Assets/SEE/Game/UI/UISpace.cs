using System.Collections.Generic;
using DynamicPanels;
using SEE.Game.UI.LiveDocumentation;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.UI
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class UISpace<T, N> : PlatformDependentComponent
        where T : MonoBehaviour
    {
        /// <summary>
        /// This Method must return the path to the unity prefab which should be rendered in the UI.
        /// </summary>
        /// <returns>The Path in the Resource directory</returns>
        public abstract string GetPrefabPath();

        /// <summary>
        /// This Method must return an identifier which is used to label the ui space.
        /// </summary>
        /// <returns></returns>
        public abstract string GetSpaceName();


        private readonly List<T> _windowsInSpace;

        private readonly List<T> _currentWindowsInSpace;

        private DynamicPanelsCanvas _panelsCanvas;

        private GameObject space;

        public UISpace()
        {
            _windowsInSpace = new List<T>();
            _currentWindowsInSpace = new List<T>();
        }


        protected override void StartDesktop()
        {
            space = Canvas.transform.Find(GetSpaceName())?.gameObject;
            if (!space)
            {
                space = PrefabInstantiator.InstantiatePrefab(GetPrefabPath(), Canvas.transform, false);
                space.name = GetSpaceName();
            }

            space.SetActive(true);
        }

        public void AddWindow(T window)
        {
            // TODO implement method
        }

        public void CloseWindow(T window)
        {
            //TODO implement method 
        }
    }
}
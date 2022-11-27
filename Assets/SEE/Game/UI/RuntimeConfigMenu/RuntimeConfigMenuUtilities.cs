using SEE.DataModel;
using SEE.Game.City;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SEE.Game.UI.RuntimeConfigMenu
{
    public static class RuntimeConfigMenuUtilities
    {
        public static void LoadCity(string cityPath)
        {
            Debug.Log("Select City: " + cityPath);
            GameObject implementation = GameObject.FindGameObjectWithTag(Tags.CodeCity);
            SEECity city;
            if (implementation && implementation.TryGetComponent(out city))
            {
                city.ConfigurationPath.Root = DataPath.RootKind.StreamingAssets;
                city.ConfigurationPath.RelativePath = cityPath;

                city.LoadConfiguration();
                city.LoadData();
                city.DrawGraph();
            }
        }
    
        public static void ResetCity()
        {
            Debug.Log("Reset City");
            GameObject implementation = GameObject.FindGameObjectWithTag(Tags.CodeCity);
            SEECity city;
            if (implementation && implementation.TryGetComponent(out city))
            {
                city.Reset();
            }
        }
        
        public static void AddPrefab(GameObject runtimeConfigMenu, string prefabPath, bool center = false)
        {
            // FIXME: Adding the instance as a child of the canvas not working
            Transform canvas = runtimeConfigMenu.transform;
            GameObject instance = PrefabInstantiator.InstantiatePrefab(prefabPath, canvas);
            if (center)
            {
                RectTransform rectTransform = instance.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = new Vector2(0, 0);
            }
            
        }
        
        public static void AddActionToButton(GameObject runtimeConfigMenu, string buttonPath, UnityAction action)
        {
            GameObject debugButton = runtimeConfigMenu.transform.Find(buttonPath).gameObject;
            Button debugButtonComponent = debugButton.GetComponent<Button>();
            debugButtonComponent.onClick.AddListener(action);
        }
    }
}

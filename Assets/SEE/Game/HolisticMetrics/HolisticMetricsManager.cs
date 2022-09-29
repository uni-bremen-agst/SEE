using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Michsky.UI.ModernUIPack;
using SEE.Game.HolisticMetrics.Metrics;

namespace SEE.Game.HolisticMetrics
{
    internal class HolisticMetricsManager : MonoBehaviour
    {
        // Let the user pass a custom name to the metrics and an id so we can delete it easily
        
        /// <summary>
        /// Reference to the menu GameObject. This is the menu which allows the user to customize the metrics board.
        /// </summary>
        [SerializeField] private GameObject menu;

        /// <summary>
        /// Reference to the MetricsCanvas. When the player creates a new widget, it will be added here.
        /// </summary>
        [SerializeField] private GameObject metricsCanvas;
        
        /// <summary>
        /// The dropdown menu that will allow the user to select a metric type.
        /// </summary>
        [SerializeField] private CustomDropdown selectMetric;
        
        /// <summary>
        /// The dropdown menu that will allow the user to select a widget.
        /// </summary>
        [SerializeField] private CustomDropdown selectWidget;

        /// <summary>
        /// When Start() is called, this will be filled with the types of all classes that inherit from class "Metric".
        /// </summary>
        private Type[] metricTypes;
        
        /// <summary>
        /// When Start() is called, this will be filled with all widget prefabs from
        /// Assets/Resources/Prefabs/HolisticMetrics.
        /// </summary>
        private GameObject[] widgetPrefabs;

        /// <summary>
        /// This contains references to all instantiated widget GameObjects.
        /// </summary>
        private List<GameObject> widgetInstances = new List<GameObject>();

        /// <summary>
        /// Reference to the canvas controller of the metrics canvas.
        /// </summary>
        private CanvasController canvasController;

        /// <summary>
        /// Dynamically gets types of all classes that inherit from Metric and puts them in an array. Also dynamically
        /// gets all widget prefabs from the Resources folder and puts them in an array. After filling the array, the
        /// dropdowns in the menu are filled with the array values.
        /// </summary>
        private void Start()
        {
            metricTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(domainAssembly => domainAssembly.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(Metric)))
                .ToArray();
                
            widgetPrefabs = Resources.LoadAll<GameObject>("Prefabs/HolisticMetrics/Widgets");

            foreach (Type metricType in metricTypes)
            {
                selectMetric.CreateNewItem(metricType.Name, null);
            }

            foreach (GameObject widgetPrefab in widgetPrefabs)
            {
                selectWidget.CreateNewItem(widgetPrefab.name, null);
            }

            canvasController = metricsCanvas.GetComponent<CanvasController>();
        }
        
        internal void ToggleMenu()
        {
            menu.SetActive(!menu.activeInHierarchy);
        }

        /// <summary>
        /// Using the currently selected metric and widget from the two dropdowns next to the button that calls this
        /// method, this method will spawn a widget on the metrics board. 
        /// </summary>
        public void AddWidget()
        {
            Type selectedMetric = metricTypes[selectMetric.selectedItemIndex];
            GameObject selectedWidget = widgetPrefabs[selectWidget.selectedItemIndex];
            canvasController.AddMetric(selectedMetric, selectedWidget);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Michsky.UI.ModernUIPack;
using SEE.Game.City;
using SEE.Game.HolisticMetrics.Metrics;
using SEE.Game.HolisticMetrics.WidgetControllers;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.HolisticMetrics.Components
{
    /// <summary>
    /// This class manages a holistic metrics board.
    /// </summary>
    internal class WidgetsManager : MonoBehaviour
    {
        /// <summary>
        /// This contains references to all widgets on the board each represented by one WidgetController and one
        /// Metric. This list is needed so we can refresh the metrics.
        /// </summary>
        internal readonly List<(WidgetController, Metric)> widgets = new List<(WidgetController, Metric)>();

        /// <summary>
        /// The title of the board that this controller controls.
        /// </summary>
        private string title;

        /// <summary>
        /// The list of all available metric types. This is shared between all BoardController instances and is not
        /// expected to change at runtime. 
        /// </summary>
        private Type[] metricTypes;

        /// <summary>
        /// The array of all available widget prefabs. This is shared by all BoardController instances and is not
        /// expected to change at runtime.
        /// </summary>
        private GameObject[] widgetPrefabs;

        /// <summary>
        /// The dropdown UI element that allows the player to select a code city for which the metrics should be
        /// displayed.
        /// </summary>
        [SerializeField] private CustomDropdown citySelection;

        /// <summary>
        /// The array of code cities in the scene. This is needed because the player can select which code city's
        /// metrics will be displayed on each board.
        /// </summary>
        private static SEECity[] cities; 
        
        /// <summary>
        /// A house icon sprite (instantiated in Awake()). 
        /// </summary>
        private static Sprite houseIcon;

        [SerializeField] private GameObject boardMover;
        
        /// <summary>
        /// Instantiates the metricTypes and widgetPrefabs arrays.
        /// </summary>
        private void Awake()
        {
            widgetPrefabs = 
                Resources.LoadAll<GameObject>("Prefabs/HolisticMetrics/Widgets");
            
            metricTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(domainAssembly => domainAssembly.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(Metric)))
                .ToArray();
            
            houseIcon = Resources.Load<Sprite>("Materials/40+ Simple Icons - Free/Home_Simple_Icons_UI");
            
            citySelection.dropdownEvent.AddListener(Redraw);
            
            OnCitySelectionClick();
        }
        
        /// <summary>
        /// Returns the title of this manager's metrics board.
        /// </summary>
        /// <returns>The title of the metrics board of this manager.</returns>
        internal string GetTitle()
        {
            return title;
        }
        
        /// <summary>
        /// Sets the title of this manager's metrics board.
        /// </summary>
        /// <param name="newTitle">The new title you want to give the metrics board</param>
        internal void SetTitle(string newTitle)
        {
            title = newTitle;
            gameObject.GetComponentInChildren<Text>().text = newTitle;
        }
        
        /// <summary>
        /// Adds the desired widget to the board.
        /// </summary>
        /// <param name="widgetConfiguration">The configuration of the new widget.</param>
        internal void Create(WidgetConfiguration widgetConfiguration)
        {
            GameObject widget = Array.Find(widgetPrefabs,
                element => element.name.Equals(widgetConfiguration.WidgetName));
            Type metricType = Array.Find(metricTypes,
                type => type.Name.Equals(widgetConfiguration.MetricType));
            if (widget is null)
            {
                Debug.LogError("Could not load widget because the widget name from the configuration " +
                               "file matches no existing widget prefab. This could be because the configuration " +
                               "file was manually changed.");
            }
            else if (metricType is null)
            {
                Debug.LogError("Could not load metric because the metric type from the configuration " +
                               "file matches no existing metric type. This could be because the configuration " +
                               "file was manually changed.");
            }
            else
            {
                GameObject widgetInstance = Instantiate(widget, transform);
                widgetInstance.transform.localPosition = widgetConfiguration.Position;
                WidgetController widgetController = widgetInstance.GetComponent<WidgetController>();
                Metric metricInstance = (Metric)widgetInstance.AddComponent(metricType);
                widgetController.ID = widgetConfiguration.ID;
                widgets.Add((widgetController, metricInstance));
                widgetController.Display(metricInstance.Refresh(GetSelectedCity()));
            }
        }

        /// <summary>
        /// Adds a WidgetDeleter component to all widgets managed by this manager.
        /// </summary>
        internal void AddWidgetDeleters()
        {
            WidgetDeleter.Setup();
            foreach ((WidgetController, Metric) tuple in widgets)
            {
                tuple.Item1.gameObject.AddComponent<WidgetDeleter>();
            }
        }

        /// <summary>
        /// Deletes the widget with the given ID if it is managed by this manager.
        /// </summary>
        /// <param name="widgetID">The ID of the widget to delete</param>
        internal void Delete(Guid widgetID)
        {
            (WidgetController, Metric) widget =
                widgets.Find(widget => widget.Item1.ID.Equals(widgetID));
            WidgetController widgetController = widget.Item1;
            widgets.Remove(widget);
            Destroy(widgetController.gameObject);
        }

        /// <summary>
        /// This has to be assigned to the city selection dropdown from the unity editor. It needs to be called
        /// everytime the dropdown is being clicked so we can then update the list of code cities.
        /// </summary>
        public void OnCitySelectionClick()
        {
            cities = FindObjectsOfType<SEECity>();
            
            string oldSelection = citySelection.selectedText.text;
            
            citySelection.dropdownItems.Clear();

            foreach (SEECity city in cities)
            {
                citySelection.CreateNewItem(city.name, houseIcon);
            }

            // If the city that was previously selected still exists, we want to reselect it. Otherwise the selection
            // might change as soon as the player first clicks the dropdown to expand it.
            if (cities.Any(city => city.name.Equals(oldSelection)))
            {
                citySelection.selectedItemIndex =
                    citySelection.dropdownItems.IndexOf(
                        citySelection.dropdownItems.Find(item => item.itemName.Equals(oldSelection)));
            }
            
            citySelection.SetupDropdown();
            
            // Refresh the widgets because the selected city could have changed already
            Redraw();
        }

        /// <summary>
        /// This method returns the code city that is currently selected in the dropdown.
        /// </summary>
        /// <returns>The currently selected code city</returns>
        private SEECity GetSelectedCity()
        {
            var selectedCity = cities.FirstOrDefault(city => city.name.Equals(citySelection.selectedText.text));
            if (selectedCity is null)
            {
                // This should not be possible, so throw an exception if this happens.
                throw new Exception();
            }
            return selectedCity;
        }

        /// <summary>
        /// This toggles a child object of the board that allows the player to move the board around.
        /// </summary>
        internal void ToggleMoving(bool enable)
        {
            boardMover.SetActive(enable);
        }
        
        /// <summary>
        /// Whenever a code city changes, this method needs to be called. It will call the Refresh() methods of all
        /// Metrics and display the results on the widgets.
        /// </summary>
        /// <param name="index">This parameter will be passed to the method by the dropdown when it is being clicked,
        /// but it is not used and can be ignored when manually calling the method.</param>
        internal void Redraw(int index = -1)
        {
            foreach ((WidgetController, Metric) tuple in widgets)
            {
                // Recalculate the metric
                MetricValue metricValue = tuple.Item2.Refresh(GetSelectedCity());

                // Display the new value on the widget
                tuple.Item1.Display(metricValue);
            }
        }
    }
}

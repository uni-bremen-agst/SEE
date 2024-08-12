using System;
using System.Collections.Generic;
using System.Linq;
using Michsky.UI.ModernUIPack;
using SEE.Controls.Actions.HolisticMetrics;
using SEE.DataModel;
using SEE.Game.City;
using SEE.Game.HolisticMetrics.ActionHelpers;
using SEE.Game.HolisticMetrics.Metrics;
using SEE.Game.HolisticMetrics.WidgetControllers;
using SEE.UI.Notification;
using SEE.Net.Actions.HolisticMetrics;
using SEE.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.HolisticMetrics
{
    /// <summary>
    /// This class manages a holistic metrics board.
    /// </summary>
    internal class WidgetsManager : MonoBehaviour, IObserver<ChangeEvent>
    {
        /// <summary>
        /// Path to the widget prefabs.
        /// </summary>
        private const string widgetsPath = "Prefabs/HolisticMetrics/Widgets";

        /// <summary>
        /// Path to the house icon used in the dropdown menu on the metrics boards.
        /// </summary>
        private const string houseIconPath = "Materials/40+ Simple Icons - Free/Home_Simple_Icons_UI";

        /// <summary>
        /// The dropdown UI element that allows the player to select a code city for which the metrics should be
        /// displayed.
        /// </summary>
        [SerializeField] private CustomDropdown citySelection;

        /// <summary>
        /// The game object that can be clicked to move the board around.
        /// </summary>
        [SerializeField] private GameObject boardMover;

        /// <summary>
        /// This contains references to all widgets on the board each represented by one WidgetController and one
        /// Metric. This list is needed so we can refresh the metrics.
        /// </summary>
        internal readonly List<(WidgetController, Metric)> Widgets = new();

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
        /// The array of code cities in the scene. This is needed because the player can select which code city's
        /// metrics will be displayed on each board.
        /// </summary>
        private static SEECity[] cities;

        /// <summary>
        /// A house icon sprite (instantiated in Awake()).
        /// </summary>
        private static Sprite houseIcon;

        /// <summary>
        /// This will be used to unsubscribe from the graph we currently listen to.
        /// </summary>
        private IDisposable graphDisposable;

        /// <summary>
        /// Instantiates the metricTypes and widgetPrefabs arrays.
        /// </summary>
        private void Awake()
        {
            houseIcon = Resources.Load<Sprite>(houseIconPath);
            widgetPrefabs = Resources.LoadAll<GameObject>(widgetsPath);
            metricTypes = Metric.GetTypes();

            citySelection.dropdownEvent.AddListener(Redraw);
            citySelection.dropdownEvent.AddListener(CitySelectionChanged);
            OnCitySelectionClick();

            graphDisposable = GetSelectedCity()?.LoadedGraph?.Subscribe(this);
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
        internal void Create(WidgetConfig widgetConfiguration)
        {
            GameObject widget = Array.Find(widgetPrefabs,
                element => element.name.Equals(widgetConfiguration.WidgetName));
            Type metricType = Array.Find(metricTypes, type => type.Name.Equals(widgetConfiguration.MetricType));
            if (widget is null)
            {
                ShowNotification.Error("Metric-board error",
                                       "Could not load widget because the widget name from the configuration " +
                                       "file matches no existing widget prefab. This could be because the configuration " +
                                       "file was manually changed.\n");
            }
            else if (metricType is null)
            {
                ShowNotification.Error("Metric-board error",
                                       "Could not load metric because the metric type from the configuration " +
                                       "file matches no existing metric type. This could be because the configuration " +
                                       "file was manually changed.\n");
            }
            else
            {
                GameObject widgetInstance = Instantiate(widget, transform);
                widgetInstance.transform.localPosition = widgetConfiguration.Position;
                WidgetController widgetController = widgetInstance.GetComponent<WidgetController>();
                Metric metricInstance = (Metric)widgetInstance.AddComponent(metricType);
                widgetController.ID = widgetConfiguration.ID;
                Widgets.Add((widgetController, metricInstance));
                try
                {
                    widgetController.Display(metricInstance.Refresh(GetSelectedCity()));
                }
                catch (Exception exception)
                {
                    ShowNotification.Error("Metric-board error",
                                           "There was an error when displaying the metric on the newly created "
                                           + $"widget, this is the exception: {exception.Message}", log: false);
                    throw;
                }
            }
        }

        /// <summary>
        /// Moves a widget to a new position.
        /// </summary>
        /// <param name="widgetID">The ID of the widget to move</param>
        /// <param name="position">The position to which the widget should be moved</param>
        internal void Move(Guid widgetID, Vector3 position)
        {
            WidgetController controller = Widgets
                .Find(widget => widget.Item1.ID.Equals(widgetID)).Item1;
            controller.transform.position = position;
        }

        /// <summary>
        /// Adds / removes a WidgetDeleter component to / from all widgets managed by this manager.
        /// </summary>
        /// <param name="enable">Whether we want to listen for clicks on the widgets for deletion</param>
        internal void ToggleWidgetDeleting(bool enable)
        {
            if (enable)
            {
                foreach ((WidgetController controller, Metric) tuple in Widgets)
                {
                    tuple.controller.gameObject.AddComponent<WidgetDeleter>();
                }
            }
            else
            {
                foreach ((WidgetController controller, Metric) tuple in Widgets)
                {
                    Destroyer.Destroy(tuple.controller.gameObject.GetComponent<WidgetDeleter>());
                }
            }
        }

        /// <summary>
        /// Determines whether one of the widgets that this manager manages is marked as "to be deleted".
        /// </summary>
        /// <param name="widgetConfig">If there is a widget that is to be deleted, that widget's configuration will be
        /// assigned to this parameter.</param>
        /// <returns>Whether one of the widgets that this manager manages is marked as "to be deleted".</returns>
        internal bool GetWidgetDeletion(out WidgetConfig widgetConfig)
        {
            foreach ((WidgetController controller, Metric) widget in Widgets)
            {
                if (widget.controller.GetComponent<WidgetDeleter>().GetDeletion(out widgetConfig))
                {
                    return true;
                }
            }

            widgetConfig = null;
            return false;
        }

        /// <summary>
        /// Deletes the widget with the given ID if it is managed by this manager.
        /// </summary>
        /// <param name="widgetID">The ID of the widget to delete</param>
        internal void Delete(Guid widgetID)
        {
            (WidgetController, Metric) widget =
                Widgets.Find(widget => widget.Item1.ID.Equals(widgetID));
            WidgetController widgetController = widget.Item1;
            Widgets.Remove(widget);
            Destroyer.Destroy(widgetController.gameObject);
        }

        /// <summary>
        /// This has to be assigned to the city selection dropdown from the unity editor. It needs to be called
        /// everytime the dropdown is being clicked so we can then update the list of code cities.
        /// </summary>
        public void OnCitySelectionClick()
        {
            cities = FindObjectsOfType<SEECity>().Where(c => c?.LoadedGraph != null).ToArray();
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
                citySelection.selectedItemIndex = citySelection.dropdownItems.IndexOf(
                    citySelection.dropdownItems.Find(item => item.itemName.Equals(oldSelection)));
            }

            citySelection.SetupDropdown();
            Redraw();  // Refresh the widgets because the selected city could have changed already
        }

        /// <summary>
        /// This method returns the code city that is currently selected in the dropdown.
        /// </summary>
        /// <returns>The currently selected code city</returns>
        private SEECity GetSelectedCity()
        {
            return cities.Where(x => x != null)
                         .FirstOrDefault(city => city.name.Equals(citySelection.selectedText.text));
        }

        /// <summary>
        /// This toggles a child object of the board that allows the player to move the board around.
        /// </summary>
        internal void ToggleMoving(bool enable)
        {
            boardMover.SetActive(enable);
        }

        /// <summary>
        /// Determines whether the board managed by this manager has a movement that has not yet been fetched
        /// by <see cref="MoveBoardAction"/>
        /// </summary>
        /// <param name="oldPosition">The position of the board before the movement</param>
        /// <param name="newPosition">The new position of the board</param>
        /// <param name="oldRotation">The rotation of the board before the movement</param>
        /// <param name="newRotation">The new rotation of the board</param>
        /// <returns>Whether the board managed by this manager has a movement that has not yet been fetched by
        /// <see cref="MoveBoardAction"/></returns>
        internal bool TryGetMovement(out Vector3 oldPosition, out Vector3 newPosition, out Quaternion oldRotation,
            out Quaternion newRotation)
        {
            return boardMover.GetComponent<BoardMover>()
                .TryGetMovement(out oldPosition, out newPosition, out oldRotation, out newRotation);
        }

        /// <summary>
        /// This method can be invoked to toggle the move-ability of the widgets.
        /// </summary>
        /// <param name="enable">Whether or not the widgets should be move-able now</param>
        internal void ToggleWidgetsMoving(bool enable)
        {
            foreach ((WidgetController, Metric) widget in Widgets)
            {
                widget.Item1.ToggleMoving(enable);
            }
        }

        /// <summary>
        /// Returns the first widget movement.
        /// </summary>
        /// <param name="originalPosition">The position of the widget before the movement</param>
        /// <param name="newPosition">The position to which the widget was moved</param>
        /// <param name="widgetID">The ID of the widget that was moved.</param>
        /// <returns>Whether a widget movement was found</returns>
        internal bool TryGetWidgetMovement(out Vector3 originalPosition, out Vector3 newPosition, out Guid widgetID)
        {
            foreach ((WidgetController, Metric) widget in Widgets)
            {
                if (widget.Item1.GetComponent<WidgetMover>().TryGetMovement(out originalPosition, out newPosition))
                {
                    widgetID = widget.Item1.ID;
                    return true;
                }
            }

            originalPosition = Vector3.zero;
            newPosition = Vector3.zero;
            widgetID = Guid.NewGuid();
            return false;
        }

        /// <summary>
        /// This method will be called when the player selects a different city in the dropdown. In that case, we want
        /// to send a message to all other clients so the selection changes on their boards too.
        /// </summary>
        /// <param name="itemIndex">The index of the dropdown item that is now selected</param>
        private void CitySelectionChanged(int itemIndex)
        {
            SEECity selectedCity = GetSelectedCity();
            if (selectedCity != null)
            {
                string cityName = selectedCity.name;
                new SwitchCityNetAction(title, cityName).Execute();
                graphDisposable?.Dispose();
                graphDisposable = selectedCity.LoadedGraph?.Subscribe(this);
            }
        }

        /// <summary>
        /// This method can be called when another client chose another city in the dropdown in which case we also want
        /// to change the city selection here. That is what this method tries.
        /// </summary>
        /// <param name="cityName">The name of the city to select</param>
        internal void SwitchCity(string cityName)
        {
            // Update the values on the list so they hopefully are synchronous to the list on the machine of the
            // requester
            OnCitySelectionClick();

            int indexInDropdown = citySelection.dropdownItems
                .FindIndex(city => city.itemName.Equals(cityName));
            if (indexInDropdown == -1)  // The return value if it was not found
            {
                Debug.LogError("From network got a signal to switch the city on a board to a city that " +
                               " cannot be found here\n");
                return;
            }

            citySelection.selectedItemIndex = indexInDropdown;
            Redraw();

            // Start listening to changes in the newly selected city
            graphDisposable?.Dispose();
            graphDisposable = GetSelectedCity()?.LoadedGraph?.Subscribe(this);
        }

        /// <summary>
        /// This method will be called when a graph has been drawn. In that case, we want to subscribe to that graph.
        /// </summary>
        internal void OnGraphDraw()
        {
            graphDisposable?.Dispose();
            graphDisposable = GetSelectedCity()?.LoadedGraph?.Subscribe(this);
            Redraw();
        }

        /// <summary>
        /// Whenever a code city changes, this method needs to be called. It will call the Refresh() methods of all
        /// Metrics and display the results on the widgets.
        /// </summary>
        /// <param name="index">This parameter will be passed to the method by the dropdown when it is being clicked,
        /// but it is not used and can be ignored when manually calling the method.</param>
        private void Redraw(int index = -1)
        {
            foreach ((WidgetController, Metric) tuple in Widgets)
            {
                MetricValue metricValue = tuple.Item2.Refresh(GetSelectedCity());
                tuple.Item1.Display(metricValue);
            }
        }

        /// <summary>
        /// This method does nothing. It is only here because it has to be because we implement the IObserver interface.
        /// </summary>
        public void OnCompleted()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// This method does nothing. It is only here because it has to be because we implement the IObserver interface.
        /// </summary>
        /// <param name="error"></param>
        public void OnError(Exception error)
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// This method is called when a new GraphEvent occurs on the graph we currently display with this board. It
        /// will trigger that the metrics get recalculated.
        /// </summary>
        /// <param name="value">The GraphEvent that occured. This is not being used.</param>
        public void OnNext(ChangeEvent value)
        {
            Redraw();
        }
    }
}

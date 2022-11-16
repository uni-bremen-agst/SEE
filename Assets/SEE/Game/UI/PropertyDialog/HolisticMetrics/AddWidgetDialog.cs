using System;
using System.Linq;
using SEE.Controls;
using SEE.Game.HolisticMetrics;
using SEE.Game.HolisticMetrics.Metrics;
using SEE.Game.UI.Notification;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SEE.Game.UI.PropertyDialog.HolisticMetrics
{
    /// <summary>
    /// This class implements a dialog that allows the player to configure a widget and then add it to a board.
    /// </summary>
    internal class AddWidgetDialog : HolisticMetricsDialog
    {
        /// <summary>
        /// The property dialog of this dialog.
        /// </summary>
        private PropertyDialog propertyDialog;

        /// <summary>
        /// The selection allowing the player to select the metric that should be displayed by the new widget.
        /// </summary>
        private SelectionProperty selectedMetric;

        /// <summary>
        /// The selection allowing the player to select the widget that should be used to display the selected metric.
        /// </summary>
        private SelectionProperty selectedWidget;

        /// <summary>
        /// When this class is constructed, this will be filled with the types of all classes that inherit from class
        /// "Metric".
        /// </summary>
        private readonly Type[] metricTypes;

        /// <summary>
        /// When this class is constructed, this will be filled with all widget prefabs from
        /// Assets/Resources/Prefabs/HolisticMetrics.
        /// </summary>
        private readonly GameObject[] widgetPrefabs;

        /// <summary>
        /// The constructor. This will fill the metricTypes and widgetPrefabs arrays.
        /// </summary>
        internal AddWidgetDialog()
        {
            // Load the metric types
            metricTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(domainAssembly => domainAssembly.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(Metric)))
                .ToArray();

            // Load the widget prefabs
            const string widgetPrefabsPath = "Prefabs/HolisticMetrics/Widgets";
            widgetPrefabs = Resources.LoadAll<GameObject>(widgetPrefabsPath);
        }
        
        /// <summary>
        /// This method will display this dialog to the player.
        /// </summary>
        internal void Open()
        {
            dialog = new GameObject("Add widget dialog");
            PropertyGroup group = dialog.AddComponent<PropertyGroup>();
            group.Name = "Add widget dialog";

            selectedMetric = dialog.AddComponent<SelectionProperty>();
            selectedMetric.Name = "Select metric";
            selectedMetric.Description = "Select the metric you want to display";
            string[] metricOptions = metricTypes.Select(type => type.Name).ToArray();
            selectedMetric.AddOptions(metricOptions);
            selectedMetric.Value = metricOptions[0];
            group.AddProperty(selectedMetric);

            selectedWidget = dialog.AddComponent<SelectionProperty>();
            selectedWidget.Name = "Select widget";
            selectedWidget.Description = "Select the widget that is supposed to display the metric";
            string[] widgetOptions = widgetPrefabs.Select(prefab => prefab.name).ToArray();
            selectedWidget.AddOptions(widgetOptions);
            selectedWidget.Value = widgetOptions[0];
            group.AddProperty(selectedWidget);
            
            propertyDialog = dialog.AddComponent<PropertyDialog>();
            propertyDialog.Title = "Add widget";
            propertyDialog.Description = "Configure the widget; then hit OK button. Then click on any metrics board" +
                                         "where you want to place the widget.";
            propertyDialog.Icon = Resources.Load<Sprite>("Materials/ModernUIPack/Plus");
            propertyDialog.AddGroup(group);
            
            propertyDialog.OnConfirm.AddListener(AddWidget);
            propertyDialog.OnCancel.AddListener(EnableKeyboardShortcuts);
            
            SEEInput.KeyboardShortcutsEnabled = false;
            propertyDialog.DialogShouldBeShown = true;
        }

        /// <summary>
        /// Will be called when the player confirms their selection. This method will close the dialog and then make the
        /// boards listen for a left click from the user. When that happens, the new widget will be added where the
        /// player left-clicked on a board.
        /// </summary>
        private void AddWidget()
        {
            // Create a widget configuration
            WidgetConfig widgetConfiguration = new WidgetConfig()
            {
                ID = Guid.NewGuid(),
                MetricType = selectedMetric.Value,
                WidgetName = selectedWidget.Value
            };
            
            // Add WidgetPositionGetters to all boards
            BoardsManager.AddWidgetAdders(widgetConfiguration);

            // Ensure they all get deleted once one of them gets a click (that should probably not be done here)
            
            // Close the dialog
            Object.Destroy(dialog);
            SEEInput.KeyboardShortcutsEnabled = true;

            ShowNotification.Info(
                "Position the widget",
                "Click on a metrics board where you want to position the widget.");
        }
    }
}

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
    internal class AddWidgetDialog
    {
        private GameObject dialog;

        private PropertyDialog propertyDialog;

        private SelectionProperty selectedMetric;

        private SelectionProperty selectedWidget;

        /// <summary>
        /// When Start() is called, this will be filled with the types of all classes that inherit from class "Metric".
        /// </summary>
        private readonly Type[] metricTypes;

        /// <summary>
        /// When Start() is called, this will be filled with all widget prefabs from
        /// Assets/Resources/Prefabs/HolisticMetrics.
        /// </summary>
        private readonly GameObject[] widgetPrefabs;

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

        private void AddWidget()
        {
            // Create a widget configuration
            WidgetConfiguration widgetConfiguration = new WidgetConfiguration()
            {
                MetricType = selectedMetric.Value,
                WidgetName = selectedWidget.Value
            };
            
            // Add WidgetPositionGetters to all boards
            BoardsManager.PositionWidget(widgetConfiguration);

            // Ensure they all get deleted once one of them gets a click (that should probably not be done here)
            
            // Close the dialog
            Object.Destroy(dialog);
            SEEInput.KeyboardShortcutsEnabled = true;

            ShowNotification.Info(
                "Position the widget",
                "Click on a metrics board where you want to position the widget.");
        }
        
        private void EnableKeyboardShortcuts()
        {
            // Destroy the dialog GameObject
            Object.Destroy(dialog);
            
            SEEInput.KeyboardShortcutsEnabled = true;
        }
    }
}
using System.Linq;
using SEE.Controls;
using SEE.Game.HolisticMetrics;
using UnityEngine;

namespace SEE.Game.UI.PropertyDialog.HolisticMetrics
{
    /// <summary>
    /// This class implements a dialog that allows the player to configure a widget and then add it to a board.
    /// </summary>
    internal class AddWidgetDialog : HolisticMetricsDialog
    {
        /// <summary>
        /// This gets assigned the metric type the player selected when the player confirms his input.
        /// </summary>
        private string metricType;

        /// <summary>
        /// This gets assigned the widget type the player selected when the player confirms his input.
        /// </summary>
        private string widgetName;

        /// <summary>
        /// The selection allowing the player to select the metric that should be displayed by the new widget.
        /// </summary>
        private SelectionProperty selectedMetric;

        /// <summary>
        /// The selection allowing the player to select the widget that should be used to display the selected metric.
        /// </summary>
        private SelectionProperty selectedWidget;
        
        /// <summary>
        /// This method will display this dialog to the player.
        /// </summary>
        internal void Open()
        {
            gotInput = false;
            
            dialog = new GameObject("Add widget dialog");
            PropertyGroup group = dialog.AddComponent<PropertyGroup>();
            group.Name = "Add widget dialog";

            selectedMetric = dialog.AddComponent<SelectionProperty>();
            selectedMetric.Name = "Select metric";
            selectedMetric.Description = "Select the metric you want to display";
            string[] metricOptions = WidgetsManager.MetricTypes.Select(type => type.Name).ToArray();
            selectedMetric.AddOptions(metricOptions);
            selectedMetric.Value = metricOptions[0];
            group.AddProperty(selectedMetric);

            selectedWidget = dialog.AddComponent<SelectionProperty>();
            selectedWidget.Name = "Select widget";
            selectedWidget.Description = "Select the widget that is supposed to display the metric";
            string[] widgetOptions = WidgetsManager.WidgetPrefabs.Select(prefab => prefab.name).ToArray();
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
            propertyDialog.OnCancel.AddListener(Cancel);
            
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
            gotInput = true;
            metricType = selectedMetric.Value;
            widgetName = selectedWidget.Value;
            Close();
        }

        /// <summary>
        /// Fetches the widget configuration.
        /// </summary>
        /// <param name="metric">This gets assigned the metric type, if it exists and wasn't already fetched</param>
        /// <param name="widget">This gets assigned the widget type, if it exists and wasn't already fetched</param>
        /// <returns>Whether there is a widget configuration present that wasn't fetched already</returns>
        internal bool GetConfig(out string metric, out string widget)
        {
            if (gotInput)
            {
                metric = metricType;
                widget = widgetName;
                gotInput = false;
                return true;
            }

            metric = null;
            widget = null;
            return false;
        }
    }
}

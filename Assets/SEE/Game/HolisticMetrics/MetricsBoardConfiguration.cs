using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.HolisticMetrics
{
    /// <summary>
    /// This class can hold all the information that is needed to configure a holistic metrics board.
    /// </summary>
    [Serializable]
    public class MetricsBoardConfiguration
    {
        /// <summary>
        /// The title that will be displayed in the top left corner of this display.
        /// </summary>
        public string title;
        
        /// <summary>
        /// The coordinates of where this board should be placed in the scene.
        /// </summary>
        public Vector3 Position;
        
        /// <summary>
        /// All the widgets that should be displayed on this board.
        /// </summary>
        public List<WidgetConfiguration> WidgetConfigurations;
    }

    /// <summary>
    /// This class can hold all the information that is needed to configure a metric widget.
    /// </summary>
    [Serializable]
    public class WidgetConfiguration
    {
        // Maybe this should rather be the anchor reference...
        /// <summary>
        /// The x and y coordinate of this widget.
        /// </summary>
        public Vector3 Position;
        
        /// <summary>
        /// The metric type that should be displayed.
        /// </summary>
        public Type MetricType;

        /// <summary>
        /// The name of this widget that will display the metric. This is equivalent to the file name of the widget
        /// (without the file name extension).
        /// </summary>
        public string WidgetName;
    }
}
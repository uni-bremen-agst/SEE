using System;
using System.Collections.Generic;
using SEE.Game.HolisticMetrics.Metrics;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.HolisticMetrics
{
    /// <summary>
    /// This class can hold all the information that is needed to configure a holistic metrics board.
    /// </summary>
    [Serializable]
    public class BoardConfig
    {
        /// <summary>
        /// The title that will be displayed in the top left corner of this display.
        /// </summary>
        public string Title;
        
        /// <summary>
        /// The coordinates of where this board should be placed in the scene.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// The orientation of this board.
        /// </summary>
        public Quaternion Rotation;
        
        /// <summary>
        /// All the widgets that should be displayed on this board.
        /// </summary>
        public List<WidgetConfig> WidgetConfigs = new List<WidgetConfig>();
        
        /// <summary>
        /// Label in the configuration file for the title of a metrics board.
        /// </summary>
        private const string BoardTitleLabel = "Title";

        /// <summary>
        /// Label in the configuration file for the x coordinate of a metrics board.
        /// </summary>
        private const string BoardPositionXLabel = "BoardPositionX";
        
        /// <summary>
        /// Label in the configuration file for the y coordinate of a metrics board.
        /// </summary>
        private const string BoardPositionYLabel = "BoardPositionY";
        
        /// <summary>
        /// Label in the configuration file for the z coordinate of a metrics board.
        /// </summary>
        private const string BoardPositionZLabel = "BoardPositionZ";

        /// <summary>
        /// Label in the configuration file for the rotation w component of a metrics board.
        /// </summary>
        private const string BoardRotationWLabel = "BoardRotationW";
        
        /// <summary>
        /// Label in the configuration file for the rotation x component of a metrics board.
        /// </summary>
        private const string BoardRotationXLabel = "BoardRotationX";
        
        /// <summary>
        /// Label in the configuration file for the rotation y component of a metrics board.
        /// </summary>
        private const string BoardRotationYLabel = "BoardRotationY";
        
        /// <summary>
        /// Label in the configuration file for the rotation z component of a metrics board.
        /// </summary>
        private const string BoardRotationZLabel = "BoardRotationZ";

        /// <summary>
        /// The label for the group of widget configurations in the configuration file.
        /// </summary>
        private const string WidgetConfigsLabel = "WidgetConfigs";

        /// <summary>
        /// Writes this instances' attributes into the given <see cref="ConfigWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="ConfigWriter"/> to write the attributes into.</param>
        internal void Save(ConfigWriter writer)
        {
            writer.Save(Title, BoardTitleLabel);
            writer.Save(Position.x, BoardPositionXLabel);
            writer.Save(Position.y, BoardPositionYLabel);
            writer.Save(Position.z, BoardPositionZLabel);
            writer.Save(Rotation.w, BoardRotationWLabel);
            writer.Save(Rotation.x, BoardRotationXLabel);
            writer.Save(Rotation.y, BoardRotationYLabel);
            writer.Save(Rotation.z, BoardRotationZLabel);
            writer.BeginList(WidgetConfigsLabel);
            foreach (WidgetConfig widgetConfig in WidgetConfigs)
            {
                widgetConfig.Save(writer);
            }
            writer.EndList();
        }
        
        /// <summary>
        /// Loads the given attributes into this instance of the class <see cref="BoardConfig"/>.
        /// </summary>
        /// <param name="attributes">The attributes to load in the format created by
        /// <see cref="ConfigWriter"/>.</param>
        /// <returns>Whether or not the attributes were loaded without any errors.</returns>
        internal bool Restore(Dictionary<string, object> attributes)
        {
            bool errorFree = true;
            if (attributes.TryGetValue(BoardTitleLabel, out object title))
            {
                Title = (string)title;
            }
            else
            {
                Title = "Could not load title" + Guid.NewGuid();
                errorFree = false;
            }

            if (attributes.TryGetValue(BoardPositionXLabel, out object boardPositionX) &&
                attributes.TryGetValue(BoardPositionYLabel, out object boardPositionY) &&
                attributes.TryGetValue(BoardPositionZLabel, out object boardPositionZ))
            {
                Position = new Vector3((float)boardPositionX, (float)boardPositionY, (float)boardPositionZ);
            }
            else
            {
                Position = Vector3.zero;
                errorFree = false;
            }
            
            if (attributes.TryGetValue(BoardRotationWLabel, out object boardRotationW) &&
                attributes.TryGetValue(BoardRotationXLabel, out object boardRotationX) &&
                attributes.TryGetValue(BoardRotationYLabel, out object boardRotationY) &&
                attributes.TryGetValue(BoardRotationZLabel, out object boardRotationZ))
            {
                Rotation = new Quaternion(
                    (float)boardRotationX,
                    (float)boardRotationY,
                    (float)boardRotationZ,
                    (float)boardRotationW);
            }
            else
            {
                Rotation = Quaternion.identity;
                errorFree = false;
            }

            if (attributes.TryGetValue(WidgetConfigsLabel, out object widgetList))
            {
                foreach (object item in (List<object>)widgetList)
                {
                    Dictionary<string, object> dict = (Dictionary<string, object>)item;
                    WidgetConfig config = new WidgetConfig();
                    config.Restore(dict);
                    WidgetConfigs.Add(config);
                }
            }
            
            return errorFree;
        }
    }

    /// <summary>
    /// This class can hold all the information that is needed to configure a metric widget.
    /// </summary>
    [Serializable]
    public class WidgetConfig
    {
        /// <summary>
        /// The unique ID of this widget.
        /// </summary>
        public Guid ID;
        
        /// <summary>
        /// The x and y coordinate of this widget.
        /// </summary>
        public Vector3 Position;
        
        /// <summary>
        /// The metric type that should be displayed.
        /// </summary>
        public string MetricType;

        /// <summary>
        /// The name of this widget that will display the metric. This is equivalent to the file name of the widget
        /// (without the file name extension).
        /// </summary>
        public string WidgetName;

        /// <summary>
        /// The label in the configuration file for the x coordinate of a widget.
        /// </summary>
        private const string WidgetPositionXLabel = "WidgetPositionX";
        
        /// <summary>
        /// The label in the configuration file for the y coordinate of a widget.
        /// </summary>
        private const string WidgetPositionYLabel = "WidgetPositionY";
        
        /// <summary>
        /// The label in the configuration file for the z coordinate of a widget.
        /// </summary>
        private const string WidgetPositionZLabel = "WidgetPositionZ";

        /// <summary>
        /// Label in the configuration file for the metric type of a widget.
        /// </summary>
        private const string MetricTypeLabel = "MetricType";

        /// <summary>
        /// Label in the configuration file for the name (type) of a widget.
        /// </summary>
        private const string WidgetNameLabel = "WidgetName";

        /// <summary>
        /// Writes this <see cref="BoardConfig"/>'s attributes into the given <see cref="ConfigWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="ConfigWriter"/> to write the attributes into.</param>
        internal void Save(ConfigWriter writer)
        {
            writer.BeginGroup();
            // TODO: Could we add the ID as a string here and read from it later? Also when transferring widgets
            // to other players?
            writer.Save(Position.x, WidgetPositionXLabel);
            writer.Save(Position.y, WidgetPositionYLabel);
            writer.Save(Position.z, WidgetPositionZLabel);
            writer.Save(MetricType, MetricTypeLabel);
            writer.Save(WidgetName, WidgetNameLabel);
            writer.EndGroup();
        }
        
        /// <summary>
        /// Given the representation of a <see cref="WidgetConfig"/> as created by the <see cref="ConfigWriter"/>, this
        /// method parses the attributes from that representation and puts them into this <see cref="WidgetConfig"/>
        /// instance.
        /// </summary>
        /// <param name="attributes">A list of labels (strings) of attributes and their values (objects). This
        /// has to be the representation of a <see cref="WidgetConfig"/> as created by
        /// <see cref="ConfigWriter"/>.</param>
        /// <returns>Whether or not the <see cref="WidgetConfig"/> was loaded without errors.</returns>
        internal bool Restore(Dictionary<string, object> attributes)
        {
            bool errors = false;
            ID = Guid.NewGuid();
            
            if (attributes.TryGetValue(WidgetPositionXLabel, out object widgetPositionX) &&
                attributes.TryGetValue(WidgetPositionYLabel, out object widgetPositionY) &&
                attributes.TryGetValue(WidgetPositionZLabel, out object widgetPositionZ))
            {
                Position = new Vector3((float)widgetPositionX, (float)widgetPositionY, (float)widgetPositionZ);
            }
            else
            {
                Position = Vector3.zero;
                errors = true;
            }
            
            if (attributes.TryGetValue(MetricTypeLabel, out object metricType))
            {
                MetricType = (string)metricType;
            }
            else
            {
                // Use some random metric type when no type could be parsed.
                MetricType = Metric.GetTypes()[0].Name;
                errors = true;
            }

            if (attributes.TryGetValue(WidgetNameLabel, out object widgetName))
            {
                WidgetName = (string)widgetName;
            }
            else
            {
                // Use some random widget name when no name could be parsed.
                const string widgetPrefabsPath = "Prefabs/HolisticMetrics/Widgets";
                WidgetName = Resources.LoadAll<GameObject>(widgetPrefabsPath)[0].name;
                errors = true;
            }

            return !errors;
        }
    }
}

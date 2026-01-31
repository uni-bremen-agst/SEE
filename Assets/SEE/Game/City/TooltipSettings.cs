using System;
using System.Collections.Generic;
using SEE.DataModel.DG;
using SEE.Utils.Config;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SEE.Game.City
{
    /// <summary>
    /// Configuration settings for the hover tooltip in a code city.
    /// Allows users to configure what information is displayed when hovering over nodes.
    /// </summary>
    [Serializable]
    [HideReferenceObjectPicker]
    public class TooltipSettings : ConfigIO.IPersistentConfigItem
    {
        #region Content Options

        /// <summary>
        /// If true, show the source name of the node.
        /// </summary>
        [Tooltip("Show the name of the node.")]
        [LabelText("Show Name")]
        [ToggleLeft]
        public bool ShowName = true;

        /// <summary>
        /// If true, show the type of the node (e.g., Class, Method, File).
        /// </summary>
        [Tooltip("Show the type of the node (e.g., Class, Method, File).")]
        [LabelText("Show Type")]
        [ToggleLeft]
        public bool ShowType = true;

        /// <summary>
        /// If true, show the number of incoming edges.
        /// </summary>
        [Tooltip("Show the number of incoming edges.")]
        [LabelText("Show Incoming Edges")]
        [ToggleLeft]
        public bool ShowIncomingEdges = false;

        /// <summary>
        /// If true, show the number of outgoing edges.
        /// </summary>
        [Tooltip("Show the number of outgoing edges.")]
        [LabelText("Show Outgoing Edges")]
        [ToggleLeft]
        public bool ShowOutgoingEdges = false;

        /// <summary>
        /// If true, show whether the node is a leaf or inner node.
        /// </summary>
        [Tooltip("Show whether the node is a Leaf or Inner node.")]
        [LabelText("Show Node Kind")]
        [ToggleLeft]
        public bool ShowNodeKind = false;

        /// <summary>
        /// If set, show the value of this metric.
        /// </summary>
        [Tooltip("If set, show the value of this metric.")]
        [LabelText("Show Metric")]
        public string ShowMetric = Metrics.LOC;

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns true if at least one content option is enabled.
        /// </summary>
        /// <returns>True if any tooltip content is enabled.</returns>
        public bool HasAnyContentEnabled()
        {
            return ShowName || ShowType || ShowIncomingEdges || ShowOutgoingEdges
                || ShowNodeKind || !string.IsNullOrWhiteSpace(ShowMetric);
        }

        #endregion

        #region Configuration Persistence

        /// <summary>
        /// Saves the settings in the configuration file using <paramref name="writer"/>
        /// under the given <paramref name="label"/>.
        /// </summary>
        /// <param name="writer">Writer to be used to save the settings.</param>
        /// <param name="label">Label under which to save the settings.</param>
        public void Save(ConfigWriter writer, string label = "")
        {
            writer.BeginGroup(label);
            writer.Save(ShowName, nameof(ShowName));
            writer.Save(ShowType, nameof(ShowType));
            writer.Save(ShowIncomingEdges, nameof(ShowIncomingEdges));
            writer.Save(ShowOutgoingEdges, nameof(ShowOutgoingEdges));
            writer.Save(ShowNodeKind, nameof(ShowNodeKind));
            writer.Save(ShowMetric, nameof(ShowMetric));
            writer.EndGroup();
        }

        /// <summary>
        /// Restores the settings from <paramref name="attributes"/> under the key <paramref name="label"/>.
        /// </summary>
        /// <param name="attributes">Dictionary of attributes from which to retrieve the settings.</param>
        /// <param name="label">The label for the settings (a key in <paramref name="attributes"/>).</param>
        /// <returns>True if restoration was successful.</returns>
        public bool Restore(Dictionary<string, object> attributes, string label = "")
        {
            if (!attributes.TryGetValue(label, out object dictionary))
            {
                return false;
            }

            if (dictionary is not Dictionary<string, object> values)
            {
                return false;
            }

            ConfigIO.Restore(values, nameof(ShowName), ref ShowName);
            ConfigIO.Restore(values, nameof(ShowType), ref ShowType);
            ConfigIO.Restore(values, nameof(ShowIncomingEdges), ref ShowIncomingEdges);
            ConfigIO.Restore(values, nameof(ShowOutgoingEdges), ref ShowOutgoingEdges);
            ConfigIO.Restore(values, nameof(ShowNodeKind), ref ShowNodeKind);
            ConfigIO.Restore(values, nameof(ShowMetric), ref ShowMetric);

            return true;
        }

        #endregion
    }
}

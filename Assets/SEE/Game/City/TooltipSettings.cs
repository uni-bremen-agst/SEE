using System;
using System.Collections.Generic;
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
        /// If true, show the lines of code metric.
        /// </summary>
        [Tooltip("Show the lines of code (LOC) metric if available.")]
        [LabelText("Show Lines of Code")]
        [ToggleLeft]
        public bool ShowLinesOfCode = false;

        #endregion

        #region Format Options

        /// <summary>
        /// The separator used between different properties in the tooltip.
        /// </summary>
        [Tooltip("The separator used between different properties.")]
        [FoldoutGroup("Format Options")]
        public string Separator = "\n";

        /// <summary>
        /// The format string for the name. Use {0} as placeholder for the value.
        /// </summary>
        [Tooltip("Format for the name. Use {0} for the value.")]
        [FoldoutGroup("Format Options")]
        public string NameFormat = "{0}";

        /// <summary>
        /// The format string for the type. Use {0} as placeholder for the value.
        /// </summary>
        [Tooltip("Format for the type. Use {0} for the value.")]
        [FoldoutGroup("Format Options")]
        public string TypeFormat = "Type: {0}";

        /// <summary>
        /// The format string for incoming edges. Use {0} as placeholder for the value.
        /// </summary>
        [Tooltip("Format for incoming edges. Use {0} for the value.")]
        [FoldoutGroup("Format Options")]
        public string IncomingEdgesFormat = "Incoming: {0}";

        /// <summary>
        /// The format string for outgoing edges. Use {0} as placeholder for the value.
        /// </summary>
        [Tooltip("Format for outgoing edges. Use {0} for the value.")]
        [FoldoutGroup("Format Options")]
        public string OutgoingEdgesFormat = "Outgoing: {0}";

        /// <summary>
        /// The format string for node kind. Use {0} as placeholder for the value.
        /// </summary>
        [Tooltip("Format for node kind. Use {0} for the value.")]
        [FoldoutGroup("Format Options")]
        public string NodeKindFormat = "Kind: {0}";

        /// <summary>
        /// The format string for lines of code. Use {0} as placeholder for the value.
        /// </summary>
        [Tooltip("Format for lines of code. Use {0} for the value.")]
        [FoldoutGroup("Format Options")]
        public string LinesOfCodeFormat = "LOC: {0}";

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns true if at least one content option is enabled.
        /// </summary>
        /// <returns>True if any tooltip content is enabled.</returns>
        public bool HasAnyContentEnabled()
        {
            return ShowName || ShowType || ShowIncomingEdges || ShowOutgoingEdges || ShowNodeKind || ShowLinesOfCode;
        }

        #endregion

        #region Configuration Persistence

        private const string ShowNameLabel = nameof(ShowName);
        private const string ShowTypeLabel = nameof(ShowType);
        private const string ShowIncomingEdgesLabel = nameof(ShowIncomingEdges);
        private const string ShowOutgoingEdgesLabel = nameof(ShowOutgoingEdges);
        private const string ShowNodeKindLabel = nameof(ShowNodeKind);
        private const string ShowLinesOfCodeLabel = nameof(ShowLinesOfCode);
        private const string SeparatorLabel = nameof(Separator);
        private const string NameFormatLabel = nameof(NameFormat);
        private const string TypeFormatLabel = nameof(TypeFormat);
        private const string IncomingEdgesFormatLabel = nameof(IncomingEdgesFormat);
        private const string OutgoingEdgesFormatLabel = nameof(OutgoingEdgesFormat);
        private const string NodeKindFormatLabel = nameof(NodeKindFormat);
        private const string LinesOfCodeFormatLabel = nameof(LinesOfCodeFormat);

        /// <summary>
        /// Saves the settings in the configuration file using <paramref name="writer"/>
        /// under the given <paramref name="label"/>.
        /// </summary>
        /// <param name="writer">Writer to be used to save the settings.</param>
        /// <param name="label">Label under which to save the settings.</param>
        public void Save(ConfigWriter writer, string label = "")
        {
            writer.BeginGroup(label);
            writer.Save(ShowName, ShowNameLabel);
            writer.Save(ShowType, ShowTypeLabel);
            writer.Save(ShowIncomingEdges, ShowIncomingEdgesLabel);
            writer.Save(ShowOutgoingEdges, ShowOutgoingEdgesLabel);
            writer.Save(ShowNodeKind, ShowNodeKindLabel);
            writer.Save(ShowLinesOfCode, ShowLinesOfCodeLabel);
            writer.Save(Separator, SeparatorLabel);
            writer.Save(NameFormat, NameFormatLabel);
            writer.Save(TypeFormat, TypeFormatLabel);
            writer.Save(IncomingEdgesFormat, IncomingEdgesFormatLabel);
            writer.Save(OutgoingEdgesFormat, OutgoingEdgesFormatLabel);
            writer.Save(NodeKindFormat, NodeKindFormatLabel);
            writer.Save(LinesOfCodeFormat, LinesOfCodeFormatLabel);
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

            ConfigIO.Restore(values, ShowNameLabel, ref ShowName);
            ConfigIO.Restore(values, ShowTypeLabel, ref ShowType);
            ConfigIO.Restore(values, ShowIncomingEdgesLabel, ref ShowIncomingEdges);
            ConfigIO.Restore(values, ShowOutgoingEdgesLabel, ref ShowOutgoingEdges);
            ConfigIO.Restore(values, ShowNodeKindLabel, ref ShowNodeKind);
            ConfigIO.Restore(values, ShowLinesOfCodeLabel, ref ShowLinesOfCode);
            ConfigIO.Restore(values, SeparatorLabel, ref Separator);
            ConfigIO.Restore(values, NameFormatLabel, ref NameFormat);
            ConfigIO.Restore(values, TypeFormatLabel, ref TypeFormat);
            ConfigIO.Restore(values, IncomingEdgesFormatLabel, ref IncomingEdgesFormat);
            ConfigIO.Restore(values, OutgoingEdgesFormatLabel, ref OutgoingEdgesFormat);
            ConfigIO.Restore(values, NodeKindFormatLabel, ref NodeKindFormat);
            ConfigIO.Restore(values, LinesOfCodeFormatLabel, ref LinesOfCodeFormat);

            return true;
        }

        #endregion
    }
}

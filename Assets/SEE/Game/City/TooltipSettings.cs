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

        private const string showNameLabel = nameof(ShowName);
        private const string showTypeLabel = nameof(ShowType);
        private const string showIncomingEdgesLabel = nameof(ShowIncomingEdges);
        private const string showOutgoingEdgesLabel = nameof(ShowOutgoingEdges);
        private const string showNodeKindLabel = nameof(ShowNodeKind);
        private const string showLinesOfCodeLabel = nameof(ShowLinesOfCode);
        private const string separatorLabel = nameof(Separator);
        private const string nameFormatLabel = nameof(NameFormat);
        private const string typeFormatLabel = nameof(TypeFormat);
        private const string incomingEdgesFormatLabel = nameof(IncomingEdgesFormat);
        private const string outgoingEdgesFormatLabel = nameof(OutgoingEdgesFormat);
        private const string nodeKindFormatLabel = nameof(NodeKindFormat);
        private const string linesOfCodeFormatLabel = nameof(LinesOfCodeFormat);

        /// <summary>
        /// Saves the settings in the configuration file using <paramref name="writer"/>
        /// under the given <paramref name="label"/>.
        /// </summary>
        /// <param name="writer">Writer to be used to save the settings.</param>
        /// <param name="label">Label under which to save the settings.</param>
        public void Save(ConfigWriter writer, string label = "")
        {
            writer.BeginGroup(label);
            writer.Save(ShowName, showNameLabel);
            writer.Save(ShowType, showTypeLabel);
            writer.Save(ShowIncomingEdges, showIncomingEdgesLabel);
            writer.Save(ShowOutgoingEdges, showOutgoingEdgesLabel);
            writer.Save(ShowNodeKind, showNodeKindLabel);
            writer.Save(ShowLinesOfCode, showLinesOfCodeLabel);
            writer.Save(Separator, separatorLabel);
            writer.Save(NameFormat, nameFormatLabel);
            writer.Save(TypeFormat, typeFormatLabel);
            writer.Save(IncomingEdgesFormat, incomingEdgesFormatLabel);
            writer.Save(OutgoingEdgesFormat, outgoingEdgesFormatLabel);
            writer.Save(NodeKindFormat, nodeKindFormatLabel);
            writer.Save(LinesOfCodeFormat, linesOfCodeFormatLabel);
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

            ConfigIO.Restore(values, showNameLabel, ref ShowName);
            ConfigIO.Restore(values, showTypeLabel, ref ShowType);
            ConfigIO.Restore(values, showIncomingEdgesLabel, ref ShowIncomingEdges);
            ConfigIO.Restore(values, showOutgoingEdgesLabel, ref ShowOutgoingEdges);
            ConfigIO.Restore(values, showNodeKindLabel, ref ShowNodeKind);
            ConfigIO.Restore(values, showLinesOfCodeLabel, ref ShowLinesOfCode);
            ConfigIO.Restore(values, separatorLabel, ref Separator);
            ConfigIO.Restore(values, nameFormatLabel, ref NameFormat);
            ConfigIO.Restore(values, typeFormatLabel, ref TypeFormat);
            ConfigIO.Restore(values, incomingEdgesFormatLabel, ref IncomingEdgesFormat);
            ConfigIO.Restore(values, outgoingEdgesFormatLabel, ref OutgoingEdgesFormat);
            ConfigIO.Restore(values, nodeKindFormatLabel, ref NodeKindFormat);
            ConfigIO.Restore(values, linesOfCodeFormatLabel, ref LinesOfCodeFormat);

            return true;
        }

        #endregion
    }
}

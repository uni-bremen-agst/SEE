using SEE.Utils.Config;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Attributes to mark changes
    /// </summary>
    [Serializable]
    public class MarkerAttributes
    {
        /// <summary>
        /// The height of posts used as markers for new and deleted elements.
        /// </summary>
        [Tooltip("The height of posts used as markers for new, changed, and deleted elements (>=0).")]
        [SerializeField, ShowInInspector]
        public float MarkerHeight = 0.2f;

        /// <summary>
        /// The width (x and z lengths) of posts used as markers for new and deleted elements.
        /// </summary>
        [Tooltip("The width (x and z lengths) of posts used as markers for new and deleted elements (>=0).")]
        [SerializeField, ShowInInspector]
        public float MarkerWidth = 0.01f;

        /// <summary>
        /// Color for power beams of newly added nodes, can be set in inspector
        /// </summary>
        [Tooltip("The color of the beam for newly created nodes.")]
        [SerializeField, ShowInInspector]
        public Color AdditionBeamColor = Color.green;

        /// <summary>
        /// Changed nodes beam color to be pickable in inspector
        /// </summary>
        [Tooltip("The color of the beam for changed nodes.")]
        [SerializeField, ShowInInspector]
        public Color ChangeBeamColor = Color.yellow;

        /// <summary>
        /// Deleted nodes beam color to be pickable in inspector
        /// </summary>
        [Tooltip("The color of the beam for deleted nodes.")]
        [SerializeField, ShowInInspector]
        public Color DeletionBeamColor = Color.black;

        #region Configuration file input/output
        //--------------------------------
        // Configuration file input/output
        //--------------------------------

        /// <summary>
        /// Label of attribute <see cref="MarkerHeight"/> in the configuration file.
        /// </summary>
        private const string markerHeightLabel = "MarkerHeight";
        /// <summary>
        /// Label of attribute <see cref="MarkerWidth"/> in the configuration file.
        /// </summary>
        private const string markerWidthLabel = "MarkerWidth";
        /// <summary>
        /// Label of attribute <see cref="AdditionBeamColor"/> in the configuration file.
        /// </summary>
        private const string additionBeamColorLabel = "AdditionBeamColor";
        /// <summary>
        /// Label of attribute <see cref="ChangeBeamColor"/> in the configuration file.
        /// </summary>
        private const string changeBeamColorLabel = "ChangeBeamColor";
        /// <summary>
        /// Label of attribute <see cref="DeletionBeamColor"/> in the configuration file.
        /// </summary>
        private const string deletionBeamColorLabel = "DeletionBeamColor";

        public void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(MarkerHeight, markerHeightLabel);
            writer.Save(MarkerWidth, markerWidthLabel);
            writer.Save(AdditionBeamColor, additionBeamColorLabel);
            writer.Save(ChangeBeamColor, changeBeamColorLabel);
            writer.Save(DeletionBeamColor, deletionBeamColorLabel);
            writer.EndGroup();
        }

        public void Restore(Dictionary<string, object> attributes)
        {
            ConfigIO.Restore(attributes, markerHeightLabel, ref MarkerHeight);
            ConfigIO.Restore(attributes, markerWidthLabel, ref MarkerWidth);
            ConfigIO.Restore(attributes, additionBeamColorLabel, ref AdditionBeamColor);
            ConfigIO.Restore(attributes, changeBeamColorLabel, ref ChangeBeamColor);
            ConfigIO.Restore(attributes, deletionBeamColorLabel, ref DeletionBeamColor);
        }
        #endregion
    }
}

using SEE.GO;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils;
using SEE.Utils.Config;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.City
{
    /// <summary>
    /// This code city is used to visualize all changes which were
    /// made to a git repository across all branches from a given
    /// date until now.
    /// </summary>
    public class BranchCity : VCSCity, ISelfValidator
    {
        /// <summary>
        /// A date string in the <see cref="SEEDate.DateFormat"/> format.
        ///
        /// All commits from the most recent to the latest commit
        /// before the date are used for the analysis.
        /// </summary>
        [InspectorName("Date Limit (" + SEEDate.DateFormat + ")"),
         Tooltip("The beginning date after which commits should be considered (" + SEEDate.DateFormat + ")"),
         TabGroup(VCSFoldoutGroup), RuntimeTab(VCSFoldoutGroup)]
        public string Date = SEEDate.Now();

        /// <summary>
        /// Backing attribute for <see cref="ShowAuthorEdges"/>.
        /// </summary>
        [OdinSerialize, NonSerialized, HideInInspector]
        private ShowAuthorEdgeStrategy showAuthorEdges = ShowAuthorEdgeStrategy.ShowOnHoverOrWithMultipleAuthors;

        /// <summary>
        /// Specifies how the edges connecting authors and their commits should be shown.
        /// See <see cref="ShowAuthorEdgeStrategy"/> for more details what each options should do.
        /// </summary>
        [ShowInInspector,
            TabGroup(EdgeFoldoutGroup), RuntimeTab(EdgeFoldoutGroup),
            Tooltip("Whether connecting lines between authors and files should be shown.")]
        public ShowAuthorEdgeStrategy ShowAuthorEdges
        {
            get => showAuthorEdges;
            set
            {
                if (value != showAuthorEdges)
                {
                    showAuthorEdges = value;
                    UpdateAuthorEdges();
                }
            }
        }

        /// <summary>
        /// Updates the visibility of all author edges.
        /// </summary>
        private void UpdateAuthorEdges()
        {
            GameObject root = gameObject.FirstChildNode();
            if (root != null)
            {
                // Edges are located under the root node.
                foreach (Transform child in root.transform)
                {
                    if (child.TryGetComponent(out GameObjects.AuthorEdge edge))
                    {
                        edge.ShowOrHide(isHovered: false);
                    }
                }
            }
        }

        /// <summary>
        /// Only relevant if <see cref="ShowAuthorEdges"/> is set to
        /// <see cref="ShowAuthorEdgeStrategy.ShowOnHoverOrWithMultipleAuthors"/>.
        ///
        /// This is the threshold for the number of authors at which edges between authors
        /// and nodes are shown permanently.
        /// If the number of authors is below this threshold, the edges will only be shown when
        /// the user hovers over the node or the author sphere.
        /// </summary>
        [ShowIf(nameof(ShowAuthorEdges), ShowAuthorEdgeStrategy.ShowOnHoverOrWithMultipleAuthors),
         RuntimeShowIf(nameof(ShowAuthorEdges), ShowAuthorEdgeStrategy.ShowOnHoverOrWithMultipleAuthors),
         Range(2, 20),
         TabGroup(EdgeFoldoutGroup),
         RuntimeTab(EdgeFoldoutGroup)]
        public int AuthorThreshold = 2;

        /// <summary>
        /// Validates <see cref="Date"/>.
        /// </summary>
        /// <param name="result">where the error results are to be added (if any)</param>
        /// <remarks>Will be used by Odin Validator.</remarks>
        public void Validate(SelfValidationResult result)
        {
            if (!SEEDate.IsValid(Date))
            {
                result.AddError("Invalid date!");
            }
        }

        #region Config I/O

        /// <summary>
        /// Label of attribute <see cref="Date"/> in the configuration file.
        /// </summary>
        private const string dateLabel = "Date";

        /// <summary>
        /// Label of attribute <see cref="ShowAuthorEdges"/> in the configuration file.
        /// </summary>
        private const string showEdgesStrategy = "ShowAuthorEdgesStrategy";

        /// <summary>
        /// Label of attribute <see cref="AuthorThreshold"/> in the configuration file.
        /// </summary>
        private const string authorThresholdLabel = "AuthorThreshold";

        protected override void Save(ConfigWriter writer)
        {
            base.Save(writer);
            writer.Save(Date, dateLabel);
            writer.Save(ShowAuthorEdges.ToString(), showEdgesStrategy);
            writer.Save(AuthorThreshold, authorThresholdLabel);
        }

        protected override void Restore(Dictionary<string, object> attributes)
        {
            base.Restore(attributes);
            ConfigIO.Restore(attributes, dateLabel, ref Date);
            {
                ShowAuthorEdgeStrategy showAuthorEdgesStrategy = ShowAuthorEdges;
                if (ConfigIO.RestoreEnum(attributes, showEdgesStrategy, ref showAuthorEdgesStrategy))
                {
                    ShowAuthorEdges = showAuthorEdgesStrategy;
                }
            }
            ConfigIO.Restore(attributes, authorThresholdLabel, ref AuthorThreshold);
        }

        #endregion
    }
}

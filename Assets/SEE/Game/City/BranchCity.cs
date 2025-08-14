using SEE.UI.RuntimeConfigMenu;
using SEE.Utils;
using SEE.Utils.Config;
using Sirenix.OdinInspector;
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
        /// Specifies how the edges connecting authors and their commits should be shown.
        /// See <see cref="ShowAuthorEdgeStrategy"/> for more details what each options should do.
        /// </summary>
        [TabGroup(EdgeFoldoutGroup),
         RuntimeTab(EdgeFoldoutGroup)]
        public ShowAuthorEdgeStrategy ShowAuthorEdgesStrategy =
                ShowAuthorEdgeStrategy.ShowOnHoverOrWithMultipleAuthors;

        /// <summary>
        /// Only relevant if <see cref="ShowAuthorEdgesStrategy"/> is set to
        /// <see cref="ShowAuthorEdgeStrategy.ShowOnHoverOrWithMultipleAuthors"/>.
        ///
        /// This is the threshold for the number of authors at which edges between authors
        /// and nodes are shown permanently.
        /// If the number of authors is below this threshold, the edges will only be shown when
        /// the user hovers over the node or the author sphere.
        /// </summary>
        [ShowIf("ShowEdgesStrategy", ShowAuthorEdgeStrategy.ShowOnHoverOrWithMultipleAuthors),
         RuntimeShowIf("ShowEdgesStrategy", ShowAuthorEdgeStrategy.ShowOnHoverOrWithMultipleAuthors),
         Range(2, 20),
         TabGroup(EdgeFoldoutGroup),
         RuntimeTab(EdgeFoldoutGroup)]
        public int AuthorThreshold = 2;

        /// <summary>
        /// Resets everything that is specific to a given graph. Here in addition to
        /// the overridden method, the <see cref="GitPoller"/> component will be
        /// removed.
        /// </summary>
        /// <remarks>This method should be called whenever <see cref="loadedGraph"/> is re-assigned.</remarks>
        [Button(ButtonSizes.Small, Name = "Reset Data")]
        [ButtonGroup(ResetButtonsGroup), RuntimeButton(ResetButtonsGroup, "Reset Data")]
        [PropertyOrder(ResetButtonsGroupOrderReset)]
        public override void Reset()
        {
            base.Reset();
            // Remove the poller.
            if (TryGetComponent(out GitPoller poller))
            {
                Destroyer.Destroy(poller);
            }
        }

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

        /// <summary>
        /// Returns or adds a <see cref="GitPoller"/> component to the game object this
        /// <paramref name="city"/> is attached to.
        /// </summary>
        /// <param name="pollingInterval">VCS polling interval in seconds.</param>
        /// <param name="markerTime">Time in seconds for how long the markers should be shown.</param>
        /// <returns>The <see cref="GitPoller"/> component</returns>
        public GitPoller GetOrAddGitPollerComponent(int pollingInterval, int markerTime)
        {
            if (TryGetComponent(out GitPoller poller))
            {
                return poller;
            }

            GitPoller newPoller = gameObject.AddComponent<GitPoller>();
            newPoller.CodeCity = this;
            newPoller.PollingInterval = pollingInterval;
            newPoller.MarkerTime = markerTime;
            return newPoller;
        }

        #region Config I/O

        /// <summary>
        /// Label of attribute <see cref="Date"/> in the configuration file.
        /// </summary>
        private const string dateLabel = "Date";

        protected override void Save(ConfigWriter writer)
        {
            base.Save(writer);
            writer.Save(Date, dateLabel);
        }

        protected override void Restore(Dictionary<string, object> attributes)
        {
            base.Restore(attributes);
            ConfigIO.Restore(attributes, dateLabel, ref Date);
        }

        #endregion
    }
}

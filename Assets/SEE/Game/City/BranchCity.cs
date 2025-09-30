using SEE.Game.CityRendering;
using SEE.GO;
using SEE.GraphProviders;
using SEE.UI.Notification;
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
            TabGroup(VCSFoldoutGroup), RuntimeTab(VCSFoldoutGroup),
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
         TabGroup(VCSFoldoutGroup),
         RuntimeTab(VCSFoldoutGroup)]
        public int AuthorThreshold = 2;

        /// <summary>
        /// Draws the graph.
        /// Precondition: The graph data have been loaded.
        /// </summary>
        [Button(ButtonSizes.Small, Name = "Draw Data")]
        [ButtonGroup(DataButtonsGroup), RuntimeButton(DataButtonsGroup, "Draw Data")]
        [PropertyOrder(DataButtonsGroupOrderDraw)]
        [EnableIf(nameof(IsGraphLoaded)), RuntimeEnableIf(nameof(IsGraphLoaded))]
        public override void DrawGraph()
        {
            if (IsPipelineRunning)
            {
                ShowNotification.Error("Graph Drawing", "Graph provider pipeline is still running.");
                return;
            }
            base.DrawGraph();

            StartPoller();
        }

        private void OnEnable()
        {
            StartPoller();
        }

        private void OnDisable()
        {
            StopPoller();
        }

        /// <summary>
        /// The poller which will regularly fetch the repository for new changes.
        /// </summary>
        private GitPoller poller;


        private TransitionRenderer transitionRenderer;

        /// <summary>
        /// Backing field for <see cref="AutoFetch"/>.
        /// </summary>
        [OdinSerialize, NonSerialized, HideInInspector]
        private bool autoFetch = false;

        /// <summary>
        /// Specifies if SEE should automatically fetch for new commits in the
        /// repository <see cref="RepositoryData"/>.
        ///
        /// This will append the path of this repo to <see cref="GitPoller"/>.
        ///
        /// Note: the repository must be fetch-able without any credentials
        /// since we can't store them securely yet.
        /// </summary>
        [ShowInInspector,
            Tooltip("If true, the repository will be polled periodically for new changes."),
            TabGroup(VCSFoldoutGroup), RuntimeTab(VCSFoldoutGroup)]
        public bool AutoFetch
        {
            get => autoFetch;
            set
            {
                if (value != autoFetch)
                {
                    autoFetch = value;
                    if (autoFetch)
                    {
                        StartPoller();
                    }
                    else
                    {
                        poller?.Stop();
                    }
                }
            }
        }

        /// <summary>
        /// The interval in seconds in which git fetch should be called.
        /// </summary>
        [Tooltip("The interval in seconds in which the repository should be polled. Used only if Auto Fetch is true."),
            EnableIf(nameof(AutoFetch)), Range(5, 200),
            TabGroup(VCSFoldoutGroup), RuntimeTab(VCSFoldoutGroup)]
        public int PollingInterval = 5; /// // FIXME: We need to update poller's PollingInterval if it changes!

        /// <summary>
        /// If file changes where picked up by the <see cref="GitPoller"/>, the affected files
        /// will be marked. This field specifies for how long these markers should appear.
        /// </summary>
        [Tooltip(
             "The time in seconds for how long the node markers should be shown for newly added or modified nodes."),
         EnableIf(nameof(AutoFetch)), Range(5, 200),
            TabGroup(VCSFoldoutGroup), RuntimeTab(VCSFoldoutGroup)]
        public int MarkerTime = 10; /// FIXME: We need to update <see cref="transitionRenderer"/> if MarkerTime changes!

        /// <summary>
        /// Creates (if it does not yet exist) and starts the <see cref="poller"/>.
        /// </summary>
        private void StartPoller()
        {
            if (AutoFetch)
            {
                if (poller != null)
                {
                    poller.Stop();
                }
                else
                {
                    GitBranchesGraphProvider provider = GetGitBranchesGraphProvider(DataProvider);
                    poller = new GitPoller(PollingInterval, provider.GitRepository);
                    // We can create the transitionRenderer only now that we know the city.
                    transitionRenderer = new(this, poller, MarkerTime);
                }
                poller.Start();
            }
        }

        private void StopPoller()
        {
            poller?.Stop();
        }

        /// <summary>
        /// Returns the <see cref="GitBranchesGraphProvider"/> from the <see cref="DataProvider"/>'s pipeline.
        ///
        /// We currently assume that the pipeline has exactly one element which is of type <see cref="GitBranchesGraphProvider"/>.
        /// </summary>
        /// <param name="dataProvider">the graph provider pipeline from which to derive the <see cref="GitBranchesGraphProvider"/></param>
        /// <returns>the <see cref="GitBranchesGraphProvider"/></returns>
        /// <exception cref="ArgumentException">thrown in case our assumptions are invalid</exception>
        private static GitBranchesGraphProvider GetGitBranchesGraphProvider(SingleGraphPipelineProvider dataProvider)
        {
            if (dataProvider == null)
            {
                throw new ArgumentException("Data provider is null.");
            }

            if (dataProvider.Pipeline.Count == 0)
            {
                throw new ArgumentException("Data provider pipeline is empty.");
            }
            if (dataProvider.Pipeline.Count > 1)
            {
                throw new ArgumentException($"Data provider pipeline has more than one element. That is currently not supported for {nameof(BranchCity)}.");
            }

            SingleGraphProvider graphProvider = dataProvider.Pipeline[0];
            if (graphProvider == null)
            {
                throw new ArgumentException("Data provider in pipeline is null.");
            }
            if (graphProvider is GitBranchesGraphProvider result)
            {
                return result;
            }
            throw new ArgumentException($"Data provider is not a {nameof(GitBranchesGraphProvider)}.");
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

        /// <summary>
        /// Label for serializing the <see cref="AutoFetch"/> field.
        /// </summary>
        private const string autoFetchLabel = "AutoFetch";

        /// <summary>
        /// Label for serializing the <see cref="PollingInterval"/> field.
        /// </summary>
        private const string pollingIntervalLabel = "PollingInterval";

        /// <summary>
        /// Label for serializing the <see cref="MarkerTime"/> field.
        /// </summary>
        private const string markerTimeLabel = "MarkerTime";

        protected override void Save(ConfigWriter writer)
        {
            base.Save(writer);
            writer.Save(Date, dateLabel);
            writer.Save(ShowAuthorEdges.ToString(), showEdgesStrategy);
            writer.Save(AuthorThreshold, authorThresholdLabel);
            writer.Save(AutoFetch, autoFetchLabel);
            writer.Save(PollingInterval, pollingIntervalLabel);
            writer.Save(MarkerTime, markerTimeLabel);
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
            ConfigIO.Restore(attributes, autoFetchLabel, ref autoFetch);
            ConfigIO.Restore(attributes, pollingIntervalLabel, ref PollingInterval);
            ConfigIO.Restore(attributes, markerTimeLabel, ref MarkerTime);
        }

        #endregion
    }
}

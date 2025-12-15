using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.Game.CityRendering;
using SEE.GameObjects;
using SEE.GameObjects.BranchCity;
using SEE.GO;
using SEE.GO.Factories;
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
        /// Backing field for <see cref="AuthorThreshold"/>.
        /// </summary>
        private int authorThreshold = 2;

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
         Range(1, 200),
         TabGroup(VCSFoldoutGroup),
         RuntimeTab(VCSFoldoutGroup)]
        public int AuthorThreshold
        {
            get => authorThreshold;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("Author threshold must not be negative.");
                }
                if (value != authorThreshold)
                {
                    authorThreshold = value;
                    UpdateAuthorEdges();
                }
            }
        }

        /// <summary>
        /// Backing field for <see cref="EnableConflictMarkers"/>.
        /// </summary>
        [SerializeField]
        private bool enableConflictMarkers = false;

        /// <summary>
        /// Unregisters the code city from <see cref="DynamicMarkerFactory"/> to destroy
        /// the dynamic marker prefab created specifically for this code city.
        /// </summary>
        private void OnDestroy()
        {
            DynamicMarkerFactory.DestroyMarkerPrefab(gameObject);
        }

        /// <summary>
        /// Whether the dynamic markers are to be enabled for file nodes with
        /// multiple authors, that is, when there is a chance of an edit conflict.
        /// </summary>
        [ShowInInspector,
         Tooltip("Enables the markers to show potential edit conflicts."),
         TabGroup(VCSFoldoutGroup),
         RuntimeTab(VCSFoldoutGroup)]
        public bool EnableConflictMarkers
        {
            get => enableConflictMarkers;
            set
            {
                enableConflictMarkers = value;
                UpdateConflictMarkers();
            }
        }

        /// <summary>
        /// Resets everything that is specific to a given graph. Here: the selected node types,
        /// the underlying and visualized graph, and all game objects visualizing information about it.
        /// And enables the <see cref="CitySelectionManager"/> to add a new <see cref="AbstractSEECity"/>.
        /// </summary>
        /// <remarks>This method should be called whenever <see cref="loadedGraph"/> is re-assigned.</remarks>
        [Button(ButtonSizes.Small, Name = "Reset Data")]
        [ButtonGroup(ResetButtonsGroup), RuntimeButton(ResetButtonsGroup, "Reset Data")]
        [PropertyOrder(ResetButtonsGroupOrderReset), RuntimeGroupOrder(ResetButtonsGroupOrderReset)]
        public override void Reset()
        {
            base.Reset();
            DynamicMarkerFactory.DestroyMarkerPrefab(gameObject);
            DestroyAuthorSpheresAndEdges();
        }

        /// <summary>
        /// <see cref="AuthorSphere"/>s and <see cref="AuthorEdge"/>s are immediate children of
        /// the game object this component is attached to and not of the graph root node.
        /// Hence, destroying the graph root node and its descendants will not destroy
        /// the <see cref="AuthorSphere"/>s and <see cref="AuthorEdge"/>s. We destroy them
        /// here by search for all game objects tagged as <see cref="Tags.Decoration"/>.
        /// </summary>
        private void DestroyAuthorSpheresAndEdges()
        {
            foreach (Transform child in transform)
            {
                if (child.CompareTag(Tags.Decoration))
                {
                    Destroyer.Destroy(child);
                }
            }
        }

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

        /// <summary>
        /// Starts the <see cref="poller"/>.
        /// </summary>
        private void OnEnable()
        {
            StartPoller();
        }

        /// <summary>
        /// Stops the <see cref="poller"/>.
        /// </summary>
        private void OnDisable()
        {
            StopPoller();
        }

        /// <summary>
        /// The poller which will periodically fetch the repository for new changes.
        /// </summary>
        private GitPoller poller;

        /// <summary>
        /// The transition renderer which will updates the rendering of the city
        /// in the light of new commits.
        /// </summary>
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
        /// Backing field for <see cref="PollingInterval"/>.
        /// </summary>
        private int pollingInterval = 5;

        /// <summary>
        /// The interval in seconds in which git fetch should be called.
        /// </summary>
        [Tooltip("The interval in seconds in which the repository should be polled. Used only if Auto Fetch is true."),
            EnableIf(nameof(AutoFetch)), RuntimeEnableIf(nameof(AutoFetch)),
            Range(5, 200),
            TabGroup(VCSFoldoutGroup), RuntimeTab(VCSFoldoutGroup),
            ShowInInspector]
        public int PollingInterval
        {
            get => pollingInterval;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("Polling interval must be positive.");
                }
                pollingInterval = value;
                if (poller != null)
                {
                    poller.PollingInterval = pollingInterval;
                }
            }
        }

        /// <summary>
        /// Creates (if it does not yet exist) and starts the <see cref="poller"/>
        /// (and creates <see cref="transitionRenderer"/>, too) when <see cref="AutoFetch"/> is true.
        /// </summary>
        private void StartPoller()
        {
            if (AutoFetch)
            {
                if (poller != null)
                {
                    StopPoller();
                }
                else
                {
                    GitBranchesGraphProvider provider = GetGitBranchesGraphProvider(DataProvider);
                    poller = new GitPoller(PollingInterval, provider.GitRepository);
                    // We can create the transitionRenderer only now that we know the city.
                    transitionRenderer = new(MarkerAttributes);
                }
                poller.OnChangeDetected += RenderTransition;
                poller.Start();
            }
        }

        /// <summary>
        /// Stops the <see cref="poller"/> if it exists.
        /// </summary>
        private void StopPoller()
        {
            if (poller != null)
            {
                poller.OnChangeDetected -= RenderTransition;
                poller.Stop();
            }
        }

        /// <summary>
        /// Runs an asynchronous rendering of the transition from the old graph to the new graph.
        /// </summary>
        void RenderTransition()
        {
            RenderTransitionAsync().Forget();
        }

        /// <summary>
        /// Renders the transition from the old graph to the new graph asynchronously.
        /// </summary>
        /// <returns>task</returns>
        async UniTask RenderTransitionAsync()
        {
            if (LoadedGraph == null)
            {
                return;
            }
            // Backup old graph
            Graph oldGraph = LoadedGraph.Clone() as Graph;
            await LoadDataAsync();
            Graph newGraph = LoadedGraph;
            bool edgesAreDrawn = EdgeLayoutSettings.Kind != EdgeLayoutKind.None;
            await transitionRenderer.RenderAsync(oldGraph, newGraph, edgesAreDrawn, gameObject, Renderer);
            /// We updated the graph elements references and <see cref="GraphElementIDMap"/>
            /// ourselves in the transition renderer, so we need to update them again in <see cref="InitializeAfterDrawn(bool)"/>.
            InitializeAfterDrawn(false);
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
                    if (child.TryGetComponent(out AuthorEdge edge))
                    {
                        edge.ShowOrHide(isHovered: false);
                    }
                }
            }
        }

        /// <summary>
        /// Updates the merge-conflict dynamic markers of all file nodes.
        /// </summary>
        private void UpdateConflictMarkers()
        {
            GameObject root = gameObject.FirstChildNode();
            if (root != null)
            {
                root.ApplyToAllDescendants(Tags.Node, go => UpdateConflictMarker(go));
            }

            static void UpdateConflictMarker(GameObject go)
            {
                if (go.TryGetComponent(out AuthorRef authorRef))
                {
                    authorRef.UpdateConflictVisibility();
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
        /// Label for serializing the <see cref="enableConflictMarkers"/> field.
        /// </summary>
        private const string enableConflictMarkersLabel = "EnableConflictMarkers";

        protected override void Save(ConfigWriter writer)
        {
            base.Save(writer);
            writer.Save(Date, dateLabel);
            writer.Save(ShowAuthorEdges.ToString(), showEdgesStrategy);
            writer.Save(AuthorThreshold, authorThresholdLabel);
            writer.Save(AutoFetch, autoFetchLabel);
            writer.Save(PollingInterval, pollingIntervalLabel);
            writer.Save(enableConflictMarkers, enableConflictMarkersLabel);
        }

        protected override void Restore(Dictionary<string, object> attributes)
        {
            base.Restore(attributes);
            // Note: We are assigning the properties instead of the backing fields
            // so that the wanted effects (like updating the author edges) are executed.
            ConfigIO.Restore(attributes, dateLabel, ref Date);
            {
                ShowAuthorEdgeStrategy showAuthorEdgesStrategy = ShowAuthorEdges;
                if (ConfigIO.RestoreEnum(attributes, showEdgesStrategy, ref showAuthorEdgesStrategy))
                {
                    ShowAuthorEdges = showAuthorEdgesStrategy;
                }
            }
            {
                int authorThreshold = AuthorThreshold;
                if (ConfigIO.Restore(attributes, authorThresholdLabel, ref authorThreshold))
                {
                    AuthorThreshold = authorThreshold;
                }
            }
            {
                bool autoFetch = AutoFetch;
                if (ConfigIO.Restore(attributes, autoFetchLabel, ref autoFetch))
                {
                    AutoFetch = autoFetch;
                }
            }
            {
                int pollingInterval = PollingInterval;
                if (ConfigIO.Restore(attributes, pollingIntervalLabel, ref pollingInterval))
                {
                    PollingInterval = pollingInterval;
                }
            }
            {
                bool enable = enableConflictMarkers;
                if (ConfigIO.Restore(attributes, enableConflictMarkersLabel, ref enable))
                {
                    EnableConflictMarkers = enable;
                }
            }
        }

        #endregion
    }
}

// Copyright 2020 Robert Bohnsack
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be included
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
// CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;
using System.Linq;
using SEE.Controls;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.GO;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace SEE.Game.Charts
{
    /// <summary>
    /// Fills Charts with data and manages that data.
    /// </summary>
    public class ChartContent : MonoBehaviour
    {
        /// <summary>
        /// The offset The gap between entries in the <see cref="scrollContent" /> indicating a new hierarchy layer.
        /// </summary>
        private const float ScrollViewEntryIndentation = 15;

        /// <summary>
        /// The height of an entry in the <see cref="scrollContent"/>.
        /// </summary>
        private const float ScrollViewEntryHeight = 25;

        // TODO(torben): this may need to be reintroduced
        /// <summary>
        /// The distance to another marker to recognize it as overlapping.
        /// </summary>
        private const float MarkerOverlapDistance = 22;

        /// <summary>
        /// This entry of the dropdown box represents not a metric but just the
        /// enumeration of nodes. This entry can be selected if one wants to have a
        /// metric on one axis and then all nodes sorted by this metric on the other
        /// axis.
        /// </summary>
        private const string NodeEnumeration = "NODES";

        /// <summary>
        /// Contains one <see cref="scrollEntryPrefab"/> for each <see cref="Node"/> in
        /// the scene.
        /// </summary>
        [SerializeField] private GameObject scrollContent;

        /// <summary>
        /// A checkbox associated to a <see cref="Node"/> in the scene to activate it in
        /// the chart.
        /// </summary>
        [SerializeField] private GameObject scrollEntryPrefab;

        /// <summary>
        /// The starting coordinates of the entries of the <see cref="scrollContent"/>.
        /// </summary>
        [SerializeField] private Vector2 scrollEntryOffset;

        /// <summary>
        /// Handles the movement of charts.
        /// </summary>
        [SerializeField] private ChartMoveHandler moveHandler;

        /// <summary>
        /// The <see cref="AxisContentDropdown" /> containing Values for the X-Axis.
        /// </summary>
        public AxisContentDropdown axisDropdownX;

        /// <summary>
        /// The <see cref="AxisContentDropdown" /> containing Values for the Y-Axis.
        /// </summary>
        public AxisContentDropdown axisDropdownY;

        /// <summary>
        /// The prefab used to display content in charts.
        /// </summary>
        [SerializeField] private GameObject markerPrefab;

        /// <summary>
        /// Used to group all content entries of a chart as children of this <see cref="GameObject" />.
        /// </summary>
        [SerializeField] private GameObject entries;

        /// <summary>
        /// Will be shown if no data is to be displayed.
        /// </summary>
        [SerializeField] private GameObject noDataWarning;

        /// <summary>
        /// The minimum value on the x-axis.
        /// </summary>
        [SerializeField] private TextMeshProUGUI minXText;

        /// <summary>
        /// The maximum value on the x-axis.
        /// </summary>
        [SerializeField] private TextMeshProUGUI maxXText;

        /// <summary>
        /// The minimum value on the y-axis.
        /// </summary>
        [SerializeField] private TextMeshProUGUI minYText;

        /// <summary>
        /// The maximum value on the y-axis.
        /// </summary>
        [SerializeField] private TextMeshProUGUI maxYText;

        /// <summary>
        /// The panel on which the <see cref="ChartMarker" />s are instantiated.
        /// </summary>
        public RectTransform dataPanel;

        /// <summary>
        /// The panel on which the buttons and scales of the chart are displayed.
        /// </summary>
        public RectTransform labelsPanel;

        /// <summary>
        /// The current data of the vertical scroll bar is used to determine the range of
        /// visible entries.
        /// </summary>
        [SerializeField] private Scrollbar verticalScrollBar;

        // TODO(torben): i'd rather have the Viewport sizes, as this is slightly too
        // large. as a result, i have to calculate 'first' in Update() every frame with
        // 'verticalScrollBar.value'

        /// <summary>
        /// The transform if the scroll view rect is used to determine the max size and
        /// thus the upper limit of entries of the panel.
        /// </summary>
        [SerializeField] private RectTransform scrollViewRectTransform;

        /// <summary>
        /// All game-node objects to be listed in the chart for the list view.
        ///
        /// Invariant: all game objects in listDataObjects are game objects tagged by
        /// Tags.Node and having a valid graph-node reference.
        /// </summary>
        private List<NodeRef> listDataObjects;

        /// <summary>
        /// All game-node objects to be listed in the chart for the tree view.
        ///
        /// Invariant: all game objects in treeDataObjects are game objects tagged by
        /// Tags.Node and having a valid graph-node reference.
        /// </summary>
        private List<NodeRef> treeDataObjects;

        /// <summary>
        /// IDs of the nodes that have been added in this revision
        /// </summary>
        private List<string> newNodeIDs = new List<string>();

        /// <summary>
        /// IDs of the nodes that have changed in this revision
        /// </summary>
        private List<string> changedNodeIDs = new List<string>();

        /// <summary>
        /// IDs of the nodes that have been removed in this revision
        /// </summary>
        private List<string> removedNodeIDs = new List<string>();

        /// <summary>
        /// Color of added node labels
        /// </summary>
        private static readonly Color addedNodesLabelColor = Color.green;

        /// <summary>
        /// Color of changed node labels
        /// </summary>
        private static readonly Color changedNodesLabelColor = Color.cyan;

        /// <summary>
        /// Color of removed node labels
        /// </summary>
        private static readonly Color removedNodeLabelColor = Color.red;

        /// <summary>
        /// The color the label will have when hovering over it
        /// </summary>
        private static readonly Color hoveringOverLabelTextColor = Color.yellow;

        /// <summary>
        /// The hierarchy-indices of every <see cref="treeDataObjects"/>-element. Both
        /// always have same number of elements.
        /// </summary>
        private List<int> treeHierarchies;

        /// <summary>
        /// A list of all <see cref="ChartMarker"/>s currently displayed in the chart.
        /// </summary>
        protected List<ChartMarker> activeMarkers = new List<ChartMarker>();

        /// <summary>
        /// Contains all metric names contained in any <see cref="GameObject" /> of <see cref="listDataObjects" />.
        /// </summary>
        public readonly SortedSet<string> allMetricNames = new SortedSet<string>();

        // FIXME: all attributes need documentation no matter whether they are public or private.

        private readonly Dictionary<string, bool> showInChartDict = new Dictionary<string, bool>();

        public delegate void ShowInChartCallbackFn(bool value);
        private readonly Dictionary<string, ShowInChartCallbackFn> callbackFnDict = new Dictionary<string, ShowInChartCallbackFn>();

        private bool scrollViewIsTree = false;

        private float totalHeight = 0;
        private int maxPanelEntryCount = 0;
        private int leafCount = 0;

        private ScrollViewEntry[] scrollViewEntries;
        private ScrollViewEntryData[] scrollViewEntryData;
        private List<ScrollViewEntry> scrollViewEntryPool;
        private List<ChartMarker> chartMarkerPool;

        private int previousFirst = 0;
        private int previousOnePastLast = 0;

        public static bool revisionChanged = false;

        private int currentRevisionCountCache = 0;

        /// <summary>
        /// Calls methods to initialize a chart.
        /// </summary>
        private void Awake()
        {
            Assert.IsTrue(scrollContent.transform.childCount == 0);

            scrollViewEntryPool = new List<ScrollViewEntry>(maxPanelEntryCount);
            chartMarkerPool = new List<ChartMarker>();

            ReloadData();
        }

        protected virtual void Start()
        {
            axisDropdownX.Initialize();
            axisDropdownY.Initialize();
            axisDropdownX.AddNodeEnumerationEntry(NodeEnumeration);
            DrawData();
        }

        private void Update()
        {
            if (currentRevisionCountCache != NodeChangesBuffer.GetSingleton().currentRevisionCounter)
            {
                // Push game objects to pool
                PushScrollViewEntriesToPool(previousFirst, previousOnePastLast);
                ReloadData();
                currentRevisionCountCache = NodeChangesBuffer.GetSingleton().currentRevisionCounter;
                NodeChangesBuffer.GetSingleton().revisionChanged = false;
            }
            // Prevents scrolling while the data is updating, as it would otherwise crash the graph (because
            // the data takes some time to update).
            if (!NodeChangesBuffer.GetSingleton().revisionChanged)
            {
                float panelEntryCount = totalHeight * (1.0f - verticalScrollBar.size) / ScrollViewEntryHeight;
                int totalEntryCount = scrollViewEntries.Length - (scrollViewIsTree ? 2 : 0);
                int first = Mathf.Max(0, Mathf.FloorToInt((1.0f - verticalScrollBar.value) * panelEntryCount));
                int onePastLast = Mathf.Min(totalEntryCount, first + maxPanelEntryCount);

                void _NewScrollViewEntries(int fst, int opl)
                {
                    if (scrollViewIsTree)
                    {
                        for (int i = fst; i < opl; i++)
                        {
                            scrollViewEntries[i] = NewScrollViewEntry(treeDataObjects[i].name, i, treeHierarchies[i]);
                            ChangeScrollViewEntryColor(scrollViewEntries[i].transform.gameObject.transform.Find("Label").gameObject, scrollViewEntries[i].transform.gameObject);
                        }
                    }
                    else
                    {
                        int j = 0;
                        for (int i = fst; i < opl; i++)
                        {
                            Assert.IsNull(scrollViewEntries[i]);

                            int leavesIndex = 0;
                            int innerNodeIndex = leafCount + 1;
                            if (i == leavesIndex) // 'Leaves' node
                            {
                                scrollViewEntries[leavesIndex] = NewScrollViewEntry("Leaves", i, 0);
                            }
                            else if (i == innerNodeIndex) // 'Inner Nodes' node
                            {
                                scrollViewEntries[innerNodeIndex] = NewScrollViewEntry("Inner Nodes", i, 0);
                            }
                            else if (i < leafCount + 1) // leaf node
                            {
                                scrollViewEntries[i] = NewScrollViewEntry(listDataObjects[i - 1].name, i, 1);
                            }
                            else // inner node
                            {
                                try
                                {
                                    scrollViewEntries[i] = NewScrollViewEntry(listDataObjects[i - 2].name, i, 1);
                                }
                                // removed node
                                catch
                                {
                                    scrollViewEntries[i] = NewScrollViewEntry(removedNodeIDs[j], i, 0);
                                    j += 1;
                                }
                            }
                            ChangeScrollViewEntryColor(scrollViewEntries[i].transform.gameObject.transform.Find("Label").gameObject, scrollViewEntries[i].transform.gameObject);
                        }
                    }
                }

                // delete out of view entries
                PushScrollViewEntriesToPool(previousFirst, Mathf.Min(previousOnePastLast, first)); // before
                PushScrollViewEntriesToPool(Mathf.Max(onePastLast, previousFirst), previousOnePastLast); // after

                // prepend and append new entries
                _NewScrollViewEntries(first, Mathf.Min(previousFirst, onePastLast)); // prepend
                _NewScrollViewEntries(Mathf.Max(previousOnePastLast, first), onePastLast); // append

                previousFirst = first;
                previousOnePastLast = onePastLast;
            }
        }

        public void AttachShowInChartCallbackFn(InteractableObject interactableObject, ShowInChartCallbackFn callbackFn)
        {
            Assert.IsTrue(!callbackFnDict.ContainsKey(interactableObject.name));
            callbackFnDict[interactableObject.name] = callbackFn;
        }

        public bool DetachShowInChartCallbackFn(InteractableObject interactableObject, ShowInChartCallbackFn callbackFn)
        {
            return callbackFnDict.Remove(interactableObject.name);
        }

        public bool ShowInChart(InteractableObject interactableObject)
        {
            return showInChartDict[interactableObject.name];
        }

        public void SetShowInChart(InteractableObject interactableObject, bool value)
        {
            if (!showInChartDict.TryGetValue(interactableObject.name, out bool oldValue))
            {
                oldValue = true;
            }
            showInChartDict[interactableObject.name] = value;
            if (oldValue != value && callbackFnDict.TryGetValue(interactableObject.name, out ShowInChartCallbackFn fn))
            {
                fn(value);
            }
        }

        public ScrollViewEntry GetScrollViewEntry(int index)
        {
            return scrollViewEntries[index];
        }

        public ref ScrollViewEntryData GetScrollViewEntryData(int index)
        {
            ref ScrollViewEntryData result = ref scrollViewEntryData[index];
            return ref result;
        }

        private void PushScrollViewEntriesToPool(int first, int onePastLast)
        {
            // Increase necessary capacity at once to reduce number of memory allocations to one
            int newCount = scrollViewEntryPool.Count + onePastLast - first;
            if (newCount > scrollViewEntryPool.Capacity)
            {
                scrollViewEntryPool.Capacity = newCount;
            }

            // Pool objects
            for (int i = first; i < onePastLast; i++)
            {
                Assert.IsNotNull(scrollViewEntries[i], "The toggle to be pooled is null!");

                scrollViewEntries[i].OnDestroy();
                scrollViewEntryPool.Add(scrollViewEntries[i]);
                scrollViewEntries[i] = null;
            }
        }

        /// <summary>
        /// Either creates or retrieves a pooled <see cref="ScrollViewEntry"/>. Pooled
        /// toggles are cleared, before they're returned.
        /// </summary>
        /// <param name="popPool">The pool, of which the toggles can be retrieved.
        /// </param>
        /// <param name="svt">The new scroll view toggle.</param>
        /// <param name="go">The game object containing the scroll view toggle.</param>
        private void RetrieveScrollViewEntry(ref ScrollViewEntry svt, ref GameObject go)
        {
            if (scrollViewEntryPool.Count > 0) // Retrieve from pooled objects...
            {
                svt = scrollViewEntryPool[scrollViewEntryPool.Count - 1];
                scrollViewEntryPool.RemoveAt(scrollViewEntryPool.Count - 1);
                go = svt.gameObject;
            }
            else // ... or create a new one
            {
                go = Instantiate(scrollEntryPrefab, scrollContent.transform);
                svt = go.GetComponent<ScrollViewEntry>();
            }
        }

        /// <summary>
        /// Creates a new scroll view toggle entry with given label.
        /// </summary>
        /// <param name="label">The label.</param>
        /// <param name="index">The index of the toggle to determine the y-offset of the
        /// entry.</param>
        /// <param name="hierarchy">The hierarchy to determine the x-offset of the entry.
        /// </param>
        /// <param name="pushPool">The pool, in which the new entries are pushed.</param>
        /// <param name="popPool">The pool, out of which pooled toggled can be retrieved.
        /// </param>
        /// <returns>The created scroll view toggle.</returns>
        private ScrollViewEntry NewScrollViewEntry(string label, int index, int hierarchy)
        {
            ScrollViewEntry entry = null;
            GameObject go = null;
            RetrieveScrollViewEntry(ref entry, ref go);

            go.name = "ScrollViewEntry: " + label;
            float x = ScrollViewEntryIndentation * hierarchy;
            float y = -ScrollViewEntryHeight * index;
            go.transform.localPosition = scrollEntryOffset + new Vector2(x, y);

            entry.Init(this, scrollViewEntryData[index], label);

            return entry;
        }

        /// <summary>
        /// Fills the scroll with as a list or a tree. Is called on start-up and
        /// thereupon only by Unity on button-press-events.
        /// </summary>
        /// <param name="asTree">Whether the scroll view is to be filled as a
        /// tree.</param>
        public void FillScrollView(bool asTree)
        {
            #region Reset

            for (int i = 0; i < listDataObjects.Count; i++)
            {
                SetShowInChart(listDataObjects[i].GetComponent<InteractableObject>(), true);
            }

            for (int i = 0; i < scrollViewEntryData.Length; i++)
            {
                if (scrollViewEntryData[i].interactableObject)
                {
                    // Note(torben): This only unsubscribes from events from the
                    // interactable object and thus can be inside of this if-statement
                    // for better performance
                    scrollViewEntryData[i].OnDestroy();
                }
            }

            PushScrollViewEntriesToPool(previousFirst, previousOnePastLast);
            previousFirst = 0;
            previousOnePastLast = 0;

            #endregion

            scrollViewIsTree = asTree;

            #region Fill ScrollViewEntryData

            if (scrollViewIsTree)
            {
                Stack<int> parentIndexStack = new Stack<int>();
                parentIndexStack.Push(ScrollViewEntryData.NoParentIndex);

                for (int i = 0; i < treeDataObjects.Count; i++)
                {
                    int hierarchy = treeHierarchies[i];

                    // remove non-relevant parent-indices
                    while (hierarchy < parentIndexStack.Count - 1)
                    {
                        parentIndexStack.Pop();
                    }

                    // count children
                    int childCount = 0;
                    for (int j = i + 1; j < treeDataObjects.Count; j++)
                    {
                        int h = treeHierarchies[j];
                        if (h <= hierarchy)
                        {
                            break;
                        }
                        if (h == hierarchy + 1)
                        {
                            childCount++;
                        }
                    }

                    InteractableObject o = treeDataObjects[i].GetComponent<InteractableObject>();
                    scrollViewEntryData[i] = new ScrollViewEntryData(i, this, o, parentIndexStack.Peek(), childCount);

                    // fill child indices
                    childCount = 0;
                    for (int j = i + 1; j < treeDataObjects.Count; j++)
                    {
                        int h = treeHierarchies[j];
                        if (h <= hierarchy)
                        {
                            break;
                        }
                        if (h == hierarchy + 1)
                        {
                            scrollViewEntryData[i].childIndices[childCount] = j;
                            childCount++;
                        }
                    }

                    parentIndexStack.Push(i);
                }
            }
            else
            {
                int idx = 0;
                int dataObjectIdx = 0;

                scrollViewEntryData[idx] = new ScrollViewEntryData(idx, this, null, ScrollViewEntryData.NoParentIndex, leafCount);
                idx++;

                for (int i = 0; i < leafCount; i++)
                {
                    InteractableObject o = listDataObjects[dataObjectIdx++].GetComponent<InteractableObject>();
                    scrollViewEntryData[idx] = new ScrollViewEntryData(idx, this, o, 0, 0);
                    scrollViewEntryData[0].childIndices[i] = idx;
                    idx++;
                }

                int innerCount = listDataObjects.Count - leafCount;
                int innerNodeIdx = idx;
                scrollViewEntryData[idx] = new ScrollViewEntryData(idx, this, null, ScrollViewEntryData.NoParentIndex, innerCount);
                idx++;

                for (int i = 0; i < innerCount; i++)
                {
                    InteractableObject o = listDataObjects[dataObjectIdx++].GetComponent<InteractableObject>();
                    scrollViewEntryData[idx] = new ScrollViewEntryData(idx, this, o, innerNodeIdx, 0);
                    scrollViewEntryData[innerNodeIdx].childIndices[i] = idx;
                    idx++;
                }

                // Add removed nodes
                foreach (string s in removedNodeIDs)
                {
                    scrollViewEntryData[idx] = new ScrollViewEntryData(idx, this, null, ScrollViewEntryData.NoParentIndex, 0);
                    idx++;
                }
            }

            #endregion

        }

        private void UpdateMaxPanelEntryCount()
        {
            // TODO(torben): 'maxPanelHeight' is slightly too large, but the viewport
            // does have a height of zero... that's why i have to calculate the current
            // height of the scrollrect in Update() every frame
            float maxPanelHeight = scrollViewRectTransform.sizeDelta.y;
            maxPanelEntryCount = Mathf.FloorToInt(maxPanelHeight / ScrollViewEntryHeight) + 1;
        }

        /// <summary>
        /// Gets all metric names for <see cref="float"/> values contained in the
        /// <see cref="NodeRef"/> of each <see cref="GameObject"/> in
        /// <see cref="listDataObjects"/>. A metric name is the name of a numeric (either
        /// float or int) node attribute that starts with the prefix
        /// ChartManager.MetricPrefix.</summary>
        private void GetAllNumericAttributes()
        {
            allMetricNames.Clear();
#if UNITY_EDITOR
            if (listDataObjects.Count == 0)
            {
                Debug.LogWarning("There are no nodes for showing metrics.\n");
            }
#endif
            foreach (string cname in Attributable.NumericAttributeNames.Where(cname => cname.StartsWith(ChartManager.MetricPrefix)))
            {
                allMetricNames.Add(cname);
            }
#if UNITY_EDITOR
            if (allMetricNames.Count == 0)
            {
                Debug.LogWarning("No metrics available for charts.\n");
            }
#endif
        }

        /// <summary>
        /// Returns all relevant nodes to be shown in the metric chart.
        /// If ChartManager.Instance.CodeCity is defined, we retrieve only the
        /// nodes contained in this city; otherwise all nodes in the scene
        /// are retrieved. If ChartManager.Instance.ShowLeafMetrics is true,
        /// all leaf nodes will be in this set. If ChartManager.Instance.ShowInnerNodeMetrics
        /// is true, all inner nodes will be present. Those two conditions are
        /// not mutually exclusive.
        /// </summary>
        /// <returns>the nodes to be shown in the chart</returns>
        private List<NodeRef> RelevantNodes()
        {
            if (ChartManager.Instance.CodeCity == null)
            {
                return SceneQueries.AllNodeRefsInScene(ChartManager.Instance.ShowLeafMetrics,
                                                       ChartManager.Instance.ShowInnerNodeMetrics);
            }
            else
            {
                List<NodeRef> result = new List<NodeRef>();
                foreach (GameObject gameNode in ChartManager.Instance.CodeCity.AllAncestors(Tags.Node))
                {
                    if (gameNode.TryGetComponent(out NodeRef nodeRef))
                    {
                        if (nodeRef.Value != null
                            && ((nodeRef.Value.IsLeaf() && ChartManager.Instance.ShowLeafMetrics)
                                 || (nodeRef.Value.IsInnerNode() && ChartManager.Instance.ShowInnerNodeMetrics)))
                        {
                            result.Add(nodeRef);
                        }
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// Fills a list with all <see cref="Node"/>s that will be in the chart.
        /// </summary>
        private void FindDataObjects()
        {
            // list
            listDataObjects = RelevantNodes();

            // Detect node changes and decorate the scrollview
            FillListsWithChanges();

            listDataObjects.Sort(delegate (NodeRef left, NodeRef right)
            {
                int result = 0;
                if (left.Value.IsLeaf() && right.Value.IsInnerNode())
                {
                    result = -1;
                }
                else if (left.Value.IsInnerNode() && right.Value.IsLeaf())
                {
                    result = 1;
                }
                return result;
            });

            // tree
            treeDataObjects = new List<NodeRef>(listDataObjects.Count);
            treeHierarchies = new List<int>(listDataObjects.Count);
            int hierarchy = 0;
            void _FindForTree(Node root)
            {
                try
                {
                    treeDataObjects.Add(NodeRef.Get(root));
                }
                // Child is a deleted node, but doesn't exist in list anymore
                catch
                {
                    return;
                }
                treeHierarchies.Add(hierarchy);

                hierarchy++;
                foreach (Node child in root.Children())
                {
                    _FindForTree(child);
                }
                hierarchy--;
            }
            foreach (Node root in SceneQueries.GetRoots(listDataObjects).Where(root => !removedNodeIDs.Contains(root.ID)))
            {
                _FindForTree(root);
            }
        }

        /// <summary>
        /// Fills the chart with data depending on the values of
        /// <see cref="axisDropdownX"/> and <see cref="axisDropdownY"/>.
        /// </summary>
        public void DrawData()
        {
            UpdateMaxPanelEntryCount();

            noDataWarning.SetActive(false);

            // Whether the user selected "NODES" on the x axis. That means the x axis
            // is just a list of nodes, not a real metric.
            bool xIsNodeEnum = axisDropdownX.CurrentlySelectedMetric == NodeEnumeration;
            // Whether the x and y axes represent the same metric.
            bool xEqY = axisDropdownX.CurrentlySelectedMetric == axisDropdownY.CurrentlySelectedMetric;

            // Note that we determine the minimal and maximal metric values of the two
            // axes globally, that is, over all nodes in listDataObjects and not just those
            // shown in this particular chart (there could be fewer nodes if a filter was
            // applied). This way, the scale of all charts for the same metric is comparable.
            float minX = float.PositiveInfinity; // globally minimal value on X axis
            float maxX = float.NegativeInfinity; // globally maximal value on X axis
            float minY = float.PositiveInfinity; // globally minimal value on Y axis
            float maxY = float.NegativeInfinity; // globally maximal value on Y axis
            List<NodeRef> toDraw = new List<NodeRef>(listDataObjects.Count); // nodes to be drawn in the chart
            foreach (NodeRef nodeRef in listDataObjects)
            {
                // x axis
                bool inX = false;
                if (nodeRef.Value.TryGetNumeric(axisDropdownX.CurrentlySelectedMetric, out float valueX) || xIsNodeEnum)
                {
                    // Note: if the node does not have the currently selected metric but the user selected
                    // NODES for the x axis, valueX will be 0 because that is the default for float.
                    minX = Mathf.Min(minX, valueX);
                    maxX = Mathf.Max(maxX, valueX);
                    // This node has the metric plotted on the x axis or the user selected NODES for the x axis
                    // in which case all nodes must be considered for the x axis.
                    inX = true;
                }
                // y axis
                bool inY = false;
                if (nodeRef.Value.TryGetNumeric(axisDropdownY.CurrentlySelectedMetric, out float valueY))
                {
                    minY = Mathf.Min(minY, valueY);
                    maxY = Mathf.Max(maxY, valueY);
                    // This node has the metric plotted on the y axis.
                    inY = true;
                }
                // Is this node to be shown in this chart at all?
                if (inX && inY)
                {
                    // Only nodes to be shown in this chart and having values for both
                    // currently selected metrics for the axes (or NODES was selected for the
                    // x axis) will be added to the chart.
                    toDraw.Add(nodeRef);
                }
            }

            // toDraw now contains all nodes to be plotted in the chart.
            if (toDraw.Count > 0)
            {
                if (xEqY)
                {
                    // If both axes show the same metric, we simply sort the values for this metric.
                    toDraw.Sort(delegate (NodeRef nodeRef0, NodeRef nodeRef1)
                    {
                        nodeRef0.Value.TryGetNumeric(axisDropdownY.CurrentlySelectedMetric, out float value1);
                        nodeRef1.Value.TryGetNumeric(axisDropdownY.CurrentlySelectedMetric, out float value2);
                        return value1.CompareTo(value2);
                    });
                }

                // Note: If the user chose NODES for the x axis, minX == maxX == 0 holds (see above).
                //FIXME: Equality comparison of floating point numbers may incur loss of precision.
                bool xEqual = minX == maxX;
                bool yEqual = minY == maxY;
                if (xEqual || yEqual)
                {
                    // If the user chose NODES for the x axis, we will definitely arrive here.
                    (float min, float max) = minX == maxX ? (minY, maxY) : (minX, maxX);
                    AddMarkers(toDraw, xIsNodeEnum, min, max, min, max);
                    minXText.text = xEqual ? "0" : minX.ToString("N0");
                    maxXText.text = xEqual ? toDraw.Count.ToString() : maxX.ToString("N0");
                    minYText.text = yEqual ? "0" : minY.ToString("N0");
                    maxYText.text = yEqual ? toDraw.Count.ToString() : maxY.ToString("N0");
                }
                else
                {
                    AddMarkers(toDraw, false, minX, maxX, minY, maxY);
                    minXText.text = minX.ToString("N0");
                    maxXText.text = maxX.ToString("N0");
                    minYText.text = minY.ToString("N0");
                    maxYText.text = maxY.ToString("N0");
                }
            }
            else
            {
                noDataWarning.SetActive(true);
                // Check for null avoids double destroy. Operator == is overloaded accordingly.
                foreach (ChartMarker marker in activeMarkers.Where(marker => marker != null && marker.gameObject != null))
                {
                    Destroy(marker.gameObject);
                }
                activeMarkers.Clear();
            }
        }

        /// <summary>
        /// Adds new markers to the chart and removes the old ones.
        /// </summary>
        /// <param name="nodeRefsToDraw">The markers to add to the chart.</param>
        /// <param name="ignoreXAxis">if true, the nodes will be enumerated on the x axis in the order of <paramref name="nodeRefsToDraw"/>;
        /// their actual value for the metric put on the x axis will be ignored</param>
        /// <param name="minX">The minimum value on the x-axis.</param>
        /// <param name="maxX">The maximum value on the x-axis.</param>
        /// <param name="minY">The minimum value on the y-axis.</param>
        /// <param name="maxY">The maximum value on the y-axis.</param>
        private void AddMarkers(IEnumerable<NodeRef> nodeRefsToDraw, bool ignoreXAxis, float minX, float maxX, float minY, float maxY)
        {
            callbackFnDict.Clear();

            List<ChartMarker> updatedMarkers = new List<ChartMarker>(activeMarkers.Count);
            Dictionary<Vector2, ChartMarker> anchoredPositionToChartMarkerDict = new Dictionary<Vector2, ChartMarker>(activeMarkers.Count);

            Rect dataRect = dataPanel.rect;
            // If ignoreXAxis is true, we just enumerate all nodes in nodeRefsToDraw in their order
            // therein on the x axis. The metric selected for the x axis is NODES and, hence, will
            // not be considered. We achieve that by representing the x values as if they define
            // a discrete value range from 1 to nodeRefsToDraw.Count() with an equidistant distance
            // of 1 between each value, i.e., [1, 2, 3, ...nodeRefsToDraw.Count()].
            if (ignoreXAxis)
            {
                minX = 1;
                maxX = nodeRefsToDraw.Count();
            }

            // Note: width and height of dataRect are measured in Unity units
            float widthFactor = minX < maxX ? dataRect.width / (maxX - minX) : 0.0f;
            float heightFactor = minY < maxY ? dataRect.height / (maxY - minY) : 0.0f;
            int positionInLayer = 0;
            int currentReusedActiveMarkerIndex = 0;

            // To enumerate the x values in case ignoreXAxis is true.
            int nodeIndex = 0;
            foreach (NodeRef nodeRef in nodeRefsToDraw)
            {
                float valueX;
                if (!ignoreXAxis)
                {
                    // We retrieve the value of the metrics chosen for the x axis.
                    if (!nodeRef.Value.TryGetNumeric(axisDropdownX.CurrentlySelectedMetric, out valueX))
                    {
                        Debug.LogError($"Node {nodeRef.Value.ID} does not have metric {axisDropdownX.CurrentlySelectedMetric}.\n");
                    }
                }
                else
                {
                    // We create the next value in our range [1, 2, 3, ...nodeRefsToDraw.Count()].
                    nodeIndex++;
                    valueX = nodeIndex;
                }
                nodeRef.Value.TryGetNumeric(axisDropdownY.CurrentlySelectedMetric, out float valueY);
                Vector2 anchoredPosition = new Vector2((valueX - minX) * widthFactor, (valueY - minY) * heightFactor);

                if (!anchoredPositionToChartMarkerDict.TryGetValue(anchoredPosition, out ChartMarker chartMarker))
                {
                    GameObject marker;
                    if (currentReusedActiveMarkerIndex < activeMarkers.Count)
                    {
                        // Reuse previously used marker
                        chartMarker = activeMarkers[currentReusedActiveMarkerIndex++];
                        chartMarker.OnDestroy();
                        marker = chartMarker.gameObject;
                    }
                    else if (chartMarkerPool.Count > 0)
                    {
                        // Retrieve marker from pool
                        chartMarker = chartMarkerPool[chartMarkerPool.Count - 1];
                        marker = chartMarker.gameObject;
                        marker.SetActive(true);
                        chartMarkerPool.RemoveAt(chartMarkerPool.Count - 1);
                    }
                    else
                    {
                        // Create new marker
                        marker = Instantiate(markerPrefab, entries.transform);
                        chartMarker = marker.GetComponent<ChartMarker>();
                    }
#if UNITY_EDITOR
                    marker.name = "ChartMarker: " + nodeRef.Value.SourceName;
#endif
                    marker.GetComponent<RectTransform>().anchoredPosition = anchoredPosition;
                    marker.GetComponent<SortingGroup>().sortingOrder = positionInLayer++;
                    chartMarker.chartContent = this;
                    updatedMarkers.Add(chartMarker);
                    anchoredPositionToChartMarkerDict.Add(anchoredPosition, chartMarker);
                }
#if UNITY_EDITOR
                else if (!chartMarker.gameObject.name.EndsWith(", [...]"))
                {
                    chartMarker.gameObject.name += ", [...]";
                }
#endif

                string infoText = "(" + valueX.ToString("0.00") + ", " + valueY.ToString("0.00") + ") " + nodeRef.Value.SourceName;
                chartMarker.PushInteractableObject(nodeRef.GetComponent<InteractableObject>(), infoText);
            }

            // Increase capacity at once to reduce number of allocations to only one.
            int markerPoolCount = chartMarkerPool.Count + activeMarkers.Count - currentReusedActiveMarkerIndex;
            if (markerPoolCount > chartMarkerPool.Capacity)
            {
                chartMarkerPool.Capacity = markerPoolCount;
            }

            for (int i = currentReusedActiveMarkerIndex; i < activeMarkers.Count; i++)
            {
                if (activeMarkers[i].gameObject != null)
                {
                    // Pool markers for future use
                    activeMarkers[i].gameObject.SetActive(false);
                    activeMarkers[i].OnDestroy();
#if UNITY_EDITOR
                    activeMarkers[i].name = "Pooled ChartMarker";
#endif
                    chartMarkerPool.Add(activeMarkers[i]);
                }
            }
            activeMarkers = updatedMarkers;
        }

        /// <summary>
        /// Hovers every interactable object of every marker that is inside given bounds.
        /// </summary>
        /// <param name="min">The min value of the bounds.</param>
        /// <param name="max">The max value of the bounds.</param>
        public virtual void AreaHover(Vector2 min, Vector2 max)
        {
            bool toggleHover = SEEInput.ToggleMetricHoveringSelection();
            foreach (ChartMarker marker in activeMarkers)
            {
                Vector2 markerPos = marker.transform.position;
                if (markerPos.x > min.x && markerPos.x < max.x && markerPos.y > min.y && markerPos.y < max.y)
                {
                    List<string> ids = marker.ids;
                    foreach (InteractableObject o in ids.Select(InteractableObject.Get).Where(o => !o.IsHoverFlagSet(HoverFlag.ChartMultiSelect)))
                    {
                        o.SetHoverFlag(HoverFlag.ChartMultiSelect, true, true);
                    }
                }
                else if (!toggleHover)
                {
                    List<string> ids = marker.ids;
                    foreach (InteractableObject o in ids.Select(InteractableObject.Get).Where(o => o.IsHoverFlagSet(HoverFlag.ChartMultiSelect)))
                    {
                        o.SetHoverFlag(HoverFlag.ChartMultiSelect, false, true);
                    }
                }
            }
        }

        /// <summary>
        /// Selects every interactable object of every marker, that is inside given
        /// bounds.
        /// </summary>
        /// <param name="min">The min value of the bounds.</param>
        /// <param name="max">The max value of the bounds.</param>
        public virtual void AreaSelection(Vector2 min, Vector2 max)
        {
            bool toggleSelect = SEEInput.ToggleMetricHoveringSelection();
            foreach (ChartMarker marker in activeMarkers)
            {
                Vector2 markerPos = marker.transform.position;
                if (markerPos.x > min.x && markerPos.x < max.x && markerPos.y > min.y && markerPos.y < max.y)
                {
                    List<string> ids = marker.ids;
                    foreach (InteractableObject o in ids.Select(InteractableObject.Get).Where(o => !o.IsSelected))
                    {
                        o.SetSelect(true, true);
                    }
                }
                else if (!toggleSelect)
                {
                    List<string> ids = marker.ids;
                    foreach (InteractableObject o in ids.Select(InteractableObject.Get).Where(o => o.IsSelected))
                    {
                        o.SetSelect(false, true);
                    }
                }
            }
        }

        /// <summary>
        /// Sets the info text of the chart.
        /// </summary>
        public void SetInfoText()
        {
            string metricX = axisDropdownX.CurrentlySelectedMetric;
            string metricY = axisDropdownY.CurrentlySelectedMetric;
            if (metricX.Equals(metricY))
            {
                moveHandler.SetInfoText(metricX);
            }
            else
            {
                moveHandler.SetInfoText("X-Axis: " + axisDropdownX.CurrentlySelectedMetric
                                        + "\n" + "Y-Axis: " + axisDropdownY.CurrentlySelectedMetric
                );
            }
        }

        /// <summary>
        /// Destroys the chart including its container. Called when the user clicks on
        /// the closing button.
        /// </summary>
        public void Destroy()
        {
            Destroy(gameObject);
        }

        /// <summary>
        /// Reloads the chart data on revision change
        /// </summary>
        private void ReloadData()
        {
            if (scrollViewEntryData != null && scrollViewEntryData.Length > 0)
            {
                for (int i = 0; i < scrollViewEntryData.Length; i++)
                {
                    if (scrollViewEntryData[i].interactableObject)
                    {
                        // Note(torben): This only unsubscribes from events from the
                        // interactable object and thus can be inside of this if-statement
                        // for better performance
                        scrollViewEntryData[i].OnDestroy();
                    }
                }
            }

            FindDataObjects();

            // Note(torben): The list view contains every node + two additional parent
            // header entries for 'Inner Nodes' and 'Leaves'. The tree view contains only the
            // nodes, so this here is the capacity.
            // Note(Leo): removedNodeIDs Count needs to be added, otherwise removed nodes won't show
            int totalEntryCount2 = 2 + listDataObjects.Count + removedNodeIDs.Count;
            totalHeight = totalEntryCount2 * ScrollViewEntryHeight;

            leafCount = listDataObjects.Count(nodeRef => nodeRef.Value.IsLeaf());

            scrollViewEntries = new ScrollViewEntry[totalEntryCount2];
            scrollViewEntryData = new ScrollViewEntryData[totalEntryCount2];

            RectTransform scrollContentRect = scrollContent.GetComponent<RectTransform>();
            scrollContentRect.sizeDelta = new Vector2(scrollContentRect.sizeDelta.x, totalHeight + 40);

            previousFirst = 0;
            previousOnePastLast = 0;

            FillScrollView(scrollViewIsTree);
            GetAllNumericAttributes();
        }

        /// <summary>
        /// Fills the newNodes, changedNodes and removedNodes lists respectively
        /// </summary>
        private void FillListsWithChanges()
        {
            newNodeIDs = NodeChangesBuffer.GetSingleton().addedNodeIDsCache;
            changedNodeIDs = NodeChangesBuffer.GetSingleton().changedNodeIDsCache;
            removedNodeIDs = NodeChangesBuffer.GetSingleton().removedNodeIDsCache;
        }

        /// <summary>
        /// Changes the text colors according to node changes that happened in the current revision
        /// </summary>
        /// <param name="scrollViewEntry">The scrollview entry, the color of which should be changed</param>
        /// <param name="parent">The parent gameObject of the scrollview entry gameObject</param>
        private void ChangeScrollViewEntryColor(GameObject scrollViewEntry, GameObject parent)
        {
            TextMeshProUGUI textMesh = scrollViewEntry.GetComponent<TextMeshProUGUI>();
            ColorBlock colors = parent.GetComponent<Toggle>().colors;

            if (removedNodeIDs.Contains(textMesh.text))
            {
                textMesh.color = removedNodeLabelColor;
                colors.normalColor = removedNodeLabelColor;
                colors.selectedColor = removedNodeLabelColor;
                colors.pressedColor = changedNodesLabelColor;
                colors.disabledColor = changedNodesLabelColor;
            }
            else if (changedNodeIDs.Contains(textMesh.text))
            {
                textMesh.color = changedNodesLabelColor;
                colors.normalColor = changedNodesLabelColor;
                colors.selectedColor = changedNodesLabelColor;
                colors.pressedColor = changedNodesLabelColor;
                colors.disabledColor = changedNodesLabelColor;
            }
            else if (newNodeIDs.Contains(textMesh.text))
            {
                textMesh.color = addedNodesLabelColor;
                colors.normalColor = addedNodesLabelColor;
                colors.selectedColor = addedNodesLabelColor;
                colors.pressedColor = addedNodesLabelColor;
                colors.disabledColor = addedNodesLabelColor;
            }
            else
            {
                textMesh.color = Color.white;
                colors.normalColor = Color.white;
            }
            colors.highlightedColor = hoveringOverLabelTextColor;
            parent.GetComponent<Toggle>().colors = colors;
        }
    }

    public class NodeChangesBuffer
    {
        /// <summary>
        /// Singleton
        /// </summary>
        private static NodeChangesBuffer singleton;

        /// <summary>
        /// Get singleton instance
        /// </summary>
        public static NodeChangesBuffer GetSingleton()
        {
            return singleton ??= new NodeChangesBuffer();
        }

        /// <summary>
        /// Current revision "id"
        /// </summary>
        public int currentRevisionCounter = 0;

        /// <summary>
        /// Detects a revision change
        /// </summary>
        public bool revisionChanged = false;

        /// <summary>
        /// Stores the IDs of nodes that have been added in the current revision
        /// </summary>
        public readonly List<string> addedNodeIDs = new List<string>();

        /// <summary>
        /// Stores the IDs of nodes that have been changed in the current revision
        /// </summary>
        public readonly List<string> changedNodeIDs = new List<string>();

        /// <summary>
        /// Stores the IDs of nodes that have been removed in the current revision
        /// </summary>
        public readonly List<string> removedNodeIDs = new List<string>();

        /// <summary>
        /// Old ids of newly added nodes, needed when closing and re-opening the chart
        /// </summary>
        public List<string> addedNodeIDsCache = new List<string>();

        /// <summary>
        /// Old ids of changed nodes, needed when closing and re-opening the chart
        /// </summary>
        public List<string> changedNodeIDsCache = new List<string>();

        /// <summary>
        /// Old ids of removed nodes, needed when closing and re-opening the chart
        /// </summary>
        public List<string> removedNodeIDsCache = new List<string>();
    }
}

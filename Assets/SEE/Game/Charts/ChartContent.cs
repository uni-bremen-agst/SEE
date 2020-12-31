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

using SEE.Controls;
using SEE.DataModel.DG;
using SEE.GO;
using SEE.Utils;
using System.Collections.Generic;
using System.Linq;
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
        /// All game-node objects to be listed in the chart. 
        /// 
        /// Invariant: all game objects in dataObjects are game objects tagged by Tags.Node
        /// and having a valid graph-node reference.
        /// </summary>
        private List<NodeRef> dataObjects;

        /// <summary>
        /// A list of all <see cref="ChartMarker" />s currently displayed in the chart.
        /// </summary>
        protected List<GameObject> activeMarkers = new List<GameObject>();

        public readonly Dictionary<NodeRef, ChartMarker> nodeRefToChartMarkerDict = new Dictionary<NodeRef, ChartMarker>(); // TODO(torben): can this be io not nodeRef?

        /// <summary>
        /// Contains all metric names contained in any <see cref="GameObject" /> of <see cref="dataObjects" />.
        /// </summary>
        public readonly SortedSet<string> allMetricNames = new SortedSet<string>();



        private readonly Dictionary<uint, bool> showInChartDict = new Dictionary<uint, bool>();

        public delegate void ShowInChartCallbackFn(bool value);
        private readonly Dictionary<uint, List<ShowInChartCallbackFn>> callbackFnDict = new Dictionary<uint, List<ShowInChartCallbackFn>>();

        public void AttachShowInChartCallbackFn(InteractableObject interactableObject, ShowInChartCallbackFn callbackFn)
        {
            if (!callbackFnDict.TryGetValue(interactableObject.ID, out List<ShowInChartCallbackFn> callbackFns))
            {
                callbackFns = new List<ShowInChartCallbackFn>();
                callbackFnDict.Add(interactableObject.ID, callbackFns);
            }
            callbackFns.Add(callbackFn);
        }

        public bool DetachShowInChartCallbackFn(InteractableObject interactableObject, ShowInChartCallbackFn callbackFn)
        {
            bool result = false;
            if (callbackFnDict.TryGetValue(interactableObject.ID, out List<ShowInChartCallbackFn> callbackFns))
            {
                result = callbackFns.Remove(callbackFn);
            }
            return result;
        }

        public bool ShowInChart(InteractableObject interactableObject)
        {
            bool result = showInChartDict[interactableObject.ID];
            return result;
        }

        public void SetShowInChart(InteractableObject interactableObject, bool value)
        {
            if (!showInChartDict.TryGetValue(interactableObject.ID, out bool oldValue))
            {
                oldValue = true;
            }
            showInChartDict[interactableObject.ID] = value;
            if (oldValue != value && callbackFnDict.TryGetValue(interactableObject.ID, out List<ShowInChartCallbackFn> callbackFns))
            {
                foreach (ShowInChartCallbackFn fn in callbackFns)
                {
                    fn(value);
                }
            }
        }



        /// <summary>
        /// Calls methods to initialize a chart.
        /// </summary>
        private void Awake()
        {
            Assert.IsTrue(scrollContent.transform.childCount == 0);

            FindDataObjects();

            float stride = ScrollViewEntryHeight;

            // Note(torben): The list view contains every node + two additional parent
            // header entries for 'Inner Nodes' and 'Leaves'. The tree view tree only the
            // nodes, so this here is the capacity.
            int totalEntryCount = 2 + dataObjects.Count;
            totalHeight = (float)totalEntryCount * stride;

            leafCount = 0;
            foreach (NodeRef nodeRef in dataObjects)
            {
                if (nodeRef.node.IsLeaf())
                {
                    leafCount++;
                }
            }

            // TODO(torben): 'maxPanelHeight' is slightly too large, but the viewport
            // does have a height of zero... that's why i have to calculate the current
            // height of the scrollrect in Update() every frame
            float maxPanelHeight = scrollViewRectTransform.sizeDelta.y;
            maxPanelEntryCount = Mathf.FloorToInt(maxPanelHeight / stride) + 1;

            scrollViewEntries = new ScrollViewEntry[totalEntryCount];
            scrollViewEntryDatas = new ScrollViewEntryData[totalEntryCount];

            int idx = 0;
            int dataObjectIdx = 0;
            scrollViewEntryDatas[idx] = new ScrollViewEntryData(idx, this, null, ScrollViewEntryData.InvalidIndex, leafCount, true);
            idx++;

            for (int i = 0; i < leafCount; i++)
            {
                scrollViewEntryDatas[idx]
                    = new ScrollViewEntryData(idx, this, dataObjects[dataObjectIdx++].GetComponent<InteractableObject>(), 0, 0, true);
                scrollViewEntryDatas[0].childIndices[i] = idx;
                idx++;
            }

            int innerCount = dataObjects.Count - leafCount;
            int innerNodeIdx = idx;
            scrollViewEntryDatas[idx] = new ScrollViewEntryData(idx, this, null, ScrollViewEntryData.InvalidIndex, innerCount, true);
            idx++;

            for (int i = 0; i < innerCount; i++)
            {
                scrollViewEntryDatas[idx]
                    = new ScrollViewEntryData(idx, this, dataObjects[dataObjectIdx++].GetComponent<InteractableObject>(), innerNodeIdx, 0, true);
                scrollViewEntryDatas[innerNodeIdx].childIndices[i] = idx;
                idx++;
            }

            pool = new Stack<ScrollViewEntry>(maxPanelEntryCount);

            RectTransform scrollContentRect = scrollContent.GetComponent<RectTransform>();
            scrollContentRect.sizeDelta = new Vector2(scrollContentRect.sizeDelta.x, totalHeight + 40);

            FillScrollView(false);
            GetAllNumericAttributes();
        }
        
        protected virtual void Start()
        {
            axisDropdownX.Initialize();
            axisDropdownY.Initialize();
            axisDropdownX.AddNodeEnumerationEntry(NodeEnumeration);
            DrawData();
        }

        [SerializeField] private Scrollbar verticalScrollBar;

        // TODO(torben): i'd rather have the Viewport sizes, as this is slightly too
        // large. as a result, i have to calculate 'first' in Update() every frame with
        // 'verticalScrollBar.value'
        [SerializeField] private RectTransform scrollViewRectTransform;

        private float totalHeight = 0;
        private int maxPanelEntryCount = 0;
        private int leafCount = 0;

        private ScrollViewEntry[] scrollViewEntries = null;
        private ScrollViewEntryData[] scrollViewEntryDatas = null;
        private Stack<ScrollViewEntry> pool = null;

        private int previousFirst = 0;
        private int previousOnePastLast = 0;

        private void Update()
        {
            float panelEntryCount = totalHeight * (1.0f - verticalScrollBar.size) / ScrollViewEntryHeight;
            int first = Mathf.Max(0, Mathf.FloorToInt((1.0f - verticalScrollBar.value) * panelEntryCount));
            int onePastLast = Mathf.Min(scrollViewEntries.Length, first + maxPanelEntryCount);

            void _PushToPool(int i)
            {
                Assert.IsNotNull(scrollViewEntries[i], "The toggle to be pooled is null!");

                scrollViewEntries[i].OnDestroy();
                pool.Push(scrollViewEntries[i]);
                scrollViewEntries[i] = null;
            }

            void _NewScrollViewEntry(int i)
            {
                Assert.IsNull(scrollViewEntries[i]);
                int leavesIndex = 0;
                int innerNodeIndex = leafCount + 1;
                int temp = i;
                if (i == leavesIndex) // 'Leaves' node
                {
                    scrollViewEntries[leavesIndex] = NewScrollViewEntry("Leaves", null, ref temp, 0);
                }
                else if (i == innerNodeIndex) // 'Inner Nodes' node
                {
                    scrollViewEntries[innerNodeIndex] = NewScrollViewEntry("Inner Nodes", null, ref temp, 0);
                }
                else if (i < leafCount + 1) // leaf node
                {
                    scrollViewEntries[i] = NewScrollViewEntry(dataObjects[i - 1], scrollViewEntries[leavesIndex], ref temp, 1);
                }
                else // inner node
                {
                    scrollViewEntries[i] = NewScrollViewEntry(dataObjects[i - 2], scrollViewEntries[innerNodeIndex], ref temp, 1);
                }
            }

            // delete out of view entries
            for (int i = previousFirst; i < Mathf.Min(previousOnePastLast, first); i++) // before
            {
                _PushToPool(i);
            }
            for (int i = Mathf.Max(onePastLast, previousFirst); i < previousOnePastLast; i++) // after
            {
                _PushToPool(i);
            }

            // prepend and append new entries
            for (int i = first; i < Mathf.Min(previousFirst, onePastLast); i++) // prepend
            {
                _NewScrollViewEntry(i);
            }
            for (int i = Mathf.Max(previousOnePastLast, first); i < onePastLast; i++) // append
            {
                _NewScrollViewEntry(i);
            }

            previousFirst = first;
            previousOnePastLast = onePastLast;
        }

        public ScrollViewEntry GetScrollViewEntry(int index)
        {
            ScrollViewEntry result = scrollViewEntries[index];
            return result;
        }

        public ref ScrollViewEntryData GetScrollViewEntryData(int index)
        {
            ref ScrollViewEntryData result = ref scrollViewEntryDatas[index];
            return ref result;
        }

        /// <summary>
        /// Either creates or retrieves a pooled <see cref="ScrollViewEntry"/>. Pooled
        /// toggles are cleared, before they're returned.
        /// </summary>
        /// <param name="popPool">The pool, of which the toggles can be retrieved.
        /// </param>
        /// <param name="svt">The new scroll view toggle.</param>
        /// <param name="go">The game object containing the scroll view toggle.</param>
        private void NewScrollViewEntry(ref ScrollViewEntry svt, ref GameObject go)
        {
            if (pool.Count > 0)
            {
                svt = pool.Pop();
                go = svt.gameObject;
            }
            else
            {
                go = Instantiate(scrollEntryPrefab, scrollContent.transform);
                svt = go.GetComponent<ScrollViewEntry>();
            }
        }

        /// <summary>
        /// Creates a new scroll view toggle entry to display given node ref.
        /// </summary>
        /// <param name="nodeRef">The node to be displayed.</param>
        /// <param name="parent">The parent of the entry.</param>
        /// <param name="index">The index of the toggle to determine the y-offset of the
        /// entry.</param>
        /// <param name="hierarchy">The hierarchy to determine the x-offset of the entry.
        /// </param>
        /// <param name="pushPool">The pool, in which the new entries are pushed.</param>
        /// <param name="popPool">The pool, out of which pooled toggled can be retrieved.
        /// </param>
        /// <returns>The created scroll view toggle.</returns>
        private ScrollViewEntry NewScrollViewEntry(NodeRef nodeRef, ScrollViewEntry parent, ref int index, int hierarchy)
        {
            ScrollViewEntry entry = null;
            GameObject go = null;
            NewScrollViewEntry(ref entry, ref go);

            go.name = "ScrollViewEntry: " + nodeRef.node.SourceName;
            go.transform.localPosition = scrollEntryOffset
                + new Vector2(ScrollViewEntryIndentation * (float)hierarchy, -ScrollViewEntryHeight * (float)index);

            entry.Init(this, ref scrollViewEntryDatas[index], nodeRef.name);

            index++;

            return entry;
        }

        /// <summary>
        /// Creates a new scroll view toggle entry with given label.
        /// </summary>
        /// <param name="label">The label.</param>
        /// <param name="parent">The parent of the entry.</param>
        /// <param name="index">The index of the toggle to determine the y-offset of the
        /// entry.</param>
        /// <param name="hierarchy">The hierarchy to determine the x-offset of the entry.
        /// </param>
        /// <param name="pushPool">The pool, in which the new entries are pushed.</param>
        /// <param name="popPool">The pool, out of which pooled toggled can be retrieved.
        /// </param>
        /// <returns>The created scroll view toggle.</returns>
        private ScrollViewEntry NewScrollViewEntry(string label, ScrollViewEntry parent, ref int index, int hierarchy)
        {
            ScrollViewEntry entry = null;
            GameObject go = null;
            NewScrollViewEntry(ref entry, ref go);

            go.name = "ScrollViewEntry: " + label;
            go.transform.localPosition = scrollEntryOffset
                + new Vector2(ScrollViewEntryIndentation * (float)hierarchy, -ScrollViewEntryHeight * (float)index);

            entry.Init(this, ref scrollViewEntryDatas[index], label);

            index++;

            return entry;
        }

        /// <summary>
        /// Recursive version of
        /// <see cref="NewScrollViewEntry(NodeRef, ScrollViewEntry, ref int, int, Stack{ScrollViewEntry}, Stack{ScrollViewEntry})"/>
        /// </summary>
        private ScrollViewEntry NewScrollViewEntries(NodeRef nodeRef, ScrollViewEntry parent, ref int index, int hierarchy)
        {
            ScrollViewEntry svt = NewScrollViewEntry(nodeRef, parent, ref index, hierarchy);
            foreach (Node childNode in nodeRef.node.Children())
            {
                NodeRef childNodeRef = dataObjects.First(entry => { return entry.node.ID.Equals(childNode.ID); });
                NewScrollViewEntries(childNodeRef, svt, ref index, hierarchy + 1);
            }
            return svt;
        }

        /// <summary>
        /// Fills the scroll with as a list or a tree. Is called on start-up and
        /// thereupon only by Unity on button-press-events.
        /// </summary>
        /// <param name="displayAsTree">Whether the scroll view is to be filled as a
        /// tree.</param>
        public void FillScrollView(bool displayAsTree)
        {
            Performance p = Performance.Begin(displayAsTree ? "FillScrollViewAsTree" : "FillScrollViewAsList");
            
#if false
            if (!displayAsTree) // display as list
            {
                [...]
            }
            else // display as tree
            {
                foreach (Node root in SceneQueries.GetRoots(dataObjects))
                {
                    NodeRef rootNodeRef = dataObjects.First(entry =>
                    {
                        return entry.node.ID.Equals(root.ID);
                    });
                    NewScrollViewEntries(
                        rootNodeRef, null, ref index, 0, pushPool, scrollViewTogglePool
#if SEE_RECT_WIDTH
                        , ref maxWidth
#endif
                    );
                }
        }
#endif

            p.End(true);
        }

        /// <summary>
        /// Gets all metric names for <see cref="float"/> values contained in the
        /// <see cref="NodeRef"/> of each <see cref="GameObject"/> in
        /// <see cref="dataObjects"/>. A metric name is the name of a numeric (either
        /// float or int) node attribute that starts with the prefix
        /// ChartManager.MetricPrefix.</summary>
        private void GetAllNumericAttributes()
        {
            allMetricNames.Clear();
#if UNITY_EDITOR
            if (dataObjects.Count == 0)
            {
                Debug.LogWarning("There are no nodes for showing metrics.\n");
            }
#endif
            foreach (string name in Attributable.NumericAttributeNames)
            {
                if (name.StartsWith(ChartManager.MetricPrefix))
                {
                    allMetricNames.Add(name);
                }
            }
#if UNITY_EDITOR
            if (allMetricNames.Count == 0)
            {
                Debug.LogWarning("No metrics available for charts.\n");
            }
#endif
        }

        /// <summary>
        /// Fills a List with all <see cref="Node"/>s that will be in the chart.
        /// </summary>
        private void FindDataObjects()
        {
            dataObjects = SceneQueries.AllNodeRefsInScene(ChartManager.Instance.ShowLeafMetrics, ChartManager.Instance.ShowInnerNodeMetrics);

            int numberOfDataObjectsWithNodeHightLights = 0;
            foreach (NodeRef entry in dataObjects)
            {
                SetShowInChart(entry.GetComponent<InteractableObject>(), true); // TODO(torben): don't get it? performance
                numberOfDataObjectsWithNodeHightLights++;
            }
            dataObjects.Sort(delegate (NodeRef n0, NodeRef n1)
            {
                int result = 0;
                if (n0.node.IsLeaf() && n1.node.IsInnerNode())
                {
                    result = -1;
                }
                else if (n0.node.IsInnerNode() && n1.node.IsLeaf())
                {
                    result = 1;
                }
                return result;
            });

            Debug.LogFormat("numberOfDataObjectsWithNodeHightLights: {0}\n", numberOfDataObjectsWithNodeHightLights);
        }

        /// <summary>
        /// Fills the chart with data depending on the values of
        /// <see cref="axisDropdownX"/> and <see cref="axisDropdownY"/>.
        /// </summary>
        public void DrawData()
        {
            noDataWarning.SetActive(false);

            bool xIsNodeEnum = axisDropdownX.CurrentlySelectedMetric.Equals(NodeEnumeration);
            bool xEqY = axisDropdownX.CurrentlySelectedMetric.Equals(axisDropdownY.CurrentlySelectedMetric);

            // Note that we determine the minimal and maximal metric values of the two
            // axes globally, that is, over all nodes in the scene and not just those
            // shown in this particular chart. This way, the scale of all charts for the
            // same metric is comparable.
            float minX = float.PositiveInfinity; // globally minimal value on X axis
            float maxX = float.NegativeInfinity; // globally maximal value on X axis
            float minY = float.PositiveInfinity; // globally minimal value on Y axis
            float maxY = float.NegativeInfinity; // globally maximal value on Y axis
            List<NodeRef> toDraw = new List<NodeRef>(); // nodes to be drawn in the chart
            foreach (NodeRef nodeRef in dataObjects)
            {
                bool inX = false;
                if (nodeRef.node.TryGetNumeric(axisDropdownX.CurrentlySelectedMetric, out float valueX) || xIsNodeEnum)
                {
                    minX = Mathf.Min(minX, valueX);
                    maxX = Mathf.Max(maxX, valueX);
                    inX = true;
                }
                bool inY = false;
                if (nodeRef.node.TryGetNumeric(axisDropdownY.CurrentlySelectedMetric, out float valueY))
                {
                    minY = Mathf.Min(minY, valueY);
                    maxY = Mathf.Max(maxY, valueY);
                    inY = true;
                }
                // Is this node shown in this chart at all?
                if (inX && inY)
                {
                    // only nodes to be shown in this chart and having values for both
                    // currently selected metrics for the axes will be added to the chart
                    toDraw.Add(nodeRef);
                }
            }

            if (toDraw.Count > 0)
            {
                if (xEqY)
                {
                    toDraw.Sort(delegate (NodeRef nodeRef0, NodeRef nodeRef1)
                    {
                        nodeRef0.node.TryGetNumeric(axisDropdownY.CurrentlySelectedMetric, out float value1);
                        nodeRef1.node.TryGetNumeric(axisDropdownY.CurrentlySelectedMetric, out float value2);
                        return value1.CompareTo(value2);
                    });
                }

                bool xEqual = minX.Equals(maxX);
                bool yEqual = minY.Equals(maxY);
                if (xEqual || yEqual)
                {
                    (float min, float max) = minX.Equals(maxX) ? (minY, maxY) : (minX, maxX);
                    AddMarkers(toDraw, min, max, min, max);
                    minXText.text = xEqual ? "0" : minX.ToString("N0");
                    maxXText.text = xEqual ? toDraw.Count.ToString() : maxX.ToString("N0");
                    minYText.text = yEqual ? "0" : minY.ToString("N0");
                    maxYText.text = yEqual ? toDraw.Count.ToString() : maxY.ToString("N0");
                }
                else
                {
                    AddMarkers(toDraw, minX, maxX, minY, maxY);
                    minXText.text = minX.ToString("N0");
                    maxXText.text = maxX.ToString("N0");
                    minYText.text = minY.ToString("N0");
                    maxYText.text = maxY.ToString("N0");
                }
            }
            else
            {
                foreach (GameObject activeMarker in activeMarkers)
                {
                    Destroy(activeMarker);
                }

                noDataWarning.SetActive(true);
            }
        }

        /// <summary>
        /// Adds new markers to the chart and removes the old ones.
        /// </summary>
        /// <param name="nodeRefsToDraw">The markers to add to the chart.</param>
        /// <param name="minX">The minimum value on the x-axis.</param>
        /// <param name="maxX">The maximum value on the x-axis.</param>
        /// <param name="minY">The minimum value on the y-axis.</param>
        /// <param name="maxY">The maximum value on the y-axis.</param>
        private void AddMarkers(IEnumerable<NodeRef> nodeRefsToDraw, float minX, float maxX, float minY, float maxY)
        {
            foreach (GameObject marker in activeMarkers)
            {
                Destroy(marker);
            }
            nodeRefToChartMarkerDict.Clear();

            List<GameObject> updatedMarkers = new List<GameObject>();
            Dictionary<Vector2, ChartMarker> anchoredPositionToChartMarkerDict = new Dictionary<Vector2, ChartMarker>();

            Rect dataRect = dataPanel.rect;
            float width = minX < maxX ? dataRect.width / (maxX - minX) : 0.0f;
            float height = minY < maxY ? dataRect.height / (maxY - minY) : 0.0f;
            int positionInLayer = 0;

            foreach (NodeRef nodeRef in nodeRefsToDraw)
            {
                nodeRef.node.TryGetNumeric(axisDropdownX.CurrentlySelectedMetric, out float valueX);
                nodeRef.node.TryGetNumeric(axisDropdownY.CurrentlySelectedMetric, out float valueY);
                Vector2 anchoredPosition = new Vector2((valueX - minX) * width, (valueY - minY) * height);

                if (!anchoredPositionToChartMarkerDict.TryGetValue(anchoredPosition, out ChartMarker chartMarker))
                {
                    GameObject marker = Instantiate(markerPrefab, entries.transform);
                    marker.name = "ChartMarker: " + nodeRef.node.SourceName;
                    marker.GetComponent<RectTransform>().anchoredPosition = anchoredPosition;
                    marker.GetComponent<SortingGroup>().sortingOrder = positionInLayer++;
                    chartMarker = marker.GetComponent<ChartMarker>();
                    chartMarker.chartContent = this;
                    updatedMarkers.Add(marker);
                    anchoredPositionToChartMarkerDict.Add(anchoredPosition, chartMarker);
                }
                else if (!chartMarker.gameObject.name.EndsWith(", [...]"))
                {
                    chartMarker.gameObject.name += ", [...]";
                }

                string infoText = nodeRef.node.SourceName + " (" + valueX.ToString("0.00") + ", " + valueY.ToString("0.00") + ")";
                chartMarker.PushInteractableObject(nodeRef.GetComponent<InteractableObject>(), infoText);
                nodeRefToChartMarkerDict.Add(nodeRef, chartMarker);
            }

            activeMarkers = updatedMarkers;
        }

        /// <summary>
        /// Hoveres every interactable object of every marker, that is inside given
        /// bounds.
        /// </summary>
        /// <param name="min">The min value of the bounds.</param>
        /// <param name="max">The max value of the bounds.</param>
        public virtual void AreaHover(Vector2 min, Vector2 max)
        {
            bool toggleHover = Input.GetKey(KeyCode.LeftControl);
            foreach (GameObject marker in activeMarkers)
            {
                HashSet<uint> ids = marker.GetComponent<ChartMarker>().ids;
                foreach (uint id in ids)
                {
                    InteractableObject o = InteractableObject.Get(id);
                    Vector2 markerPos = marker.transform.position;
                    if (markerPos.x > min.x && markerPos.x < max.x && markerPos.y > min.y && markerPos.y < max.y)
                    {
                        if (!o.IsHovered)
                        {
                            o.SetHoverFlag(HoverFlag.ChartMultiSelect, true, true);
                        }
                    }
                    else if (!toggleHover && o.IsHovered)
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
            bool toggleSelect = Input.GetKey(KeyCode.LeftControl);
            foreach (GameObject marker in activeMarkers)
            {
                HashSet<uint> ids = marker.GetComponent<ChartMarker>().ids;
                foreach (uint id in ids)
                {
                    InteractableObject o = InteractableObject.Get(id);
                    Vector2 markerPos = marker.transform.position;
                    if (markerPos.x > min.x && markerPos.x < max.x && markerPos.y > min.y && markerPos.y < max.y)
                    {
                        if (!o.IsSelected)
                        {
                            o.SetSelect(true, true);
                        }
                    }
                    else if (!toggleSelect && o.IsSelected)
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
                moveHandler.SetInfoText(
                    "X-Axis: " + axisDropdownX.CurrentlySelectedMetric + "\n" + "Y-Axis: " + axisDropdownY.CurrentlySelectedMetric
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
    }
}
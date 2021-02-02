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
using SEE.Game.Evolution;

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
        /// All game-node objects to be listed in the chart for the list-view.
        /// 
        /// Invariant: all game objects in dataObjects are game objects tagged by
        /// Tags.Node and having a valid graph-node reference.
        /// </summary>
        private List<NodeRef> listDataObjects;

        /// <summary>
        /// All game-node objects to be listed in the chart for the tree-view.
        /// 
        /// Invariant: all game objects in dataObjects are game objects tagged by
        /// Tags.Node and having a valid graph-node reference.
        /// </summary>
        private List<NodeRef> treeDataObjects;

        /// <summary>
        /// Ids of the nodes that have been added in this revision
        /// </summary>
        List<string> newNodeIDs = new List<string>();

        /// <summary>
        /// Ids of the nodes that have changed in this revision
        /// </summary>
        List<string> changedNodeIDs = new List<string>();

        /// <summary>
        /// Ids of the nodes that have been removed in this revision
        /// </summary>
        List<string> removedNodeIDs = new List<string>();

        /// <summary>
        /// Color of added node labels
        /// </summary> 
        private Color addedNodesLabelColor;
        
        /// <summary>
        /// Color of changed node labels
        /// </summary>
        private Color changedNodesLabelColor;

        /// <summary>
        /// Color of removed node labels
        /// </summary>
        private Color removedNodeLabelColor;

        /// <summary>
        /// The color the label will have when hovering over it
        /// </summary>
        private Color hoveringOverLabelTextColor;

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

        private readonly Dictionary<uint, bool> showInChartDict = new Dictionary<uint, bool>();

        public delegate void ShowInChartCallbackFn(bool value);
        private readonly Dictionary<uint, ShowInChartCallbackFn> callbackFnDict = new Dictionary<uint, ShowInChartCallbackFn>();

        private bool scrollViewIsTree = false;

        private float totalHeight = 0;
        private int maxPanelEntryCount = 0;
        private int leafCount = 0;

        private ScrollViewEntry[] scrollViewEntries = null;
        private ScrollViewEntryData[] scrollViewEntryDatas = null;
        private Stack<ScrollViewEntry> pool = null;

        private int previousFirst = 0;
        private int previousOnePastLast = 0;


        /// <summary>
        /// Calls methods to initialize a chart.
        /// </summary>
        private void Awake()
        {
            // Load color profile for chart entries
            FetchLableColorProfile();

            Assert.IsTrue(scrollContent.transform.childCount == 0);

            FindDataObjects();

            // Note(torben): The list view contains every node + two additional parent
            // header entries for 'Inner Nodes' and 'Leaves'. The tree view tree only the
            // nodes, so this here is the capacity.
            int totalEntryCount = 2 + listDataObjects.Count;
            totalHeight = (float)totalEntryCount * ScrollViewEntryHeight;

            leafCount = 0;
            foreach (NodeRef nodeRef in listDataObjects)
            {
                if (nodeRef.Value.IsLeaf())
                {
                    leafCount++;
                }
            }

            scrollViewEntries = new ScrollViewEntry[totalEntryCount];
            scrollViewEntryDatas = new ScrollViewEntryData[totalEntryCount];

            pool = new Stack<ScrollViewEntry>(maxPanelEntryCount);

            RectTransform scrollContentRect = scrollContent.GetComponent<RectTransform>();
            scrollContentRect.sizeDelta = new Vector2(scrollContentRect.sizeDelta.x, totalHeight + 40);

            FillScrollView(scrollViewIsTree);
            GetAllNumericAttributes();
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
                            scrollViewEntries[i] = NewScrollViewEntry(listDataObjects[i - 2].name, i, 1);
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



        public void AttachShowInChartCallbackFn(InteractableObject interactableObject, ShowInChartCallbackFn callbackFn)
        {
            Assert.IsTrue(!callbackFnDict.ContainsKey(interactableObject.ID));
            callbackFnDict[interactableObject.ID] = callbackFn;
        }

        public bool DetachShowInChartCallbackFn(InteractableObject interactableObject, ShowInChartCallbackFn callbackFn)
        {
            bool result = callbackFnDict.Remove(interactableObject.ID);
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
            if (oldValue != value && callbackFnDict.TryGetValue(interactableObject.ID, out ShowInChartCallbackFn fn))
            {
                fn(value);
            }
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

        private void PushScrollViewEntriesToPool(int first, int onePastLast)
        {
            for (int i = first; i < onePastLast; i++)
            {
                Assert.IsNotNull(scrollViewEntries[i], "The toggle to be pooled is null!");

                scrollViewEntries[i].OnDestroy();
                pool.Push(scrollViewEntries[i]);
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
            float x = ScrollViewEntryIndentation * (float)hierarchy;
            float y = -ScrollViewEntryHeight * (float)index;
            go.transform.localPosition = scrollEntryOffset + new Vector2(x, y);

            entry.Init(this, ref scrollViewEntryDatas[index], label);

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
            Performance p = Performance.Begin(asTree ? "FillScrollViewAsTree" : "FillScrollViewAsList");

            #region Reset

            for (int i = 0; i < listDataObjects.Count; i++)
            {
                SetShowInChart(listDataObjects[i].GetComponent<InteractableObject>(), true);
            }

            for (int i = 0; i < scrollViewEntryDatas.Length; i++)
            {
                if (scrollViewEntryDatas[i].interactableObject)
                {
                    // Note(torben): This only unsubscribes from events from the
                    // interactable object and thus can be inside of this if-statement
                    // for better performance
                    scrollViewEntryDatas[i].OnDestroy();
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
                    scrollViewEntryDatas[i] = new ScrollViewEntryData(i, this, o, parentIndexStack.Peek(), childCount);

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
                            scrollViewEntryDatas[i].childIndices[childCount] = j;
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

                scrollViewEntryDatas[idx] = new ScrollViewEntryData(idx, this, null, ScrollViewEntryData.NoParentIndex, leafCount);
                idx++;

                for (int i = 0; i < leafCount; i++)
                {
                    InteractableObject o = listDataObjects[dataObjectIdx++].GetComponent<InteractableObject>();
                    scrollViewEntryDatas[idx] = new ScrollViewEntryData(idx, this, o, 0, 0);
                    scrollViewEntryDatas[0].childIndices[i] = idx;
                    idx++;
                }

                int innerCount = listDataObjects.Count - leafCount;
                int innerNodeIdx = idx;
                scrollViewEntryDatas[idx] = new ScrollViewEntryData(idx, this, null, ScrollViewEntryData.NoParentIndex, innerCount);
                idx++;

                for (int i = 0; i < innerCount; i++)
                {
                    InteractableObject o = listDataObjects[dataObjectIdx++].GetComponent<InteractableObject>();
                    scrollViewEntryDatas[idx] = new ScrollViewEntryData(idx, this, o, innerNodeIdx, 0);
                    scrollViewEntryDatas[innerNodeIdx].childIndices[i] = idx;
                    idx++;
                }
            }

            #endregion

            p.End(true);
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
            // list
            listDataObjects = SceneQueries.AllNodeRefsInScene(ChartManager.Instance.ShowLeafMetrics, ChartManager.Instance.ShowInnerNodeMetrics);
            // Detect node changes and decorate the scrollview
            FillListsWithChanges(listDataObjects);

            listDataObjects.Sort(delegate (NodeRef n0, NodeRef n1)
            {
                int result = 0;
                if (n0.Value.IsLeaf() && n1.Value.IsInnerNode())
                {
                    result = -1;
                }
                else if (n0.Value.IsInnerNode() && n1.Value.IsLeaf())
                {
                    result = 1;
                }
                return result;
            });

            Debug.LogFormat("numberOfDataObjectsWithNodeHightLights: {0}\n", listDataObjects.Count);

            // tree
            treeDataObjects = new List<NodeRef>(listDataObjects.Count);
            treeHierarchies = new List<int>(listDataObjects.Count);
            int hierarchy = 0;
            void _FindForTree(Node root)
            {
                treeDataObjects.Add(NodeRef.Get(root));
                treeHierarchies.Add(hierarchy);

                hierarchy++;
                foreach (Node child in root.Children())
                {
                    _FindForTree(child);
                }
                hierarchy--;
            }
            HashSet<Node> roots = SceneQueries.GetRoots(listDataObjects);
            foreach (Node root in roots)
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
            List<NodeRef> toDraw = new List<NodeRef>(activeMarkers.Count); // nodes to be drawn in the chart
            foreach (NodeRef nodeRef in listDataObjects)
            {
                bool inX = false;
                if (nodeRef.Value.TryGetNumeric(axisDropdownX.CurrentlySelectedMetric, out float valueX) || xIsNodeEnum)
                {
                    minX = Mathf.Min(minX, valueX);
                    maxX = Mathf.Max(maxX, valueX);
                    inX = true;
                }
                bool inY = false;
                if (nodeRef.Value.TryGetNumeric(axisDropdownY.CurrentlySelectedMetric, out float valueY))
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
                        nodeRef0.Value.TryGetNumeric(axisDropdownY.CurrentlySelectedMetric, out float value1);
                        nodeRef1.Value.TryGetNumeric(axisDropdownY.CurrentlySelectedMetric, out float value2);
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
                foreach (ChartMarker marker in activeMarkers)
                {
                    Destroy(marker.gameObject);
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
            callbackFnDict.Clear();

            List<ChartMarker> updatedMarkers = new List<ChartMarker>(activeMarkers.Count);
            Dictionary<Vector2, ChartMarker> anchoredPositionToChartMarkerDict = new Dictionary<Vector2, ChartMarker>(activeMarkers.Count);

            Rect dataRect = dataPanel.rect;
            float width = minX < maxX ? dataRect.width / (maxX - minX) : 0.0f;
            float height = minY < maxY ? dataRect.height / (maxY - minY) : 0.0f;
            int positionInLayer = 0;
            int currentReusedActiveMarkerIndex = 0;

            foreach (NodeRef nodeRef in nodeRefsToDraw)
            {
                nodeRef.Value.TryGetNumeric(axisDropdownX.CurrentlySelectedMetric, out float valueX);
                nodeRef.Value.TryGetNumeric(axisDropdownY.CurrentlySelectedMetric, out float valueY);
                Vector2 anchoredPosition = new Vector2((valueX - minX) * width, (valueY - minY) * height);

                if (!anchoredPositionToChartMarkerDict.TryGetValue(anchoredPosition, out ChartMarker chartMarker))
                {
                    GameObject marker;
                    if (currentReusedActiveMarkerIndex < activeMarkers.Count)
                    {
                        chartMarker = activeMarkers[currentReusedActiveMarkerIndex++];
                        chartMarker.OnDestroy();
                        marker = chartMarker.gameObject;
                    }
                    else
                    {
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

            for (int i = currentReusedActiveMarkerIndex; i < activeMarkers.Count; i++)
            {
                Destroy(activeMarkers[i].gameObject); // TODO(torben): these could potentially still be pooled for future rebuilds
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
            foreach (ChartMarker marker in activeMarkers)
            {
                Vector2 markerPos = marker.transform.position;
                if (markerPos.x > min.x && markerPos.x < max.x && markerPos.y > min.y && markerPos.y < max.y)
                {
                    List<uint> ids = marker.ids;
                    for (int i = 0; i < ids.Count; i++)
                    {
                        InteractableObject o = InteractableObject.Get(ids[i]);
                        if (!o.IsHoverFlagSet(HoverFlag.ChartMultiSelect))
                        {
                            o.SetHoverFlag(HoverFlag.ChartMultiSelect, true, true);
                        }
                    }
                }
                else if (!toggleHover)
                {
                    List<uint> ids = marker.ids;
                    for (int i = 0; i < ids.Count; i++)
                    {
                        InteractableObject o = InteractableObject.Get(ids[i]);
                        if (o.IsHoverFlagSet(HoverFlag.ChartMultiSelect))
                        {
                            o.SetHoverFlag(HoverFlag.ChartMultiSelect, false, true);
                        }
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
            foreach (ChartMarker marker in activeMarkers)
            {
                Vector2 markerPos = marker.transform.position;
                if (markerPos.x > min.x && markerPos.x < max.x && markerPos.y > min.y && markerPos.y < max.y)
                {
                    List<uint> ids = marker.ids;
                    foreach (uint id in ids)
                    {
                        InteractableObject o = InteractableObject.Get(id);
                        if (!o.IsSelected)
                        {
                            o.SetSelect(true, true);
                        }
                    }
                }
                else if (!toggleSelect)
                {
                    List<uint> ids = marker.ids;
                    foreach (uint id in ids)
                    {
                        InteractableObject o = InteractableObject.Get(id);
                        if (o.IsSelected)
                        {
                            o.SetSelect(false, true);
                        }
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
        
        

        /// <summary>
        /// Fetches the color profile selected in the ChartManager for added, edited and removed node labels
        /// </summary>
        private void FetchLableColorProfile()
        {
            this.hoveringOverLabelTextColor = Color.yellow;
            // Load colors used for power beams, to match the text with the visible objects
            try
            {
                this.addedNodesLabelColor = new Color(AdditionalBeamDetails.newBeamColor.r, AdditionalBeamDetails.newBeamColor.g, AdditionalBeamDetails.newBeamColor.b);
                this.changedNodesLabelColor = new Color(AdditionalBeamDetails.changedBeamColor.r, AdditionalBeamDetails.changedBeamColor.g, AdditionalBeamDetails.changedBeamColor.b);
                this.removedNodeLabelColor = new Color(AdditionalBeamDetails.deletedBeamColor.r, AdditionalBeamDetails.deletedBeamColor.g, AdditionalBeamDetails.deletedBeamColor.b);
            }
            catch
            {
                // Set default colors: red for removed, green for added, cyan for changed
                this.addedNodesLabelColor = Color.green;
                this.changedNodesLabelColor = Color.cyan;
                this.removedNodeLabelColor = Color.red;
            }
        }

        /// <summary>
        /// Fills the newNodes, changedNodes and removedNodes lists respectively
        /// </summary>
        private void FillListsWithChanges(List<NodeRef> nodeRefs)
        {
            // Temporary storage of node id lists
            List<string> newNodes = NodeChangesBuffer.GetSingleton().addedNodeIDs;
            List<string> changedNodes = NodeChangesBuffer.GetSingleton().changedNodeIDs;
            List<string> removedNodes = NodeChangesBuffer.GetSingleton().removedNodeIDs;

            // Lists have been cleared, use cache of lists to load the data instead
            // used when graph is closed and re-opened while in same revision
            if (newNodes.Count <= 0 && changedNodes.Count <= 0 && removedNodes.Count <= 0)
            {
                this.newNodeIDs = NodeChangesBuffer.GetSingleton().addedNodeIDsCache;
                this.changedNodeIDs = NodeChangesBuffer.GetSingleton().changedNodeIDsCache;
                this.removedNodeIDs = NodeChangesBuffer.GetSingleton().removedNodeIDsCache;
            }
            // Read the lists data and copy it to the cache and store it locally
            // used when a new revision is loaded
            else
            {
                NodeChangesBuffer.GetSingleton().addedNodeIDsCache.Clear();
                NodeChangesBuffer.GetSingleton().changedNodeIDsCache.Clear();
                NodeChangesBuffer.GetSingleton().removedNodeIDsCache.Clear();

                foreach (string s in newNodes)
                {
                    this.newNodeIDs.Add(s);
                    NodeChangesBuffer.GetSingleton().addedNodeIDsCache.Add(s);
                }
                foreach (string s in changedNodes)
                {
                    this.changedNodeIDs.Add(s);
                    NodeChangesBuffer.GetSingleton().changedNodeIDsCache.Add(s);
                }
                foreach (string s in removedNodes)
                {
                    this.removedNodeIDs.Add(s);
                    NodeChangesBuffer.GetSingleton().removedNodeIDsCache.Add(s);
                }
                // Clear previous lists, in preparation for future changes
                NodeChangesBuffer.GetSingleton().addedNodeIDs.Clear();
                NodeChangesBuffer.GetSingleton().changedNodeIDs.Clear();
                NodeChangesBuffer.GetSingleton().removedNodeIDs.Clear();
            }
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
            
            if (this.newNodeIDs.Contains(textMesh.text))
            {
                textMesh.color = addedNodesLabelColor;
                colors.normalColor = addedNodesLabelColor;
                colors.selectedColor = addedNodesLabelColor;
                colors.pressedColor = addedNodesLabelColor;
                colors.disabledColor = addedNodesLabelColor;
            }
            else if (this.changedNodeIDs.Contains(textMesh.text))
            {
                textMesh.color = changedNodesLabelColor;
                colors.normalColor = changedNodesLabelColor;
                colors.selectedColor = changedNodesLabelColor;
                colors.pressedColor = changedNodesLabelColor;
                colors.disabledColor = changedNodesLabelColor;
            }
            else if (this.removedNodeIDs.Contains(textMesh.text))
            {
                textMesh.color = removedNodeLabelColor;
                colors.normalColor = removedNodeLabelColor;
                colors.selectedColor = removedNodeLabelColor;
                colors.pressedColor = changedNodesLabelColor;
                colors.disabledColor = changedNodesLabelColor;
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
        private static NodeChangesBuffer singleton = null;

        /// <summary>
        /// Get singleton instance
        /// </summary>
        public static NodeChangesBuffer GetSingleton()
        {
            if (singleton == null)
            {
                singleton = new NodeChangesBuffer();
            }
            return singleton;
        }

        /// <summary>
        /// Stores the IDs of nodes that have been added in the current revision
        /// </summary>
        public List<string> addedNodeIDs = new List<string>();

        /// <summary>
        /// Stores the IDs of nodes that have been changed in the current revision
        /// </summary>
        public List<string> changedNodeIDs = new List<string>();

        /// <summary>
        /// Stores the IDs of nodes that have been removed in the current revision
        /// </summary>
        public List<string> removedNodeIDs = new List<string>();

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
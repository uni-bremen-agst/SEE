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
        /// The distance to another marker to recognize it as overlapping.
        /// </summary>
        private const float MarkerOverlapDistance = 22;

        /// <summary>
        /// Contains one <see cref="scrollEntryPrefab" /> for each <see cref="Node" /> in the scene.
        /// </summary>
        [SerializeField] private GameObject scrollContent;

        /// <summary>
        /// A checkbox associated to a <see cref="Node" /> in the scene to activate it in the chart.
        /// A checkbox associated to a <see cref="Node" /> in the scene to activate it in the chart.
        /// </summary>
        [SerializeField] private GameObject scrollEntryPrefab;

        /// <summary>
        /// The starting coordinates of the headers in the <see cref="scrollContent" />.
        /// </summary>
        [SerializeField] private Vector2 headerOffset;

        /// <summary>
        /// The starting coordinates of the children in the <see cref="scrollContent" />.
        /// </summary>
        [SerializeField] private Vector2 childOffset;

        /// <summary>
        /// Determines if the entries in the <see cref="scrollContent" /> are displayed as tree.
        /// </summary>
        private bool _displayAsTree;

        /// <summary>
        /// The gap between entries in the <see cref="scrollContent" /> indicating a new hierarchy layer.
        /// </summary>
        private float _xGap;

        /// <summary>
        /// The gap between entries in the <see cref="scrollContent" /> to not make them overlap.
        /// </summary>
        private float _yGap;

        /// <summary>
        /// All game-node objects to be listed in the chart. 
        /// 
        /// Invariant: all game objects in _dataObjects are game objects tagged by Tags.Node
        /// and having a valid graph-node reference.
        /// </summary>
        private ICollection<NodeRef> _dataObjects;

        /// <summary>
        /// A list of all <see cref="ChartMarker" />s currently displayed in the chart.
        /// </summary>
        protected List<GameObject> ActiveMarkers = new List<GameObject>();

        public readonly Dictionary<NodeRef, ChartMarker> nodeRefToChartMarkerDict = new Dictionary<NodeRef, ChartMarker>();

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
        /// A parent of this object. Used in VR to destroy the whole construct of a moveable chart.
        /// </summary>
        public GameObject parent;

        /// <summary>
        /// The panel on which the <see cref="ChartMarker" />s are instantiated.
        /// </summary>
        [Header("For resizing and minimizing")]
        public RectTransform dataPanel;

        /// <summary>
        /// The panel on which the buttons and scales of the chart are displayed.
        /// </summary>
        public RectTransform labelsPanel;

        /// <summary>
        /// Contains all metric names contained in any <see cref="GameObject" /> of <see cref="_dataObjects" />.
        /// </summary>
        public readonly SortedSet<string> AllMetricNames = new SortedSet<string>();

        /// <summary>
        /// The number of game objects representing a graph node in the current scene.
        /// A game object representing a graph node is one that is tagged by Tags.Node
        /// having a valid NodeRef to a graph node. Note that this number is across
        /// all current graphs represented in the scene, and not just one particular
        /// graph.
        /// </summary>
        public int TotalNumberOfGraphNodesInTheScene => _dataObjects.Count;

        /// <summary>
        /// Calls methods to initialize a chart.
        /// </summary>
        private void Awake()
        {
            _xGap = childOffset.x - headerOffset.x;
            _yGap = childOffset.y - headerOffset.y;
            FindDataObjects();
            GetAllNumericAttributes();
        }

        // This entry of the dropdown box represents not a metric but just the enumeration of nodes.
        // This entry can be selected if one wants to have a metric on one axis and then all nodes 
        // sorted by this metric on the other axis.
        private const string NodeEnumeration = "NODES";
        
        protected virtual void Start()
        {
            axisDropdownX.Initialize();
            axisDropdownY.Initialize();
            axisDropdownX.AddNodeEnumerationEntry(NodeEnumeration);
            DrawData();
        }

        private void FillScrollView(bool tree)
        {
            foreach (Transform child in scrollContent.transform)
            {
                Destroy(child.gameObject);
            }

            if (!tree)
            {
                FillScrollViewAsList();
            }
            else
            {
                FillScrollViewAsTree();
            }
        }

        /// <summary>
        /// Fills the scroll view on the right of the chart with one entry for each node in the scene including
        /// two headers to toggle all buildings and all nodes.
        /// </summary>
        private void FillScrollViewAsList()
        {
            Performance p = Performance.Begin("FillScrollViewAsList");

            GameObject scrollEntry = Instantiate(scrollEntryPrefab, scrollContent.transform);
            scrollEntry.name = "ScrollViewToggle: Leaves";
            scrollEntry.transform.localPosition = headerOffset;
            ScrollViewToggle parentToggle = scrollEntry.GetComponent<ScrollViewToggle>();
            parentToggle.Initialize("Leaves", this);

            int index = 0;
            foreach (NodeRef dataObject in _dataObjects)
            {
                if (dataObject.node.IsLeaf())
                {
                    CreateChildToggle(dataObject, parentToggle, index++, _yGap);
                }
            }

            scrollEntry = Instantiate(scrollEntryPrefab, scrollContent.transform);
            scrollEntry.name = "ScrollViewToggle: Inner Nodes";
            scrollEntry.transform.localPosition = headerOffset + new Vector2(0, _yGap) * ++index;
            parentToggle = scrollEntry.GetComponent<ScrollViewToggle>();
            parentToggle.Initialize("Inner Nodes", this);

            foreach (NodeRef dataObject in _dataObjects)
            {
                if (dataObject.node.IsInnerNode())
                {
                    CreateChildToggle(dataObject, parentToggle, index++, _yGap);
                }
            }

            RectTransform rect = scrollContent.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, index * Mathf.Abs(_yGap) + 40);

            p.End(true);
        }

        private void FillScrollViewAsTree()
        {
            Performance p = Performance.Begin("FillScrollViewAsTree");

            int index = 0;
            int hierarchy = 0;
            int maxHierarchy = 0;

            foreach (Node root in SceneQueries.GetRoots(_dataObjects))
            {
                NodeRef rootNodeRef = _dataObjects.First(entry =>
                {
                    return entry.node.ID.Equals(root.ID);
                });
                GameObject scrollViewToggleGameObject = Instantiate(scrollEntryPrefab, scrollContent.transform);
                scrollViewToggleGameObject.name = "ScrollViewToggle: " + rootNodeRef.node.SourceName;
                ScrollViewToggle rootToggle = scrollViewToggleGameObject.GetComponent<ScrollViewToggle>();
                rootToggle.LinkedObject = rootNodeRef.highlights;
                rootNodeRef.highlights.scrollViewToggle = rootToggle;
                scrollViewToggleGameObject.transform.localPosition = headerOffset + new Vector2(0f, _yGap) * index;
                rootToggle.Initialize(root.SourceName, this);
                if (hierarchy > maxHierarchy)
                {
                    maxHierarchy = hierarchy;
                }

                hierarchy = 0;
                CreateChildToggles(root, rootToggle, ref index, ref hierarchy);
            }

            if (hierarchy > maxHierarchy)
            {
                maxHierarchy = hierarchy; //TODO: Use this...
            }

            RectTransform rect = scrollContent.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, index * Mathf.Abs(_yGap) + 40);

            p.End(true);
        }

        /// <summary>
        /// Creates a toggle for an object in the scene that is a node.
        /// </summary>
        /// <param name="nodeRef">The object to be toggled.</param>
        /// <param name="parentToggle">The toggle that will toggle this one when clicked.</param>
        /// <param name="index">The position of the toggle in the scrollview.</param>
        /// <param name="gap">The gap between two toggles in the scrollview.</param>
        private void CreateChildToggle(NodeRef nodeRef, ScrollViewToggle parentToggle, int index, float gap)
        {
            GameObject tempObject = Instantiate(scrollEntryPrefab, scrollContent.transform);
            tempObject.name = "ScrollViewToggle: " + nodeRef.node.SourceName;
            ScrollViewToggle toggle = tempObject.GetComponent<ScrollViewToggle>();
            toggle.Parent = parentToggle;
            toggle.LinkedObject = nodeRef.highlights;
            nodeRef.highlights.scrollViewToggle = toggle;
            tempObject.transform.localPosition = childOffset + new Vector2(0f, gap) * index;
            toggle.Initialize(nodeRef.name, this);
            parentToggle.AddChild(toggle);
        }

        private void CreateChildToggles(Node root, ScrollViewToggle parentToggle, ref int index, ref int hierarchy)
        {
            if (root.IsInnerNode())
            {
                hierarchy++;
                foreach (Node childNode in root.Children())
                {
                    NodeRef childNodeRef = _dataObjects.First(entry =>
                    {
                        return entry.node.ID.Equals(childNode.ID);
                    });
                    GameObject scrollViewToggleGameObject = Instantiate(scrollEntryPrefab, scrollContent.transform);
                    scrollViewToggleGameObject.name = "ScrollViewToggle: " + childNodeRef.node.SourceName;
                    ScrollViewToggle scrollViewToggle = scrollViewToggleGameObject.GetComponent<ScrollViewToggle>();
                    scrollViewToggle.Parent = parentToggle;
                    scrollViewToggle.LinkedObject = childNodeRef.highlights;
                    childNodeRef.highlights.scrollViewToggle = scrollViewToggle;
                    scrollViewToggleGameObject.transform.localPosition = childOffset + new Vector2(_xGap, 0f) * hierarchy + new Vector2(0f, _yGap) * index++;
                    scrollViewToggle.Initialize(childNode.SourceName, this);
                    parentToggle.AddChild(scrollViewToggle);
                    int tempHierarchy = hierarchy;
                    CreateChildToggles(childNode, scrollViewToggle, ref index, ref tempHierarchy);
                }
            }
        }

        /// <summary>
        /// Gets all metric names for <see cref="float" /> values contained in the <see cref="NodeRef" /> of each
        /// <see cref="GameObject" /> in <see cref="_dataObjects" />. A metric name is the name of a
        /// numeric (either float or int) node attribute that starts with the prefix ChartManager.MetricPrefix.
        /// </summary>
        private void GetAllNumericAttributes()
        {
            AllMetricNames.Clear();
#if UNITY_EDITOR
            if (_dataObjects.Count == 0)
            {
                Debug.LogWarning("There are no nodes for showing metrics.\n");
            }
#endif
            foreach (NodeRef nodeRef in _dataObjects)
            {
                foreach (string key in nodeRef.node.FloatAttributes.Keys)
                {
                    if (key.StartsWith(ChartManager.MetricPrefix))
                    {
                        AllMetricNames.Add(key);
                    }
                }
                foreach (string key in nodeRef.node.IntAttributes.Keys)
                {
                    if (key.StartsWith(ChartManager.MetricPrefix))
                    {
                        AllMetricNames.Add(key);
                    }
                }
            }
#if UNITY_EDITOR
            if (AllMetricNames.Count == 0)
            {
                Debug.LogWarning("No metrics available for charts.\n");
            }
#endif
        }

        /// <summary>
        /// Fills a List with all <see cref="Node" />s that will be in the chart.
        /// </summary>
        private void FindDataObjects()
        {
            _dataObjects = SceneQueries.AllNodeRefsInScene(ChartManager.Instance.ShowLeafMetrics, ChartManager.Instance.ShowInnerNodeMetrics);

            int numberOfDataObjectsWithNodeHightLights = 0;
            foreach (NodeRef entry in _dataObjects)
            {
                if (entry.highlights)
                {
                    entry.highlights.showInChart[this] = true;
                    numberOfDataObjectsWithNodeHightLights++;
                }
            }
            Debug.LogFormat("numberOfDataObjectsWithNodeHightLights: {0}\n", numberOfDataObjectsWithNodeHightLights);

            FillScrollView(_displayAsTree);
        }

        /// <summary>
        /// Fills the chart with data depending on the values of <see cref="axisDropdownX"/> and
        /// <see cref="axisDropdownY"/>.
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
            foreach (NodeRef nodeRef in _dataObjects)
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
                if (inX && inY && (bool)nodeRef.highlights.showInChart[this])
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
                foreach (GameObject activeMarker in ActiveMarkers)
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
            foreach (GameObject marker in ActiveMarkers)
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
                    marker.GetComponent<RectTransform>().anchoredPosition = anchoredPosition;
                    marker.GetComponent<SortingGroup>().sortingOrder = positionInLayer++;
                    chartMarker = marker.GetComponent<ChartMarker>();
                    chartMarker.chartContent = this;
                    updatedMarkers.Add(marker);
                    anchoredPositionToChartMarkerDict.Add(anchoredPosition, chartMarker);
                }

                string infoText = nodeRef.node.SourceName + " (" + valueX.ToString("0.00") + ", " + valueY.ToString("0.00") + ")";
                chartMarker.PushInteractableObject(nodeRef.GetComponent<InteractableObject>(), infoText);
                nodeRefToChartMarkerDict.Add(nodeRef, chartMarker);
            }

            ActiveMarkers = updatedMarkers;
        }

        public void AreaHover(Vector2 min, Vector2 max)
        {
            bool toggleHover = Input.GetKey(KeyCode.LeftControl);
            foreach (GameObject marker in ActiveMarkers)
            {
                IEnumerable<InteractableObject> interactableObjects = marker.GetComponent<ChartMarker>().LinkedInteractableObjects;
                foreach (InteractableObject interactableObject in interactableObjects)
                {
                    Vector2 markerPos = marker.transform.position;
                    if (markerPos.x > min.x && markerPos.x < max.x && markerPos.y > min.y && markerPos.y < max.y)
                    {
                        if (!interactableObject.IsHovered)
                        {
                            interactableObject.SetHoverFlag(HoverFlag.ChartMultiSelect, true, true);
                        }
                    }
                    else if (!toggleHover && interactableObject.IsHovered)
                    {
                        interactableObject.SetHoverFlag(HoverFlag.ChartMultiSelect, false, true);
                    }
                }
            }
        }

        public virtual void AreaSelection(Vector2 min, Vector2 max)
        {
            bool toggleSelect = Input.GetKey(KeyCode.LeftControl);
            foreach (GameObject marker in ActiveMarkers)
            {
                IEnumerable<InteractableObject> interactableObjects = marker.GetComponent<ChartMarker>().LinkedInteractableObjects;
                foreach (InteractableObject interactableObject in interactableObjects)
                {
                    Vector2 markerPos = marker.transform.position;
                    if (markerPos.x > min.x && markerPos.x < max.x && markerPos.y > min.y && markerPos.y < max.y)
                    {
                        if (!interactableObject.IsSelected)
                        {
                            interactableObject.SetSelect(true, true);
                        }
                    }
                    else if (!toggleSelect && interactableObject.IsSelected)
                    {
                        interactableObject.SetSelect(false, true);
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
                moveHandler.SetInfoText("X-Axis: " + axisDropdownX.CurrentlySelectedMetric + "\n" + "Y-Axis: " +
                                        axisDropdownY.CurrentlySelectedMetric);
            }
        }

        /// <summary>
        /// Sets if the scroll view will display the original tree of the file structure
        /// or the more convenient grouping into buildings and nodes.
        /// </summary>
        /// <param name="displayAsTree">Whether the content should be displayed as a tree
        /// or a list.</param>
        public void SetDisplayAsTree(bool displayAsTree)
        {
            _displayAsTree = displayAsTree;
            FillScrollView(_displayAsTree);
        }

        public void UnhoverAll()
        {
            foreach (GameObject activeMarker in ActiveMarkers)
            {
                if (activeMarker.TryGetComponent(out InteractableObject interactableObject))
                {
                    interactableObject.SetHoverFlags(0, true);
                }
            }
        }

        public void UnselectAll()
        {
            foreach (GameObject activeMarker in ActiveMarkers)
            {
                if (activeMarker.TryGetComponent(out InteractableObject interactableObject))
                {
                    interactableObject.SetSelect(false, true);
                }
            }
        }

        /// <summary>
        /// Destroys the chart including its container. Called when the user clicks on
        /// the closing button.
        /// </summary>
        public void Destroy()
        {
            Destroy(parent);
        }

        /// <summary>
        /// Removes this chart from all <see cref="NodeHighlights.showInChart" /> dictionaries.
        /// </summary>
        public void OnDestroy()
        {
            ChartManager.Instance.UnregisterChart(gameObject);
            foreach (NodeRef dataObject in _dataObjects)
            {
                dataObject.highlights.showInChart.Remove(this);
            }
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SEE.DataModel;
using SEE.Layout;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace SEECity.Charts.Scripts
{
	/// <summary>
	/// Fills Charts with data and manages that data.
	/// </summary>
	public class ChartContent : MonoBehaviour
	{
		/// <summary>
		/// Contains some settings used in this script.
		/// </summary>
		protected ChartManager chartManager;

		private float _markerOverlapDistance = 22;

		[SerializeField] private GameObject scrollContent;

		[SerializeField] private GameObject scrollEntryPrefab;

		[SerializeField] private Vector2 headerOffset;

		[SerializeField] private Vector2 childOffset;

		[HideInInspector] public Coroutine drawing;

		/// <summary>
		/// All objects to be listed in the chart.
		/// </summary>
		private GameObject[] _dataObjects;

		[HideInInspector] public int citySize;

		/// <summary>
		/// A list of all <see cref="ChartMarker" />s currently displayed in the chart.
		/// </summary>
		protected List<GameObject> activeMarkers = new List<GameObject>();

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
		/// Game Object to group all content entries of a chart.
		/// </summary>
		[SerializeField] private GameObject entries;

		[SerializeField] private GameObject noDataWarning;

		[SerializeField] private TextMeshProUGUI minXText;

		[SerializeField] private TextMeshProUGUI maxXText;

		[SerializeField] private TextMeshProUGUI minYText;

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
		/// Contains all keys contained in any <see cref="GameObject" /> of <see cref="_dataObjects" />.
		/// </summary>
		public List<string> AllKeys { get; } = new List<string>();

		/// <summary>
		/// Calls methods to initialize a chart.
		/// </summary>
		private void Awake()
		{
			chartManager = GameObject.FindGameObjectWithTag("ChartManager")
				.GetComponent<ChartManager>();
			FindDataObjects();
			GetAllFloats();
			GetAllIntegers();
		}

		/// <summary>
		/// Fills the chart for the first time.
		/// </summary>
		protected virtual void Start()
		{
			var time = citySize > 50 ? 5f : 0.2f;
			Invoke(nameof(CallDrawData), time);
		}

		/// <summary>
		/// Fills the scroll view on the right of the chart with one entry for each node in the scene including
		/// two headers to toggle all buildings and all nodes.
		/// </summary>
		private void FillScrollView()
		{
			var gap = childOffset.y - headerOffset.y;
			foreach (Transform child in scrollContent.transform) Destroy(child.gameObject);

			GameObject tempObject = Instantiate(scrollEntryPrefab, scrollContent.transform);
			ScrollViewToggle parentToggle = tempObject.GetComponent<ScrollViewToggle>();
			parentToggle.SetLabel("Buildings");
			tempObject.transform.localPosition = headerOffset;
			parentToggle.Initialize(this);

			var i = 0;
			foreach (GameObject dataObject in _dataObjects)
				if (dataObject.tag.Equals("Building"))
					CreateChildToggle(dataObject, parentToggle, i++, gap);

			tempObject = Instantiate(scrollEntryPrefab, scrollContent.transform);
			parentToggle = tempObject.GetComponent<ScrollViewToggle>();
			parentToggle.SetLabel("Nodes");
			tempObject.transform.localPosition = headerOffset + new Vector2(0, gap) * ++i;
			parentToggle.Initialize(this);

			foreach (GameObject dataObject in _dataObjects)
				if (dataObject.tag.Equals("Node"))
					CreateChildToggle(dataObject, parentToggle, i++, gap);
			scrollContent.GetComponent<RectTransform>().sizeDelta = new Vector2(
				scrollContent.GetComponent<RectTransform>().sizeDelta.x, i * Mathf.Abs(gap) + 40);
		}

		/// <summary>
		/// Creates a toggle for an object in the scene that is a node.
		/// </summary>
		/// <param name="dataObject">The object to be toggled.</param>
		/// <param name="parentToggle">The toggle that will toggle this one when clicked.</param>
		/// <param name="i">The position of the toggle in the scrollview.</param>
		/// <param name="gap">The gap between two toggles in the scrollview.</param>
		private void CreateChildToggle(GameObject dataObject, ScrollViewToggle parentToggle, int i,
			float gap)
		{
			GameObject tempObject = Instantiate(scrollEntryPrefab, scrollContent.transform);
			ScrollViewToggle toggle = tempObject.GetComponent<ScrollViewToggle>();
			toggle.Parent = parentToggle;
			NodeHighlights highlights = dataObject.GetComponent<NodeHighlights>();
			toggle.LinkedObject = highlights;
			highlights.scrollViewToggle = toggle;
			toggle.SetLabel(dataObject.name);
			tempObject.transform.localPosition = childOffset + new Vector2(0f, gap) * i;
			toggle.Initialize(this);
			parentToggle.AddChild(toggle);
		}

		/// <summary>
		/// Gets all keys for <see cref="float" /> values contained in the <see cref="NodeRef" /> of each
		/// <see cref="GameObject" /> in <see cref="_dataObjects" />.
		/// </summary>
		private void GetAllFloats()
		{
			foreach (GameObject data in _dataObjects)
			foreach (var key in data.GetComponent<NodeRef>().node.FloatAttributes.Keys)
				if (!AllKeys.Contains(key))
					AllKeys.Add(key);
		}

		/// <summary>
		/// Gets all keys for <see cref="int" /> values contained in the <see cref="NodeRef" /> of each
		/// <see cref="GameObject" /> in <see cref="_dataObjects" />.
		/// </summary>
		private void GetAllIntegers()
		{
			foreach (GameObject data in _dataObjects)
			foreach (var key in data.GetComponent<NodeRef>().node.IntAttributes.Keys)
				if (!AllKeys.Contains(key))
					AllKeys.Add(key);
		}

		/// <summary>
		/// Fills a List with all objects that will be in the chart.
		/// </summary>
		private void FindDataObjects()
		{
			GameObject[] buildings = GameObject.FindGameObjectsWithTag("Building");
			GameObject[] nodes = GameObject.FindGameObjectsWithTag("Node");
			GameObject[] combined = new GameObject[buildings.Length + nodes.Length];
			Array.Copy(buildings, combined, buildings.Length);
			Array.Copy(nodes, 0, combined, buildings.Length, nodes.Length);
			_dataObjects = combined;

			foreach (GameObject entry in combined)
				if (!entry.GetComponent<NodeHighlights>().showInChart.Contains(this))
					entry.GetComponent<NodeHighlights>().showInChart.Add(this, true);
			citySize = _dataObjects.Length;

			FillScrollView();
		}

		/// <summary>
		/// Since <see cref="MonoBehaviour.Invoke" /> does not support calls with parameter, it calls this
		/// method to do the work.
		/// </summary>
		private void CallDrawData()
		{
			DrawData(true);
		}

		/// <summary>
		/// Starts the Draw after a set time to handle calls in quick succession and improve performance.
		/// </summary>
		/// <returns></returns>
		public IEnumerator QueueDraw()
		{
			if (citySize > 50)
				yield return new WaitForSeconds(2f);
			else
				yield return new WaitForSeconds(0.5f);

			DrawData(false);
			drawing = null;
		}

		/// <summary>
		/// Fills the chart with data depending on the values of <see cref="axisDropdownX" /> and
		/// <see cref="axisDropdownY" />.
		/// </summary>
		public void DrawData(bool needData)
		{
			if (needData) FindDataObjects();
			noDataWarning.SetActive(false);

			if (axisDropdownX.Value.Equals(axisDropdownY.Value))
				DrawOneAxis();
			else
				DrawTwoAxes();

			if (activeMarkers.Count == 0) noDataWarning.SetActive(true);
		}

		/// <summary>
		/// Adds a marker for every <see cref="Node" /> containing the metrics from both axes. It's position
		/// depends on the values of those metrics.
		/// </summary>
		private void DrawTwoAxes()
		{
			var i = 0;
			Node node = _dataObjects[i].GetComponent<NodeRef>().node;
			var contained = node.TryGetNumeric(axisDropdownX.Value, out var minX);
			while (!contained)
			{
				i++;
				node = _dataObjects[i].GetComponent<NodeRef>().node;
				contained = node.TryGetNumeric(axisDropdownX.Value, out minX);
			}

			var maxX = minX;
			i = 0;
			node = _dataObjects[i].GetComponent<NodeRef>().node;
			contained = node.TryGetNumeric(axisDropdownY.Value, out var minY);
			while (!contained)
			{
				i++;
				node = _dataObjects[i].GetComponent<NodeRef>().node;
				contained = node.TryGetNumeric(axisDropdownY.Value, out minY);
			}

			float maxY = minY;
			List<GameObject> toDraw = new List<GameObject>();
			foreach (GameObject data in _dataObjects)
			{
				node = data.GetComponent<NodeRef>().node;
				bool inX = false;
				bool inY = false;
				if (node.TryGetNumeric(axisDropdownX.Value, out var tempX))
				{
					if (tempX < minX) minX = tempX;
					if (tempX > maxX) maxX = tempX;
					inX = true;
				}

				if (node.TryGetNumeric(axisDropdownY.Value, out var tempY))
				{
					if (tempY > maxY) maxY = tempY;
					if (tempY < minY) minY = tempY;
					inY = true;
				}

				if (inX && inY && (bool) data.GetComponent<NodeHighlights>().showInChart[this])
					toDraw.Add(data);
			}

			if (toDraw.Count > 0)
			{
				bool xEqual = minX.Equals(maxX);
				bool yEqual = minY.Equals(maxY);
				if (xEqual || yEqual)
				{
					(float min, float max) = minX.Equals(maxX) ? (minY, maxY) : (minX, maxX);
					AddMarkers(toDraw, min, max);
					minXText.text = xEqual ? "0" : minX.ToString("0.00");
					maxXText.text = xEqual ? toDraw.Count.ToString() : maxX.ToString("0.00");
					minYText.text = yEqual ? "0" : minY.ToString("0.00");
					maxYText.text = yEqual ? toDraw.Count.ToString() : maxY.ToString("0.00");
				}
				else
				{
					AddMarkers(toDraw, minX, maxX, minY, maxY);
					minXText.text = minX.ToString("0.00");
					maxXText.text = maxX.ToString("0.00");
					minYText.text = minY.ToString("0.00");
					maxYText.text = maxY.ToString("0.00");
				}
			}
			else
			{
				foreach (GameObject activeMarker in activeMarkers) Destroy(activeMarker);
				noDataWarning.SetActive(true);
			}
		}

		/// <summary>
		/// Adds a marker for every <see cref="Node" /> containing the metric that is the same for both axes.
		/// Therefore markers will be ordered by that one value and the distance between markers on the x-Axis
		/// will be consistent.
		/// </summary>
		private void DrawOneAxis()
		{
			List<GameObject> toDraw = new List<GameObject>();
			string metric = axisDropdownY.Value;

			foreach (GameObject dataObject in _dataObjects)
				if (dataObject.GetComponent<NodeRef>().node.TryGetNumeric(metric, out float _) &&
				    (bool) dataObject.GetComponent<NodeHighlights>().showInChart[this])
					toDraw.Add(dataObject);

			if (toDraw.Count > 0)
			{
				toDraw.Sort(delegate(GameObject go1, GameObject go2)
				{
					go1.GetComponent<NodeRef>().node.TryGetNumeric(metric, out float value1);
					go2.GetComponent<NodeRef>().node.TryGetNumeric(metric, out float value2);
					return value1.CompareTo(value2);
				});

				toDraw.First().GetComponent<NodeRef>().node.TryGetNumeric(metric, out float min);
				toDraw.Last().GetComponent<NodeRef>().node.TryGetNumeric(metric, out float max);

				AddMarkers(toDraw, min, max);

				minXText.text = "0";
				maxXText.text = toDraw.Count.ToString();
				minYText.text = min.ToString("0.00");
				maxYText.text = max.ToString("0.00");
			}
			else
			{
				foreach (GameObject activeMarker in activeMarkers) Destroy(activeMarker);
				noDataWarning.SetActive(true);
			}
		}

		/// <summary>
		/// Adds new markers to the chart and removes the old ones.
		/// </summary>
		/// <param name="toDraw">The markers to add to the chart.</param>
		/// <param name="minX">The minimum value on the x-axis.</param>
		/// <param name="maxX">The maximum value on the x-axis.</param>
		/// <param name="minY">The minimum value on the y-axis.</param>
		/// <param name="maxY">The maximum value on the y-axis.</param>
		private void AddMarkers(List<GameObject> toDraw, float minX, float maxX, float minY,
			float maxY)
		{
			List<GameObject> updatedMarkers = new List<GameObject>();
			Rect dataRect = dataPanel.rect;
			float width = dataRect.width / (maxX - minX);
			float height = dataRect.height / (maxY - minY);
			int positionInLayer = 0;

			foreach (GameObject data in toDraw)
			{
				GameObject marker = Instantiate(markerPrefab, entries.transform);
				marker.GetComponent<SortingGroup>().sortingOrder = positionInLayer++;
				ChartMarker script = marker.GetComponent<ChartMarker>();
				script.linkedObject = data;
				script.scrollViewToggle = data.GetComponent<NodeHighlights>().scrollViewToggle;
				Node node = data.GetComponent<NodeRef>().node;
				node.TryGetNumeric(axisDropdownX.Value, out float valueX);
				node.TryGetNumeric(axisDropdownY.Value, out float valueY);
				string type = node.IsLeaf() ? "Building" : "Node";
				script.SetInfoText("Linked to: " + data.name + " of type " + type + "\nX: " +
				                   valueX.ToString("0.00") + ", Y: " + valueY.ToString("0.00"));
				marker.GetComponent<RectTransform>().anchoredPosition =
					new Vector2((valueX - minX) * width, (valueY - minY) * height);
				CheckOverlapping(marker, updatedMarkers.ToArray());
				updatedMarkers.Add(marker);

				float highlightTimeLeft = CheckOldMarkers(data);
				if (highlightTimeLeft > 0f)
					script.TriggerTimedHighlight(chartManager.highlightDuration -
					                             highlightTimeLeft, true);
			}

			foreach (GameObject marker in activeMarkers) Destroy(marker);
			activeMarkers = updatedMarkers;
		}

		/// <summary>
		/// Adds new markers to the chart if the same metric is displayed on both axes.
		/// </summary>
		/// <param name="toDraw">The markers to add to the chart.</param>
		/// <param name="min">The minimum value of the metric.</param>
		/// <param name="max">The maximum value of the metric.</param>
		private void AddMarkers(List<GameObject> toDraw, float min, float max)
		{
			if (min.Equals(max))
			{
				AddMarkers(toDraw);
			}
			else
			{
				List<GameObject> updatedMarkers = new List<GameObject>();
				Rect dataRect = dataPanel.rect;
				float width = dataRect.width / (toDraw.Count - 1);
				float height = dataRect.height / (max - min);
				string metric = axisDropdownY.Value;
				int x = 0;
				int positionInLayer = 0;

				foreach (GameObject data in toDraw)
				{
					GameObject marker = Instantiate(markerPrefab, entries.transform);
					marker.GetComponent<SortingGroup>().sortingOrder = positionInLayer++;
					ChartMarker script = marker.GetComponent<ChartMarker>();
					script.linkedObject = data;
					script.scrollViewToggle = data.GetComponent<NodeHighlights>().scrollViewToggle;
					Node node = data.GetComponent<NodeRef>().node;
					node.TryGetNumeric(metric, out float value);
					string type = node.IsLeaf() ? "Building" : "Node";
					script.SetInfoText("Linked to: " + data.name + " of type " + type + "\n" +
					                   metric +
					                   ": " + value.ToString("0.00"));
					marker.GetComponent<RectTransform>().anchoredPosition =
						new Vector2(x++ * width, (value - min) * height);
					CheckOverlapping(marker, updatedMarkers.ToArray());
					updatedMarkers.Add(marker);

					if (activeMarkers.Count > 0)
					{
						float highlightTimeLeft = CheckOldMarkers(data);
						if (highlightTimeLeft > 0f)
							script.TriggerTimedHighlight(
								chartManager.highlightDuration - highlightTimeLeft, true);
					}
				}

				foreach (GameObject marker in activeMarkers) Destroy(marker);
				activeMarkers = updatedMarkers;
			}
		}

		/// <summary>
		/// Adds markers to the chart where all markers have the same value.
		/// </summary>
		/// <param name="toDraw">The markers to add to the chart.</param>
		private void AddMarkers(List<GameObject> toDraw)
		{
			List<GameObject> updatedMarkers = new List<GameObject>();
			Rect dataRect = dataPanel.rect;
			float width = dataRect.width / toDraw.Count;
			float height = dataRect.height / toDraw.Count;
			int x = 0;
			int y = 0;
			int positionInLayer = 0;

			foreach (GameObject data in toDraw)
			{
				GameObject marker = Instantiate(markerPrefab, entries.transform);
				marker.GetComponent<SortingGroup>().sortingOrder = positionInLayer++;
				ChartMarker script = marker.GetComponent<ChartMarker>();
				script.linkedObject = data;
				script.scrollViewToggle = data.GetComponent<NodeHighlights>().scrollViewToggle;
				Node node = data.GetComponent<NodeRef>().node;
				node.TryGetNumeric(axisDropdownX.Value, out float valueX);
				node.TryGetNumeric(axisDropdownY.Value, out float valueY);
				string type = node.IsLeaf() ? "Building" : "Node";
				script.SetInfoText("Linked to: " + data.name + " of type " + type + "\nX: " +
				                   valueX.ToString("0.00") + ", Y: " + valueY.ToString("0.00"));
				marker.GetComponent<RectTransform>().anchoredPosition =
					new Vector2(x++ * width, y++ * height);
				CheckOverlapping(marker, updatedMarkers.ToArray());
				updatedMarkers.Add(marker);

				if (activeMarkers.Count > 0)
				{
					float highlightTimeLeft = CheckOldMarkers(data);
					if (highlightTimeLeft > 0f)
						script.TriggerTimedHighlight(
							chartManager.highlightDuration - highlightTimeLeft, true);
				}
			}

			foreach (GameObject marker in activeMarkers) Destroy(marker);
			activeMarkers = updatedMarkers;
		}

		/// <summary>
		/// Checks if a marker is overlapping with any of the already existing new markers and changes its
		/// color for each overlapping marker.
		/// </summary>
		/// <param name="marker">The marker to check.</param>
		/// <param name="updatedMarkers">The already active new markers.</param>
		private void CheckOverlapping(GameObject marker, GameObject[] updatedMarkers)
		{
			Image image = marker.GetComponent<Image>();
			if (updatedMarkers.Length > 10)
				for (int i = updatedMarkers.Length - 10; i < updatedMarkers.Length; i++)
				{
					GameObject updatedMarker = updatedMarkers[i];
					if (Vector3.Distance(marker.transform.position,
							updatedMarker.transform.position)
						.CompareTo(_markerOverlapDistance * marker.transform.lossyScale.x) < 0)
						if (image.color.g - 0.1f >= 0)
						{
							Color oldColor = image.color;
							image.color = new Color(oldColor.r, oldColor.g - 0.1f,
								oldColor.b - 0.1f);
						}
				}
			else
				foreach (GameObject updatedMarker in updatedMarkers)
					if (Vector3.Distance(marker.transform.position,
							updatedMarker.transform.position)
						.CompareTo(_markerOverlapDistance * marker.transform.lossyScale.x) < 0)
						if (image.color.g - 0.1f >= 0)
						{
							Color oldColor = image.color;
							image.color = new Color(oldColor.r, oldColor.g - 0.1f,
								oldColor.b - 0.1f);
						}
		}

		/// <summary>
		/// Checks if any of the old markers that will be removed were highlighted. If so, the highlight will
		/// be carried over to the new marker.
		/// </summary>
		/// <param name="marker">The new marker.</param>
		/// <returns></returns>
		private float CheckOldMarkers(GameObject marker)
		{
			loop:
			foreach (GameObject oldMarker in activeMarkers)
				if (oldMarker.Equals(null))
				{
					Destroy(oldMarker);
					activeMarkers.Remove(oldMarker);
					goto loop;
				}
				else if (oldMarker.TryGetComponent(out ChartMarker oldScript) &&
				         oldScript.linkedObject.GetInstanceID() == marker.GetInstanceID() &&
				         oldScript.TimedHighlight != null)
				{
					activeMarkers.Remove(oldMarker);
					Destroy(oldMarker);
					return oldScript.HighlightTime;
				}

			return 0f;
		}

		/// <summary>
		/// Calls <see cref="ChartMarker.TriggerTimedHighlight" /> for all Markers in a rectangle in the chart.
		/// </summary>
		/// <param name="min">The starting edge of the rectangle.</param>
		/// <param name="max">The ending edge of the rectangle.</param>
		/// <param name="direction">True if min lies below max, false if not.</param>
		public virtual void AreaSelection(Vector2 min, Vector2 max, bool direction)
		{
			if (direction)
				foreach (GameObject marker in activeMarkers)
				{
					Vector2 markerPos = marker.transform.position;
					if (markerPos.x > min.x && markerPos.x < max.x && markerPos.y > min.y &&
					    markerPos.y < max.y)
						chartManager.HighlightObject(
							marker.GetComponent<ChartMarker>().linkedObject);
				}
			else
				foreach (GameObject marker in activeMarkers)
				{
					Vector2 markerPos = marker.transform.position;
					if (markerPos.x > min.x && markerPos.x < max.x && markerPos.y < min.y &&
					    markerPos.y > max.y)
						chartManager.HighlightObject(
							marker.GetComponent<ChartMarker>().linkedObject);
				}
		}

		/// <summary>
		/// Sets the info text of the chart.
		/// </summary>
		public void SetInfoText()
		{
			string metricX = axisDropdownX.Value;
			string metricY = axisDropdownY.Value;
			if (metricX.Equals(metricY))
				moveHandler.SetInfoText(metricX);
			else
				moveHandler.SetInfoText("X-Axis: " + axisDropdownX.Value + "\n" + "Y-Axis: " +
				                        axisDropdownY.Value);
		}

		/// <summary>
		/// Finds all markers that refer to a given <see cref="GameObject" /> and toggles their highlight
		/// across all charts.
		/// </summary>
		/// <param name="highlight">The object the marker will refer to.</param>
		public void HighlightCorrespondingMarker(GameObject highlight)
		{
			foreach (GameObject activeMarker in activeMarkers)
				if (!activeMarker.Equals(null))
				{
					ChartMarker script = activeMarker.GetComponent<ChartMarker>();
					if (script.linkedObject.Equals(highlight))
					{
						script.TriggerTimedHighlight(chartManager.highlightDuration, false);
						break;
					}
				}
		}

		/// <summary>
		/// Finds all markers that refer to a given <see cref="GameObject" /> and if they are highlighted,
		/// their accentuation will be toggled.
		/// </summary>
		/// <param name="highlight">The object the marker will refer to.</param>
		public void AccentuateCorrespondingMarker(GameObject highlight)
		{
			foreach (GameObject activeMarker in activeMarkers)
			{
				ChartMarker script = activeMarker.GetComponent<ChartMarker>();
				if (script.linkedObject.Equals(highlight))
				{
					script.Accentuate();
					break;
				}
			}
		}

		/// <summary>
		/// Destroys the chart including its container if VR is activated.
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
			foreach (GameObject dataObject in _dataObjects)
				if (dataObject != null)
					dataObject.GetComponent<NodeHighlights>().showInChart.Remove(this);
		}
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using SEE.DataModel;
using SEE.Layout;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

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
		protected ChartManager ChartManager;

		/// <summary>
		/// All objects to be listed in the chart.
		/// </summary>
		private GameObject[] _dataObjects;

		/// <summary>
		/// A list of all <see cref="ChartMarker" />s currently displayed in the chart.
		/// </summary>
		protected List<GameObject> ActiveMarkers = new List<GameObject>();

		[SerializeField] private ChartMoveHandler moveHandler;

		/// <summary>
		/// The <see cref="AxisContentDropdown" /> containing Values for the X-Axis.
		/// </summary>
		[FormerlySerializedAs("AxisDropdownX")]
		public AxisContentDropdown axisDropdownX;

		/// <summary>
		/// The <see cref="AxisContentDropdown" /> containing Values for the Y-Axis.
		/// </summary>
		[FormerlySerializedAs("AxisDropdownY")]
		public AxisContentDropdown axisDropdownY;

		/// <summary>
		/// The prefab used to display content in charts.
		/// </summary>
		[SerializeField] private GameObject markerPrefab;

		/// <summary>
		/// Game Object to group all content entries of a chart.
		/// </summary>
		[SerializeField] private GameObject entries;

		[FormerlySerializedAs("_noDataWarning"), SerializeField]
		private GameObject noDataWarning;

		[FormerlySerializedAs("minX"), SerializeField]
		private TextMeshProUGUI minXText;

		[FormerlySerializedAs("maxX"), SerializeField]
		private TextMeshProUGUI maxXText;

		[FormerlySerializedAs("minY"), SerializeField]
		private TextMeshProUGUI minYText;

		[FormerlySerializedAs("maxY"), SerializeField]
		private TextMeshProUGUI maxYText;

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
			ChartManager = GameObject.FindGameObjectWithTag("ChartManager")
				.GetComponent<ChartManager>();
			FindDataObjects();
			GetAllFloats();
			GetAllIntegers();
		}

		/// <summary>
		/// Fills the chart for the first time.
		/// </summary>
		private void Start()
		{
			Invoke(nameof(CallDrawData), 0.2f);
		}

		/// <summary>
		/// Gets all keys for <see cref="float" /> values contained in the <see cref="NodeRef" /> of each
		/// <see cref="GameObject" /> in <see cref="_dataObjects" />.
		/// </summary>
		private void GetAllFloats()
		{
			foreach (GameObject data in _dataObjects)
			foreach (string key in data.GetComponent<NodeRef>().node.FloatAttributes.Keys)
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
			foreach (string key in data.GetComponent<NodeRef>().node.IntAttributes.Keys)
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

			if (ActiveMarkers.Count == 0) noDataWarning.SetActive(true);
		}

		/// <summary>
		/// Adds a marker for every <see cref="Node" /> containing the metrics from both axes. It's position
		/// depends on the values of those metrics.
		/// </summary>
		private void DrawTwoAxes()
		{
			int i = 0;
			Node node = _dataObjects[i].GetComponent<NodeRef>().node;
			bool contained = node.TryGetNumeric(axisDropdownX.Value, out float minX);
			while (!contained)
			{
				i++;
				node = _dataObjects[i].GetComponent<NodeRef>().node;
				contained = node.TryGetNumeric(axisDropdownX.Value, out minX);
			}

			float maxX = minX;
			i = 0;
			node = _dataObjects[i].GetComponent<NodeRef>().node;
			contained = node.TryGetNumeric(axisDropdownY.Value, out float minY);
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
				if (node.TryGetNumeric(axisDropdownX.Value, out float tempX))
				{
					if (tempX < minX) minX = tempX;
					if (tempX > maxX) maxX = tempX;
					inX = true;
				}

				if (node.TryGetNumeric(axisDropdownY.Value, out float tempY))
				{
					if (tempY > maxY) maxY = tempY;
					if (tempY < minY) minY = tempY;
					inY = true;
				}

				if (inX && inY) toDraw.Add(data);
			}

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
				if (dataObject.GetComponent<NodeRef>().node.TryGetNumeric(metric, out float _))
					toDraw.Add(dataObject);

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

			foreach (GameObject data in toDraw)
			{
				GameObject marker = Instantiate(markerPrefab, entries.transform);
				ChartMarker script = marker.GetComponent<ChartMarker>();
				script.linkedObject = data;
				Node node = data.GetComponent<NodeRef>().node;
				node.TryGetNumeric(axisDropdownX.Value, out float valueX);
				node.TryGetNumeric(axisDropdownY.Value, out float valueY);
				string type = node.IsLeaf() ? "Building" : "Node";
				script.SetInfoText("Linked to: " + data.name + " of type " + type + "\nX: " +
				                   valueX.ToString("0.00") + ", Y: " + valueY.ToString("0.00"));
				marker.GetComponent<RectTransform>().anchoredPosition =
					new Vector2((valueX - minX) * width, (valueY - minY) * height);
				updatedMarkers.Add(marker);

				float highlightTimeLeft = CheckOldMarkers(data);
				if (highlightTimeLeft > 0f)
					script.TriggerTimedHighlight(ChartManager.highlightDuration -
					                             highlightTimeLeft, true);
			}

			foreach (GameObject marker in ActiveMarkers) Destroy(marker);
			ActiveMarkers = updatedMarkers;
		}

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
				//toDraw[0].GetComponent<NodeRef>().node
				//	.TryGetNumeric(axisDropdownX.Value, out float dropdownValue);
				//axisDropdownX.SetText(axisDropdownX.Value + " " + dropdownValue.ToString("0.00"));
				int x = 0;

				foreach (GameObject data in toDraw)
				{
					GameObject marker = Instantiate(markerPrefab, entries.transform);
					ChartMarker script = marker.GetComponent<ChartMarker>();
					script.linkedObject = data;
					Node node = data.GetComponent<NodeRef>().node;
					node.TryGetNumeric(metric, out float value);
					string type = node.IsLeaf() ? "Building" : "Node";
					script.SetInfoText("Linked to: " + data.name + " of type " + type + "\n" +
					                   metric +
					                   ": " + value.ToString("0.00"));
					marker.GetComponent<RectTransform>().anchoredPosition =
						new Vector2(x++ * width, (value - min) * height);
					updatedMarkers.Add(marker);

					float highlightTimeLeft = CheckOldMarkers(data);
					if (highlightTimeLeft > 0f)
						script.TriggerTimedHighlight(ChartManager.highlightDuration -
						                             highlightTimeLeft, true);
				}

				foreach (GameObject marker in ActiveMarkers) Destroy(marker);
				ActiveMarkers = updatedMarkers;
			}
		}

		private void AddMarkers(List<GameObject> toDraw)
		{
			List<GameObject> updatedMarkers = new List<GameObject>();
			Rect dataRect = dataPanel.rect;
			float width = dataRect.width / toDraw.Count;
			float height = dataRect.height / toDraw.Count;
			int x = 0;
			int y = 0;

			foreach (GameObject data in toDraw)
			{
				GameObject marker = Instantiate(markerPrefab, entries.transform);
				ChartMarker script = marker.GetComponent<ChartMarker>();
				script.linkedObject = data;
				Node node = data.GetComponent<NodeRef>().node;
				node.TryGetNumeric(axisDropdownX.Value, out float valueX);
				node.TryGetNumeric(axisDropdownY.Value, out float valueY);
				string type = node.IsLeaf() ? "Building" : "Node";
				script.SetInfoText("Linked to: " + data.name + " of type " + type + "\nX: " +
				                   valueX.ToString("0.00") + ", Y: " + valueY.ToString("0.00"));
				marker.GetComponent<RectTransform>().anchoredPosition =
					new Vector2(x++ * width, y++ * height);
				updatedMarkers.Add(marker);

				float highlightTimeLeft = CheckOldMarkers(data);
				if (highlightTimeLeft > 0f)
					script.TriggerTimedHighlight(ChartManager.highlightDuration -
					                             highlightTimeLeft, true);
			}

			foreach (GameObject marker in ActiveMarkers) Destroy(marker);
			ActiveMarkers = updatedMarkers;
		}

		private float CheckOldMarkers(GameObject marker)
		{
			foreach (GameObject oldMarker in ActiveMarkers)
			{
				ChartMarker oldScript = oldMarker.GetComponent<ChartMarker>();
				if (oldScript.linkedObject.GetInstanceID() == marker.GetInstanceID() &&
				    oldScript.TimedHighlight != null) return oldScript.HighlightTime;
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
				foreach (GameObject marker in ActiveMarkers)
				{
					Vector2 markerPos = marker.transform.position;
					if (markerPos.x > min.x && markerPos.x < max.x && markerPos.y > min.y &&
					    markerPos.y < max.y)
						ChartManager.HighlightObject(
							marker.GetComponent<ChartMarker>().linkedObject);
				}
			else
				foreach (GameObject marker in ActiveMarkers)
				{
					Vector2 markerPos = marker.transform.position;
					if (markerPos.x > min.x && markerPos.x < max.x && markerPos.y < min.y &&
					    markerPos.y > max.y)
						ChartManager.HighlightObject(
							marker.GetComponent<ChartMarker>().linkedObject);
				}
		}

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

		public void HighlightCorrespondingMarker(GameObject highlight)
		{
			foreach (GameObject activeMarker in ActiveMarkers)
			{
				ChartMarker script = activeMarker.GetComponent<ChartMarker>();
				if (script.linkedObject.Equals(highlight))
				{
					script.TriggerTimedHighlight(ChartManager.highlightDuration, false);
					break;
				}
			}
		}

		public void AccentuateCorrespondingMarker(GameObject highlight)
		{
			foreach (GameObject activeMarker in ActiveMarkers)
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
	}
}
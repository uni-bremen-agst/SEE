using System.Collections.Generic;
using SEE.Layout;
using TMPro;
using UnityEngine;

namespace Assets.SEECity.Charts.Scripts
{
	/// <summary>
	/// Fills Charts with data and manages that data.
	/// </summary>
	public class ChartContent : MonoBehaviour
	{
		/// <summary>
		/// Contains settings used by <see cref="ChartContent" />.
		/// </summary>
		private GameManager _gameManager;

		/// <summary>
		/// All objects to be listed in the chart.
		/// </summary>
		private GameObject[] _dataObjects;

		/// <summary>
		/// A list of all <see cref="ChartMarker" />s currently displayed in the chart.
		/// </summary>
		private readonly List<GameObject> _activeMarkers = new List<GameObject>();

		/// <summary>
		/// The <see cref="AxisContentDropdown" /> containing Values for the X-Axis.
		/// </summary>
		public AxisContentDropdown AxisDropdownX;

		/// <summary>
		/// The <see cref="AxisContentDropdown" /> containing Values for the Y-Axis.
		/// </summary>
		public AxisContentDropdown AxisDropdownY;

		/// <summary>
		/// The prefab used to display content in charts.
		/// </summary>
		[SerializeField] private GameObject _markerPrefab = null;

		/// <summary>
		/// Game Object to group all content entries of a chart.
		/// </summary>
		[SerializeField] private GameObject _entries = null;

		[SerializeField] private TextMeshProUGUI _minX = null;

		[SerializeField] private TextMeshProUGUI _maxX = null;

		[SerializeField] private TextMeshProUGUI _minY = null;

		[SerializeField] private TextMeshProUGUI _maxY = null;

		/// <summary>
		/// A parent of this object. Used in VR to destroy the whole construct of a moveable chart.
		/// </summary>
		[SerializeField] private GameObject _parent = null;

		/// <summary>
		/// The panel on which the <see cref="ChartMarker" />s are instantiated.
		/// </summary>
		[Header("For resizing and minimizing")]
		public RectTransform DataPanel;

		/// <summary>
		/// The panel on which the buttons and scales of the chart are displayed.
		/// </summary>
		public RectTransform LabelsPanel;

		/// <summary>
		/// Contains all keys contained in any <see cref="GameObject" /> of <see cref="_dataObjects" />.
		/// </summary>
		[HideInInspector]
		public List<string> AllKeys { get; } = new List<string>();

		/// <summary>
		/// Calls methods to initialize a chart.
		/// </summary>
		private void Awake()
		{
			_gameManager = GameObject.FindGameObjectWithTag("GameManager")
				.GetComponent<GameManager>();
			FindDataObjects();
			GetAllFloats();
			GetAllIntegers();
		}

		private void Start()
		{
			//DrawData();
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
			_dataObjects = GameObject.FindGameObjectsWithTag("Building");
		}

		/// <summary>
		/// Fills the chart with data depending on the values of <see cref="AxisDropdownX" /> and
		/// <see cref="AxisDropdownY" />.
		/// </summary>
		public void DrawData()
		{
			foreach (GameObject marker in _activeMarkers) Destroy(marker);
			_activeMarkers.Clear();
			FindDataObjects();

			int i = 0;
			bool contained = _dataObjects[i].GetComponent<NodeRef>().node
				.TryGetNumeric(AxisDropdownX.Value, out float minX);
			while (!contained)
				contained = _dataObjects[i].GetComponent<NodeRef>().node
					.TryGetNumeric(AxisDropdownX.Value, out minX);
			float maxX = minX;
			contained = _dataObjects[0].GetComponent<NodeRef>().node
				.TryGetNumeric(AxisDropdownY.Value, out float minY);
			while (!contained)
				contained = _dataObjects[0].GetComponent<NodeRef>().node
					.TryGetNumeric(AxisDropdownY.Value, out minY);
			float maxY = minY;
			List<GameObject> toDraw = new List<GameObject>();
			foreach (GameObject data in _dataObjects)
			{
				bool inX = false;
				bool inY = false;
				if (data.GetComponent<NodeRef>().node
					.TryGetNumeric(AxisDropdownX.Value, out float tempX))
				{
					if (tempX < minX) minX = tempX;
					if (tempX > maxX) maxX = tempX;
					inX = true;
				}

				if (data.GetComponent<NodeRef>().node
					.TryGetNumeric(AxisDropdownY.Value, out float tempY))
				{
					if (tempY > maxY) maxY = tempY;
					if (tempY < minY) minY = tempY;
					inY = true;
				}

				if (inX && inY) toDraw.Add(data);
			}

			float width = DataPanel.rect.width / (maxX - minX);
			float height = DataPanel.rect.height / (maxY - minY);
			foreach (GameObject data in toDraw)
			{
				GameObject marker = Instantiate(_markerPrefab, _entries.transform);
				ChartMarker script = marker.GetComponent<ChartMarker>();
				script.LinkedObject = data;
				data.GetComponent<NodeRef>().node
					.TryGetNumeric(AxisDropdownX.Value, out float valueX);
				data.GetComponent<NodeRef>().node
					.TryGetNumeric(AxisDropdownY.Value, out float valueY);
				script.SetInfoText("Building: " + script.LinkedObject.name + "\nX: " +
				                   valueX.ToString("0.00") + ", Y: " + valueY.ToString("0.00"));
				marker.GetComponent<RectTransform>().anchoredPosition = new Vector2(
					(valueX - minX) * width, (valueY - minY) * height);
				_activeMarkers.Add(marker);
			}

			_minX.text = minX.ToString("0.00");
			_maxX.text = maxX.ToString("0.00");
			_minY.text = minY.ToString("0.00");
			_maxY.text = maxY.ToString("0.00");
		}

		public void AreaSelection(Vector2 min, Vector2 max, bool direction)
		{
			float highlightDuration = _gameManager.HighlightDuration;
			if (direction)
				foreach (GameObject marker in _activeMarkers)
				{
					Vector2 markerPos = marker.transform.position;
					if (markerPos.x > min.x && markerPos.x < max.x && markerPos.y > min.y &&
					    markerPos.y < max.y)
						marker.GetComponent<ChartMarker>().TimedHighlight(highlightDuration);
				}
			else
				foreach (GameObject marker in _activeMarkers)
				{
					Vector2 markerPos = marker.transform.position;
					if (markerPos.x > min.x && markerPos.x < max.x && markerPos.y < min.y &&
					    markerPos.y > max.y)
						marker.GetComponent<ChartMarker>().TimedHighlight(highlightDuration);
				}
		}

		/// <summary>
		/// Destroys the chart including its container if VR is activated.
		/// </summary>
		[SerializeField]
		private void Destroy()
		{
			Destroy(_parent);
		}
	}
}
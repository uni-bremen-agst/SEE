using System.Collections;
using System.Collections.Generic;
using SEE.Layout;
using UnityEngine;

namespace Assets.SEECity.Charts.Scripts
{
	/// <summary>
	/// Fills Charts with data and manages that data.
	/// </summary>
	public class ChartContent : MonoBehaviour
	{
		private GameObject[] _dataObjects;

		/// <summary>
		/// The <see cref="AxisContentDropdown" /> containing Values for the X-Axis.
		/// </summary>
		[SerializeField] private AxisContentDropdown _xAxisDropdown;

		/// <summary>
		/// The <see cref="AxisContentDropdown" /> containing Values for the Y-Axis.
		/// </summary>
		[SerializeField] private AxisContentDropdown _yAxisDropdown;

		/// <summary>
		/// A <see cref="ChartMarker" /> to display content in charts.
		/// </summary>
		[SerializeField] private GameObject _markerPrefab;

		/// <summary>
		/// A list of all <see cref="ChartMarker" />s currently displayed in the chart.
		/// </summary>
		private List<GameObject> _activeMarkers = new List<GameObject>();

		/// <summary>
		/// Game Object to group all content entries of a chart.
		/// </summary>
		[SerializeField] private GameObject _entries;

		/// <summary>
		/// The panel on which the <see cref="ChartMarker" />s are instantiated.
		/// </summary>
		[SerializeField] private RectTransform _dataPanel;

		/// <summary>
		/// A parent of this object. Used in VR to destroy the whole construct of a moveable chart.
		/// </summary>
		[SerializeField] private GameObject _parent;

		/// <summary>
		/// Calls methods to initialize a chart.
		/// </summary>
		private void Start()
		{
			StartCoroutine(FirstInitialization());
		}

		/// <summary>
		/// Calls <see cref="DrawData" /> during the first initialization after everything else has been
		/// initialized.
		/// </summary>
		/// <returns>A <see cref="WaitForEndOfFrame" />.</returns>
		private IEnumerator FirstInitialization()
		{
			yield return new WaitForEndOfFrame();
			DrawData();
		}

		/// <summary>
		/// Fills a List with all objects that will be in the chart.
		/// TODO: Show different types of objects in chart (edges, nodes)?
		/// </summary>
		private void FindDataObjects()
		{
			_dataObjects = GameObject.FindGameObjectsWithTag("Building");
		}

		/// <summary>
		/// Fills the chart with data depending on the values of <see cref="_xAxisDropdown" /> and
		/// <see cref="_yAxisDropdown" />.
		/// </summary>
		public void DrawData()
		{
			foreach (GameObject marker in _activeMarkers) Destroy(marker);
			_activeMarkers.Clear();
			FindDataObjects();
			_dataObjects[0].GetComponent<NodeRef>().node
				.TryGetNumeric(_xAxisDropdown.Value, out float minX);
			float maxX = minX;
			_dataObjects[0].GetComponent<NodeRef>().node
				.TryGetNumeric(_yAxisDropdown.Value, out float minY);
			float maxY = minY;
			foreach (GameObject data in _dataObjects)
			{
				data.GetComponent<NodeRef>().node
					.TryGetNumeric(_xAxisDropdown.Value, out float tempX);
				if (tempX < minX) minX = tempX;
				if (tempX > maxX) maxX = tempX;
				data.GetComponent<NodeRef>().node
					.TryGetNumeric(_yAxisDropdown.Value, out float tempY);
				if (tempY > maxY) maxY = tempY;
				if (tempY < minY) minY = tempY;
			}

			float width = _dataPanel.rect.width / (maxX - minX);
			float height = _dataPanel.rect.height / (maxY - minY);
			foreach (GameObject data in _dataObjects)
			{
				GameObject marker = Instantiate(_markerPrefab, _entries.transform);
				marker.GetComponent<ChartMarker>().LinkedObject = data;
				data.GetComponent<NodeRef>().node
					.TryGetNumeric(_xAxisDropdown.Value, out float valueX);
				data.GetComponent<NodeRef>().node
					.TryGetNumeric(_yAxisDropdown.Value, out float valueY);
				marker.GetComponent<RectTransform>().anchoredPosition = new Vector2(
					(valueX - minX) * width, (valueY - minY) * height);
				_activeMarkers.Add(marker);
			}
		}

		/// <summary>
		/// Destroys the chart including its container if VR is activated.
		/// </summary>
		public void Destroy()
		{
			Destroy(_parent);
		}
	}
}
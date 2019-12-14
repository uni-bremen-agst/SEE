using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.SEECity.Charts.Scripts
{
	/// <summary>
	/// Fills Charts with data.
	/// </summary>
	public class ChartContent : MonoBehaviour
	{
		private GameObject[] _dataObjects;

		[SerializeField] private TMP_Dropdown _xAxisDropdown;
		[SerializeField] private TMP_Dropdown _yAxisDropdown;

		/// <summary>
		/// A marker to display content in charts.
		/// </summary>
		[SerializeField] private GameObject markerPrefab;

		/// <summary>
		/// Game Object to group all content entries of a chart.
		/// </summary>
		[SerializeField] private GameObject entries;

		/// <summary>
		/// The panel on which the markers are instantiated.
		/// </summary>
		[SerializeField] private RectTransform dataPanel;

		/// <summary>
		/// A parent of this object. Used in VR to destroy the whole construct of a moveable chart.
		/// </summary>
		[SerializeField] private GameObject parent;

		/// <summary>
		/// Calls methods that should be called when the user presses a button in the final version - for
		/// testing.
		/// </summary>
		private void Start()
		{
			FindDataObjects();
			DrawData();
		}

		/// <summary>
		/// Fills a List with all objects that will be in the chart. Right now that's all buildings.
		/// TODO: Show different types of objects in chart?
		/// </summary>
		private void FindDataObjects()
		{
			_dataObjects = GameObject.FindGameObjectsWithTag("Building");
		}

		/// <summary>
		/// Fills the chart with data.
		/// </summary>
		private void DrawData()
		{
			float minX = _dataObjects[0].transform.localScale.x;
			float maxX = _dataObjects[0].transform.localScale.x;
			float minY = _dataObjects[0].transform.localScale.y;
			float maxY = _dataObjects[0].transform.localScale.y;
			foreach (GameObject data in _dataObjects)
			{
				float tempX = data.transform.localScale.x;
				if (tempX < minX) minX = tempX;
				if (tempX > maxX) maxX = tempX;
				float tempY = data.transform.localScale.y;
				if (tempY > maxY) maxY = tempY;
				if (tempY < minY) minY = tempY;
			}

			float width = dataPanel.rect.width / (maxX - minX);
			float height = dataPanel.rect.height / (maxY - minY);
			foreach (GameObject data in _dataObjects)
			{
				GameObject marker = Instantiate(markerPrefab, entries.transform);
				marker.GetComponent<ChartMarker>().LinkedObject = data;
				marker.GetComponent<RectTransform>().anchoredPosition = new Vector2(
					(data.transform.localScale.x - minX) * width,
					(data.transform.localScale.y - minY) * height);
			}
		}

		/// <summary>
		/// Fills the chart with data.
		/// </summary>
		private void Fill()
		{
			Vector3[] vectors = new Vector3[_dataObjects.Length];
			float minX = 0;
			float maxX = 0;
			float minY = 0;
			float maxY = 0;

			//Create entries
			float width = dataPanel.rect.width / (maxX - minX);
			float height = dataPanel.rect.height / (maxY - minY);

			for (int i = 0; i < vectors.Length; i++)
			{
				GameObject marker = Instantiate(markerPrefab, entries.transform);
				marker.GetComponent<ChartMarker>().LinkedObject = _dataObjects[i];
				Vector3 vector = vectors[i];
				marker.GetComponent<RectTransform>().anchoredPosition =
					new Vector2((vector.x - minX) * width, (vector.y - minY) * height);
			}
		}

		/// <summary>
		/// Destroys the chart.
		/// </summary>
		public void Destroy()
		{
			Destroy(parent);
		}
	}
}
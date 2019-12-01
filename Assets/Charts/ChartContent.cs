using TMPro;
using UnityEngine;

namespace Assets.Charts
{
	public class ChartContent : MonoBehaviour
	{
		private GameObject[] _dataObjects;
		private AxisContent _xAxisContent;
		private AxisContent _yAxisContent;

		[SerializeField] private GameObject markerPrefab;
		[SerializeField] private GameObject entries;
		[SerializeField] private GameObject dataPanel;
		[SerializeField] private TextMeshProUGUI xLabel;
		[SerializeField] private TextMeshProUGUI yLabel;

		/// <summary>
		/// Calls methods that should be called when the user presses a button in the final version - for
		/// testing.
		/// </summary>
		private void Start()
		{
			FindDataObjects();
			DrawData();
		}

		public void Initialize(AxisContent xAxisContent, AxisContent yAxisContent)
		{
			_xAxisContent = xAxisContent;
			_yAxisContent = yAxisContent;
			//DrawData();
		}

		/// <summary>
		/// Fills a List with all objects that will be in the chart. Right now that's all buildings.
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
			xLabel.text = "Local Scale X";
			yLabel.text = "Local Scale Y";
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

			RectTransform field = dataPanel.GetComponent<RectTransform>();
			float width = field.rect.width / (maxX - minX);
			float height = field.rect.height / (maxY - minY);
			foreach (GameObject data in _dataObjects)
			{
				GameObject marker = Instantiate(markerPrefab, entries.transform);
				marker.GetComponent<ChartMarker>().LinkedObject = data;
				marker.GetComponent<RectTransform>().anchoredPosition = new Vector3(
					(data.transform.localScale.x - minX) * width,
					(data.transform.localScale.y - minY) * height, 0);
			}
		}

		/// <summary>
		/// Destroys the chart.
		/// </summary>
		public void Destroy()
		{
			Destroy(gameObject);
		}
	}
}
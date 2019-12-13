using TMPro;
using UnityEngine;

namespace Assets.Charts
{
	/// <summary>
	/// The different Axes on which data can be displayed in charts.
	/// TODO: Might contain colors etc. for more details.
	/// </summary>
	public enum ChartAxis
	{
		X,
		Y
	}

	/// <summary>
	/// Fills Charts with data.
	/// </summary>
	public class ChartContent : MonoBehaviour
	{
		private GameObject[] _dataObjects;
		private AxisContent _xAxisContent;
		private AxisContent _yAxisContent;

		/// <summary>
		/// A marker to display content in charts.
		/// </summary>
		[SerializeField] private GameObject markerPrefab;

		/// <summary>
		/// GO to group all content entries of a chart.
		/// </summary>
		[SerializeField] private GameObject entries;

		/// <summary>
		/// The panel on which the markers are instantiated.
		/// </summary>
		[SerializeField] private RectTransform dataPanel;

		/// <summary>
		/// The text of the label of the x-axis.
		/// </summary>
		[SerializeField] private TextMeshProUGUI xLabel;

		/// <summary>
		/// The text of the label of the y-axis.
		/// </summary>
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
			xLabel.text = _xAxisContent.ToString();
			yLabel.text = _yAxisContent.ToString();
			Vector3[] vectors = new Vector3[_dataObjects.Length];
			float minX = 0;
			float maxX = 0;
			float minY = 0;
			float maxY = 0;

			//Get x-Axis data
			switch (_xAxisContent)
			{
				case AxisContent.LocalScaleX:
					(minX, maxX, vectors) = FillLocalScaleX(vectors, ChartAxis.X);
					break;
				case AxisContent.LocalScaleY:
					(minX, maxX, vectors) = FillLocalScaleY(vectors, ChartAxis.X);
					break;
			}

			//Get y-Axis data
			switch (_yAxisContent)
			{
				case AxisContent.LocalScaleX:
					(minY, maxY, vectors) = FillLocalScaleX(vectors, ChartAxis.Y);
					break;
				case AxisContent.LocalScaleY:
					(minY, maxY, vectors) = FillLocalScaleY(vectors, ChartAxis.Y);
					break;
			}

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
		/// Retrieves data for the chart entries.
		/// </summary>
		/// <param name="vectors"></param>
		/// <param name="axis"></param>
		/// <returns></returns>
		private (float, float, Vector3[]) FillLocalScaleX(Vector3[] vectors, ChartAxis axis)
		{
			float min = _dataObjects[0].transform.localScale.x;
			float max = _dataObjects[0].transform.localScale.x;
			for (int i = 0; i < _dataObjects.Length; i++)
			{
				GameObject data = _dataObjects[i];
				//Determine min/max
				float temp = data.transform.localScale.x;
				if (temp < min)
					min = temp;
				else if (temp > max) max = temp;
				//Set vectors
				switch (axis)
				{
					case ChartAxis.X:
						vectors[i] = new Vector3(data.transform.localScale.x, vectors[i].y,
							vectors[i].z);
						break;
					case ChartAxis.Y:
						vectors[i] = new Vector3(vectors[i].y, data.transform.localScale.x,
							vectors[i].z);
						break;
				}
			}

			return (min, max, vectors);
		}

		/// <summary>
		/// Retrieves data for the chart entries.
		/// </summary>
		/// <param name="vectors"></param>
		/// <param name="axis"></param>
		/// <returns></returns>
		private (float, float, Vector3[]) FillLocalScaleY(Vector3[] vectors, ChartAxis axis)
		{
			float min = _dataObjects[0].transform.localScale.y;
			float max = _dataObjects[0].transform.localScale.y;
			for (int i = 0; i < _dataObjects.Length; i++)
			{
				GameObject data = _dataObjects[i];
				float temp = data.transform.localScale.y;
				if (temp < min)
					min = temp;
				else if (temp > max) max = temp;
				switch (axis)
				{
					case ChartAxis.X:
						vectors[i] = new Vector3(data.transform.localScale.x, vectors[i].y,
							vectors[i].z);
						break;
					case ChartAxis.Y:
						vectors[i] = new Vector3(vectors[i].y, data.transform.localScale.x,
							vectors[i].z);
						break;
				}
			}

			return (min, max, vectors);
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
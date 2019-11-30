using TMPro;
using UnityEngine;

namespace Assets.Charts
{
	public class ChartCreator : MonoBehaviour
	{
		private GameObject[] dataObjects;
		[SerializeField] private GameObject markerPrefab;
		[SerializeField] private GameObject entries;
		[SerializeField] private GameObject dataPanel;
		[SerializeField] private TextMeshProUGUI xLabel;
		[SerializeField] private TextMeshProUGUI yLabel;

		/// <summary>
		/// Calls methods that should be called when the user presses a button in the final version - for testing.
		/// </summary>
		private void Start()
		{
			FindDataObjects();
			DrawData();
		}

		/// <summary>
		/// Fills a List with all objects that will be in the chart. Right now that's all buildings.
		/// </summary>
		private void FindDataObjects()
		{
			dataObjects = GameObject.FindGameObjectsWithTag("Building");
		}

		/// <summary>
		/// Fills the chart with data.
		/// </summary>
		private void DrawData()
		{
			xLabel.text = "Local Scale X";
			yLabel.text = "Local Scale Y";
			var minX = dataObjects[0].transform.localScale.x;
			var maxX = dataObjects[0].transform.localScale.x;
			var minY = dataObjects[0].transform.localScale.y;
			var maxY = dataObjects[0].transform.localScale.y;
			foreach (var data in dataObjects)
			{
				var tempX = data.transform.localScale.x;
				if (tempX < minX) minX = tempX;
				if (tempX > maxX) maxX = tempX;
				var tempY = data.transform.localScale.y;
				if (tempY > maxY) maxY = tempY;
				if (tempY < minY) minY = tempY;
			}

			var field = dataPanel.GetComponent<RectTransform>();
			var width = field.rect.width / (maxX - minX);
			var height = field.rect.height / (maxY - minY);
			foreach (var data in dataObjects)
			{
				var marker = Instantiate(markerPrefab, entries.transform);
				marker.GetComponent<ChartMarker>().LinkedObject = data;
				marker.GetComponent<RectTransform>().anchoredPosition = new Vector3(
					(data.transform.localScale.x - minX) * width, (data.transform.localScale.y - minY) * height, 0);
			}
		}
	}
}
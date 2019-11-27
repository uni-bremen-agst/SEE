using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ChartCreator : MonoBehaviour
{
	GameObject[] dataObjects;
	[SerializeField]
	GameObject markerPrefab;
	[SerializeField]
	GameObject entries;
	[SerializeField]
	GameObject dataPanel;
	[SerializeField]
	TextMeshProUGUI xLabel;
	[SerializeField]
	TextMeshProUGUI yLabel;

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
	void FindDataObjects()
	{
		dataObjects = GameObject.FindGameObjectsWithTag("Building");
	}

	/// <summary>
	/// Fills the chart with data.
	/// </summary>
	void DrawData()
	{
		xLabel.text = "Local Scale X";
		yLabel.text = "Local Scale Y";
		float minX = dataObjects[0].transform.localScale.x;
		float maxX = dataObjects[0].transform.localScale.x;
		float minY = dataObjects[0].transform.localScale.y;
		float maxY = dataObjects[0].transform.localScale.y;
		foreach (GameObject data in dataObjects)
		{
			float tempX = data.transform.localScale.x;
			if (tempX < minX)
			{
				minX = tempX;
			}
			if (tempX > maxX)
			{
				maxX = tempX;
			}
			float tempY = data.transform.localScale.y;
			if (tempY > maxY)
			{
				maxY = tempY;
			}
			if (tempY < minY)
			{
				minY = tempY;
			}
		}
		RectTransform field = dataPanel.GetComponent<RectTransform>();
		float width = field.rect.width / (maxX - minX);
		float height = field.rect.height / (maxY - minY);
		foreach (GameObject data in dataObjects)
		{
			GameObject marker = Instantiate(markerPrefab, entries.transform);
			marker.GetComponent<ChartMarker>().LinkedObject = data;
			marker.GetComponent<RectTransform>().anchoredPosition = new Vector3((data.transform.localScale.x - minX) * width, (data.transform.localScale.y - minY) * height, 0);
		}
	}
}

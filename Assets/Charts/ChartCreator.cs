using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChartCreator : MonoBehaviour
{
	GameObject[] dataObjects;
	[SerializeField]
	GameObject markerPrefab;
	[SerializeField]
	GameObject entries;

	private void Start()
	{
		FindDataObjects();
		Debug.Log(dataObjects.Length);
		DrawData();
	}

	void FindDataObjects()
	{
		dataObjects = GameObject.FindGameObjectsWithTag("Building");
	}

	void DrawData()
	{
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
		Debug.Log(maxX + " " + minX + " " + maxY + " " + minY);
		RectTransform field = GetComponent<RectTransform>();
		float width = field.rect.width / (maxX - minX);
		float height = field.rect.height / (maxY - minY);
		foreach (GameObject data in dataObjects)
		{
			GameObject marker = Instantiate(markerPrefab);
			marker.GetComponent<ChartMarker>().LinkedObject = data;
			marker.transform.SetParent(entries.transform);
			marker.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
			marker.transform.position = new Vector3(((data.transform.localScale.x - minX) / maxX) * width + entries.transform.position.x, ((data.transform.localScale.y - minY) / maxY)
				* height + entries.transform.position.y, field.transform.position.z);
		}
	}
}

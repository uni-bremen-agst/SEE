using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChartUIController : MonoBehaviour
{
	[SerializeField]
	GameObject chartsClosed;
	[SerializeField]
	GameObject chartsOpen;

	public void OpenCharts()
	{
		chartsClosed.SetActive(false);
		chartsOpen.SetActive(true);
	}

	public void CloseCharts()
	{
		chartsClosed.SetActive(true);
		chartsOpen.SetActive(false);
	}
}

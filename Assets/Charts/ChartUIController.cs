using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Charts
{
	public class ChartUIController : MonoBehaviour
	{
		[SerializeField] private GameObject chartsClosed;
		[SerializeField] private GameObject chartsOpen;

		/// <summary>
		/// Opens the chart view.
		/// </summary>
		public void OpenCharts()
		{
			chartsClosed.SetActive(false);
			chartsOpen.SetActive(true);
		}

		/// <summary>
		/// Closes the chart view.
		/// </summary>
		public void CloseCharts()
		{
			chartsClosed.SetActive(true);
			chartsOpen.SetActive(false);
		}
	}
}
using UnityEngine;

namespace Assets.Charts
{
	/// <summary>
	/// Contains methods for controlling the chart UI.
	/// </summary>
	public class ChartUIController : MonoBehaviour
	{
		[SerializeField] private GameObject chartsClosed;
		[SerializeField] private GameObject chartsOpen;
		[SerializeField] private GameObject chartCreator;

		/// <summary>
		/// Toggles the chart view.
		/// </summary>
		public void ToggleCharts(bool open)
		{
			chartsClosed.SetActive(!open);
			chartsOpen.SetActive(open);
		}

		/// <summary>
		/// Opens the window for creating new charts.
		/// </summary>
		public void OpenChartCreator()
		{
			chartCreator.SetActive(true);
		}
	}
}
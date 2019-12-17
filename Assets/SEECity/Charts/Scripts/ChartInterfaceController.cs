using UnityEngine;

namespace Assets.SEECity.Charts.Scripts
{
	/// <summary>
	/// Sets the active states of different layers of the chart UI.
	/// </summary>
	public class ChartInterfaceController : MonoBehaviour
	{
		/// <summary>
		/// UI when charts are closed.
		/// </summary>
		[SerializeField] private GameObject _chartsClosed;

		/// <summary>
		/// UI when charts are open.
		/// </summary>
		[SerializeField] private GameObject _chartsOpen;

		/// <summary>
		/// UI for creating new charts.
		/// </summary>
		[SerializeField] private GameObject _chartCreator;

		/// <summary>
		/// Toggles the chart view.
		/// </summary>
		public void ToggleCharts(bool open)
		{
			_chartsClosed.SetActive(!open);
			_chartsOpen.SetActive(open);
		}

		/// <summary>
		/// Opens the dialogue for creating new charts.
		/// </summary>
		public void OpenChartCreator()
		{
			_chartCreator.SetActive(true);
		}
	}
}
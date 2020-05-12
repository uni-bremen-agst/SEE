using SEE.Charts.Scripts;
using UnityEngine;

namespace SEE.Controls
{
	public class DesktopChartAction : ChartAction
	{
		private ChartManager _chartManager;

		private void Start()
		{
			_chartManager = GameObject.FindGameObjectWithTag("ChartManager")
				.GetComponent<ChartManager>();
		}

		private void Update()
		{
			if (chartControlsDevice.Toggle) _chartManager.ToggleCharts();
			if (chartControlsDevice.Select) _chartManager.ToggleSelectionMode();
		}
	}
}
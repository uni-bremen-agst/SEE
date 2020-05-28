namespace SEE.Controls
{
	public class DesktopChartAction : ChartAction
	{
		private void Update()
		{
			if (chartControlsDevice.Toggle) ChartManager.ToggleCharts();
			if (chartControlsDevice.Select) ChartManager.ToggleSelectionMode();
		}
	}
}
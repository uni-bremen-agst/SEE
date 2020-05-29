using SEE.Charts.Scripts;

namespace SEE.Controls
{
	public class VrChartAction : ChartAction
	{
		private bool _lastClick;

		private void Update()
		{
			if (chartControlsDevice.ResetCharts) ChartManager.ResetPosition();
			if (!chartControlsDevice.Move.y.Equals(0)) move = chartControlsDevice.Move.y;
			if (chartControlsDevice.Create) ChartManager.CreateChartVr();
			clickDown = false;
			clickUp = false;
			if (!_lastClick && chartControlsDevice.Click) clickDown = true;
			if (_lastClick && !chartControlsDevice.Click) clickUp = true;
			_lastClick = chartControlsDevice.Click;
		}
	}
}
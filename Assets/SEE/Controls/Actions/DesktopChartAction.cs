using SEE.Charts.Scripts;
using SEE.GO;
using UnityEngine;

namespace SEE.Controls
{
	public class DesktopChartAction : ChartAction
	{
		private void Update()
		{
			if (chartControlsDevice.Toggle) ChartManager.Instance.ToggleCharts();
			if (chartControlsDevice.Select) ChartManager.Instance.ToggleSelectionMode();
			if (!chartControlsDevice.Click) return;
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray, out var hit, 100f) &&
			    hit.transform.gameObject.TryGetComponent(out NodeRef _))
				ChartManager.HighlightObject(hit.transform.gameObject, false);
		}
	}
}
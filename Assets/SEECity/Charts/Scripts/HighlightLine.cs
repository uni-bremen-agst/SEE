using UnityEngine;

namespace SEECity.Charts.Scripts
{
	public class HighlightLine : MonoBehaviour
	{
		private ChartManager _chartManager;

		private Color _standardColor;

		private Color _accentuationColor;

		private bool _accentuated;

		private LineRenderer _line;

		private void Awake()
		{
			GetSettingData();
		}

		private void GetSettingData()
		{
			_chartManager = GameObject.FindGameObjectWithTag("ChartManager")
				.GetComponent<ChartManager>();
			_standardColor = _chartManager.standardColor;
			_accentuationColor = _chartManager.accentuationColor;
		}

		private void Start()
		{
			_line = GetComponent<LineRenderer>();
		}

		public void ToggleAccentuation()
		{
			_line.startColor = _accentuated ? _standardColor : _accentuationColor;
			_accentuated = !_accentuated;
		}
	}
}
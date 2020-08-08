using UnityEngine;

namespace SEE.Charts.Scripts
{
	/// <summary>
	/// A line placed above a node in the scene that is highlighted.
	/// </summary>
	public class HighlightLine : MonoBehaviour
	{
		/// <summary>
		/// The color of the line for normal highlights.
		/// </summary>
		private Color _standardColor;

		/// <summary>
		/// The color of the line for accentuated highlights.
		/// </summary>
		private Color _accentuationColor;

		/// <summary>
		/// If the line is accentuated or not.
		/// </summary>
		private bool _accentuated;

		/// <summary>
		/// The actual renderer visualizing the line.
		/// </summary>
		private LineRenderer _line;

		/// <summary>
		/// Initializes some values.
		/// </summary>
		private void Awake()
		{
			GetSettingData();
		}

		/// <summary>
		/// Links the <see cref="ChartManager" /> and gets its setting data.
		/// </summary>
		private void GetSettingData()
		{
			_standardColor = ChartManager.Instance.standardColor;
			_accentuationColor = ChartManager.Instance.accentuationColor;
		}

		/// <summary>
		/// Initializes the <see cref="_line" />.
		/// </summary>
		private void Start()
		{
			_line = GetComponent<LineRenderer>();
		}

		/// <summary>
		/// Toggles the accentuation color of the line.
		/// </summary>
		public void ToggleAccentuation()
		{
			_line.startColor = _accentuated ? _standardColor : _accentuationColor;
			_accentuated = !_accentuated;
		}
	}
}
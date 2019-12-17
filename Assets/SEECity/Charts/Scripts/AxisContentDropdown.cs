using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Assets.SEECity.Charts.Scripts
{
	/// <summary>
	/// Manages results and options of <see cref="TMP_Dropdown" />s used to select what metric to display
	/// on a charts axis.
	/// </summary>
	public class AxisContentDropdown : MonoBehaviour
	{
		/// <summary>
		/// Visualizes the content displayed in the chart.
		/// </summary>
		[SerializeField] private ChartContent _chartContent;

		/// <summary>
		/// A dropdown containing options for different metrics to display on a charts axes.
		/// </summary>
		private TMP_Dropdown _dropdown;

		private readonly List<string> _options = new List<string>();

		/// <summary>
		/// The currently selected option of <see cref="_dropdown" />.
		/// </summary>
		public string Value { get; private set; }

		/// <summary>
		/// Adds all possible options to the <see cref="TMP_Dropdown" />.
		/// </summary>
		private void Start()
		{
			_dropdown = GetComponent<TMP_Dropdown>();
			_dropdown.ClearOptions();
			_options.Add("Metric.Clone_Rate");
			_options.Add("Metric.Number_of_Tokens");
			_dropdown.AddOptions(_options);
			Value = _dropdown.options[0].text;
		}

		/// <summary>
		/// Updates <see cref="Value" /> to match the selected option of <see cref="_dropdown" />
		/// </summary>
		public void ChangeValue()
		{
			Value = _dropdown.options[_dropdown.value].text;
			_chartContent.DrawData();
		}
	}
}
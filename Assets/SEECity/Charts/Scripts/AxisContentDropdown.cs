using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SEECity.Charts.Scripts
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
		private ChartContent _chartContent;

		/// <summary>
		/// A dropdown containing options for different metrics to display on a charts axes.
		/// </summary>
		private TMP_Dropdown _dropdown;

		/// <summary>
		/// A list containing all options for the <see cref="_dropdown" />
		/// </summary>
		private List<string> _options;

		/// <summary>
		/// The currently selected option of <see cref="_dropdown" />.
		/// </summary>
		[HideInInspector]
		public string Value { get; private set; }

		/// <summary>
		/// Adds all possible options to the <see cref="TMP_Dropdown" />.
		/// </summary>
		private void Start()
		{
			_chartContent = transform.parent.parent.GetComponent<ChartContent>();
			_dropdown = GetComponent<TMP_Dropdown>();
			GetKeys();
			Value = _dropdown.options[0].text;
		}

		private void GetKeys()
		{
			_dropdown.ClearOptions();
			_options = _chartContent.AllKeys;
			_dropdown.AddOptions(_options);
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
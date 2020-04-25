using System.Linq;
using TMPro;
using UnityEngine;

namespace SEE.Charts.Scripts
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
		/// The other dropdown of this chart.
		/// </summary>
		private AxisContentDropdown _other;

		/// <summary>
		/// The old value of the other <see cref="AxisContentDropdown" />.
		/// </summary>
		private int _oldNone;

		/// <summary>
		/// A list containing all options for the <see cref="_dropdown" />
		/// </summary>
		private string[] _options;

		/// <summary>
		/// The currently selected option of the <see cref="_dropdown" />.
		/// </summary>
		public string Value { get; private set; }

		/// <summary>
		/// Adds all possible options to the <see cref="TMP_Dropdown" />.
		/// </summary>
		private void Start()
		{
			_chartContent = transform.parent.parent.GetComponent<ChartContent>();
			_dropdown = GetComponent<TMP_Dropdown>();
			GetKeys();
			Value = "Metric." + _dropdown.options[0].text;
			_chartContent.SetInfoText();
			var noneText = "(NONE) " + _dropdown.options[0].text;
			_dropdown.options[0].text = noneText;
			_dropdown.captionText.text = noneText;
		}

		/// <summary>
		/// Adds all metrics contained in the chart to the dropdown.
		/// </summary>
		private void GetKeys()
		{
			_dropdown.ClearOptions();
			_options = _chartContent.AllKeys.ToArray();
			for (var i = 0; i < _options.Length; i++) _options[i] = _options[i].Remove(0, 7);
			_dropdown.AddOptions(_options.ToList());
		}

		/// <summary>
		/// Updates <see cref="Value" /> to match the selected option of <see cref="_dropdown" />
		/// </summary>
		public void ChangeValue()
		{
			var currentValue = _dropdown.options[_dropdown.value].text;
			if (currentValue.StartsWith("(NONE) ")) currentValue = currentValue.Remove(0, 7);
			Value = "Metric." + currentValue;
			_chartContent.DrawData(true);
			_chartContent.SetInfoText();
			_other.OtherChanged(_dropdown.value);
		}

		/// <summary>
		/// Adds an indicator to a value in the dropdown, that signalizes that the same value is used on the
		/// other axis.
		/// </summary>
		/// <param name="value">The value of the other <see cref="AxisContentDropdown" />.</param>
		private void OtherChanged(int value)
		{
			_dropdown.options[_oldNone].text = _dropdown.options[_oldNone].text.Remove(0, 7);
			if (_oldNone == _dropdown.value)
				_dropdown.captionText.text = _dropdown.captionText.text.Remove(0, 7);
			_dropdown.options[value].text = "(NONE) " + _dropdown.options[value].text;
			if (value == _dropdown.value)
				_dropdown.captionText.text = _dropdown.options[value].text;
			_oldNone = value;
		}

		/// <summary>
		/// Changes the text of the dropdown.
		/// </summary>
		/// <param name="text"></param>
		public void SetText(string text)
		{
			_dropdown.captionText.text = text;
		}

		public void SetOther(AxisContentDropdown other)
		{
			_other = other;
		}
	}
}
// Copyright 2020 Robert Bohnsack
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be included
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
// CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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
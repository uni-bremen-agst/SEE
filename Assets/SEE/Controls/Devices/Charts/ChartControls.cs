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

namespace SEE.Controls.Devices
{
	/// <summary>
	/// Abstract super class of all input devices providing user input for
	/// the handling of metric charts.
	/// </summary>
	public abstract class ChartControls : InputDevice
	{
		/// <summary>
		/// Whether the toggle to turn on/off the metric chart mode (in which metrics
		/// are shown) is pressed.
		/// </summary>
		public abstract bool Toggle { get; }

		/// <summary>
		/// Whether the selection of elements in the metric charts is activated.
		/// </summary>
		public abstract bool Select { get; }

		public abstract Vector2 Move { get; }

		/// <summary>
		/// Whether the user wants all metric to be reset in front of her/him.
		/// </summary>
		public abstract bool ResetCharts { get; }

		/// <summary>
		/// Whether the user clicks.
		/// </summary>
		public abstract bool Click { get; }

		/// <summary>
		/// Whether the user wants to create a new metric chart.
		/// </summary>
		public abstract bool Create { get; }
	}
}
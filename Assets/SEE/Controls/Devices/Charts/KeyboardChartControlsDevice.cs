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
	public class KeyboardChartControlsDevice : ChartControls
	{
		private const KeyCode ToggleKey = KeyCode.G;
		private const KeyCode SelectionKey = KeyCode.LeftControl;
		private const KeyCode ClickKey = KeyCode.Mouse0;

		public override bool Toggle => Input.GetKeyDown(ToggleKey);

		public override bool Select =>
			Input.GetKeyDown(SelectionKey) || Input.GetKeyUp(SelectionKey);

		public override Vector2 Move => Vector2.zero;

		public override bool ResetCharts => false;

		public override bool Click => Input.GetKeyDown(ClickKey);

		public override bool Create => false;
	}
}
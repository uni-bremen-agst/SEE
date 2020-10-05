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

        private void Start()
        {
			Debug.LogFormat("DesktopChartAction started with control device: {0}\n",
				chartControlsDevice == null ? "NULL": chartControlsDevice.Name);
        }
    }
}
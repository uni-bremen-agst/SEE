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

using SEE.Game.Charts;
using Valve.VR;

namespace SEE.Controls.Actions
{
    public class XRChartAction : ChartAction
    {
        private readonly SteamVR_Action_Vector2 moveAction =
            SteamVR_Input.GetVector2Action(XRInput.DefaultActionSetName, XRInput.MoveActionName);

        private readonly SteamVR_Action_Boolean resetAction =
            SteamVR_Input.GetBooleanAction(XRInput.DefaultActionSetName, XRInput.ResetChartsName);

        private readonly SteamVR_Action_Boolean clickAction =
            SteamVR_Input.GetBooleanAction(XRInput.DefaultActionSetName, XRInput.ClickActionName);

        private readonly SteamVR_Action_Boolean createAction =
            SteamVR_Input.GetBooleanAction(XRInput.DefaultActionSetName, XRInput.CreateChartActionName);

        private bool _lastClick;

        private void Update()
        {
            if (resetAction.stateDown)
            {
                ChartManager.ResetPosition();
            }
            if (moveAction.axis.y != 0.0f)
            {
                move = moveAction.axis.y;
            }
            if (createAction.stateDown)
            {
                ChartManager.Instance.CreateChartVR();
            }

            clickDown = false;
            clickUp = false;

            if (!_lastClick && clickAction.state)
            {
                clickDown = true;
            }
            if (_lastClick && !clickAction.state)
            {
                clickUp = true;
            }
            _lastClick = clickAction.state;
        }
    }
}

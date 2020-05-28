using UnityEngine;
using Valve.VR;

namespace SEE.Controls.Devices
{
	public class VrChartControlsDevice : ChartControls
	{
		private readonly SteamVR_Action_Vector2 _moveAction =
			SteamVR_Input.GetVector2Action(DefaultActionSet, MoveActionName);

		public override bool Toggle => false;

		public override bool Select => false;

		public override Vector2 Move => _moveAction != null ? _moveAction.axis : Vector2.zero;
	}
}
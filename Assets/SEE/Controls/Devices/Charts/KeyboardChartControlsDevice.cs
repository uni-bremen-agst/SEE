using UnityEngine;

namespace SEE.Controls.Devices
{
	public class KeyboardChartControlsDevice : ChartControls
	{
		private const KeyCode ToggleKey = KeyCode.G;
		private const KeyCode SelectionKey = KeyCode.LeftControl;

		public override bool Toggle => Input.GetKeyDown(ToggleKey);

		public override bool Select =>
			Input.GetKeyDown(SelectionKey) || Input.GetKeyUp(SelectionKey);

		public override Vector2 Move => Vector2.zero;
	}
}
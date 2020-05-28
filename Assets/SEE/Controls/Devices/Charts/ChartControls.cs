using UnityEngine;

namespace SEE.Controls.Devices
{
	public abstract class ChartControls : InputDevice
	{
		public abstract bool Toggle { get; }

		public abstract bool Select { get; }

		public abstract Vector2 Move { get; }
	}
}
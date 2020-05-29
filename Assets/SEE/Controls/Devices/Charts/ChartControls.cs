using UnityEngine;

namespace SEE.Controls.Devices
{
	public abstract class ChartControls : InputDevice
	{
		public abstract bool Toggle { get; }

		public abstract bool Select { get; }

		public abstract Vector2 Move { get; }

		public abstract bool ResetCharts { get; }

		public abstract bool Click { get; }

		public abstract bool Create { get; }
	}
}
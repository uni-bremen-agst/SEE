using UnityEngine;

namespace SEE.Controls
{
	/// <summary>
	/// Abstract super class of all input devices. An input devices abstracts
	/// from the low-level details of the different kinds of controls which
	/// depend upon the environment such as keyboard&mouse, touch screen,
	/// gamepads, or virtual reality controllers. The input devices also
	/// hide the concrete implementation (input for virtual reality is
	/// derived from SteamVR, other data may be derived from Unity Input
	/// system or InControl). An input device also lifts the abstraction
	/// from low-level controls (e.g., a trigger is pressed) to higher-level
	/// actions (a throttle is activated to give gas).
	/// </summary>
	public abstract class InputDevice : MonoBehaviour
	{
		[Tooltip("Name of the device")] public string Name;

		/// <summary>
		/// Name of the action set defined by VR Steam Input.
		/// </summary>
		protected const string DefaultActionSet = "default";

		/// <summary>
		/// Name of the throttle action defined by VR Steam Input.
		/// </summary>
		protected const string ThrottleActionName = "Throttle";

		/// <summary>
		/// Name of the reset charts action defined by VR Steam Input
		/// </summary>
		protected const string ResetChartsName = "ResetCharts";

		/// <summary>
		/// Name of the create chart action defined by VR Steam Input.
		/// </summary>
		protected const string CreateChartActionName = "CreateChart";

		/// <summary>
		/// Name of the create chart action defined by VR Steam Input.
		/// </summary>
		protected const string ClickActionName = "InteractUI";

		/// <summary>
		/// Name of the move chart action defined by VR Steam Input.
		/// </summary>
		protected const string MoveActionName = "Move";

		/// <summary>
		/// Name of the mouse X axis as defined in the Unity Input Manager.
		/// </summary>
		protected const string MouseXActionName = "mouse x";

		/// <summary>
		/// Name of the mouse Y axis as defined in the Unity Input Manager.
		/// </summary>
		protected const string MouseYActionName = "mouse y";
	}
}
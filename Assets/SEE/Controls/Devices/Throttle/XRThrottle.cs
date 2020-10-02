using Valve.VR;

namespace SEE.Controls.Devices
{
	/// <summary>
	/// A throttle device retrieving data from a virtual-reality controller
	/// managed by SteamVR.
	/// </summary>
	public class XRThrottle : Throttle
	{
		private readonly SteamVR_Action_Single _throttleAction =
			SteamVR_Input.GetSingleAction(DefaultActionSetName, ThrottleActionName);

		/// <summary>
		/// Yields the value from the "Throttle" action as assigned by the user in SteamVR.
		/// </summary>
		public override float Value => _throttleAction != null ? _throttleAction.axis : 0;
	}
}
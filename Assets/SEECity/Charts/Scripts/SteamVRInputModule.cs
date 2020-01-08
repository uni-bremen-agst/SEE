using Valve.VR;

namespace Assets.SEECity.Charts.Scripts
{
	public class SteamVRInputModule : VRInputModule
	{
		public SteamVR_Input_Sources Source = SteamVR_Input_Sources.RightHand;
		public SteamVR_Action_Boolean Click;

		public override void Process()
		{
			base.Process();
			if (Click.GetStateDown(Source)) Press();
			if (Click.GetStateUp(Source)) Release();
		}
	}
}
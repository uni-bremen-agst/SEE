// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedType.Global
namespace InControl.NativeDeviceProfiles
{
	// @cond nodoc
	[Preserve, NativeInputDeviceProfile]
	public class HoriFightingEdgeArcadeStickMacNativeProfile : Xbox360DriverMacNativeProfile
	{
		public override void Define()
		{
			base.Define();

			DeviceName = "Hori Fighting Edge Arcade Stick";
			DeviceNotes = "Hori Fighting Edge Arcade Stick on Mac";

			Matchers = new[]
			{
				new InputDeviceMatcher
				{
					DriverType = InputDeviceDriverType.HID,
					VendorID = 0x24c6,
					ProductID = 0x5503,
				},
			};
		}
	}

	// @endcond
}

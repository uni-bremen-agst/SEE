// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedType.Global
namespace InControl.NativeDeviceProfiles
{
	// @cond nodoc
	[Preserve, NativeInputDeviceProfile]
	public class HarmonixKeyboardMacNativeProfile : Xbox360DriverMacNativeProfile
	{
		public override void Define()
		{
			base.Define();

			DeviceName = "Harmonix Keyboard";
			DeviceNotes = "Harmonix Keyboard on Mac";

			Matchers = new[]
			{
				new InputDeviceMatcher
				{
					VendorID = 0x1bad,
					ProductID = 0x1338,
				},
			};
		}
	}

	// @endcond
}

// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedType.Global
namespace InControl.NativeDeviceProfiles
{
	// @cond nodoc
	[Preserve, NativeInputDeviceProfile]
	public class ThrustmasterTMXMacNativeProfile : Xbox360DriverMacNativeProfile
	{
		public override void Define()
		{
			base.Define();

			DeviceName = "Thrustmaster TMX";
			DeviceNotes = "Thrustmaster TMX on Mac";

			Matchers = new[]
			{
				new InputDeviceMatcher
				{
					VendorID = 0x044f,
					ProductID = 0xb67e,
				},
			};
		}
	}

	// @endcond
}

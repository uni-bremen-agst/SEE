// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedType.Global
namespace InControl.NativeDeviceProfiles
{
	// @cond nodoc
	[Preserve, NativeInputDeviceProfile]
	public class LogitechThunderpadMacNativeProfile : Xbox360DriverMacNativeProfile
	{
		public override void Define()
		{
			base.Define();

			DeviceName = "Logitech Thunderpad";
			DeviceNotes = "Logitech Thunderpad on Mac";

			Matchers = new[]
			{
				new InputDeviceMatcher
				{
					VendorID = 0x046d,
					ProductID = 0xca88,
				},
			};
		}
	}

	// @endcond
}

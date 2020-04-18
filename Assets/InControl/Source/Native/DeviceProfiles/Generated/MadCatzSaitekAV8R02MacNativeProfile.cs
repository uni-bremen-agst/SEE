// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedType.Global
namespace InControl.NativeDeviceProfiles
{
	// @cond nodoc
	[Preserve, NativeInputDeviceProfile]
	public class MadCatzSaitekAV8R02MacNativeProfile : Xbox360DriverMacNativeProfile
	{
		public override void Define()
		{
			base.Define();

			DeviceName = "Mad Catz Saitek AV8R02";
			DeviceNotes = "Mad Catz Saitek AV8R02 on Mac";

			Matchers = new[]
			{
				new InputDeviceMatcher
				{
					VendorID = 0x0738,
					ProductID = 0xcb29,
				},
			};
		}
	}

	// @endcond
}

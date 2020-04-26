// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedType.Global
namespace InControl.NativeDeviceProfiles
{
	// @cond nodoc
	[Preserve, NativeInputDeviceProfile]
	public class MicrosoftXbox360ControllerMacNativeProfile : Xbox360DriverMacNativeProfile
	{
		public override void Define()
		{
			base.Define();

			DeviceName = "Microsoft Xbox 360 Controller";
			DeviceNotes = "Microsoft Xbox 360 Controller on Mac";

			Matchers = new[]
			{
				new InputDeviceMatcher
				{
					VendorID = 0x045e,
					ProductID = 0x028e,
				},
				new InputDeviceMatcher
				{
					VendorID = 0x045e,
					ProductID = 0x028f,
				},
				new InputDeviceMatcher
				{
					VendorID = 0x0e6f,
					ProductID = 0x0133,
				},
				new InputDeviceMatcher
				{
					VendorID = 0x0e6f,
					ProductID = 0xf701,
				},
				new InputDeviceMatcher
				{
					VendorID = 0x0e6f,
					ProductID = 0x02a0,
				},
				new InputDeviceMatcher
				{
					VendorID = 0x0e6f,
					ProductID = 0xf501,
				},
				new InputDeviceMatcher
				{
					VendorID = 0x045e,
					ProductID = 0x02a0,
				},
			};
		}
	}

	// @endcond
}

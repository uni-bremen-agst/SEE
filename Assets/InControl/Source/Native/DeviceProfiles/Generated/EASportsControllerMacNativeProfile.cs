// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedType.Global
namespace InControl.NativeDeviceProfiles
{
	// @cond nodoc
	[Preserve, NativeInputDeviceProfile]
	public class EASportsControllerMacNativeProfile : Xbox360DriverMacNativeProfile
	{
		public override void Define()
		{
			base.Define();

			DeviceName = "EA Sports Controller";
			DeviceNotes = "EA Sports Controller on Mac";

			Matchers = new[]
			{
				new InputDeviceMatcher
				{
					VendorID = 0x0e6f,
					ProductID = 0x0131,
				},
			};
		}
	}

	// @endcond
}

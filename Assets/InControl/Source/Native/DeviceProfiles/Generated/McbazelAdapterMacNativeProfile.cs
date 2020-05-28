// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedType.Global
namespace InControl.NativeDeviceProfiles
{
	// @cond nodoc
	[Preserve, NativeInputDeviceProfile]
	public class McbazelAdapterMacNativeProfile : Xbox360DriverMacNativeProfile
	{
		public override void Define()
		{
			base.Define();

			DeviceName = "Mcbazel Adapter";
			DeviceNotes = "Mcbazel Adapter on Mac";

			Matchers = new[]
			{
				new InputDeviceMatcher
				{
					VendorID = 0x0e6f,
					ProductID = 0x028e,
				},
			};
		}
	}

	// @endcond
}

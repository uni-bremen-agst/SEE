// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedType.Global
namespace InControl.NativeDeviceProfiles
{
	// @cond nodoc
	[Preserve, NativeInputDeviceProfile]
	public class RockBandGuitarMacNativeProfile : Xbox360DriverMacNativeProfile
	{
		public override void Define()
		{
			base.Define();

			DeviceName = "Rock Band Guitar";
			DeviceNotes = "Rock Band Guitar on Mac";

			Matchers = new[]
			{
				new InputDeviceMatcher
				{
					VendorID = 0x1bad,
					ProductID = 0x0002,
				},
			};
		}
	}

	// @endcond
}

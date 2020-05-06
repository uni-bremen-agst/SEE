// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedType.Global
namespace InControl.NativeDeviceProfiles
{
	// @cond nodoc
	[Preserve, NativeInputDeviceProfile]
	public class HoriRealArcadeProEXMacNativeProfile : Xbox360DriverMacNativeProfile
	{
		public override void Define()
		{
			base.Define();

			DeviceName = "Hori Real Arcade Pro EX";
			DeviceNotes = "Hori Real Arcade Pro EX on Mac";

			Matchers = new[]
			{
				new InputDeviceMatcher
				{
					VendorID = 0x1bad,
					ProductID = 0xf504,
				},
			};
		}
	}

	// @endcond
}

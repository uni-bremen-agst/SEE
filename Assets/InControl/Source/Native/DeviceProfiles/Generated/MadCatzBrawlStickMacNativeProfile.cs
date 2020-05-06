// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedType.Global
namespace InControl.NativeDeviceProfiles
{
	// @cond nodoc
	[Preserve, NativeInputDeviceProfile]
	public class MadCatzBrawlStickMacNativeProfile : Xbox360DriverMacNativeProfile
	{
		public override void Define()
		{
			base.Define();

			DeviceName = "Mad Catz Brawl Stick";
			DeviceNotes = "Mad Catz Brawl Stick on Mac";

			Matchers = new[]
			{
				new InputDeviceMatcher
				{
					VendorID = 0x1bad,
					ProductID = 0xf019,
				},
			};
		}
	}

	// @endcond
}

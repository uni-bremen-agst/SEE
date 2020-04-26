// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedType.Global
namespace InControl.NativeDeviceProfiles
{
	// @cond nodoc
	[Preserve, NativeInputDeviceProfile]
	public class FusionXboxOneControllerMacNativeProfile : XboxOneDriverMacNativeProfile
	{
		public override void Define()
		{
			base.Define();

			DeviceName = "Fusion Xbox One Controller";
			DeviceNotes = "Fusion Xbox One Controller on Mac";

			Matchers = new[]
			{
				new InputDeviceMatcher
				{
					VendorID = 0x24c6,
					ProductID = 0x551a,
				},
				new InputDeviceMatcher
				{
					VendorID = 0x24c6,
					ProductID = 0x561a,
				},
			};
		}
	}

	// @endcond
}

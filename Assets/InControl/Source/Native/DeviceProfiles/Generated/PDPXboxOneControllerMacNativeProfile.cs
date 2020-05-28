// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedType.Global
namespace InControl.NativeDeviceProfiles
{
	// @cond nodoc
	[Preserve, NativeInputDeviceProfile]
	public class PDPXboxOneControllerMacNativeProfile : XboxOneDriverMacNativeProfile
	{
		public override void Define()
		{
			base.Define();

			DeviceName = "PDP Xbox One Controller";
			DeviceNotes = "PDP Xbox One Controller on Mac";

			Matchers = new[]
			{
				new InputDeviceMatcher
				{
					VendorID = 0x0e6f,
					ProductID = 0x02a4,
				},
				new InputDeviceMatcher
				{
					VendorID = 0x0e6f,
					ProductID = 0x02cb,
				},
				new InputDeviceMatcher
				{
					VendorID = 0x0e6f,
					ProductID = 0x013a,
				},
				new InputDeviceMatcher
				{
					VendorID = 0x0e6f,
					ProductID = 0x0162,
				},
				new InputDeviceMatcher
				{
					VendorID = 0x24c6,
					ProductID = 0x561a,
				},
				new InputDeviceMatcher
				{
					VendorID = 0x0e6f,
					ProductID = 0x0161,
				},
				new InputDeviceMatcher
				{
					VendorID = 0x0e6f,
					ProductID = 0x0163,
				},
				new InputDeviceMatcher
				{
					VendorID = 0x0e6f,
					ProductID = 0x02ab,
				},
				new InputDeviceMatcher
				{
					VendorID = 0x0e6f,
					ProductID = 0x0160,
				},
				new InputDeviceMatcher
				{
					VendorID = 0x0e6f,
					ProductID = 0x02a8,
				},
				new InputDeviceMatcher
				{
					VendorID = 0x0e6f,
					ProductID = 0x02a2,
				},
				new InputDeviceMatcher
				{
					VendorID = 0x0e6f,
					ProductID = 0x015b,
				},
				new InputDeviceMatcher
				{
					VendorID = 0x0e6f,
					ProductID = 0x02a5,
				},
				new InputDeviceMatcher
				{
					VendorID = 0x0e6f,
					ProductID = 0x02ad,
				},
				new InputDeviceMatcher
				{
					VendorID = 0x0e6f,
					ProductID = 0x02c0,
				},
				new InputDeviceMatcher
				{
					VendorID = 0x0e6f,
					ProductID = 0x02a7,
				},
				new InputDeviceMatcher
				{
					VendorID = 0x0e6f,
					ProductID = 0x02a6,
				},
			};
		}
	}

	// @endcond
}

// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedType.Global
namespace InControl.UnityDeviceProfiles
{
	using System;


	// @cond nodoc
	[Preserve] [UnityInputDeviceProfile]
	public class EightBitdoNES30ProMacUnityProfile : InputDeviceProfile
	{
		public override void Define()
		{
			base.Define();

			DeviceName = "8Bitdo NES30 Pro Controller";
			DeviceNotes = "8Bitdo NES30 Pro Controller on Mac";
			// Link = "https://www.amazon.com/gp/product/B01MXLEYZ8";

			DeviceClass = InputDeviceClass.Controller;
			DeviceStyle = InputDeviceStyle.NintendoNES;

			IncludePlatforms = new[]
			{
				"OS X"
			};

			Matchers = new[]
			{
				new InputDeviceMatcher { NameLiteral = "Unknown 8Bitdo NES30 Pro" },
				new InputDeviceMatcher { NameLiteral = "8Bitdo NES30 Pro   8Bitdo NES30 Pro" }
			};

			ButtonMappings = new[]
			{
				new InputControlMapping
				{
					Name = "A",
					Target = InputControlType.Action2,
					Source = Button( 0 ),
				},
				new InputControlMapping
				{
					Name = "B",
					Target = InputControlType.Action1,
					Source = Button( 1 ),
				},
				new InputControlMapping
				{
					Name = "X",
					Target = InputControlType.Action4,
					Source = Button( 3 ),
				},
				new InputControlMapping
				{
					Name = "Y",
					Target = InputControlType.Action3,
					Source = Button( 4 ),
				},
				new InputControlMapping
				{
					Name = "L1",
					Target = InputControlType.LeftBumper,
					Source = Button( 6 ),
				},
				new InputControlMapping
				{
					Name = "R1",
					Target = InputControlType.RightBumper,
					Source = Button( 7 ),
				},
				new InputControlMapping
				{
					Name = "L2",
					Target = InputControlType.LeftTrigger,
					Source = Button( 8 ),
				},
				new InputControlMapping
				{
					Name = "R2",
					Target = InputControlType.RightTrigger,
					Source = Button( 9 ),
				},
				new InputControlMapping
				{
					Name = "Select",
					Target = InputControlType.Select,
					Source = Button( 10 ),
				},
				new InputControlMapping
				{
					Name = "Start",
					Target = InputControlType.Start,
					Source = Button( 11 ),
				},
				new InputControlMapping
				{
					Name = "Left Stick Button",
					Target = InputControlType.LeftStickButton,
					Source = Button( 13 ),
				},
				new InputControlMapping
				{
					Name = "Right Stick Button",
					Target = InputControlType.RightStickButton,
					Source = Button( 14 ),
				},
			};

			AnalogMappings = new[]
			{
				new InputControlMapping
				{
					Name = "Left Stick Left",
					Target = InputControlType.LeftStickLeft,
					Source = Analog( 0 ),
					SourceRange = InputRangeType.ZeroToMinusOne,
					TargetRange = InputRangeType.ZeroToOne,
				},
				new InputControlMapping
				{
					Name = "Left Stick Right",
					Target = InputControlType.LeftStickRight,
					Source = Analog( 0 ),
					SourceRange = InputRangeType.ZeroToOne,
					TargetRange = InputRangeType.ZeroToOne,
				},
				new InputControlMapping
				{
					Name = "Left Stick Up",
					Target = InputControlType.LeftStickUp,
					Source = Analog( 1 ),
					SourceRange = InputRangeType.ZeroToMinusOne,
					TargetRange = InputRangeType.ZeroToOne,
				},
				new InputControlMapping
				{
					Name = "Left Stick Down",
					Target = InputControlType.LeftStickDown,
					Source = Analog( 1 ),
					SourceRange = InputRangeType.ZeroToOne,
					TargetRange = InputRangeType.ZeroToOne,
				},
				new InputControlMapping
				{
					Name = "Right Stick Left",
					Target = InputControlType.RightStickLeft,
					Source = Analog( 2 ),
					SourceRange = InputRangeType.ZeroToMinusOne,
					TargetRange = InputRangeType.ZeroToOne,
				},
				new InputControlMapping
				{
					Name = "Right Stick Right",
					Target = InputControlType.RightStickRight,
					Source = Analog( 2 ),
					SourceRange = InputRangeType.ZeroToOne,
					TargetRange = InputRangeType.ZeroToOne,
				},
				new InputControlMapping
				{
					Name = "Right Stick Up",
					Target = InputControlType.RightStickUp,
					Source = Analog( 3 ),
					SourceRange = InputRangeType.ZeroToMinusOne,
					TargetRange = InputRangeType.ZeroToOne,
				},
				new InputControlMapping
				{
					Name = "Right Stick Down",
					Target = InputControlType.RightStickDown,
					Source = Analog( 3 ),
					SourceRange = InputRangeType.ZeroToOne,
					TargetRange = InputRangeType.ZeroToOne,
				},
				new InputControlMapping
				{
					Name = "DPad Left",
					Target = InputControlType.DPadLeft,
					Source = Analog( 4 ),
					SourceRange = InputRangeType.ZeroToMinusOne,
					TargetRange = InputRangeType.ZeroToOne,
				},
				new InputControlMapping
				{
					Name = "DPad Right",
					Target = InputControlType.DPadRight,
					Source = Analog( 4 ),
					SourceRange = InputRangeType.ZeroToOne,
					TargetRange = InputRangeType.ZeroToOne,
				},
				new InputControlMapping
				{
					Name = "DPad Up",
					Target = InputControlType.DPadUp,
					Source = Analog( 5 ),
					SourceRange = InputRangeType.ZeroToMinusOne,
					TargetRange = InputRangeType.ZeroToOne,
				},
				new InputControlMapping
				{
					Name = "DPad Down",
					Target = InputControlType.DPadDown,
					Source = Analog( 5 ),
					SourceRange = InputRangeType.ZeroToOne,
					TargetRange = InputRangeType.ZeroToOne,
				},
			};
		}
	}

	// @endcond
}

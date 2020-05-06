// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedType.Global
namespace InControl.UnityDeviceProfiles
{
	// @cond nodoc
	[Preserve] [UnityInputDeviceProfile]
	public class LogitechWingManWindowsUnityProfile : InputDeviceProfile
	{
		public override void Define()
		{
			base.Define();

			DeviceName = "Logitech WingMan Controller";
			DeviceNotes = "Logitech WingMan Controller on Windows";

			DeviceClass = InputDeviceClass.FlightStick;

			IncludePlatforms = new[]
			{
				"Windows"
			};

			Matchers = new[] { new InputDeviceMatcher { NameLiteral = "WingMan Cordless Gamepad" } };

			ButtonMappings = new[]
			{
				new InputControlMapping
				{
					Name = "A",
					Target = InputControlType.Action1,
					Source = Button( 1 )
				},
				new InputControlMapping
				{
					Name = "B",
					Target = InputControlType.Action2,
					Source = Button( 2 )
				},
				new InputControlMapping
				{
					Name = "C",
					Target = InputControlType.Action5,
					Source = Button( 2 )
				},
				new InputControlMapping
				{
					Name = "X",
					Target = InputControlType.Action3,
					Source = Button( 4 )
				},
				new InputControlMapping
				{
					Name = "Y",
					Target = InputControlType.Action4,
					Source = Button( 5 )
				},
				new InputControlMapping
				{
					Name = "Z",
					Target = InputControlType.Action6,
					Source = Button( 6 )
				},
				new InputControlMapping
				{
					Name = "Left Bumper",
					Target = InputControlType.LeftBumper,
					Source = Button( 7 )
				},
				new InputControlMapping
				{
					Name = "Right Bumper",
					Target = InputControlType.RightBumper,
					Source = Button( 8 )
				},
				new InputControlMapping
				{
					Name = "Left Trigger",
					Target = InputControlType.LeftTrigger,
					Source = Button( 10 )
				},
				new InputControlMapping
				{
					Name = "Right Trigger",
					Target = InputControlType.RightTrigger,
					Source = Button( 11 ),
				},
				new InputControlMapping
				{
					Name = "Start",
					Target = InputControlType.Start,
					Source = Button( 9 )
				}
			};

			AnalogMappings = new[]
			{
				LeftStickLeftMapping( 0 ),
				LeftStickRightMapping( 0 ),
				LeftStickUpMapping( 1 ),
				LeftStickDownMapping( 1 ),

				RightStickLeftMapping( 3 ),
				RightStickRightMapping( 3 ),
				RightStickUpMapping( 4 ),
				RightStickDownMapping( 4 ),

				DPadLeftMapping( 5 ),
				DPadRightMapping( 5 ),
				DPadUpMapping( 6 ),
				DPadDownMapping( 6 ),

				new InputControlMapping
				{
					Name = "Throttle",
					Target = InputControlType.Analog0,
					Source = Analog( 2 )
				}
			};
		}
	}

	// @endcond
}

// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedType.Global
namespace InControl.UnityDeviceProfiles
{
	// @cond nodoc
	[Preserve] [UnityInputDeviceProfile]
	public class OuyaLinuxUnityProfile : InputDeviceProfile
	{
		public override void Define()
		{
			base.Define();

			DeviceName = "OUYA Controller";
			DeviceNotes = "OUYA Controller on Linux";

			DeviceClass = InputDeviceClass.Controller;
			DeviceStyle = InputDeviceStyle.Ouya;

			IncludePlatforms = new[]
			{
				"Linux"
			};

			Matchers = new[] { new InputDeviceMatcher { NameLiteral = "OUYA Game Controller" } };

			MaxUnityVersion = new VersionInfo( 4, 9, 0, 0 );

			LowerDeadZone = 0.3f;

			ButtonMappings = new[]
			{
				new InputControlMapping
				{
					Name = "O",
					Target = InputControlType.Action1,
					Source = Button( 0 )
				},
				new InputControlMapping
				{
					Name = "A",
					Target = InputControlType.Action2,
					Source = Button( 3 )
				},
				new InputControlMapping
				{
					Name = "U",
					Target = InputControlType.Action3,
					Source = Button( 1 )
				},
				new InputControlMapping
				{
					Name = "Y",
					Target = InputControlType.Action4,
					Source = Button( 2 )
				},
				new InputControlMapping
				{
					Name = "Left Bumper",
					Target = InputControlType.LeftBumper,
					Source = Button( 4 )
				},
				new InputControlMapping
				{
					Name = "Right Bumper",
					Target = InputControlType.RightBumper,
					Source = Button( 5 )
				},
				new InputControlMapping
				{
					Name = "Left Stick Button",
					Target = InputControlType.LeftStickButton,
					Source = Button( 6 )
				},
				new InputControlMapping
				{
					Name = "Right Stick Button",
					Target = InputControlType.RightStickButton,
					Source = Button( 7 )
				},
				new InputControlMapping
				{
					Name = "System",
					Target = InputControlType.System,
					Source = MenuKey
				},
				// new InputControlMapping
				// {
				// 	Name = "TouchPad Button",
				// 	Target = InputControlType.TouchPadButton,
				// 	Source = MouseButton0
				// },
				new InputControlMapping
				{
					Name = "DPad Left",
					Target = InputControlType.DPadLeft,
					Source = Button( 10 ),
				},
				new InputControlMapping
				{
					Name = "DPad Right",
					Target = InputControlType.DPadRight,
					Source = Button( 11 ),
				},
				new InputControlMapping
				{
					Name = "DPad Up",
					Target = InputControlType.DPadUp,
					Source = Button( 8 ),
				},
				new InputControlMapping
				{
					Name = "DPad Down",
					Target = InputControlType.DPadDown,
					Source = Button( 9 ),
				},
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

				new InputControlMapping
				{
					Name = "Left Trigger",
					Target = InputControlType.LeftTrigger,
					Source = Analog( 2 )
				},
				new InputControlMapping
				{
					Name = "Right Trigger",
					Target = InputControlType.RightTrigger,
					Source = Analog( 5 )
				},

				// new InputControlMapping
				// {
				// 	Name = "TouchPad X Axis",
				// 	Target = InputControlType.TouchPadXAxis,
				// 	Source = MouseXAxis,
				// 	Raw = true
				// },
				// new InputControlMapping
				// {
				// 	Name = "TouchPad Y Axis",
				// 	Target = InputControlType.TouchPadYAxis,
				// 	Source = MouseYAxis,
				// 	Raw = true
				// }
			};
		}
	}

	// @endcond
}

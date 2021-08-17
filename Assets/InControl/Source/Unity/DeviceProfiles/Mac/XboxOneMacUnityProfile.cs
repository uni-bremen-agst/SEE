// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedType.Global
namespace InControl.UnityDeviceProfiles
{
	// @cond nodoc
	[Preserve] [UnityInputDeviceProfile]
	public class XboxOneMacUnityProfile : InputDeviceProfile
	{
		public override void Define()
		{
			base.Define();

			DeviceName = "Xbox One Controller";
			DeviceNotes = "Xbox One Controller on OSX";

			DeviceClass = InputDeviceClass.Controller;
			DeviceStyle = InputDeviceStyle.XboxOne;

			IncludePlatforms = new[]
			{
				"OS X"
			};

			Matchers = new[] { new InputDeviceMatcher { NameLiteral = "Microsoft Xbox One Wired Controller" } };

			ButtonMappings = new[]
			{
				new InputControlMapping
				{
					Name = "A",
					Target = InputControlType.Action1,
					Source = Button( 16 )
				},
				new InputControlMapping
				{
					Name = "B",
					Target = InputControlType.Action2,
					Source = Button( 17 )
				},
				new InputControlMapping
				{
					Name = "X",
					Target = InputControlType.Action3,
					Source = Button( 18 )
				},
				new InputControlMapping
				{
					Name = "Y",
					Target = InputControlType.Action4,
					Source = Button( 19 )
				},
				new InputControlMapping
				{
					Name = "DPad Up",
					Target = InputControlType.DPadUp,
					Source = Button( 5 )
				},
				new InputControlMapping
				{
					Name = "DPad Down",
					Target = InputControlType.DPadDown,
					Source = Button( 6 ),
				},
				new InputControlMapping
				{
					Name = "DPad Left",
					Target = InputControlType.DPadLeft,
					Source = Button( 7 )
				},
				new InputControlMapping
				{
					Name = "DPad Right",
					Target = InputControlType.DPadRight,
					Source = Button( 8 )
				},
				new InputControlMapping
				{
					Name = "Left Bumper",
					Target = InputControlType.LeftBumper,
					Source = Button( 13 )
				},
				new InputControlMapping
				{
					Name = "Right Bumper",
					Target = InputControlType.RightBumper,
					Source = Button( 14 )
				},
				new InputControlMapping
				{
					Name = "Left Stick Button",
					Target = InputControlType.LeftStickButton,
					Source = Button( 11 )
				},
				new InputControlMapping
				{
					Name = "Right Stick Button",
					Target = InputControlType.RightStickButton,
					Source = Button( 12 )
				},
				new InputControlMapping
				{
					Name = "View",
					Target = InputControlType.View,
					Source = Button( 10 )
				},
				new InputControlMapping
				{
					Name = "Menu",
					Target = InputControlType.Menu,
					Source = Button( 9 )
				},
				new InputControlMapping
				{
					Name = "Guide",
					Target = InputControlType.Guide,
					Source = Button( 15 )
				}
			};

			AnalogMappings = new[]
			{
				LeftStickLeftMapping( 0 ),
				LeftStickRightMapping( 0 ),
				LeftStickUpMapping( 1 ),
				LeftStickDownMapping( 1 ),

				RightStickLeftMapping( 2 ),
				RightStickRightMapping( 2 ),
				RightStickUpMapping( 3 ),
				RightStickDownMapping( 3 ),

				LeftTriggerMapping( 4 ),
				RightTriggerMapping( 5 ),
			};
		}
	}

	// @endcond
}

// Don't uncomment the line below; it's for development/testing only.
//#define UNITY_XBOXONE


#if UNITY_XBOXONE
// If you're getting a compilation error about the type or namespace 'Gamepad' being missing,
// you need to install the Xbox One native plugin which can be found in the Unity Xbox Forums:
// > Build Downloads (Sticky Topic)
// > Latest Builds
// > Unity's Xbox One Native Plugins
// Gamepad.dll can be found in Binaries\Native\Variations\Durango_Release
// GamepadImport.dll can be found in Binaries\Managed\Variations\AnyCPU_Release
// Put both in Assets/Plugins/XboxOne.
// Use Unity's Plugin Inspector (when you click on the plugin files in the Unity editor)
// to make sure Gamepad.dll is set to only be activated on Xbox One and
// that GamepadImport.dll is enabled for all platforms, or at least for
// the Unity editor on the platform you're developing.
using Gamepad;
#endif


namespace InControl
{
	using UnityEngine;


	public class XboxOneInputDevice : InputDevice
	{
		const uint AnalogLeftStickX = 0;
		const uint AnalogLeftStickY = 1;

		const uint AnalogRightStickX = 3;
		const uint AnalogRightStickY = 4;

		const uint AnalogLeftTrigger = 8;
		const uint AnalogRightTrigger = 9;

		const float LowerDeadZone = 0.2f;
		const float UpperDeadZone = 0.9f;

		internal uint JoystickId { get; private set; }

		public ulong ControllerId { get; private set; }


		public XboxOneInputDevice( uint joystickId )
			: base( "Xbox One Controller" )
		{
			JoystickId = joystickId;
			SortOrder = (int) joystickId;
			Meta = "Xbox One Device #" + joystickId;

			DeviceClass = InputDeviceClass.Controller;
			DeviceStyle = InputDeviceStyle.XboxOne;

			CacheAnalogAxisNames();

#if UNITY_XBOXONE
			ControllerId = XboxOneInput.GetControllerId( joystickId );
#endif

			AddControl( InputControlType.LeftStickLeft, "Left Stick Left", LowerDeadZone, UpperDeadZone );
			AddControl( InputControlType.LeftStickRight, "Left Stick Right", LowerDeadZone, UpperDeadZone );
			AddControl( InputControlType.LeftStickUp, "Left Stick Up", LowerDeadZone, UpperDeadZone );
			AddControl( InputControlType.LeftStickDown, "Left Stick Down", LowerDeadZone, UpperDeadZone );

			AddControl( InputControlType.RightStickLeft, "Right Stick Left", LowerDeadZone, UpperDeadZone );
			AddControl( InputControlType.RightStickRight, "Right Stick Right", LowerDeadZone, UpperDeadZone );
			AddControl( InputControlType.RightStickUp, "Right Stick Up", LowerDeadZone, UpperDeadZone );
			AddControl( InputControlType.RightStickDown, "Right Stick Down", LowerDeadZone, UpperDeadZone );

			AddControl( InputControlType.LeftTrigger, "Left Trigger", LowerDeadZone, UpperDeadZone );
			AddControl( InputControlType.RightTrigger, "Right Trigger", LowerDeadZone, UpperDeadZone );

			AddControl( InputControlType.DPadUp, "DPad Up", LowerDeadZone, UpperDeadZone );
			AddControl( InputControlType.DPadDown, "DPad Down", LowerDeadZone, UpperDeadZone );
			AddControl( InputControlType.DPadLeft, "DPad Left", LowerDeadZone, UpperDeadZone );
			AddControl( InputControlType.DPadRight, "DPad Right", LowerDeadZone, UpperDeadZone );

			AddControl( InputControlType.Action1, "A" );
			AddControl( InputControlType.Action2, "B" );
			AddControl( InputControlType.Action3, "X" );
			AddControl( InputControlType.Action4, "Y" );

			AddControl( InputControlType.LeftBumper, "Left Bumper" );
			AddControl( InputControlType.RightBumper, "Right Bumper" );

			AddControl( InputControlType.LeftStickButton, "Left Stick Button" );
			AddControl( InputControlType.RightStickButton, "Right Stick Button" );

			AddControl( InputControlType.View, "View" );
			AddControl( InputControlType.Menu, "Menu" );
		}

		public override void Update( ulong updateTick, float deltaTime )
		{
#if UNITY_XBOXONE
			ControllerId = XboxOneInput.GetControllerId( JoystickId );

			var lsv = new Vector2( GetAnalogValue( AnalogLeftStickX ), -GetAnalogValue( AnalogLeftStickY ) );
			UpdateLeftStickWithValue( lsv, updateTick, deltaTime );

			var rsv = new Vector2( GetAnalogValue( AnalogRightStickX ), -GetAnalogValue( AnalogRightStickY ) );
			UpdateRightStickWithValue( rsv, updateTick, deltaTime );

			UpdateWithValue( InputControlType.LeftTrigger, GetAnalogValue( AnalogLeftTrigger ), updateTick, deltaTime );
			UpdateWithValue( InputControlType.RightTrigger, GetAnalogValue( AnalogRightTrigger ), updateTick, deltaTime );

			UpdateWithState( InputControlType.DPadUp, GetButtonState( XboxOneKeyCode.GamepadButtonDPadUp ), updateTick, deltaTime );
			UpdateWithState( InputControlType.DPadDown, GetButtonState( XboxOneKeyCode.GamepadButtonDPadDown ), updateTick, deltaTime );
			UpdateWithState( InputControlType.DPadLeft, GetButtonState( XboxOneKeyCode.GamepadButtonDPadLeft ), updateTick, deltaTime );
			UpdateWithState( InputControlType.DPadRight, GetButtonState( XboxOneKeyCode.GamepadButtonDPadRight ), updateTick, deltaTime );

			UpdateWithState( InputControlType.Action1, GetButtonState( XboxOneKeyCode.GamepadButtonA ), updateTick, deltaTime );
			UpdateWithState( InputControlType.Action2, GetButtonState( XboxOneKeyCode.GamepadButtonB ), updateTick, deltaTime );
			UpdateWithState( InputControlType.Action3, GetButtonState( XboxOneKeyCode.GamepadButtonX ), updateTick, deltaTime );
			UpdateWithState( InputControlType.Action4, GetButtonState( XboxOneKeyCode.GamepadButtonY ), updateTick, deltaTime );

			UpdateWithState( InputControlType.LeftBumper, GetButtonState( XboxOneKeyCode.GamepadButtonLeftShoulder ), updateTick, deltaTime );
			UpdateWithState( InputControlType.RightBumper, GetButtonState( XboxOneKeyCode.GamepadButtonRightShoulder ), updateTick, deltaTime );

			UpdateWithState( InputControlType.LeftStickButton, GetButtonState( XboxOneKeyCode.GamepadButtonLeftThumbstick ), updateTick, deltaTime );
			UpdateWithState( InputControlType.RightStickButton, GetButtonState( XboxOneKeyCode.GamepadButtonRightThumbstick ), updateTick, deltaTime );

			UpdateWithState( InputControlType.View, GetButtonState( XboxOneKeyCode.GamepadButtonView ), updateTick, deltaTime );
			UpdateWithState( InputControlType.Menu, GetButtonState( XboxOneKeyCode.GamepadButtonMenu ), updateTick, deltaTime );

			Commit( updateTick, deltaTime );
#endif
		}


#if UNITY_XBOXONE
		XboxOneKeyCode GetButtonKeyCode( XboxOneKeyCode keyCode )
		{
			const int offset = XboxOneKeyCode.Gamepad2ButtonA - XboxOneKeyCode.Gamepad1ButtonA;
			return (XboxOneKeyCode) ((int) keyCode + JoystickId * offset);
		}


		bool GetButtonState( XboxOneKeyCode keyCode )
		{
			return XboxOneInput.GetKey( GetButtonKeyCode( keyCode ) );
		}


		float GetAnalogValue( uint analogId )
		{
			return Input.GetAxisRaw( AnalogAxisNameForId( analogId ) );
		}
#endif


		public bool IsConnected
		{
			get
			{
#if UNITY_XBOXONE
				return XboxOneInput.IsGamepadActive( JoystickId );
#else
				return false;
#endif
			}
		}


		public override void Vibrate( float leftMotor, float rightMotor )
		{
#if UNITY_XBOXONE
			GamepadPlugin.SetGamepadVibration(
				ControllerId,
				Mathf.Clamp01( leftMotor ),
				Mathf.Clamp01( rightMotor ),
				0, 0
			);
#endif
		}


		public void Vibrate( float leftMotor, float rightMotor, float leftTrigger, float rightTrigger )
		{
#if UNITY_XBOXONE
			GamepadPlugin.SetGamepadVibration(
				ControllerId,
				Mathf.Clamp01( leftMotor ),
				Mathf.Clamp01( rightMotor ),
				Mathf.Clamp01( leftTrigger ),
				Mathf.Clamp01( rightTrigger )
			);
#endif
		}


		string[] analogAxisNameForId;
		string AnalogAxisNameForId( uint analogId )
		{
			return analogAxisNameForId[analogId];
		}


		void CacheAnalogAxisNameForId( uint analogId )
		{
			analogAxisNameForId[analogId] = "joystick " + JoystickId + " analog " + analogId;
		}


		void CacheAnalogAxisNames()
		{
			analogAxisNameForId = new string[16];
			CacheAnalogAxisNameForId( AnalogLeftStickX );
			CacheAnalogAxisNameForId( AnalogLeftStickY );
			CacheAnalogAxisNameForId( AnalogRightStickX );
			CacheAnalogAxisNameForId( AnalogRightStickY );
			CacheAnalogAxisNameForId( AnalogLeftTrigger );
			CacheAnalogAxisNameForId( AnalogRightTrigger );
		}
	}
}


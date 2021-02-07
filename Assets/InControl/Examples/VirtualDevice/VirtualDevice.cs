namespace VirtualDeviceExample
{
	using InControl;
	using UnityEngine;


	// An example of how to map keyboard/mouse input (or anything else) to a virtual device.
	//
	public class VirtualDevice : InputDevice
	{
		const float Sensitivity = 0.1f;
		const float MouseScale = 0.05f;

		// To store keyboard x, y for smoothing.
		float kx, ky;

		// To store mouse x, y for smoothing.
		float mx, my;


		public VirtualDevice()
			: base( "Virtual Controller" )
		{
			// We need to add the controls we want to emulate here.
			// For this example we'll only have analog sticks and four action buttons.

			AddControl( InputControlType.LeftStickLeft, "Left Stick Left" );
			AddControl( InputControlType.LeftStickRight, "Left Stick Right" );
			AddControl( InputControlType.LeftStickUp, "Left Stick Up" );
			AddControl( InputControlType.LeftStickDown, "Left Stick Down" );

			AddControl( InputControlType.RightStickLeft, "Right Stick Left" );
			AddControl( InputControlType.RightStickRight, "Right Stick Right" );
			AddControl( InputControlType.RightStickUp, "Right Stick Up" );
			AddControl( InputControlType.RightStickDown, "Right Stick Down" );

			AddControl( InputControlType.Action1, "A" );
			AddControl( InputControlType.Action2, "B" );
			AddControl( InputControlType.Action3, "X" );
			AddControl( InputControlType.Action4, "Y" );
		}


		// This method will be called by the input manager every update tick.
		// You are expected to update control states where appropriate passing
		// through the updateTick and deltaTime unmodified.
		//
		public override void Update( ulong updateTick, float deltaTime )
		{
			// Get a smoothed vector from keyboard input (see methods below).
			var leftStickVector = GetVectorFromKeyboard( deltaTime, true );

			// With a vector you can use UpdateLeftStickWithValue()
			UpdateLeftStickWithValue( leftStickVector, updateTick, deltaTime );

			// Get a smoothed vector from mouse input (see methods below).
			var rightStickVector = GetVectorFromMouse( deltaTime, true );

			// Submit it as a raw value so it doesn't get processed down to -1.0 to +1.0 range.
			UpdateRightStickWithRawValue( rightStickVector, updateTick, deltaTime );

			// You could also read from keyboard input and submit into the virtual device left stick
			// unsmoothed which would be much simpler.
			// UpdateWithState( InputControlType.LeftStickLeft, Input.GetKey( KeyCode.LeftArrow ), updateTick, deltaTime );
			// UpdateWithState( InputControlType.LeftStickRight, Input.GetKey( KeyCode.RightArrow ), updateTick, deltaTime );
			// UpdateWithState( InputControlType.LeftStickUp, Input.GetKey( KeyCode.UpArrow ), updateTick, deltaTime );
			// UpdateWithState( InputControlType.LeftStickDown, Input.GetKey( KeyCode.DownArrow ), updateTick, deltaTime );

			// For float values use:
			// UpdateWithValue( InputControlType.LeftStickLeft, floatValue, updateTick, deltaTime );

			// Read from keyboard input presses to submit into action buttons.
			UpdateWithState( InputControlType.Action1, Input.GetKey( KeyCode.Space ), updateTick, deltaTime );
			UpdateWithState( InputControlType.Action2, Input.GetKey( KeyCode.S ), updateTick, deltaTime );
			UpdateWithState( InputControlType.Action3, Input.GetKey( KeyCode.D ), updateTick, deltaTime );
			UpdateWithState( InputControlType.Action4, Input.GetKey( KeyCode.F ), updateTick, deltaTime );
		}


		Vector2 GetVectorFromKeyboard( float deltaTime, bool smoothed )
		{
			if (smoothed)
			{
				kx = ApplySmoothing( kx, GetXFromKeyboard(), deltaTime, Sensitivity );
				ky = ApplySmoothing( ky, GetYFromKeyboard(), deltaTime, Sensitivity );
			}
			else
			{
				kx = GetXFromKeyboard();
				ky = GetYFromKeyboard();
			}

			return new Vector2( kx, ky );
		}


		static float GetXFromKeyboard()
		{
			var l = Input.GetKey( KeyCode.LeftArrow ) ? -1.0f : 0.0f;
			var r = Input.GetKey( KeyCode.RightArrow ) ? 1.0f : 0.0f;
			return l + r;
		}


		static float GetYFromKeyboard()
		{
			var u = Input.GetKey( KeyCode.UpArrow ) ? 1.0f : 0.0f;
			var d = Input.GetKey( KeyCode.DownArrow ) ? -1.0f : 0.0f;
			return u + d;
		}


		Vector2 GetVectorFromMouse( float deltaTime, bool smoothed )
		{
			if (smoothed)
			{
				mx = ApplySmoothing( mx, Input.GetAxisRaw( "mouse x" ) * MouseScale, deltaTime, Sensitivity );
				my = ApplySmoothing( my, Input.GetAxisRaw( "mouse y" ) * MouseScale, deltaTime, Sensitivity );
			}
			else
			{
				mx = Input.GetAxisRaw( "mouse x" ) * MouseScale;
				my = Input.GetAxisRaw( "mouse y" ) * MouseScale;
			}

			return new Vector2( mx, my );
		}


		static float ApplySmoothing( float lastValue, float thisValue, float deltaTime, float sensitivity )
		{
			sensitivity = Mathf.Clamp( sensitivity, 0.001f, 1.0f );

			if (Mathf.Approximately( sensitivity, 1.0f ))
			{
				return thisValue;
			}

			return Mathf.Lerp( lastValue, thisValue, deltaTime * sensitivity * 100.0f );
		}
	}
}

namespace InControl
{
	using System;
	using System.Runtime.CompilerServices;
	using UnityEngine;


	public class UnityMouseProvider : IMouseProvider
	{
		// ReSharper disable once UnusedMember.Local
		const string mouseXAxis = "mouse x";

		// ReSharper disable once UnusedMember.Local
		const string mouseYAxis = "mouse y";

		readonly bool[] lastButtonPressed = new bool[16];
		readonly bool[] buttonPressed = new bool[16];

		// ReSharper disable once NotAccessedField.Local
		Vector2 lastPosition;
		Vector2 position;
		Vector2 delta;
		float scroll;


		public void Setup()
		{
			ClearState();
		}


		public void Reset()
		{
			ClearState();
		}


		public void Update()
		{
			#if INCONTROL_USE_NEW_UNITY_INPUT
			var mouse = UnityEngine.InputSystem.Mouse.current;
			if (mouse != null)
			{
				Array.Copy( buttonPressed, lastButtonPressed, buttonPressed.Length );
				buttonPressed[(int) Mouse.LeftButton] = mouse.leftButton.isPressed;
				buttonPressed[(int) Mouse.RightButton] = mouse.rightButton.isPressed;
				buttonPressed[(int) Mouse.MiddleButton] = mouse.middleButton.isPressed;
				buttonPressed[(int) Mouse.Button4] = mouse.backButton.isPressed;
				buttonPressed[(int) Mouse.Button5] = mouse.forwardButton.isPressed;
				position = mouse.position.ReadValue();
				delta = mouse.delta.ReadValue();
				scroll = mouse.scroll.y.ReadValue() / 20.0f; // Old Unity input is 20 times less; scale for compatibility.
			}
			else
			{
				ClearState();
			}
			#else
			if (Input.mousePresent)
			{
				Array.Copy( buttonPressed, lastButtonPressed, buttonPressed.Length );
				buttonPressed[(int) Mouse.LeftButton] = SafeGetMouseButton( 0 );
				buttonPressed[(int) Mouse.RightButton] = SafeGetMouseButton( 1 );
				buttonPressed[(int) Mouse.MiddleButton] = SafeGetMouseButton( 2 );
				buttonPressed[(int) Mouse.Button4] = SafeGetMouseButton( 3 );
				buttonPressed[(int) Mouse.Button5] = SafeGetMouseButton( 4 );
				buttonPressed[(int) Mouse.Button6] = SafeGetMouseButton( 5 );
				buttonPressed[(int) Mouse.Button7] = SafeGetMouseButton( 6 );
				buttonPressed[(int) Mouse.Button8] = SafeGetMouseButton( 7 );
				buttonPressed[(int) Mouse.Button9] = SafeGetMouseButton( 8 );
				lastPosition = position;
				position = Input.mousePosition;
				delta = new Vector2( Input.GetAxisRaw( mouseXAxis ), Input.GetAxisRaw( mouseYAxis ) );
				scroll = Input.mouseScrollDelta.y;
			}
			else
			{
				ClearState();
			}
			#endif
		}


		// Old Unity input doesn't allow mouse buttons above certain numbers on some platforms.
		// For example, the limit on Windows 7 appears to be 6.
		// ReSharper disable once UnusedMember.Local
		static bool SafeGetMouseButton( int button )
		{
			try
			{
				return Input.GetMouseButton( button );
			}
			catch (ArgumentException) {}

			return false;
		}


		void ClearState()
		{
			Array.Clear( lastButtonPressed, 0, lastButtonPressed.Length );
			Array.Clear( buttonPressed, 0, buttonPressed.Length );
			lastPosition = Vector2.zero;
			position = Vector2.zero;
			delta = Vector2.zero;
			scroll = 0.0f;
		}


		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public Vector2 GetPosition()
		{
			return position;
		}


		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public float GetDeltaX()
		{
			return delta.x;
		}


		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public float GetDeltaY()
		{
			return delta.y;
		}


		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public float GetDeltaScroll()
		{
			return scroll;
		}


		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public bool GetButtonIsPressed( Mouse control )
		{
			return buttonPressed[(int) control];
		}


		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public bool GetButtonWasPressed( Mouse control )
		{
			return buttonPressed[(int) control] && !lastButtonPressed[(int) control];
		}


		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public bool GetButtonWasReleased( Mouse control )
		{
			return !buttonPressed[(int) control] && lastButtonPressed[(int) control];
		}


		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public bool HasMousePresent()
		{
			#if INCONTROL_USE_NEW_UNITY_INPUT
			return UnityEngine.InputSystem.Mouse.current != null;
			#else
			return Input.mousePresent;
			#endif
		}
	}
}

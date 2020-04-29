namespace InControl
{
	using System;
	using System.IO;
	using UnityEngine;


	public class MouseBindingSource : BindingSource
	{
		public Mouse Control { get; protected set; }
		public static float ScaleX = 0.05f;
		public static float ScaleY = 0.05f;
		public static float ScaleZ = 0.05f;
		public static float JitterThreshold = 0.05f;


		internal MouseBindingSource() {}


		public MouseBindingSource( Mouse mouseControl )
		{
			Control = mouseControl;
		}


		// Unity doesn't allow mouse buttons above certain numbers on
		// some platforms. For example, the limit on Windows 7 appears
		// to be 6.
		internal static bool SafeGetMouseButton( int button )
		{
			try
			{
				return Input.GetMouseButton( button );
			}
			catch (ArgumentException) {}

			return false;
		}


		// This is necessary to maintain backward compatibility. :(
		readonly static int[] buttonTable = new[]
		{
			-1, 0, 1, 2, -1, -1, -1, -1, -1, -1, 3, 4, 5, 6, 7, 8
		};

		internal static bool ButtonIsPressed( Mouse control )
		{
			var button = buttonTable[(int) control];
			if (button >= 0)
			{
				return SafeGetMouseButton( button );
			}

			return false;
		}


		internal static bool NegativeScrollWheelIsActive( float threshold )
		{
			var value = Mathf.Min( Input.GetAxisRaw( "mouse z" ) * ScaleZ, 0.0f );
			return value < -threshold;
		}


		internal static bool PositiveScrollWheelIsActive( float threshold )
		{
			var value = Mathf.Max( 0.0f, Input.GetAxisRaw( "mouse z" ) * ScaleZ );
			return value > threshold;
		}


		internal static float GetValue( Mouse mouseControl )
		{
			var button = buttonTable[(int) mouseControl];
			if (button >= 0)
			{
				return SafeGetMouseButton( button ) ? 1.0f : 0.0f;
			}

			switch (mouseControl)
			{
				case Mouse.NegativeX:
					return -Mathf.Min( Input.GetAxisRaw( "mouse x" ) * ScaleX, 0.0f );
				case Mouse.PositiveX:
					return Mathf.Max( 0.0f, Input.GetAxisRaw( "mouse x" ) * ScaleX );

				case Mouse.NegativeY:
					return -Mathf.Min( Input.GetAxisRaw( "mouse y" ) * ScaleY, 0.0f );
				case Mouse.PositiveY:
					return Mathf.Max( 0.0f, Input.GetAxisRaw( "mouse y" ) * ScaleY );

				case Mouse.NegativeScrollWheel:
					return -Mathf.Min( Input.GetAxisRaw( "mouse z" ) * ScaleZ, 0.0f );
				case Mouse.PositiveScrollWheel:
					return Mathf.Max( 0.0f, Input.GetAxisRaw( "mouse z" ) * ScaleZ );
			}

			return 0.0f;
		}


		public override float GetValue( InputDevice inputDevice )
		{
			return GetValue( Control );
		}


		public override bool GetState( InputDevice inputDevice )
		{
			return Utility.IsNotZero( GetValue( inputDevice ) );
		}


		public override string Name
		{
			get
			{
				return Control.ToString();
			}
		}


		public override string DeviceName
		{
			get
			{
				return "Mouse";
			}
		}


		public override InputDeviceClass DeviceClass
		{
			get
			{
				return InputDeviceClass.Mouse;
			}
		}


		public override InputDeviceStyle DeviceStyle
		{
			get
			{
				return InputDeviceStyle.Unknown;
			}
		}


		public override bool Equals( BindingSource other )
		{
			if (other == null)
			{
				return false;
			}

			var bindingSource = other as MouseBindingSource;
			if (bindingSource != null)
			{
				return Control == bindingSource.Control;
			}

			return false;
		}


		public override bool Equals( object other )
		{
			if (other == null)
			{
				return false;
			}

			var bindingSource = other as MouseBindingSource;
			if (bindingSource != null)
			{
				return Control == bindingSource.Control;
			}

			return false;
		}


		public override int GetHashCode()
		{
			return Control.GetHashCode();
		}


		public override BindingSourceType BindingSourceType
		{
			get
			{
				return BindingSourceType.MouseBindingSource;
			}
		}


		public override void Save( BinaryWriter writer )
		{
			writer.Write( (int) Control );
		}


		public override void Load( BinaryReader reader, UInt16 dataFormatVersion )
		{
			Control = (Mouse) reader.ReadInt32();
		}
	}
}

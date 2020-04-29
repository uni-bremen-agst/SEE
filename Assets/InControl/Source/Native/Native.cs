namespace InControl
{
	using System;
	using System.Runtime.InteropServices;
	using DeviceHandle = System.UInt32;


	// @cond nodoc
	static class Native
	{
#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
		const string libraryName = "__Internal";
#else
		const string libraryName = "InControlNative";
#endif


#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IOS || UNITY_TVOS


		[DllImport( libraryName, EntryPoint = "InControl_Init" )]
		public static extern void Init( NativeInputOptions options );


		[DllImport( libraryName, EntryPoint = "InControl_Stop" )]
		public static extern void Stop();


		[DllImport( libraryName, EntryPoint = "InControl_GetVersionInfo" )]
		public static extern void GetVersionInfo( out NativeVersionInfo versionInfo );


		[DllImport( libraryName, EntryPoint = "InControl_GetDeviceInfo" )]
		public static extern bool GetDeviceInfo( DeviceHandle handle, out InputDeviceInfo deviceInfo );


		[DllImport( libraryName, EntryPoint = "InControl_GetDeviceState" )]
		public static extern bool GetDeviceState( DeviceHandle handle, out IntPtr deviceState );


		[DllImport( libraryName, EntryPoint = "InControl_GetDeviceEvents" )]
		public static extern Int32 GetDeviceEvents( out IntPtr deviceEvents );


		[DllImport( libraryName, EntryPoint = "InControl_SetHapticState" )]
		public static extern void SetHapticState( UInt32 handle, Byte motor0, Byte motor1 );


		[DllImport( libraryName, EntryPoint = "InControl_SetLightColor" )]
		public static extern void SetLightColor( UInt32 handle, Byte red, Byte green, Byte blue );


		[DllImport( libraryName, EntryPoint = "InControl_SetLightFlash" )]
		public static extern void SetLightFlash( UInt32 handle, Byte flashOnDuration, Byte flashOffDuration );


#else
		public static void Init( NativeInputOptions options ) { }
		public static void Stop() { }
		public static void GetVersionInfo( out NativeVersionInfo versionInfo ) { versionInfo = new NativeVersionInfo(); }
		public static bool GetDeviceInfo( DeviceHandle handle, out InputDeviceInfo deviceInfo ) { deviceInfo = new InputDeviceInfo(); return false; }
		public static bool GetDeviceState( DeviceHandle handle, out IntPtr deviceState ) { deviceState = IntPtr.Zero; return false; }
		public static Int32 GetDeviceEvents( out IntPtr deviceEvents ) { deviceEvents = IntPtr.Zero; return 0; }
		public static void SetHapticState( UInt32 handle, Byte motor0, Byte motor1 ) { }
		public static void SetLightColor( UInt32 handle, Byte red, Byte green, Byte blue ) { }
		public static void SetLightFlash( UInt32 handle, Byte flashOnDuration, Byte flashOffDuration ) { }

#endif
	}

	//@endcond
}

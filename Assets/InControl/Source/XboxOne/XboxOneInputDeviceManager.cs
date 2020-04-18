namespace InControl
{
	using System.Collections.Generic;
	using UnityEngine;


	public class XboxOneInputDeviceManager : InputDeviceManager
	{
		const int maxDevices = 8;
		bool[] deviceConnected = new bool[maxDevices];


		public XboxOneInputDeviceManager()
		{
			for (uint joystickId = 1; joystickId <= maxDevices; joystickId++)
			{
				devices.Add( new XboxOneInputDevice( joystickId ) );
			}

			UpdateInternal( 0, 0.0f );
		}


		void UpdateInternal( ulong updateTick, float deltaTime )
		{
			for (var deviceIndex = 0; deviceIndex < maxDevices; deviceIndex++)
			{
				var device = devices[deviceIndex] as XboxOneInputDevice;

				if (device.IsConnected != deviceConnected[deviceIndex])
				{
					if (device.IsConnected)
					{
						InputManager.AttachDevice( device );
					}
					else
					{
						InputManager.DetachDevice( device );
					}

					deviceConnected[deviceIndex] = device.IsConnected;
				}
			}
		}


		public override void Update( ulong updateTick, float deltaTime )
		{
			UpdateInternal( updateTick, deltaTime );
		}


		public override void Destroy()
		{
		}


		public static bool CheckPlatformSupport( ICollection<string> errors )
		{
			if (Application.platform != RuntimePlatform.XboxOne)
			{
				return false;
			}

			return true;
		}


		internal static bool Enable()
		{
			var errors = new List<string>();
			if (CheckPlatformSupport( errors ))
			{
				InputManager.AddDeviceManager<XboxOneInputDeviceManager>();
				return true;
			}
			else
			{
				foreach (var error in errors)
				{
					Logger.LogError( error );
				}
				return false;
			}
		}
	}
}

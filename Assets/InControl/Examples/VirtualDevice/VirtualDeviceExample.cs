namespace VirtualDeviceExample
{
	using InControl;
	using UnityEngine;


	// This example illustrates how to make a custom virtual controller for
	// the purpose of feeding custom input into InControl.
	//
	// A virtual device is necessary because InControl is device centric internally and
	// this allows custom input to interact naturally with other devices, whether it be
	// joysticks, touch controls, or whatever else. Custom input cannot be "force fed"
	// into other existing devices. A device is considered authoritative over the input
	// it provides and cannot be directly overriden. However, by creating your own
	// virtual device, you can provide whatever input you desire and you gain all the
	// benefits of being a first class device within InControl.
	//
	// For more advanced situations you may want to have a device manager to organize
	// multiple devices. For an example of how to accomplish this,
	// see XInputDeviceManager and XInputDevice.
	//
	public class VirtualDeviceExample : MonoBehaviour
	{
		public GameObject leftObject;
		public GameObject rightObject;

		VirtualDevice virtualDevice;


		void OnEnable()
		{
			virtualDevice = new VirtualDevice();

			// We hook into the OnSetup callback to ensure the device is attached
			// after the InputManager has had a chance to initialize properly.
			InputManager.OnSetup += () => InputManager.AttachDevice( virtualDevice );
		}


		void OnDisable()
		{
			InputManager.DetachDevice( virtualDevice );
		}


		void Update()
		{
			// Use last device which provided input.
			var inputDevice = InputManager.ActiveDevice;

			// Rotate left object with left stick.
			leftObject.transform.Rotate( Vector3.down, 500.0f * Time.deltaTime * inputDevice.LeftStickX, Space.World );
			leftObject.transform.Rotate( Vector3.right, 500.0f * Time.deltaTime * inputDevice.LeftStickY, Space.World );

			// Rotate right object with right stick.
			rightObject.transform.Rotate( Vector3.down, 500.0f * Time.deltaTime * inputDevice.RightStickX, Space.World );
			rightObject.transform.Rotate( Vector3.right, 500.0f * Time.deltaTime * inputDevice.RightStickY, Space.World );

			// Get color based on action buttons.
			var color = Color.white;
			if (inputDevice.Action1.IsPressed)
			{
				color = Color.green;
			}
			if (inputDevice.Action2.IsPressed)
			{
				color = Color.red;
			}
			if (inputDevice.Action3.IsPressed)
			{
				color = Color.blue;
			}
			if (inputDevice.Action4.IsPressed)
			{
				color = Color.yellow;
			}

			// Color the object.
			leftObject.GetComponent<Renderer>().material.color = color;
		}
	}
}


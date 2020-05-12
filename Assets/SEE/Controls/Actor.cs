using SEE.Controls.Devices;
using UnityEngine;
using UnityEngine.Serialization;

namespace SEE.Controls
{
	/// <summary>
	/// An actor defines the reactions of the player on input stimuli. It
	/// maps input devices onto high-level actions, such as movements
	/// or selections, requiring and reacting to these inputs.
	/// This mapping can be specified by the user in the inspector.
	/// For instance, in a desktop environment, input of input devices
	/// based on mice or keyboards are mapped onto the respective actions
	/// in that kind of environment. In a virtual reality environment,
	/// on the other hand, input from VR controllers based on SteamVR
	/// will be mapped onto actions that may be specific to this kind
	/// of environment.
	/// The implementation of high-level actions may be specific to the
	/// kind of environment (e.g., how feedback is given when the player
	/// selects an object) and may also require particular kinds of
	/// inputs (e.g., 2D or 3D directions). Hence, the mapping between
	/// input devices and actions is not arbitrary.
	/// The reactions may depend upon the state of the player.
	/// </summary>
	public class Actor : MonoBehaviour
	{
		[Tooltip("The camera of this player.")]
		public Camera mainCamera;

		[Tooltip("The device from which to read the input for speed.")]
		public Throttle throttleDevice;

		[Tooltip(
			"The device from which to retrieve the boost for movements. The boost amplifies speed.")]
		public Boost boostDevice;

		[Tooltip("The device from which to read the input for the direction of movements.")]
		public Direction directionDevice;

		[Tooltip("The device from which to read the viewpoint.")]
		public Viewpoint viewpointDevice;

		[Tooltip("The device from which to read selection input.")]
		public Selection selectionDevice;

		[Tooltip("The device from which to read viewport selection input.")]
		private ControllerSelection viewportSelectionDevice;

		[Tooltip("The action applied to move the camera.")]
		public CameraAction cameraAction;

		[Tooltip("The action applied to select an object.")]
		public SelectionAction selectionAction;

		[Tooltip("The action applied to select an object by way of the viewport.")]
		private SelectionViewportAction viewportSelectionAction;

		[Tooltip("The device from which to get inputs for the charts.")]
		public ChartControls chartControlDevice;

		[Tooltip("The action applied to handle chart controls.")]
		public ChartAction chartAction;

		private void Start()
		{
			CameraSetup();
			SelectionSetup();
			ChartSetup();
		}

		/// <summary>
		/// Sets up and connects the selection input device and the selection action.
		/// </summary>
		private void SelectionSetup()
		{
			if (selectionAction == null)
			{
				Debug.LogError("Selection action must be set.\n");
			}
			else
			{
				if (selectionDevice == null)
				{
					Debug.LogWarning("Selection device not set.\n");
					selectionDevice = gameObject.AddComponent<NullSelection>();
				}

				selectionAction.SelectionDevice = selectionDevice;
				selectionAction.MainCamera = mainCamera;
			}

			// The selectionDevice and selectionAction are suitable for 2D or 3D
			// positional selection. As a secondary way to select something with
			// a device that offers neither 2D nor 3D positional data (e.g., if we
			// only have a gamepad controller that allows use to fly through a scene
			// but offers no way to point to something), we use a viewportSelectionDevice
			// and viewportSelectionAction, which simply selects an objects that can be
			// hit by a ray through the center of the viewport.
			if (viewportSelectionDevice == null)
			{
				viewportSelectionDevice = gameObject.AddComponent<ControllerSelection>();
				if (viewportSelectionAction == null)
					viewportSelectionAction = gameObject.AddComponent<SelectionViewportAction>();
				viewportSelectionAction.SelectionDevice = viewportSelectionDevice;
				viewportSelectionAction.MainCamera = mainCamera;
			}
		}

		/// <summary>
		/// Sets up the connection between the camera and the actions steering it
		/// (throttle, boost, direction, and viewpoint).
		/// </summary>
		private void CameraSetup()
		{
			if (cameraAction == null)
			{
				Debug.LogError("Camera action must be set.\n");
			}
			else
			{
				if (throttleDevice == null)
				{
					Debug.LogWarning("Throttle device not set.\n");
					cameraAction.ThrottleDevice = gameObject.AddComponent<NullThrottle>();
				}

				cameraAction.ThrottleDevice = throttleDevice;
				if (directionDevice == null)
				{
					Debug.LogWarning("Direction device not set.\n");
					cameraAction.DirectionDevice = gameObject.AddComponent<NullDirection>();
				}

				cameraAction.DirectionDevice = directionDevice;
				if (viewpointDevice == null)
				{
					Debug.LogWarning("Viewpoint device not set.\n");
					cameraAction.ViewpointDevice = gameObject.AddComponent<NullViewpoint>();
				}

				cameraAction.ViewpointDevice = viewpointDevice;
				if (boostDevice == null)
				{
					Debug.LogWarning("Boost device not set.\n");
					cameraAction.BoostDevice = gameObject.AddComponent<NullBoost>();
				}

				cameraAction.BoostDevice = boostDevice;
			}
		}

		private void ChartSetup()
		{
			chartAction.chartControlsDevice = chartControlDevice;
		}
	}
}
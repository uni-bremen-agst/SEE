using UnityEngine;

namespace SEECity.Charts.Scripts.VR
{
	/// <summary>
	/// Handles setup of new charts entering the scene.
	/// </summary>
	public class VRCanvasSetup : MonoBehaviour
	{
		/// <summary>
		/// Assigns the <see cref="Camera" /> component of the VRPointer to this world space
		/// <see cref="Canvas" />.
		/// </summary>
		private void Awake()
		{
			Camera eventCamera = GetComponent<Canvas>().worldCamera =
				GameObject.FindGameObjectWithTag("Pointer").GetComponent<Camera>();
			eventCamera.enabled = false;
		}
	}
}
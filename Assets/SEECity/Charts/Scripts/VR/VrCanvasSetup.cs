using UnityEngine;

namespace SEE.Charts.Scripts.VR
{
	/// <summary>
	/// Handles setup of new charts entering the scene.
	/// </summary>
	public class VrCanvasSetup : MonoBehaviour
	{
		/// <summary>
		/// Assigns the <see cref="Camera" /> component of the VrPointer to this world space
		/// <see cref="Canvas" />.
		/// </summary>
		private void Awake()
		{
			var eventCamera = GetComponent<Canvas>().worldCamera =
				GameObject.FindGameObjectWithTag("Pointer").GetComponent<Camera>();
			eventCamera.enabled = false;
		}
	}
}
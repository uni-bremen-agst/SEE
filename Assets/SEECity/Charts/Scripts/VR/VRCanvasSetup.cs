using UnityEngine;

namespace Assets.SEECity.Charts.Scripts.VR
{
	public class VRCanvasSetup : MonoBehaviour
	{
		private void Awake()
		{
			Camera eventCamera = GetComponent<Canvas>().worldCamera =
				GameObject.FindGameObjectWithTag("Pointer").GetComponent<Camera>();
			eventCamera.enabled = false;
		}
	}
}
using UnityEngine;

namespace SEECity.Charts.Scripts.VR
{
	public class ChartCreatorVR : MonoBehaviour
	{
		[SerializeField] private GameObject chartPrefab;

		[SerializeField] private Transform parent;

		public void CreateChart()
		{
			Transform cameraPosition = Camera.main.transform;

			Instantiate(chartPrefab, cameraPosition.position + 2 * cameraPosition.forward,
				Quaternion.identity, parent);
		}
	}
}
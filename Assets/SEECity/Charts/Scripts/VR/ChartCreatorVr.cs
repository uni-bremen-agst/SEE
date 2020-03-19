using UnityEngine;

namespace SEECity.Charts.Scripts.VR
{
	/// <summary>
	/// The VR version of <see cref="ChartCreator" />.
	/// </summary>
	public class ChartCreatorVr : MonoBehaviour
	{
		/// <summary>
		/// The prefab of the chart.
		/// </summary>
		[SerializeField] private GameObject chartPrefab;

		/// <summary>
		/// The parent of the new chart.
		/// </summary>
		[SerializeField] private Transform parent;

		/// <summary>
		/// Initializes a new chart in front of the player.
		/// </summary>
		public void CreateChart()
		{
			var cameraPosition = Camera.main.transform;

			Instantiate(chartPrefab, cameraPosition.position + 2 * cameraPosition.forward,
				Quaternion.identity, parent);
		}
	}
}
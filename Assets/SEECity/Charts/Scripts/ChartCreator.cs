using UnityEngine;

namespace SEECity.Charts.Scripts
{
	/// <summary>
	/// Used to create new charts.
	/// </summary>
	public class ChartCreator : MonoBehaviour
	{
		/// <summary>
		/// The prefab for a new chart.
		/// </summary>
		[SerializeField] private GameObject chartPrefab;

		/// <summary>
		/// The <see cref="Canvas" /> on which the chart is created.
		/// </summary>
		[SerializeField] private Transform chartsCanvas;

		/// <summary>
		/// Initializes the new chart as GameObject.
		/// </summary>
		public void CreateChart()
		{
			Instantiate(chartPrefab, chartsCanvas).GetComponent<ChartContent>();
		}
	}
}
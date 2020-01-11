using UnityEngine;

namespace Assets.SEECity.Charts.Scripts
{
	/// <summary>
	/// Contains all the information needed to create the next chart.
	/// </summary>
	public class ChartCreator : MonoBehaviour
	{
		/// <summary>
		/// The prefab for a new chart.
		/// </summary>
		[SerializeField] private GameObject _chartPrefab = null;

		/// <summary>
		/// The <see cref="Canvas" /> on which the chart is created.
		/// </summary>
		[SerializeField] private Transform _chartsCanvas = null;

		/// <summary>
		/// Initializes the new chart as GameObject.
		/// </summary>
		[SerializeField]
		private void CreateChart()
		{
			ChartContent content =
				Instantiate(_chartPrefab, _chartsCanvas).GetComponent<ChartContent>();
			gameObject.SetActive(false);
		}
	}
}
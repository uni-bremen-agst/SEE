using UnityEngine;

namespace Assets.SEECity.Charts.Scripts
{
	/// <summary>
	/// Contains all the information needed to create the next chart.
	/// </summary>
	public class ChartCreator : MonoBehaviour
	{
		[SerializeField] private GameObject _chartPrefab;
		[SerializeField] private Transform _chartsCanvas;

		/// <summary>
		/// Initializes the new chart as GameObject.
		/// </summary>
		public void CreateChart()
		{
			ChartContent content =
				Instantiate(_chartPrefab, _chartsCanvas).GetComponent<ChartContent>();
			gameObject.SetActive(false);
		}
	}
}
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Charts.Scripts
{
	/// <summary>
	/// This class is used to put into a working scene using charts. It eases access to specific components
	/// and <see cref="GameObject" />s during testing.
	/// </summary>
	public class TestHelper : MonoBehaviour
	{
		public ChartManager manager;
		public ChartCreator creator;
		public GameObject charts;

		public Button closeChartsButton;
		public Button createChartButton;

		/// <summary>
		/// Fill this with prefabs of cities in the following order:
		/// 1. A city with multiple buildings
		/// 2. A city with one building
		/// 3. A city with zero buildings
		/// </summary>
		public GameObject[] cityPrefabs;
	}
}
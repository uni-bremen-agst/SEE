using UnityEngine;
using UnityEngine.UI;

namespace SEE.Charts.Scripts
{
    /// <summary>
    ///     This class is used to put into a working scene using charts. It eases access to specific components
    ///     and <see cref="GameObject" />s during testing.
    /// </summary>
    public class TestHelper : MonoBehaviour
    {
        public GameObject charts;

        /// <summary>
        ///     Fill this with prefabs of cities in the following order:
        ///     1. A city with multiple buildings
        ///     2. A city with one building
        ///     3. A city with zero buildings
        ///     They might have to be regenerated after changes to the software.
        /// </summary>
        public GameObject[] cityPrefabs;

        public Button closeChartsButton;
        public Button createChartButton;
        public ChartCreator creator;
        public ChartManager manager;
    }
}
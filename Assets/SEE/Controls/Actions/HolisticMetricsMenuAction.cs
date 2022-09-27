using SEE.Game.HolisticMetrics;
using UnityEngine;

namespace SEE.Controls.Actions
{
    public class HolisticMetricsMenuAction : MonoBehaviour
    {
        private HolisticMetricsManager holisticMetricsManager;

        private void Start()
        {
            holisticMetricsManager = GameObject.Find("HolisticMetricsManager").GetComponent<HolisticMetricsManager>();
        }

        private void Update()
        {
            if (SEEInput.ToggleHolisticMetricsMenu())
            {
                 holisticMetricsManager.ToggleMenu();
            }
        }
    }
}
using UnityEngine;

namespace SEE.Controls.Actions
{
    public class HolisticMetricsMenuAction : MonoBehaviour
    {
        [SerializeField] private GameObject menu;

        private void Update()
        {
            if (SEEInput.ToggleHolisticMetricsMenu())
            {
                 menu.SetActive(!menu.activeInHierarchy);
            }
        }
    }
}
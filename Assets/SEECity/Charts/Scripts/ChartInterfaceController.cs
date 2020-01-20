using SEE;
using UnityEngine;

namespace Assets.SEECity.Charts.Scripts
{
	/// <summary>
	/// Sets the active states of different layers of the chart UI.
	/// </summary>
	public class ChartInterfaceController : MonoBehaviour
	{
		/// <summary>
		/// The script controlling the camera.
		/// </summary>
		private FlyCamera _flyCamera;

		/// <summary>
		/// UI when charts are open.
		/// </summary>
		[SerializeField] private GameObject _chartsOpen = null;

		/// <summary>
		/// UI for creating new charts.
		/// </summary>
		[SerializeField] private GameObject _chartCreator = null;

		private void Awake()
		{
			_flyCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<FlyCamera>();
		}

		/// <summary>
		/// Checks if keys for chart interaction have been pressed.
		/// </summary>
		private void Update()
		{
			if (Input.GetButtonDown("ToggleCharts")) ToggleCharts(!_chartsOpen.activeInHierarchy);
		}

		/// <summary>
		/// Toggles the chart view.
		/// </summary>
		public void ToggleCharts(bool open)
		{
			_chartsOpen.SetActive(open);
			_flyCamera.SetLastMouse();
			_flyCamera.enabled = !open;
		}

		/// <summary>
		/// Opens the dialogue for creating new charts.
		/// </summary>
		public void OpenChartCreator()
		{
			_chartCreator.SetActive(true);
		}
	}
}
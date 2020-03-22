using SEE;
using UnityEngine;

namespace SEE.Charts.Scripts
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
		[SerializeField] private GameObject chartsOpen;

		/// <summary>
		/// UI for creating new charts.
		/// </summary>
		[SerializeField] private GameObject chartCreator;

		/// <summary>
		/// Assigns attributes.
		/// </summary>
		private void Awake()
		{
			_flyCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<FlyCamera>();
		}

		/// <summary>
		/// Checks if keys for chart interaction have been pressed.
		/// </summary>
		private void Update()
		{
			if (Input.GetButtonDown("ToggleCharts")) ToggleCharts(!chartsOpen.activeInHierarchy);
		}

		/// <summary>
		/// Toggles the chart view.
		/// </summary>
		public void ToggleCharts(bool open)
		{
			chartsOpen.SetActive(open);
			_flyCamera.SetLastMouse();
			_flyCamera.enabled = !open;
		}

		/// <summary>
		/// Opens the dialogue for creating new charts.
		/// </summary>
		public void OpenChartCreator()
		{
			chartCreator.SetActive(true);
		}
	}
}
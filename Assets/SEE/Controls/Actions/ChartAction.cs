using SEE.Charts.Scripts;
using SEE.Controls.Devices;
using UnityEngine;

namespace SEE.Controls
{
	public class ChartAction : MonoBehaviour
	{
		protected ChartManager ChartManager;

		[HideInInspector] public ChartControls chartControlsDevice;

		private void Start()
		{
			ChartManager = GameObject.FindGameObjectWithTag("ChartManager")
				.GetComponent<ChartManager>();
		}
	}
}
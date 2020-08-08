using SEE.Charts.Scripts;
using SEE.Controls.Devices;
using SEE.GO;
using UnityEngine;

namespace SEE.Controls
{
	public class ChartAction : MonoBehaviour
	{
        protected ChartManager ChartManager;

		[HideInInspector] public ChartControls chartControlsDevice;

		[HideInInspector] public float move;

		/// <summary>
		/// Click was false in the last update and true in this update.
		/// </summary>
		[HideInInspector] public bool clickDown;

		/// <summary>
		/// Click was true in the last update and false in this update.
		/// </summary>
		[HideInInspector] public bool clickUp;

		private void Start()
		{
            GameObject chartManagerObject = GameObject.FindGameObjectWithTag(GlobalGameObjectNames.ChartManagerTag);
			if (chartManagerObject == null)
			{
				Debug.LogErrorFormat("There is no chart manager named {0} in the scene\n",
					GlobalGameObjectNames.ChartManagerTag);
			}
			else
			{
				ChartManager = chartManagerObject.GetComponent<ChartManager>();
			}
		}
	}
}
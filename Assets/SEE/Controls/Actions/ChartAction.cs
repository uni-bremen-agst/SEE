using SEE.Charts.Scripts;
using SEE.Controls.Devices;
using SEE.GO;
using UnityEngine;

namespace SEE.Controls
{
	public class ChartAction : MonoBehaviour
	{
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
	}
}
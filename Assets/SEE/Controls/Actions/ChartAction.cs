using SEE.Controls.Devices;
using UnityEngine;

namespace SEE.Controls
{
	/// <summary>
	/// Abstract super class of the actions applied to metric charts.
	/// </summary>
	public abstract class ChartAction : MonoBehaviour
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
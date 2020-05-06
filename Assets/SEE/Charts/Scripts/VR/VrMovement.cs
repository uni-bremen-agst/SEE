using System.Collections;
using UnityEngine;
using Valve.VR;

namespace SEE.Charts.Scripts.VR
{
	/// <summary>
	/// Handles the players movement in VR.
	/// </summary>
	public class VrMovement : MonoBehaviour
	{
		/// <summary>
		/// Contains settings used in this script.
		/// </summary>
		private ChartManager _chartManager;

		/// <summary>
		/// The controller to activate movement with.
		/// </summary>
		private SteamVR_Input_Sources _movementSource;

		/// <summary>
		/// The axis containing the amount the player wants to move.
		/// </summary>
		private SteamVR_Action_Single _movement;

		/// <summary>
		/// A multiplier to adjust movement speed, if it is not right.
		/// </summary>
		private const float MovementSpeed = 1f;

		/// <summary>
		/// If the charts get deactivated, this will be the <see cref="Coroutine" /> doing that.
		/// </summary>
		private Coroutine _chartsDeactivated;

		/// <summary>
		/// The hand with which the player is moving to determine the direction to move towards.
		/// </summary>
		[SerializeField] private Transform hand;

		/// <summary>
		/// Contains all charts in the scene.
		/// </summary>
		[SerializeField] private GameObject charts;

		/// <summary>
		/// Initializes some attributes.
		/// </summary>
		private void Start()
		{
			GetSettingData();
		}

		/// <summary>
		/// Links the <see cref="ChartManager" /> and gets its setting data.
		/// </summary>
		private void GetSettingData()
		{
			_chartManager = GameObject.FindGameObjectWithTag("ChartManager")
				.GetComponent<ChartManager>();
			_movementSource = _chartManager.movementSource;
			_movement = _chartManager.movement;
		}

		/// <summary>
		/// Checks if the player triggered movement and translates him.
		/// </summary>
		private void Update()
		{
			var axis = _movement.GetAxis(_movementSource);
			if (axis.Equals(0)) return;
			if (_chartsDeactivated != null) StopCoroutine(_chartsDeactivated);
			_chartsDeactivated = StartCoroutine(DeactivateCharts());
			transform.Translate(hand.forward * axis * MovementSpeed);
		}

		/// <summary>
		/// Deactivates all charts for a short time to not distract while moving in VR.
		/// </summary>
		/// <returns></returns>
		private IEnumerator DeactivateCharts()
		{
			charts.SetActive(false);
			yield return new WaitForSeconds(1f);
			charts.SetActive(true);
		}
	}
}
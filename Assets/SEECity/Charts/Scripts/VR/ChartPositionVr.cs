using System.Collections;
using UnityEngine;

namespace SEECity.Charts.Scripts.VR
{
	/// <summary>
	/// Manages the position of the <see cref="GameObject" /> containing the charts in VR.
	/// </summary>
	public class ChartPositionVr : MonoBehaviour
	{
		/// <summary>
		/// Contains some settings used in this script.
		/// </summary>
		private ChartManager _chartManager;

		/// <summary>
		/// Contains position data of the assigned camera.
		/// </summary>
		[SerializeField] private Transform cameraTransform;

		/// <summary>
		/// The minimum distance between the players head and the <see cref="GameObject" /> the charts are
		/// attached to to trigger it to follow the players head.
		/// </summary>
		private float _distanceThreshold;

		/// <summary>
		/// The <see cref="Coroutine" /> that moves the <see cref="GameObject" /> containing the charts.
		/// </summary>
		private Coroutine _movingChart;

		/// <summary>
		/// Calls methods for initialization.
		/// </summary>
		private void Awake()
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
			_distanceThreshold = _chartManager.distanceThreshold;
		}

		/// <summary>
		/// Checks if the player moved more than <see cref="_distanceThreshold" />. If so, the charts will
		/// follow the player.
		/// </summary>
		private void Update()
		{
			if (Vector3.Distance(transform.position, cameraTransform.position) <=
			    _distanceThreshold) return;
			if (_movingChart != null)
			{
				StopCoroutine(_movingChart);
				gameObject.SetActive(true);
			}

			_movingChart = StartCoroutine(MoveChart());
		}

		/// <summary>
		/// Moves the charts towards the players position.
		/// </summary>
		/// <returns></returns>
		private IEnumerator MoveChart()
		{
			var startPosition = transform.position;
			for (var time = 0f; time < 1f; time += Time.deltaTime)
			{
				transform.position = Vector3.Lerp(startPosition, cameraTransform.position, time);
				yield return new WaitForEndOfFrame();
			}

			gameObject.SetActive(true);
			_movingChart = null;
		}
	}
}
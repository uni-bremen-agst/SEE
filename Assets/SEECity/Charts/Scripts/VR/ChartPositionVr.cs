using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace SEECity.Charts.Scripts.VR
{
	public class ChartPositionVr : MonoBehaviour
	{
		private ChartManager _chartManager;

		[SerializeField] private Transform cameraTransform;

		private float _distanceThreshold;

		private Coroutine _movingChart;

		private void Awake()
		{
			GetSettingData();
		}

		private void GetSettingData()
		{
			_chartManager = GameObject.FindGameObjectWithTag("ChartManager")
				.GetComponent<ChartManager>();
			_distanceThreshold = _chartManager.distanceThreshold;
		}

		private void Update()
		{
			if (Vector3.Distance(transform.position, cameraTransform.position) > _distanceThreshold)
			{
				if (_movingChart != null) StopCoroutine(_movingChart);
				_movingChart = StartCoroutine(MoveChart());
			}
		}

		private IEnumerator MoveChart()
		{
			Vector3 startPosition = transform.position;
			for (float time = 0f; time < 1f; time += Time.deltaTime)
			{
				transform.position = Vector3.Lerp(startPosition, cameraTransform.position, time);
				yield return new WaitForEndOfFrame();
			}

			_movingChart = null;
		}
	}
}
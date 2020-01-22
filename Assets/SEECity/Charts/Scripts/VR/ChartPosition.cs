using System.Collections;
using UnityEngine;

namespace SEECity.Charts.Scripts.VR
{
	public class ChartPosition : MonoBehaviour
	{
		private ChartManager _chartManager;

		[SerializeField] private Transform _camera;

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
			if (Vector3.Distance(transform.position, _camera.position) > _distanceThreshold)
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
				transform.position = Vector3.Lerp(startPosition, _camera.position, time);
				yield return new WaitForEndOfFrame();
			}

			_movingChart = null;
		}
	}
}
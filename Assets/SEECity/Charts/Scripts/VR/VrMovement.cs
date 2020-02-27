using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

namespace SEECity.Charts.Scripts.VR
{
    public class VrMovement : MonoBehaviour
    {
        private ChartManager _chartManager;
        private SteamVR_Input_Sources _movementSource;
        private SteamVR_Action_Single _movement;
        private float movementSpeed = 2f;
        private Coroutine _chartsDeactivated = null;

        [SerializeField] private Transform _hand;
        [SerializeField] private GameObject _charts;

        private void Start()
        {
            GetSettingData();
        }

        private void GetSettingData()
        {
            _chartManager = GameObject.FindGameObjectWithTag("ChartManager").GetComponent<ChartManager>();
            _movementSource = _chartManager.movementSource;
            _movement = _chartManager.movement;
        }

        private void Update()
        {
            float axis = _movement.GetAxis(_movementSource);
            if (axis != 0) {
                if (_chartsDeactivated != null)
                {
                    StopCoroutine(_chartsDeactivated);
                }
                _chartsDeactivated = StartCoroutine(DeactivateCharts());
                transform.Translate(_hand.forward * axis * movementSpeed);
            }
        }

        private IEnumerator DeactivateCharts()
        {
            _charts.SetActive(false);
            yield return new WaitForSeconds(1f);
            _charts.SetActive(true);
        }
    }
}
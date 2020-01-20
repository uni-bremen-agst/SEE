using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChartCreatorVR : MonoBehaviour
{
    [SerializeField] private GameObject _chartPrefab;

    [SerializeField] private Transform _parent;

    public void CreateChart()
    {
        Transform cameraPosition = Camera.main.transform;

        Instantiate(_chartPrefab, cameraPosition.position + 2 * cameraPosition.forward, Quaternion.identity, _parent);
    }
}

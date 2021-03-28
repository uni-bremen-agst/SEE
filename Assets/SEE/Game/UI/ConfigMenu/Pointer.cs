using System.Collections;
using System.Collections.Generic;
using SEE.Game.UI.ConfigMenu;
using UnityEngine;
using UnityEngine.EventSystems;

public class Pointer : MonoBehaviour
{
    public float DefaultLength = 5.0f;
    public VRInputModule InputModule;

    private GameObject _dot;
    private LineRenderer _lineRenderer;

    void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _dot = transform.Find("Dot").gameObject;
    }

    void Update()
    {
        UpdateLine();
    }

    private void UpdateLine()
    {
        PointerEventData data = InputModule.GetData();
        float targetLength = data.pointerCurrentRaycast.distance == 0 ? DefaultLength
            : data.pointerCurrentRaycast.distance;
        RaycastHit hit = CreateRaycast(targetLength);
        Vector3 endPosition = transform.position + transform.forward * targetLength;
        if (hit.collider != null)
        {
            endPosition = hit.point;
        }

        _dot.transform.position = endPosition;

        _lineRenderer.SetPosition(0, transform.position);
        _lineRenderer.SetPosition(1, endPosition);
    }

    private RaycastHit CreateRaycast()
    {
        RaycastHit hit;
        Ray ray = new Ray(transform.position, transform.forward);
        Physics.Raycast(ray, out hit, DefaultLength);

        return hit;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using SEE.Controls;
using Valve.VR;
using Valve.VR.InteractionSystem;


/// <summary>
/// Provids a visible ray to interact with the annotationEditor and the annotations in VR.
/// </summary>
public class Pointer : MonoBehaviour
{
    public float default_length = 5.0f;
    public GameObject dot;
    public VRGUIInputModule input_module;

    public SteamVR_Input_Sources targetSource;
    public SteamVR_Action_Boolean clickAction;

    private LineRenderer lineRenderer = null;

    private AnnotatableObject annotatableObject = null;


    public enum SelectionState
    {
        None,
        UI,
        Annotations
    }

    private SelectionState selectionState = SelectionState.None;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Update()
    {
        UpdateLine();
    }

    /// <summary>
    /// Updates the ray and initiats intactions with the annotationEditor or an annotation on button press.
    /// </summary>
    private void UpdateLine()
    {
        PointerEventData data = input_module.GetData();

        if (selectionState == SelectionState.UI)
        {
            input_module.enabled = true;
            input_module.Use();
        }
        else
        {
            input_module.enabled = false;
        }

        float targetLength = data.pointerCurrentRaycast.distance == 0 ? default_length : data.pointerCurrentRaycast.distance;

        RaycastHit hit = CreateRaycast(targetLength);

        Vector3 endPosition = transform.position + (transform.forward * targetLength);

        if (hit.collider != null)
        {
            endPosition = hit.point;
            if(selectionState == SelectionState.Annotations && clickAction.GetState(targetSource))
            {
                annotatableObject.AnnotationClicked(hit.transform.gameObject.transform.parent.gameObject);
            }
        }

        dot.transform.position = endPosition;

        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, endPosition);
    }

    private RaycastHit CreateRaycast(float length)
    {
        RaycastHit hit;
        Ray ray = new Ray(transform.position, transform.forward);
        Physics.Raycast(ray, out hit, length);

        return hit;
    }

    public void SetSelectionState(SelectionState selectionState, AnnotatableObject annotatableObject)
    {
        this.annotatableObject = annotatableObject;
        this.selectionState = selectionState;
    }
}

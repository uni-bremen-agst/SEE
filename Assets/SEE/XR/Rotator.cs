using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class Rotator : MonoBehaviour
{
    [SerializeField]
    Transform linkedDial;
    [SerializeField]
    private int snapRotationAmount = 25;
    [SerializeField]
    private float angleTolerance;
    [SerializeField]
    private GameObject RighthandModel;
    [SerializeField]
    private GameObject LefthandModel;
    [SerializeField]
    bool shouldUseDummyHands;

    private XRBaseInteractor interactor;
    private float startAngle;
    private bool requiresStartAngle = true;
    private bool shouldGetHandRotation = false;
    private XRGrabInteractable grabInteractor => GetComponent<XRGrabInteractable>();

    private void OnEnable()
    {
        grabInteractor.selectEntered.AddListener(GrabbedBy);
        grabInteractor.selectExited.AddListener(GrabEnd);
    }

    private void OnDisable()
    {
        grabInteractor.selectEntered.RemoveListener(GrabbedBy);
        grabInteractor.selectExited.RemoveListener(GrabEnd);
    }

    private void GrabEnd(SelectExitEventArgs args)
    {
        shouldGetHandRotation = false;
        requiresStartAngle = true;
    }

    private void GrabbedBy(SelectEnterEventArgs args)
    {
        interactor = (XRBaseInteractor)GetComponent<XRGrabInteractable>().interactorsSelecting[0];
        shouldGetHandRotation = true;
        startAngle = 0f;

    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (shouldGetHandRotation)
        {
            var rotationAngle = GetInteractorRotation();
            GetRotationDistance(rotationAngle);
        }
    }

    public float GetInteractorRotation() => interactor.GetComponent<Transform>().eulerAngles.z;

    private void GetRotationDistance(float currentAngle)
    {
        if (!requiresStartAngle)
        {
            var angleDifference = Mathf.Abs(startAngle - currentAngle);
            if (angleDifference > angleTolerance)
            {
                if (angleDifference > 270f)
                {
                    float angleCheck;
                    if (startAngle < currentAngle)
                    {
                        angleCheck = CheckAngle(currentAngle, startAngle);
                        if (angleCheck < angleTolerance)
                        {
                            return;
                        }
                        else
                        {
                            RotateDialClockwise();
                            startAngle = currentAngle;
                        }
                    }
                    else if (startAngle > currentAngle)
                    {
                        angleCheck = CheckAngle(currentAngle, startAngle);
                        if (angleCheck < angleTolerance)
                        {
                            return;
                        }
                        else
                        {
                            RotateDialAntiClockwise();
                            startAngle = currentAngle;
                        }
                    }
                }
                else
                {
                    if (startAngle < currentAngle)
                    {
                        RotateDialAntiClockwise();
                        startAngle = currentAngle;
                    }
                    else if (startAngle > currentAngle)
                    {
                        RotateDialClockwise();
                        startAngle = currentAngle;
                    }
                }
            }
        }
        else
        {
            requiresStartAngle = false;
            startAngle = currentAngle;
        }
    }

    private float CheckAngle(float currentAngle, float startAngle) => (360f - currentAngle) + startAngle;

    private void RotateDialClockwise()
    {
        linkedDial.localEulerAngles = new Vector3(linkedDial.localEulerAngles.x, linkedDial.localEulerAngles.y + snapRotationAmount, linkedDial.localEulerAngles.z);
        Transform rotateObject = XRSEEActions.RotateObject.transform;
        rotateObject.localEulerAngles = new Vector3(rotateObject.localEulerAngles.x, linkedDial.localEulerAngles.y, rotateObject.localEulerAngles.z);
    }

    private void RotateDialAntiClockwise()
    {
        linkedDial.localEulerAngles = new Vector3(linkedDial.localEulerAngles.x, linkedDial.localEulerAngles.y - snapRotationAmount, linkedDial.localEulerAngles.z);
        Transform rotateObject = XRSEEActions.RotateObject.transform;
        rotateObject.localEulerAngles = new Vector3(rotateObject.localEulerAngles.x, linkedDial.localEulerAngles.y, rotateObject.localEulerAngles.z);
    }

}

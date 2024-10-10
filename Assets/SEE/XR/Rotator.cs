using SEE.Controls.Actions;
using SEE.Utils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
/// <summary>
/// This class is used to rotate nodes in VR.
/// This script is based on this tutorial: "https://www.youtube.com/watch?v=vIrgCMNsE3s".
/// </summary>
public class Rotator : MonoBehaviour
{
    /// <summary>
    /// The actual dial, which gets rotated.
    /// </summary>
    [SerializeField]
    Transform linkedDial;
    /// <summary>
    /// The amount of degrees the dial gets roated each time.
    /// </summary>
    [SerializeField]
    private int snapRotationAmount = 25;
    /// <summary>
    /// The amount of degrees at which the dial is starting to rotate.
    /// </summary>
    [SerializeField]
    private float angleTolerance;
    /// <summary>
    /// The controller, which is used to rotated the dial/node.
    /// </summary>
    private XRBaseInteractor interactor;
    /// <summary>
    /// The base angle.
    /// </summary>
    private float startAngle;
    /// <summary>
    /// This bool is used, to determin if it is the first rotation, or not.
    /// </summary>
    private bool requiresStartAngle = true;
    /// <summary>
    /// Is true, when the dial/node should be roated according to the hand-rotation.
    /// </summary>
    public static bool shouldGetHandRotation { get; private set; } = false;
    /// <summary>
    /// The grab-interactor of the dial.
    /// </summary>
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
    /// <summary>
    /// This method gets called, when the user stops to use the dial.
    /// </summary>
    /// <param name="args">Event data associated with the event when an Interactor stops selecting an Interactable.</param>
    private void GrabEnd(SelectExitEventArgs args)
    {
        shouldGetHandRotation = false;
        requiresStartAngle = true;
    }
    /// <summary>
    /// This method gets called, when the user begins to use the dial.
    /// </summary>
    /// <param name="args">Event data associated with the event when an Interactor first initiates selecting an Interactable.</param>
    private void GrabbedBy(SelectEnterEventArgs args)
    {
        interactor = (XRBaseInteractor)GetComponent<XRGrabInteractable>().interactorsSelecting[0];
        shouldGetHandRotation = true;
        startAngle = 0f;

    }

    // Update is called once per frame
    void Update()
    {
        if (GlobalActionHistory.Current() != ActionStateTypes.Rotate)
        {
            Destroyer.Destroy(gameObject);
        }
        if (shouldGetHandRotation)
        {
            var rotationAngle = GetInteractorRotation();
            GetRotationDistance(rotationAngle);
        }
    }
    /// <summary>
    /// Gets the current roation-angle from the controller.
    /// </summary>
    /// <returns>The current rotation-angle from the controller.</returns>
    public float GetInteractorRotation() => interactor.GetComponent<Transform>().eulerAngles.z;
    /// <summary>
    /// Determins in which direction and how much the dial/node should be rotated.
    /// </summary>
    /// <param name="currentAngle">The current angle from the controller.</param>
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
    /// <summary>
    /// This method is used, to calculate, if the rotation angle is significant enough, to rotate the dial/node.
    /// </summary>
    /// <param name="currentAngle">The current angle of the controller.</param>
    /// <param name="startAngle">The base angle.</param>
    /// <returns></returns>
    private float CheckAngle(float currentAngle, float startAngle) => (360f - currentAngle) + startAngle;

    /// <summary>
    /// This method is used, to rotate the dial/node clockwise.
    /// </summary>
    private void RotateDialClockwise()
    {
        linkedDial.localEulerAngles = new Vector3(linkedDial.localEulerAngles.x, linkedDial.localEulerAngles.y + snapRotationAmount, linkedDial.localEulerAngles.z);
        Transform rotateObject = XRSEEActions.RotateObject.transform;
        rotateObject.localEulerAngles = new Vector3(rotateObject.localEulerAngles.x, linkedDial.localEulerAngles.y, rotateObject.localEulerAngles.z);
    }
    /// <summary>
    /// This method is used, to rotate the dial/node anticlockwise.
    /// </summary>
    private void RotateDialAntiClockwise()
    {
        linkedDial.localEulerAngles = new Vector3(linkedDial.localEulerAngles.x, linkedDial.localEulerAngles.y - snapRotationAmount, linkedDial.localEulerAngles.z);
        Transform rotateObject = XRSEEActions.RotateObject.transform;
        rotateObject.localEulerAngles = new Vector3(rotateObject.localEulerAngles.x, linkedDial.localEulerAngles.y, rotateObject.localEulerAngles.z);
    }

}

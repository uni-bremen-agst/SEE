using UnityEngine;
using SEE.Controls.Devices;

public class TransformationAction : MonoBehaviour
{

    [Tooltip("The object to be transformed by this action."), SerializeField]
    public GameObject TransformedObject;

    private Transformation tranformation;

    public Transformation TranformationDevice
    {
        get => tranformation;
        set => tranformation = value;
    }

    /// <summary>
    /// The kind of transformation seen last.
    /// </summary>
    protected Transformation.Kind previousTransformation = Transformation.Kind.None;

    /// <summary>
    /// Whether a transformation request other than None has already been detected at the last request.
    /// </summary>
    private bool isContinued = false;

    void Start()
    {
        if (TranformationDevice == null)
        {
            Debug.LogError("No transformation device assigned to this transformation action.\n");
            return;
        }
    }

    // Update is called once per frame
    void Update()
    {
        Transformation.Kind current = tranformation.Recognize();
        Behave(previousTransformation, current);
        previousTransformation = current;
    }

    private void Behave(Transformation.Kind oldGesture, Transformation.Kind newGesture)
    {
        if (oldGesture == Transformation.Kind.None)
        {
            // a new behaviour starts
        }
        else if (newGesture == Transformation.Kind.None)
        {
            // assert: oldGesture != Gesture.None
            // a running behaviour ends
        }
        else
        {
            // assert: oldGesture != Gesture.None and newGesture != Gesture.None
            // a running behaviour continues
            switch (newGesture)
            {
                case Transformation.Kind.Zoom:
                    ScaleObject();
                    break;
                case Transformation.Kind.MoveRight:
                    MoveObject(Vector3.right);
                    break;
                case Transformation.Kind.MoveLeft:
                    MoveObject(Vector3.left);
                    break;
                case Transformation.Kind.MoveForward:
                    MoveObject(Vector3.forward);
                    break;
                case Transformation.Kind.MoveBackward:
                    MoveObject(Vector3.back);
                    break;
            }
        }
    }

    private void MoveObject(Vector3 direction)
    {
        if (TransformedObject != null)
        {
            TransformedObject.transform.position += direction * MoveSpeed * Time.deltaTime;
        }
    }

    public float MinimalWidth = 0.05f;
    /// <summary>
    /// Invariant: MinimalWidth <= MaxminallWidth
    /// </summary>
    public float MaxminalWidth = 0.5f;

    public float ZoomSpeed = 1.0f;

    public float MoveSpeed = 1.0f;

    private void ScaleObject()
    {
        if (TransformedObject != null)
        {
            float factor = tranformation.ZoomFactor * ZoomSpeed;
            if (TransformedObject.transform.localScale.x * factor < MinimalWidth)
            {
                factor = MinimalWidth / TransformedObject.transform.localScale.x;
            }
            else if (TransformedObject.transform.localScale.x * factor > MaxminalWidth)
            {
                factor = MaxminalWidth / TransformedObject.transform.localScale.x;
            }
            Vector3 scale = TransformedObject.transform.localScale * factor;
            //cubeText.text = string.Format("initial {0,5:N2} current {1,5:N2} factor {2,5:N2} scale ",
            //                           initialDistanceBetweenHands, currentDistanceBetweenHands, factor) + scale;
            //iTween.ScaleUpdate(cube, scale, 0.25f);
            TransformedObject.transform.localScale = Vector3.Lerp(TransformedObject.transform.localScale, scale, ZoomSpeed * Time.deltaTime);

            //cube.transform.localScale = scale;
        }
    }
}

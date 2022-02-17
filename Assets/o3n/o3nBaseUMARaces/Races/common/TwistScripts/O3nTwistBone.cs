using UnityEngine;

public class O3nTwistBone : MonoBehaviour
{
    public float twistValue;
    public float[] twistValues;
    public float twistLimit = 45f;

    public Transform[] twistBone;
    public Transform[] refBone;
    public Vector3[] axisVector;
    public Quaternion[] originalRefRotation;
    public bool[] shoulderTwist;
    public bool[] enabled;

    private float[] originalRefRotationAngle;
    private float[] twistRotation;
    private Vector3 rotated;

    void Start()
    {
        if ((twistBone != null) && (refBone != null) && (twistBone.Length == refBone.Length))
        {
            twistRotation = new float[twistBone.Length];
            originalRefRotationAngle = new float[twistBone.Length];
            for (int i = 0; i < twistBone.Length; i++)
            {
                rotated = originalRefRotation[i] * Vector3.up;
                originalRefRotationAngle[i] = Mathf.Atan2(rotated.z, rotated.y) * Mathf.Rad2Deg;
            }
        }
    }

    // LateUpdate is called once per frame
    void LateUpdate()
    {
        twistValue = Mathf.Clamp(twistValue, 0f, 1f);
        for (int i = 0; i < twistBone.Length; i++)
        {
            if (enabled[i])
            {
                rotated = refBone[i].localRotation * axisVector[i];
                twistRotation[i] = Mathf.DeltaAngle(originalRefRotationAngle[i], Mathf.Atan2(rotated.z, rotated.y) * Mathf.Rad2Deg);
                twistBone[i].localEulerAngles = (shoulderTwist[i]? Vector3.left : Vector3.right) * ClampAngle(Mathf.Lerp(0.0f, twistRotation[i], twistValues[i] * twistValue));
            }
        }
    }

    float ClampAngle(float angle)
    {
        if (angle < -180)
            angle += 360;
        if (angle > 180)
            angle -= 360;
        return Mathf.Clamp(angle, - twistLimit, twistLimit);
    }
}
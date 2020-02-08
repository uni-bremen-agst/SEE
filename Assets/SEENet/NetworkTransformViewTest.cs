using UnityEngine;

public class NetworkTransformViewTest : MonoBehaviour
{
    private const float POSITION_SPEED_X = 0.7f;
    private const float POSITION_TIME = 1.9f;
    private const float POSITION_AMPLITUDE = 2.9f;
    private float positionLoopTime = 0;

    private readonly Vector3 ROTATION_EULER = new Vector3(61.0f, 197.0f, Mathf.PI * 100.0f);

    private const float SCALE_TIME = 1.1f;
    private const float SCALE_SIZE_INCREMENT = 0.1f * Mathf.PI * Mathf.PI;
    private float scaleLoopTime = 0;

    void Update()
    {
        bool isOwner = GetComponent<SEE.Net.ViewContainer>().IsOwner();
        if (isOwner)
        {
            { // position
                positionLoopTime += Time.deltaTime;
                while (positionLoopTime > POSITION_TIME)
                {
                    positionLoopTime -= POSITION_TIME;
                }
                float x = transform.position.x + POSITION_SPEED_X * Time.deltaTime;
                float y = POSITION_AMPLITUDE * Mathf.Sin(2.0f * Mathf.PI * positionLoopTime / POSITION_TIME);
                float z = 0.0f;
                transform.position = new Vector3(x, y, z);
            }

            { // rotation
                transform.rotation *= Quaternion.Euler(Time.deltaTime * ROTATION_EULER);
            }

            { // scale
                scaleLoopTime += Time.deltaTime;
                while (scaleLoopTime > SCALE_TIME)
                {
                    scaleLoopTime -= SCALE_TIME;
                }
                float s = 1.0f + 0.5f * (SCALE_SIZE_INCREMENT + SCALE_SIZE_INCREMENT * Mathf.Sin(2.0f * Mathf.PI * scaleLoopTime / SCALE_TIME));
                transform.localScale = new Vector3(s, s, s);
            }
        }
    }
}

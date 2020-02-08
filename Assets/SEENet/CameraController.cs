using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float movementSpeed = 10.0f;
    public float rotationSpeed = 4.0f;
    public float sprintFactor = 2.0f;

    private float yaw;
    private float pitch;
    private Vector3 position;
    private Quaternion rotation;

    void Start()
    {
        yaw = Input.GetAxis("Mouse X");
        pitch = Input.GetAxis("Mouse Y");

        position = Camera.main.transform.position;
        rotation = Quaternion.identity;
    }

    void Update()
    {
        float movementSpeed = this.movementSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintFactor : 1.0f) * Time.deltaTime;

        Vector3 velocity = Vector3.zero;
        if (Input.GetKey(KeyCode.W))
            velocity += transform.forward;
        if (Input.GetKey(KeyCode.A))
            velocity -= transform.right;
        if (Input.GetKey(KeyCode.S))
            velocity -= transform.forward;
        if (Input.GetKey(KeyCode.D))
            velocity += transform.right;
        if (Input.GetKey(KeyCode.Space))
            velocity += Vector3.up;
        if (Input.GetKey(KeyCode.LeftControl))
            velocity -= Vector3.up;
        position += velocity * movementSpeed;

        Vector3 mousePosition = Input.mousePosition;
        if (Input.GetMouseButton(2))
        {
            yaw += Input.GetAxis("Mouse X") * rotationSpeed;
            pitch -= Input.GetAxis("Mouse Y") * rotationSpeed;
            rotation.eulerAngles = new Vector3(pitch, yaw, 0.0f);
        }

        Camera.main.transform.localPosition = position;
        Camera.main.transform.localRotation = rotation;
    }
}

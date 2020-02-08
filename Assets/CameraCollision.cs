using UnityEngine;

public class CameraCollision : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Collision with camera.\n");

        //Rigidbody-Komponente des anderen kollidierenden Objektes
        //einer Variablen zuweisen
        Rigidbody rigidbody = collision.gameObject.GetComponent<Rigidbody>();
        //Dem anderen Rigidbody eine Kraft zufuegen
        //rigidbody.AddForce(Vector3.forward * 10, ForceMode.Impulse);
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Camera was triggered.\n");
    }
}

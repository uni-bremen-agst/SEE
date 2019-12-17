using UnityEngine;

public class WallBehavior : MonoBehaviour
{
    Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        //rb.constraints = RigidbodyConstraints.FreezeRotationZ;
    }

    bool activated = false;

    void OnTriggerEnter(Collider other)
    {     
        if (! activated)
        {
            // apply the force only once
            activated = true;
            Debug.LogFormat("wall {0} in parent {1} triggered by {2}\n", 
                             gameObject.name,
                             transform.parent != null ? transform.parent.name : "none",
                             other.gameObject.name);
            ApplyForce();
            //AddTorque();
        }

    }

    private void AddTorque()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        float torque = 0.3f;
        rb.AddTorque(transform.up * torque * h, ForceMode.Impulse);
        rb.AddTorque(transform.right * torque * v, ForceMode.Impulse);
    }

    private void ApplyForce()
    {
        // where to apply the force: center at the upper edge
        Vector3 location = rb.transform.position;
        location.y += rb.transform.localScale.y / 2.0f;

        // the strength of the force to be applied
        float force = 0.003f;

        // center of mass is at lower edge
        rb.centerOfMass = new Vector3(0, -rb.transform.localScale.y / 2.0f, 0);

        //rb.useGravity = true;
        
        rb.AddForceAtPosition(Vector3.forward * force, location, ForceMode.Impulse);
    }
}


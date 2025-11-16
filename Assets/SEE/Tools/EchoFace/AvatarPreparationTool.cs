using UnityEngine;

public class AvatarPreparationTool : MonoBehaviour
{
    void Start()
    {
        // Disable any component named "AvatarAimingSystem"
        var aimingSystem = GetComponent("AvatarAimingSystem") as Behaviour;
        if (aimingSystem != null) aimingSystem.enabled = false;

        // Disable any component named "DesktopPlayerMovement"
        var movementSystem = GetComponent("DesktopPlayerMovement") as Behaviour;
        if (movementSystem != null) movementSystem.enabled = false;

        // Set position
        transform.position = new Vector3(-0.325f, 0f, 0f);

        // Set rotation
        transform.rotation = Quaternion.Euler(0f, -180f, 0f);

        // Disable Animator if present
        var animator = GetComponent<Animator>();
        if (animator != null) animator.enabled = false;

        Debug.Log("[EchoFace] AvatarPreparationTool: position & rotation set.");
    }
}

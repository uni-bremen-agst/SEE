using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Allows to move the camera with WASD, Shift and Space.
 * 
Written by Windexglow 11-13-10.  Use it, edit it, steal it I don't care.
Converted to C# 27-02-13 - no credit wanted.
Reformatted and cleaned by Ryan Breaker 23-6-18
Original comment:
Simple flycam I made, since I couldn't find any others made public.
Made simple to use (drag and drop, done) for regular keyboard layout.
Controls:
WASD  : Directional movement
Shift : Increase speed
Space : Moves camera directly up per its local Y-axis
*/

public class FlyCamera : MonoBehaviour
{
    // These variables are exposed to the editor and can be changed by the user.
    public float normalSpeed = 10.0f;   // Normal speed without acceleration
    public float acceleration = 25.0f;  // Amount to accelerate when shift is pressed
    public float maximalSpeed = 100.0f; // Maximum speed when holding shift

    public float camSens = 0.15f;   // Mouse sensitivity
    public float rotationFactor = 100f; // degree of spinning for each tick without acceleration

    // the position of the mouse cursor of the last tick
    private Vector3 lastMouse = new Vector3(255, 255, 255);  // kind of in the middle of the screen, rather than at the top (play)

    // the accumulated time of the acceleration across ticks
    private float accelerationPeriod = 1.0f;

    private Vector3 previousPosition = new Vector3(0f, 0f, 0f);
    private Quaternion previousRotation = new Quaternion(0f, 0f, 0f, 0f);

    // Spin the object in given direction around the origin of the object at rotationFactor per tick.
    private void Rotate(Vector3 direction, bool accelerationMode)
    {
        float degree = accelerationMode ? accelerationPeriod * rotationFactor : rotationFactor;
        transform.RotateAround(transform.position, direction, degree * Time.deltaTime);
    }

    // Update is called once per frame
    void Update()
    {
        // if the user wants us to accelerate by holding the shift key
        bool accelerationMode = Input.GetKey(KeyCode.LeftShift);

        // Handle acceleration
        if (accelerationMode)
        {
            // still speeding up
            accelerationPeriod += Time.deltaTime;
        }
        else
        {
            // cool down acceleration by half
            accelerationPeriod = Mathf.Clamp(accelerationPeriod * 0.5f, 1f, 1000f);
        }

        // Rotation; keys have higher priority than mouse pointer
        if (Input.GetKey(KeyCode.E))
        {
            Rotate(Vector3.up, accelerationMode);
        } 
        else if (Input.GetKey(KeyCode.Q))
        {
            Rotate(Vector3.down, accelerationMode);
        }
        else if (Input.GetKey(KeyCode.Y))
        {
            Rotate(Vector3.left, accelerationMode);
        }
        else if (Input.GetKey(KeyCode.X))
        {
            Rotate(Vector3.right, accelerationMode);
        }
        else
        {
            lastMouse = Input.mousePosition - lastMouse;
            lastMouse = new Vector3(-lastMouse.y * camSens, lastMouse.x * camSens, 0);
            lastMouse = new Vector3(transform.eulerAngles.x + lastMouse.x, transform.eulerAngles.y + lastMouse.y, 0);
            transform.eulerAngles = lastMouse;
            lastMouse = Input.mousePosition;  
        }
        // Rotation of the object is done.

        // Moving the object.
        // Keyboard commands give the basic direction
        Vector3 newPosition = GetBaseInput();

        if (accelerationMode)
        {
            // handle acceleration
            newPosition *= accelerationPeriod * acceleration;
            newPosition.x = Mathf.Clamp(newPosition.x, -maximalSpeed, maximalSpeed);
            newPosition.y = Mathf.Clamp(newPosition.y, -maximalSpeed, maximalSpeed);
            newPosition.z = Mathf.Clamp(newPosition.z, -maximalSpeed, maximalSpeed);
        }
        else
        {
            newPosition *= normalSpeed;
        }
    
        // Make the move to the new position
        transform.Translate(newPosition * Time.deltaTime);

        if (previousPosition != transform.position)
        {
            Debug.Log("position: " + transform.position + "\n");
            previousPosition = transform.position;
        }
        if (previousRotation != transform.rotation)
        {
            Debug.Log("rotation: " + transform.rotation + "\n");
            previousRotation = transform.rotation;
        }
    }

    // Returns the basic values, if it's 0 than it's not active.
    private Vector3 GetBaseInput()
    {
        Vector3 p_Velocity = new Vector3();

        // Forwards
        if (Input.GetKey(KeyCode.W))
            p_Velocity += Vector3.forward;

        // Backwards
        if (Input.GetKey(KeyCode.S))
            p_Velocity += Vector3.back;

        // Left
        if (Input.GetKey(KeyCode.A))
            p_Velocity += Vector3.left;

        // Right
        if (Input.GetKey(KeyCode.D))
            p_Velocity += Vector3.right;

        // Up
        if (Input.GetKey(KeyCode.Space))
            p_Velocity += Vector3.up;

        // Down
        if (Input.GetKey(KeyCode.LeftControl))
            p_Velocity += Vector3.down;

        return p_Velocity;
    }
}

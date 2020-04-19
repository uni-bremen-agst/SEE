using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// Input from a keyboard. Supports throttle and movement direction.
    /// </summary>
    public class KeyboardDevice : InputDevice
    {
        private const KeyCode throttleKey = KeyCode.LeftShift;

        private void Update()
        {
            if (Input.GetKeyUp(throttleKey))
            {
                throttle.Invoke(0);
            }
            if (Input.GetKeyDown(throttleKey))
            {
                throttle.Invoke(1);
            }
            movementDirection.Invoke(GetDirection());
        }

        private Vector3 GetDirection()
        {
            Vector3 direction = new Vector3();
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                direction += Vector3.forward;
            }
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                direction += Vector3.back;
            }
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                direction += Vector3.left;
            }
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                direction += Vector3.right;
            }
            if (Input.GetKey(KeyCode.Q))
            {
                direction += Vector3.down;
            }
            if (Input.GetKey(KeyCode.E))
            {
                direction += Vector3.up;
            }
            return direction;
        }
    }
}
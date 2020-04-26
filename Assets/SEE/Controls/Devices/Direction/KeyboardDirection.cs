using UnityEngine;

namespace SEE.Controls.Devices
{
    /// <summary>
    /// An input device providing the direction of movements based on the keyboard
    /// by WASDQE and the arrow keys.
    /// </summary>
    public class KeyboardDirection: Direction
    {
        /// <summary>
        /// W arrow-up    => forward
        /// S arrow-down  => backward
        /// A arrow-left  => left
        /// D arrow-right => right
        /// Q             => down
        /// E             => up
        /// </summary>
        public override Vector3 Value
        {
            get
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
}
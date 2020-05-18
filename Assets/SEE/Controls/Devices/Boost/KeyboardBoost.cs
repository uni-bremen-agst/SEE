using UnityEngine;

namespace SEE.Controls.Devices
{
    /// <summary>
    /// An input device providing a boost factor for movements based on a keyboard.
    /// </summary>
    public class KeyboardBoost : Boost
    {
        [Tooltip("The value to be added to the boost factor for each increment."), Range(0.01f, 10.0f)]
        public float Delta = 0.1f;

        private void Update()
        {
            if (Input.GetKey(KeyCode.Plus))
            {
                boost += Delta;
            } 
            else if (Input.GetKey(KeyCode.Minus))
            {
                boost -= Delta;
            }
            if (boost <= 0)
            {
                boost = 0.01f;
            }
        }
    }
}

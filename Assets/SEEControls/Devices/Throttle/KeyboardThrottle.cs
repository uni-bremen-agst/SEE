using UnityEngine;

namespace SEE.Controls.Devices
{
    public class KeyboardThrottle : Throttle
    {
        private const KeyCode throttleKey = KeyCode.LeftShift;

        public override float Value
        {
            get => Input.GetKey(throttleKey) ? 1 : 0;
        }
    }
}
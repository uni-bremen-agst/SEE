using UnityEngine;
using UnityEngine.Events;

namespace Autohand{
    //THIS MAY NOT WORK AS A GRABBABLE AT THIS TIME - Try PhysicsGadgetSlider instead
    public class PhysicsGadgetButton : PhysicsGadgetConfigurableLimitReader{
        bool pressed = false;

        [Tooltip("The percentage (0-1) from the required value needed to call the event, if threshold is 0.1 OnPressed will be called at 0.9, and OnUnpressed at 0.1"), Min(0.01f)]
        public float threshold = 0.1f;
        public bool lockOnPressed = false;
        [Space]
        public UnityEvent OnPressed;
        public UnityEvent OnUnpressed;

        Vector3 startPos;
        Vector3 pressedPos;
        float pressedValue;

        new protected void Start(){
            base.Start();
            startPos = transform.localPosition;
        }


        protected void FixedUpdate(){
            var value = GetValue();
            if(!pressed && value+threshold >= 1) {
                Pressed();
            }
            else if(!lockOnPressed && pressed && value-threshold <= 0){
                Unpressed();
            }

            if (value < 0)
                transform.localPosition = startPos;

            if (pressed && lockOnPressed && value + threshold < pressedValue)
                transform.localPosition = pressedPos;
        }


        public void Pressed() {
            pressed = true;
            pressedValue = GetValue();
            pressedPos = transform.localPosition;
            OnPressed?.Invoke();
        }

        public void Unpressed(){
            pressed = false;
            OnUnpressed?.Invoke();
        }

        public void Unlock() {
            lockOnPressed = false;
            GetComponent<Rigidbody>().WakeUp();
        }
    }
}

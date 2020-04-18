using UnityEngine;

namespace SEE.Controls
{
    public abstract class InputDevice : MonoBehaviour
    {
        [Tooltip("Name of the device")]
        public string Name;

        protected ButtonChangeEvent buttonA = new ButtonChangeEvent();
        public void ListenButtonA(UnityEngine.Events.UnityAction<bool> call)
        {
            buttonA.AddListener(call);
        }

        protected ButtonChangeEvent buttonB = new ButtonChangeEvent();
        public void ListenButtonB(UnityEngine.Events.UnityAction<bool> call)
        {
            buttonB.AddListener(call);
        }

        protected AxisEvent throttle = new AxisEvent();
        public void ListenThrottle(UnityEngine.Events.UnityAction<float> call)
        {
            throttle.AddListener(call);
        }

        protected AxisEvent trigger = new AxisEvent();
        public void ListenTrigger(UnityEngine.Events.UnityAction<float> call)
        {
            trigger.AddListener(call);
        }

        protected Vector3Event movementDirection = new Vector3Event();
        public void ListenMovemementDirection(UnityEngine.Events.UnityAction<Vector3> call)
        {
            movementDirection.AddListener(call);
        }

        protected Vector3Event pointingDirection = new Vector3Event();
        public void ListenPointingDirection(UnityEngine.Events.UnityAction<Vector3> call)
        {
            pointingDirection.AddListener(call);
        }

        protected AxisEvent scroll = new AxisEvent();
        public void ListenScroll(UnityEngine.Events.UnityAction<float> call)
        {
            scroll.AddListener(call);
        }
    }
}
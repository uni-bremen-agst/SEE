using UnityEngine;

namespace Dissonance.Demo
{
    public class ToggleInvertMute
        : MonoBehaviour
    {
        public VoiceBroadcastTrigger Trigger;

        public bool IsUnmuted
        {
            set
            {
                if (Trigger)
                    Trigger.IsMuted = !value;
            }
        }
    }
}

using Dissonance;
using SEE.Controls;
using UnityEngine;

namespace SEE.Dissonance
{
    /// <summary>
    /// This component controls whether the global voice chat is muted or not.
    /// </summary>
    public class VoiceChatInputController : MonoBehaviour
    {
        [Tooltip("The voice broadcast trigger that will be muted or unmuted when the voice chat toggle is pressed.")]
        public VoiceBroadcastTrigger Trigger;

        /// <summary>
        /// Toggles the global voice chat when the voice chat toggle is pressed.
        /// </summary>
        private void Update()
        {
            if (Trigger != null && SEEInput.ToggleVoiceChat())
            {
                Trigger.IsMuted = !Trigger.IsMuted;
                Debug.Log("Voice chat is now " + (Trigger.IsMuted ? "muted.\n" : "unmuted.\n"));
            }
        }
    }
}

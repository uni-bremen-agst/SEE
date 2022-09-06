using UnityEngine;
using Crosstales.RTVoice;

namespace CrazyMinnow.SALSA.RTVoice
{
    /// <summary>
    /// SALSA / RT-Voice integration example using the RT-Voice Speaker.Speak method
    /// </summary>
    [AddComponentMenu("Crazy Minnow Studio/SALSA LipSync/Add-ons/Salsa_RTVoice")]
    public class Salsa_RTVoice : MonoBehaviour
    {
        public AudioSource audioSrc; // AudioSource used by SALSA
        public string speakText = "This is a test using SALSA with RT-Voice"; // Text to pass to Speak
        public bool speak; // Inspector button to fire the speak event

        /// <summary>
        /// Get the AudioSource component used by SALSA
        /// </summary>
        void Awake()
        {
            if (!audioSrc) audioSrc = GetComponent<AudioSource>();
        }

        /// <summary>
        /// This is only used for testing and can be deleted in an implementation where you
        /// make your own call's to [Speaker.Speak]. Click [Speak] in this inspector
        /// to demonstrate send the [speakText] to the [Speaker.SpeakNative] RT-Voice method.
        /// </summary>
        void LateUpdate()
        {
            if (speak)
            {
                speak = false;
                Speaker.Instance.Speak(speakText, audioSrc, Speaker.Instance.VoicesForCulture("en")[0]);
            }
        }
    }
}
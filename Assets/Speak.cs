using UnityEngine;
using Crosstales.RTVoice;
using Crosstales.RTVoice.Model;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// This component is intended to be attached to a UMA character
    /// that is supposed to speak. The character must have a <see cref="AudioSource"/>
    /// component attached to it.
    /// </summary>
    public class Speak : MonoBehaviour
    {
        /// <summary>
        /// The audio source used to say the text.
        /// </summary>
        private AudioSource audioSource;

        /// <summary>
        /// The voice used to speak.
        /// </summary>
        private Voice voice;

        /// <summary>
        /// Sets <see cref="audioSource"/>. If no <see cref="AudioSource"/>
        /// can be found, this component will be disabled.
        /// </summary>
        void Start()
        {
            if (!TryGetComponent(out audioSource))
            {
                Debug.LogError("No AudioSource found.\n");
                enabled = false;
            }
        }

        /// <summary>
        /// Dumps the available voices on the current platform to the
        /// debugging console.
        /// </summary>
        private void DumpVoices()
        {
            foreach (Voice voice in Speaker.Instance.Voices)
            {
                Debug.Log($"Voice: {voice}\n");
            }
        }

        /// <summary>
        /// You can use Speech Synthesis Markup Language (SSML) to
        /// influence the pronounciation. See, for instance,
        /// https://cloud.google.com/text-to-speech/docs/ssml
        /// </summary>
        const string text = "Welcome to the wonderful world of SEE, "
            + "<prosody rate=\"slow\"><say-as interpret-as=\"characters\">S E E</say-as></prosody>, "
            + "for software engineering experience. "
            + "SEE let's you visualize your software as code cities. "
            + "The hierarchical decomposition of a program forms a tree. "
            + "The leaves of this tree are visualized as blocks where "
            + "different metrics can be used to determine the width, height, "
            + "depth, and color of the blocks. "
            + "Inner nodes of this tree can be visualized as nested circles or rectangles "
            + "depending on the layout you choose. "
            + "Dependencies can be depicted by connecting edges between blocks. "
            + "You can hover on the objects to get additional details. "
            + "You can zoom in and out of a code city using the mouse wheel. "
            + "You can drag the code city by moving the mouse while holding the "
            + "middle mouse button pressed. "
            + "You can reset the code city to its original position by hitting key, "
            + "<prosody rate = \"slow\"><say-as interpret-as=\"characters\">R </say-as></prosody>. "
            + "You can circle around a focused code city using the mouse while "
            + "holding the right mouse button. "
            + "You can move forward, backward, or sideways using the keys, "
            + "<prosody rate = \"slow\"><say-as interpret-as= \"characters\"> W A S D</say-as></prosody>, "
            + "as in many computer games. "
            + "If you want to navigate freely through the room, for instance, from "
            + "one table to another one, just hit the key, <prosody rate = \"slow\"><say-as interpret-as= \"characters\"> L </say-as></prosody>, "
            + "which will unlock you from the focused city.If unlocked, you can additionally "
            + "use the keys, <prosody rate = \"slow\"><say-as interpret-as= \"characters\">Q </say-as></prosody>, "
            + "and, <prosody rate = \"slow\"><say-as interpret-as= \"characters\"> E </say-as></prosody>, "
            + "to move up and down. "
            + "To bring up the menu for additional actions, just hit the space bar. "
            + "And now <emphasis level=\"strong\">have fun</emphasis>!";

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.T))
            {                
                Say(text);
            }
        }

        /// <summary>
        /// Speaks the given <paramref name="text"/>. The text can be annotated
        /// in Speech Synthesis Markup Language (SSML).
        /// 
        /// A female US English voice will be used if available.
        /// </summary>
        /// <param name="text">text to be spoken</param>
        private void Say(string text)
        {
            /// Note: We do not set <see cref="voice"/> in <see cref="Start"/>
            /// because we do not want to rely on the order in which the various
            /// <see cref="Start"/> calls are being made by Unity. RTVoice has
            /// its own <see cref="Start"/> which retrieves the available voices
            /// from the system. If that <see cref="Start"/> is called after ours,
            /// <see cref="Speaker.Instance.VoiceForGender"/> cannot return any voice.
            if (voice == null)
            {
                voice = Speaker.Instance.VoiceForGender(Crosstales.RTVoice.Model.Enum.Gender.FEMALE, culture: "en-US");
                if (voice == null)
                {
                    Debug.LogWarning("Requested voice not found.\n");
                    DumpVoices();
                }
            }
            Speaker.Instance.Speak(text, audioSource, voice: voice);
        }
    }
}
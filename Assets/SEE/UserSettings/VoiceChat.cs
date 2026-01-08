using Dissonance;
using System;
using UnityEngine;

namespace SEE.User
{
    /// <summary>
    /// The kinds of voice-chats system we support. None means no voice
    /// chat whatsoever.
    /// </summary>
    public enum VoiceChatSystems
    {
        None = 0,       // no voice chat
        Dissonance = 1, // Dissonance voice chat
    }

    [Serializable]
    internal static class VoiceChat
    {
        /// <summary>
        /// Starts the selected voice chat system according to <see cref="VoiceChatSystem"/>.
        /// </summary>
        /// <param name="voiceChatSystem">The kind of voice chat.</param>
        public static void StartVoiceChat(VoiceChatSystems voiceChatSystem)
        {
            switch (voiceChatSystem)
            {
                case VoiceChatSystems.Dissonance:
                    EnableDissonance(true);
                    break;
                case VoiceChatSystems.None:
                    EnableDissonance(false);
                    break;
                default:
                    EnableDissonance(false);
                    throw new NotImplementedException($"Unhanded voice chat option {voiceChatSystem}.");
            }
        }

        /// <summary>
        /// Shuts down the voice chat system.
        /// </summary>
        /// <param name="voiceChatSystem">The kind of voice chat.</param>
        public static void EndVoiceChat(VoiceChatSystems voiceChatSystem)
        {
            switch (voiceChatSystem)
            {
                case VoiceChatSystems.None:
                    // nothing to be done
                    break;
                case VoiceChatSystems.Dissonance:
                    // nothing to be done
                    break;
                default:
                    throw new NotImplementedException($"Unhanded voice chat option {voiceChatSystem}.");
            }
        }

        /// <summary>
        /// Enables/disables Dissonance as the voice chat system.
        /// </summary>
        /// <param name="enable">Whether to enable Dissonance.</param>
        private static void EnableDissonance(bool enable)
        {
            // The DissonanceComms is initially active and the local player is not muted and not deafened.
            DissonanceComms dissonanceComms = UnityEngine.Object.FindAnyObjectByType<DissonanceComms>(FindObjectsInactive.Include);
            if (dissonanceComms != null)
            {
                dissonanceComms.IsMuted = !enable;
                dissonanceComms.IsDeafened = !enable;
                dissonanceComms.enabled = enable;
            }
            else
            {
                Debug.LogError($"There is no {typeof(DissonanceComms)} in the current scene.\n");
            }
        }
    }
}

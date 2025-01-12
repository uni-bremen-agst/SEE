using SEE.Controls.KeyActions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEE.Controls.VoiceActions
{
    internal class VoiceBindings
    {
        private static readonly Dictionary<VoiceAction, bool> voiceActionStates = new();
        /// <summary>
        /// Checks if a user said this Action just before
        /// </summary>
        /// <param name="action">Action to be asked about</param>
        /// <returns></returns>
        public static bool isSaid(VoiceAction action)
        {
            // Check if the action has been triggered
            if (voiceActionStates.TryGetValue(action, out bool isSaid) && isSaid)
            {
                // Reset the state after being checked
                voiceActionStates[action] = false;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the ActionState when user request this action
        /// </summary>
        /// <param name="action"></param>
        /// <param name="state"></param>
        public static void SetVoiceActionState(VoiceAction action, bool state)
        {
            voiceActionStates[action] = state;
        }

    }
}

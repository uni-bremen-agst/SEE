﻿using UnityEngine;
using UnityEngine.UI;

namespace Crosstales.RTVoice.UI
{
   /// <summary>Speaks the name of a voice with the actual voice.</summary>
   [RequireComponent(typeof(Text))]
   [HelpURL("https://crosstales.com/media/data/assets/rtvoice/api/class_crosstales_1_1_r_t_voice_1_1_u_i_1_1_speak_u_i_text_voice.html")]
   public class SpeakUITextVoice : SpeakUIText
   {
      #region Overridden methods

      protected override string speak(string text)
      {
         return Mode == Crosstales.RTVoice.Model.Enum.SpeakMode.Speak
            ? Speaker.Instance.Speak(text, Source, Speaker.Instance.VoiceForName(TextComponent.text), true, Rate, Pitch, Volume)
            : Speaker.Instance.SpeakNative(text, Speaker.Instance.VoiceForName(TextComponent.text), Rate, Pitch, Volume);
      }

      #endregion
   }
}
// © 2021-2022 crosstales LLC (https://www.crosstales.com)
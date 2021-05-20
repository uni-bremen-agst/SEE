using UnityEngine;

namespace Crosstales.RTVoice
{
   /// <summary>Wrapper of the main component from RT-Voice for MonoBehaviour-access (like "SendMessage").</summary>
   [ExecuteInEditMode]
   [DisallowMultipleComponent]
   [HelpURL("https://www.crosstales.com/media/data/assets/rtvoice/api/class_crosstales_1_1_r_t_voice_1_1_live_speaker.html")]
   public class LiveSpeaker : MonoBehaviour
   {
      private static readonly char[] splitChar = {';'};


      #region Public methods

      /// <summary>Speaks a text with a given wrapper -> native mode.</summary>
      /// <param name="wrapper">Wrapper with the speech details.</param>
      public void SpeakNativeLive(Model.Wrapper wrapper)
      {
         Speaker.Instance.SpeakNative(wrapper);
      }

      /// <summary>Speaks a text with a given array of arguments (native mode).</summary>
      /// <param name="args">Argument string delimited by ';': 0 = text, 1 = culture (optional), 2 = voiceName (optional), 3 = rate (optional), 4 = volume (optional), 5 = pitch (optional).</param>
      public void SpeakNativeLive(string args)
      {
         if (!string.IsNullOrEmpty(args))
         {
            SpeakNativeLive(args.Split(splitChar, System.StringSplitOptions.RemoveEmptyEntries));
         }
         else
         {
            Debug.LogWarning("'args' is null or empty!", this);
         }
      }

      /// <summary>Speaks a text with a given array of arguments (native mode).</summary>
      /// <param name="args">Argument index: 0 = text, 1 = culture (optional), 2 = voiceName (optional), 3 = rate (optional), 4 = pitch (optional), 5 = volume (optional).</param>
      public void SpeakNativeLive(string[] args)
      {
         if (args != null && args.Length >= 1)
         {
            string text = args[0];

            string culture = null;
            if (args.Length >= 2)
            {
               culture = args[1];
            }

            Model.Voice voice = null;
            if (args.Length >= 3)
            {
               voice = Speaker.Instance.VoiceForName(args[2]);
            }

            float rate = 1f;
            if (args.Length >= 4)
            {
               if (!float.TryParse(args[3], out rate))
               {
                  Debug.LogWarning("Argument 3 (= rate) is not a number: '" + args[3] + "'", this);
                  rate = 1f;
               }
            }

            float pitch = 1f;
            if (args.Length >= 5)
            {
               if (!float.TryParse(args[4], out pitch))
               {
                  Debug.LogWarning("Argument 4 (= pitch) is not a number: '" + args[4] + "'", this);
                  pitch = 1f;
               }
            }

            float volume = 1f;
            if (args.Length >= 6)
            {
               if (!float.TryParse(args[5], out volume))
               {
                  Debug.LogWarning("Argument 5 (= volume) is not a number: '" + args[5] + "'", this);
                  volume = 1f;
               }
            }

            if (voice == null)
            {
               voice = Speaker.Instance.VoiceForCulture(culture);
            }

            SpeakNativeLive(new Model.Wrapper(text, voice, rate, pitch, volume, true)); //TODO add ForceSSML as parameter?
         }
         else
         {
            Debug.LogError("'args' is null or wrong number of arguments given!" + System.Environment.NewLine + "Please verify that you pass a string-array with at least one argument (text).", this);
         }
      }

      /// <summary>Speaks a text with a given wrapper.</summary>
      /// <param name="wrapper">Wrapper with the speech details.</param>
      public void SpeakLive(Model.Wrapper wrapper)
      {
         Speaker.Instance.Speak(wrapper);
      }

      /// <summary>
      /// Speaks a text with a given array of arguments.
      /// <remarks>Important: you can't specify the AudioSource with this method!</remarks>
      /// </summary>
      /// <param name="args">Argument string delimited by ';': 0 = text, 1 = culture (optional), 2 = voiceName (optional), 3 = rate (optional), 4 = volume (optional), 5 = pitch (optional).</param>
      public void SpeakLive(string args)
      {
         if (!string.IsNullOrEmpty(args))
         {
            SpeakLive(args.Split(splitChar, System.StringSplitOptions.RemoveEmptyEntries));
         }
         else
         {
            Debug.LogWarning("'args' is null or empty!", this);
         }
      }

      /// <summary>
      /// Speaks a text with a given array of arguments.
      /// <remarks>Important: you can't specify the AudioSource with this method!</remarks>
      /// </summary>
      /// <param name="args">Argument index: 0 = text, 1 = culture (optional), 2 = voiceName (optional), 3 = rate (optional), 4 = pitch (optional), 5 = volume (optional).</param>
      public void SpeakLive(string[] args)
      {
         if (args != null && args.Length >= 1)
         {
            string text = args[0];

            string culture = null;
            if (args.Length >= 2)
            {
               culture = args[1];
            }

            Model.Voice voice = null;
            if (args.Length >= 3)
            {
               voice = Speaker.Instance.VoiceForName(args[2]);
            }

            float rate = 1f;
            if (args.Length >= 4)
            {
               if (!float.TryParse(args[3], out rate))
               {
                  Debug.LogWarning("Argument 3 (= rate) is not a number: '" + args[3] + "'", this);
                  rate = 1f;
               }
            }

            float pitch = 1f;
            if (args.Length >= 5)
            {
               if (!float.TryParse(args[4], out pitch))
               {
                  Debug.LogWarning("Argument 5 (= pitch) is not a number: '" + args[4] + "'", this);
                  pitch = 1f;
               }
            }

            float volume = 1f;
            if (args.Length >= 6)
            {
               if (!float.TryParse(args[5], out volume))
               {
                  Debug.LogWarning("Argument 4 (= volume) is not a number: '" + args[5] + "'", this);
                  volume = 1f;
               }
            }

            if (voice == null)
            {
               voice = Speaker.Instance.VoiceForCulture(culture);
            }

            SpeakLive(new Model.Wrapper(text, voice, rate, pitch, volume, null));
         }
         else
         {
            Debug.LogError("'args' is null or wrong number of arguments given!" + System.Environment.NewLine + "Please verify that you pass a string-array with at least one argument (text).", this);
         }
      }

      /// <summary>Silence all active TTS-voices.</summary>
      public void SilenceLive()
      {
         Speaker.Instance.Silence();
      }

      #endregion
   }
}
// © 2015-2021 crosstales LLC (https://www.crosstales.com)
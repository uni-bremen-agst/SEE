using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Crosstales.RTVoice.UI
{
   /// <summary>Base-class for all speakable UI elements.</summary>
   [DisallowMultipleComponent]
   public abstract class SpeakUIBase : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
   {
      #region Variables

      [Header("Configuration")] [Tooltip("Voices for the speech."), SerializeField] private Model.VoiceAlias voices;

      [Tooltip("Speak mode (default: 'Speak')."), SerializeField] private Model.Enum.SpeakMode mode = Model.Enum.SpeakMode.Speak;

      [Tooltip("Delay in seconds before the speech starts (default: 1.5)."), Range(0f, 10f), SerializeField]
      private float delay = 1.5f;

      [Tooltip("Speak the text only once (default: false)."), SerializeField] private bool speakOnlyOnce;

      [Tooltip("Silence the speech once exit (default: true)."), SerializeField] private bool silenceOnExit = true;

      [Header("Optional Settings"), Tooltip("AudioSource for the output (optional)."), SerializeField] private AudioSource source;

      [Tooltip("Speech rate of the speaker in percent (1 = 100%, default: 1, optional)."), Range(0f, 3f), SerializeField]
      private float rate = 1f;

      [Tooltip("Speech pitch of the speaker in percent (1 = 100%, default: 1, optional, mobile only)."), Range(0f, 2f), SerializeField]
      private float pitch = 1f;

      [Tooltip("Volume of the speaker in percent (1 = 100%, default: 1, optional, Windows only)."), Range(0f, 1f), SerializeField]
      private float volume = 1f;

      protected float elapsedTime;

      protected string uid;
      protected bool isInside;
      protected bool spoken;

      #endregion


      #region Properties

      /// <summary>Voices for the speech.</summary>
      public Model.VoiceAlias Voices
      {
         get => voices;
         set => voices = value;
      }

      /// <summary>Speak mode.</summary>
      public Model.Enum.SpeakMode Mode
      {
         get => mode;
         set => mode = value;
      }

      /// <summary>Delay in seconds before the speech starts.</summary>
      public float Delay
      {
         get => delay;
         set => delay = Mathf.Abs(value);
      }

      /// <summary>Speak the text only once.</summary>
      public bool SpeakOnlyOnce
      {
         get => speakOnlyOnce;
         set => speakOnlyOnce = value;
      }

      /// <summary>Silence the speech once exit.</summary>
      public bool SilenceOnExit
      {
         get => silenceOnExit;
         set => silenceOnExit = value;
      }

      /// <summary>AudioSource for the output (optional).</summary>
      public AudioSource Source
      {
         get => source;
         set => source = value;
      }

      /// <summary>Speech rate of the speaker in percent (range: 0-3).</summary>
      public float Rate
      {
         get => rate;
         set => rate = Mathf.Clamp(value, 0, 3);
      }

      /// <summary>Speech pitch of the speaker in percent (range: 0-2).</summary>
      public float Pitch
      {
         get => pitch;
         set => pitch = Mathf.Clamp(value, 0, 2);
      }

      /// <summary>Volume of the speaker in percent (range: 0-1).</summary>
      public float Volume
      {
         get => volume;
         set => volume = Mathf.Clamp01(value);
      }

      #endregion


      #region MonoBehaviour methods

      private void Start()
      {
         Speaker.Instance.OnSpeakComplete += onSpeakComplete;
      }

      #endregion


      #region Implemented methods

      public virtual void OnPointerEnter(PointerEventData eventData)
      {
         isInside = true;
      }

      public virtual void OnPointerExit(PointerEventData eventData)
      {
         isInside = false;

         if (SilenceOnExit)
         {
            if (uid != null)
            {
               if (Mode == Model.Enum.SpeakMode.Speak)
               {
                  Speaker.Instance.Silence(uid);
               }
               else
               {
                  Speaker.Instance.Silence();
               }
            }
         }
      }

      #endregion


      #region Private methods

      protected virtual string speak(string text)
      {
         return Mode == Model.Enum.SpeakMode.Speak
            ? Speaker.Instance.Speak(text, Source, Voices.Voice, true, Rate, Pitch, Volume)
            : Speaker.Instance.SpeakNative(text, Voices.Voice, Rate, Pitch, Volume);
      }

      protected virtual void onSpeakComplete(Model.Wrapper wrapper)
      {
         if (wrapper.Uid == uid)
         {
            isInside = false;
            spoken = true;
            elapsedTime = 0f;
            uid = null;
         }
      }

      #endregion
   }
}
// © 2021 crosstales LLC (https://www.crosstales.com)
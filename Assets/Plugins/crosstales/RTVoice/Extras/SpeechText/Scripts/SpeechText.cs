using UnityEngine;

namespace Crosstales.RTVoice.Tool
{
   /// <summary>Allows to speak and store generated audio.</summary>
   [HelpURL("https://www.crosstales.com/media/data/assets/rtvoice/api/class_crosstales_1_1_r_t_voice_1_1_tool_1_1_speech_text.html")]
   public class SpeechText : MonoBehaviour
   {
      #region Variables

      [Header("Configuration")]
      [UnityEngine.Serialization.FormerlySerializedAsAttribute("Text")] [Tooltip("Text to speak."), TextArea(5, 15), SerializeField]
      private string text = string.Empty;

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("Voices")] [Tooltip("Voices for the speech."), SerializeField]
      private Model.VoiceAlias voices;

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("Mode")] [Tooltip("Speak mode (default: 'Speak')."), SerializeField]
      private Model.Enum.SpeakMode mode = Model.Enum.SpeakMode.Speak;


      [UnityEngine.Serialization.FormerlySerializedAsAttribute("Source")] [Header("Optional Settings"), Tooltip("AudioSource for the output (optional)."), SerializeField]
      private AudioSource source;

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("Rate")] [Tooltip("Speech rate of the speaker in percent (1 = 100%, default: 1, optional)."), Range(0f, 3f), SerializeField]
      private float rate = 1f;

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("Pitch")] [Tooltip("Speech pitch of the speaker in percent (1 = 100%, default: 1, optional, mobile only)."), Range(0f, 2f), SerializeField]
      private float pitch = 1f;

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("Volume")] [Tooltip("Volume of the speaker in percent (1 = 100%, default: 1, optional, Windows only)."), Range(0f, 1f), SerializeField]
      private float volume = 1f;


      [UnityEngine.Serialization.FormerlySerializedAsAttribute("PlayOnStart")] [Header("Behaviour Settings"), Tooltip("Enable speaking of the text on start (default: false)."), SerializeField]
      private bool playOnStart;

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("Delay")] [Tooltip("Delay in seconds until the speech for this text starts (default: 0.1)."), SerializeField]
      private float delay = 0.1f;


      [UnityEngine.Serialization.FormerlySerializedAsAttribute("GenerateAudioFile")] [Header("Output File Settings"), Tooltip("Generate audio file on/off (default: false)."), SerializeField]
      private bool generateAudioFile;

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("FileName")] [Tooltip("File name (incl. path) for the generated audio."), SerializeField]
      private string fileName = @"_generatedAudio/Speech01";

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("FileInsideAssets")] [Tooltip("Is the generated file path inside the Assets-folder (current project)? If this option is enabled, it prefixes the path with 'Application.dataPath'."), SerializeField]
      private bool fileInsideAssets = true;

      private string uid;

      private bool played;

      //private long lastPlaytime = long.MinValue;
      private float lastSpeaktime = float.MinValue;

      #endregion


      #region Events

      [Header("Events")] public SpeechTextStartEvent OnStarted;
      public SpeechTextStartEvent OnCompleted;

      /// <summary>An event triggered whenever a SpeechText 'Speak' is started.</summary>
      public event SpeechTextStart OnSpeechTextStart;

      /// <summary>An event triggered whenever a SpeechText 'Speak' is completed.</summary>
      public event SpeechTextComplete OnSpeechTextComplete;

      #endregion


      #region Properties

      /// <summary>Text to speak.</summary>
      public string Text
      {
         get => text;
         set => text = value;
      }

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

      /// <summary>Enable speaking of the text on start.</summary>
      public bool PlayOnStart
      {
         get => playOnStart;
         set => playOnStart = value;
      }

      /// <summary>Delay until the speech for this text starts.</summary>
      public float Delay
      {
         get => delay;
         set => delay = Mathf.Abs(value);
      }

      /// <summary>Generate audio file on/off.</summary>
      public bool GenerateAudioFile
      {
         get => generateAudioFile;
         set => generateAudioFile = value;
      }

      /// <summary>File name (incl. path) for the generated audio.</summary>
      public string FileName
      {
         get => fileName;
         set => fileName = value;
      }

      /// <summary>Is the generated file path inside the Assets-folder (current project)? If this option is enabled, it prefixes the path with 'Application.dataPath'.</summary>
      public bool FileInsideAssets
      {
         get => fileInsideAssets;
         set => fileInsideAssets = value;
      }

      #endregion


      #region MonoBehaviour methods

      private void Start()
      {
         Speaker.Instance.OnVoicesReady += onVoicesReady;
         Speaker.Instance.OnSpeakStart += onSpeakStart;
         Speaker.Instance.OnSpeakComplete += onSpeakComplete;

         play();
      }

      private void OnDestroy()
      {
         if (Speaker.Instance != null)
         {
            Speaker.Instance.OnVoicesReady -= onVoicesReady;
            Speaker.Instance.OnSpeakStart -= onSpeakStart;
            Speaker.Instance.OnSpeakComplete -= onSpeakComplete;
         }
      }

      private void OnValidate()
      {
         if (delay < 0f)
            delay = 0f;

         rate = Mathf.Clamp(rate, 0f, 3f);
         pitch = Mathf.Clamp(pitch, 0f, 2f);
         volume = Mathf.Clamp01(volume);

         if (!string.IsNullOrEmpty(fileName))
            fileName = Util.Helper.ValidateFile(fileName);
      }

      #endregion


      #region Public methods

      /// <summary>Speak the text.</summary>
      public void Speak()
      {
         float currentTime = Time.realtimeSinceStartup;

         if (lastSpeaktime + Util.Constants.SPEAK_CALL_SPEED < currentTime)
         {
            lastSpeaktime = currentTime;

            Silence();

            string path = null;

            if (generateAudioFile)
            {
               if (!string.IsNullOrEmpty(fileName))
               {
                  path = fileInsideAssets
                     ? Util.Helper.ValidateFile(Application.dataPath + @"/" + fileName)
                     : Util.Helper.ValidateFile(fileName);
               }
               else
               {
                  Debug.LogWarning("'FileName' is null or empty! Can't generate audio file.", this);
               }
            }

            if (Util.Helper.isEditorMode)
            {
#if UNITY_EDITOR
               Speaker.Instance.SpeakNative(text, voices.Voice, rate, pitch, volume);
               if (generateAudioFile)
               {
                  Speaker.Instance.Generate(text, path, voices.Voice, rate, pitch, volume);
               }
#endif
            }
            else
            {
               uid = mode == Model.Enum.SpeakMode.Speak
                  ? Speaker.Instance.Speak(text, source, voices.Voice, true, rate, pitch, volume, path)
                  : Speaker.Instance.SpeakNative(text, voices.Voice, rate, pitch, volume);
            }
         }
         else
         {
            Debug.LogWarning("'Speak' called too fast - please slow down!", this);
         }
      }

      /// <summary>Silence the speech.</summary>
      public void Silence()
      {
         if (Util.Helper.isEditorMode)
         {
            Speaker.Instance.Silence();
         }
         else
         {
            if (!string.IsNullOrEmpty(uid))
               Speaker.Instance.Silence(uid);
         }
      }

      #endregion


      #region Private methods

      private void play()
      {
         if (playOnStart && !played && Speaker.Instance.Voices.Count > 0)
         {
            played = true;

            Invoke(nameof(Speak), delay);
         }
      }

      #endregion


      #region Callbacks

      private void onVoicesReady()
      {
         play();
      }

      private void onSpeakStart(Model.Wrapper wrapper)
      {
         if (wrapper.Uid.Equals(uid))
            onStart();
      }

      private void onSpeakComplete(Model.Wrapper wrapper)
      {
         if (wrapper.Uid.Equals(uid))
            onComplete();
      }

      #endregion


      #region Event-trigger methods

      private void onStart()
      {
         if (Util.Config.DEBUG)
            Debug.Log("onStart", this);

         if (!Util.Helper.isEditorMode)
            OnStarted?.Invoke();

         OnSpeechTextStart?.Invoke();
      }

      private void onComplete()
      {
         if (Util.Config.DEBUG)
            Debug.Log("onComplete", this);

         if (!Util.Helper.isEditorMode)
            OnCompleted?.Invoke();

         OnSpeechTextComplete?.Invoke();
      }

      #endregion
   }
}
// © 2016-2021 crosstales LLC (https://www.crosstales.com)
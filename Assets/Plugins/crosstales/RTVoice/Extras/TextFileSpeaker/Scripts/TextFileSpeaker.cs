using UnityEngine;

namespace Crosstales.RTVoice.Tool
{
   /// <summary>Allows to speak text files.</summary>
   [ExecuteInEditMode]
   [HelpURL("https://www.crosstales.com/media/data/assets/rtvoice/api/class_crosstales_1_1_r_t_voice_1_1_tool_1_1_text_file_speaker.html")]
   public class TextFileSpeaker : MonoBehaviour
   {
      #region Variables

      [Header("Configuration")]
      [UnityEngine.Serialization.FormerlySerializedAsAttribute("TextFiles")] [Tooltip("Text files to speak."), SerializeField]
      private TextAsset[] textFiles;

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


      [UnityEngine.Serialization.FormerlySerializedAsAttribute("PlayOnStart")] [Header("Behaviour Settings"), Tooltip("Enable speaking of a random text file on start (default: false)."), SerializeField]
      private bool playOnStart;

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("PlayAllOnStart")] [Tooltip("Enable speaking of a random text file on start (default: false)."), SerializeField]
      private bool playAllOnStart;

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("SpeakRandom")] [Tooltip("Speaks the text files in random order (default: false)."), SerializeField]
      private bool speakRandom;

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("Delay")] [Tooltip("Delay in seconds until the speech for this text starts (default: 0.1)."), SerializeField]
      private float delay = 0.1f;

      private string[] texts;
      private string[] randomTexts;

      private int textIndex = -1;
      private int randomTextIndex = -1;

      //private Voice voice;

      private static readonly System.Random rnd = new System.Random();

      private string uid = string.Empty;

      private bool played;
      private bool playAll;

      private float lastSpeaktime = float.MinValue;

      private int lastNumberOfTextfiles = -1;

      #endregion


      #region Properties

      /// <summary>Text files to speak.</summary>
      public TextAsset[] TextFiles
      {
         get => textFiles;
         set => textFiles = value;
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

      /// <summary>
      /// Enable speaking of a all random text files on start (default: false).
      /// NOTE: this can only be stopped with the "StopAll"-method
      /// </summary>

      public bool PlayAllOnStart
      {
         get => playAllOnStart;
         set => playAllOnStart = value;
      }

      /// <summary>Speaks the text files in random order.</summary>
      public bool SpeakRandom
      {
         get => speakRandom;
         set => speakRandom = value;
      }

      /// <summary>Delay in seconds until the speech for this text starts.</summary>
      public float Delay
      {
         get => delay;
         set => delay = Mathf.Abs(value);
      }

      #endregion


      #region Events

      [Header("Events")] public TextFileSpeakerStartEvent OnStarted;
      public TextFileSpeakerCompleteEvent OnCompleted;

      /// <summary>An event triggered whenever a TextFileSpeaker 'Speak' is started.</summary>
      public event TextFileSpeakerStart OnTextFileSpeakerStart;

      /// <summary>An event triggered whenever a TextFileSpeaker 'Speak' is completed.</summary>
      public event TextFileSpeakerComplete OnTextFileSpeakerComplete;

      #endregion


      #region MonoBehaviour methods

      private void Start()
      {
         Speaker.Instance.OnVoicesReady += onVoicesReady;
         Speaker.Instance.OnSpeakStart += onSpeakStart;
         Speaker.Instance.OnSpeakComplete += onSpeakComplete;

         Reload();

         play();
      }

      private void OnDestroy()
      {
         if (!Util.Helper.isEditorMode && Speaker.Instance != null)
         {
            Speaker.Instance.OnVoicesReady -= onVoicesReady;
            Speaker.Instance.OnSpeakStart -= onSpeakStart;
            Speaker.Instance.OnSpeakComplete -= onSpeakComplete;
         }
      }

      private void Update()
      {
         if (textFiles.Length != lastNumberOfTextfiles)
            Reload();
      }

      private void OnValidate()
      {
         if (delay < 0f)
            delay = 0f;

         rate = Mathf.Clamp(rate, 0f, 3f);
         pitch = Mathf.Clamp(pitch, 0f, 2f);
         volume = Mathf.Clamp01(volume);
      }

      #endregion


      #region Public methods

      /// <summary>Speaks all texts until StopAll is called.</summary>
      public void SpeakAll()
      {
         playAll = true;
         Next();
      }

      /// <summary>Stops speaking all texts.</summary>
      public void StopAll()
      {
         playAll = false;
         Silence();
      }

      /// <summary>Speaks the next text (main use for UI).</summary>
      public void Next()
      {
         Next(speakRandom);
      }

      /// <summary>Speaks the next text.</summary>
      /// <param name="random">Speak a random text</param>
      public void Next(bool random)
      {
         int index;

         if (random)
         {
            if (randomTextIndex > -1 && randomTextIndex + 1 < randomTexts.Length)
            {
               randomTextIndex++;
            }
            else
            {
               randomTextIndex = 0;
            }

            index = randomTextIndex;
         }
         else
         {
            if (textIndex > -1 && textIndex + 1 < texts.Length)
            {
               textIndex++;
            }
            else
            {
               textIndex = 0;
            }

            index = textIndex;
         }

         SpeakText(index, random);
      }

      /// <summary>Speaks the previous text (main use for UI).</summary>
      public void Previous()
      {
         Previous(speakRandom);
      }

      /// <summary>Speaks the previous text.</summary>
      /// <param name="random">Speak a random text</param>
      public void Previous(bool random)
      {
         int index;

         if (random)
         {
            if (randomTextIndex > 0 && randomTextIndex < randomTexts.Length)
            {
               randomTextIndex--;
            }
            else
            {
               randomTextIndex = randomTexts.Length - 1;
            }

            index = randomTextIndex;
         }
         else
         {
            if (textIndex > 0 && textIndex < texts.Length)
            {
               textIndex--;
            }
            else
            {
               textIndex = texts.Length - 1;
            }

            index = textIndex;
         }

         SpeakText(index, random);
      }

      /// <summary>Speaks a text (main use for UI).</summary>
      public void Speak()
      {
         Next();
      }

      /// <summary>Speaks a text with an optional index.</summary>
      /// <param name="index">Index of the text (default: -1 (random), optional).</param>
      /// <param name="random">Speak a random text (default: false, optional)</param>
      /// <returns>UID of the speaker.</returns>
      public string SpeakText(int index = -1, bool random = false)
      {
         float currentTime = Time.realtimeSinceStartup;

         if (lastSpeaktime + Util.Constants.SPEAK_CALL_SPEED < currentTime)
         {
            lastSpeaktime = currentTime;

            Silence();

            string result = string.Empty;

            if (texts.Length > 0)
            {
               if (random)
               {
                  if (index < 0)
                  {
                     result = speak(randomTexts[rnd.Next(randomTexts.Length)]);
                  }
                  else
                  {
                     if (index < texts.Length)
                     {
                        result = speak(randomTexts[index]);
                     }
                     else
                     {
                        Debug.LogWarning("Text file index is out of bounds: " + index +
                                         " - maximal index is: " + (randomTexts.Length - 1), this);
                        result = speak(randomTexts[randomTexts.Length - 1]);
                     }
                  }
               }
               else
               {
                  if (index < 0)
                  {
                     result = speak(texts[rnd.Next(texts.Length)]);
                  }
                  else
                  {
                     if (index < texts.Length)
                     {
                        result = speak(texts[index]);
                     }
                     else
                     {
                        Debug.LogWarning("Text file index is out of bounds: " + index +
                                         " - maximal index is: " + (texts.Length - 1), this);
                        result = speak(texts[texts.Length - 1]);
                     }
                  }
               }
            }
            else
            {
               Debug.LogError("No text files added - speak cancelled!", this);
            }

            uid = result;
         }
         else
         {
            Debug.LogWarning("'SpeakText' called too fast - please slow down!", this);
         }

         return uid;
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

      /// <summary>Reloads all text files (e.g. when new text files were added during runtime).</summary>
      public void Reload()
      {
         if (textFiles.Length > 0)
         {
            texts = new string[textFiles.Length];
            randomTexts = new string[textFiles.Length];
            lastNumberOfTextfiles = textFiles.Length;

            for (int ii = 0; ii < textFiles.Length; ii++)
            {
               if (textFiles[ii] != null)
               {
                  randomTexts[ii] = texts[ii] = textFiles[ii].text;
               }
               else
               {
                  randomTexts[ii] = texts[ii] = string.Empty;
               }
            }

            randomTexts.CTShuffle();

            textIndex = -1;
            randomTextIndex = -1;
         }
      }

      #endregion


      #region Private methods

      private void play()
      {
         if (!Util.Helper.isEditorMode)
         {
            if (!played && Speaker.Instance.Voices.Count > 0)
            {
               played = true;

               if (playOnStart)
               {
                  Invoke(nameof(Next), delay);
               }
               else if (playAllOnStart)
               {
                  Invoke(nameof(SpeakAll), delay);
               }
            }
         }
      }

      private string speak(string text)
      {
         return mode == Model.Enum.SpeakMode.Speak
            ? Speaker.Instance.Speak(text, source, voices.Voice, true, rate, pitch, volume)
            : Speaker.Instance.SpeakNative(text, voices.Voice, rate, pitch, volume);
      }

      #endregion


      #region Callbacks

      private void onVoicesReady()
      {
         play();
      }

      #endregion


      #region Event-trigger methods

      private void onSpeakStart(Crosstales.RTVoice.Model.Wrapper wrapper)
      {
         if (wrapper.Uid.Equals(uid))
         {
            if (!Util.Helper.isEditorMode)
               OnStarted?.Invoke();

            OnTextFileSpeakerStart?.Invoke();
         }
      }

      private void onSpeakComplete(Model.Wrapper wrapper)
      {
         if (wrapper.Uid.Equals(uid))
         {
            if (!Util.Helper.isEditorMode)
               OnCompleted?.Invoke();

            OnTextFileSpeakerComplete?.Invoke();

            if (playAll)
               Invoke(nameof(Next), delay);
         }
      }

      #endregion
   }
}
// © 2016-2021 crosstales LLC (https://www.crosstales.com)
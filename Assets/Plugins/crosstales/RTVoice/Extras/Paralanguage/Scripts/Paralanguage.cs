using UnityEngine;
using System.Collections;
using System.Linq;

namespace Crosstales.RTVoice.Tool
{
   /// <summary>Para-language simulator with audio files.</summary>
   //[ExecuteInEditMode]
   [RequireComponent(typeof(AudioSource))]
   [HelpURL("https://www.crosstales.com/media/data/assets/rtvoice/api/class_crosstales_1_1_r_t_voice_1_1_tool_1_1_paralanguage.html")]
   public class Paralanguage : MonoBehaviour
   {
      #region Variables

      [Header("Configuration")]
      [UnityEngine.Serialization.FormerlySerializedAsAttribute("Text")] [Tooltip("Text to speak."), TextArea(3, 15), SerializeField]
      private string text = string.Empty;

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("Voices")] [Tooltip("Voices for the speech."), SerializeField]
      private Model.VoiceAlias voices;

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("Mode")] [Tooltip("Speak mode (default: 'Speak')."), SerializeField]
      private Model.Enum.SpeakMode mode = Model.Enum.SpeakMode.Speak;

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("Clips")] [Tooltip("Audio clips to play."), SerializeField]
      private AudioClip[] clips;

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("Rate")] [Header("Optional Settings"), Tooltip("Speech rate of the speaker in percent (1 = 100%, default: 1, optional)."), Range(0f, 3f), SerializeField]
      private float rate = 1f;

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("Pitch")] [Tooltip("Speech pitch of the speaker in percent (1 = 100%, default: 1, optional)."), Range(0f, 2f), SerializeField]
      private float pitch = 1f;

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("Volume")] [Tooltip("Volume of the speaker in percent (1 = 100%, default: 1, optional)."), Range(0f, 1f), SerializeField]
      private float volume = 1f;


      [UnityEngine.Serialization.FormerlySerializedAsAttribute("PlayOnStart")] [Header("Behaviour Settings"), Tooltip("Enable speaking of the text on start (default: false)."), SerializeField]
      private bool playOnStart;

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("Delay")] [Tooltip("Delay until the speech for this text starts (default: 0.1"), SerializeField]
      private float delay = 0.1f;

      private static readonly System.Text.RegularExpressions.Regex splitRegex = new System.Text.RegularExpressions.Regex(@"#.*?#");

      private string uid;

      private bool played;

      private readonly System.Collections.Generic.IDictionary<int, string> stack = new System.Collections.Generic.SortedDictionary<int, string>();

      private readonly System.Collections.Generic.IDictionary<string, AudioClip> clipDict = new System.Collections.Generic.Dictionary<string, AudioClip>();

      private AudioSource audioSource;

      private bool next;

      #endregion


      #region Events

      [Header("Events")] public ParalanguageStartEvent OnStarted;
      public ParalanguageCompleteEvent OnCompleted;

      /// <summary>An event triggered whenever a Paralanguage 'Speak' is started.</summary>
      public event ParalanguageStart OnParalanguageStart;

      /// <summary>An event triggered whenever a Paralanguage 'Speak' is completed.</summary>
      public event ParalanguageComplete OnParalanguageComplete;

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

      /// <summary>Audio clips to play.</summary>
      public AudioClip[] Clips
      {
         get => clips;
         set => clips = value;
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

      #endregion


      #region MonoBehaviour methods

      private void OnDestroy()
      {
         if (!Util.Helper.isEditorMode && Speaker.Instance != null)
         {
            Speaker.Instance.OnVoicesReady -= onVoicesReady;
            Speaker.Instance.OnSpeakComplete -= onSpeakComplete;
         }
      }

      private void Awake()
      {
         audioSource = GetComponent<AudioSource>();
         audioSource.playOnAwake = false;
         audioSource.loop = false;
         audioSource.Stop(); //always stop the AudioSource at startup
      }

      private void Start()
      {
         Speaker.Instance.OnVoicesReady += onVoicesReady;
         Speaker.Instance.OnSpeakComplete += onSpeakComplete;

         play();
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

      /// <summary>Speak the text.</summary>
      public void Speak()
      {
         Silence();
         stack.Clear();
         clipDict.Clear();

         foreach (AudioClip clip in clips)
         {
            clipDict.Add("#" + clip.name + "#", clip);
         }

         string[] speechParts = splitRegex.Split(text).Where(s => s != string.Empty).ToArray();

         System.Text.RegularExpressions.MatchCollection mc = splitRegex.Matches(text);

         int index = 0;

         foreach (System.Text.RegularExpressions.Match match in mc)
         {
            //Debug.Log("MATCH: '" + match + "' - " + Text.IndexOf(match.ToString(), index));
            stack.Add(index = text.CTIndexOf(match.ToString(), index), match.ToString());
            index++;
         }

         index = 0;
         foreach (string speech in speechParts)
         {
            //Debug.Log("PART: '" + speech + "' - " + Text.IndexOf(speech, index));
            stack.Add(index = text.CTIndexOf(speech, index), speech);
            index++;
         }

         StartCoroutine(processStack());
      }

      /// <summary>Silence the speech.</summary>
      public void Silence()
      {
         StopAllCoroutines();

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

      private IEnumerator processStack()
      {
         onStart();

         foreach (System.Collections.Generic.KeyValuePair<int, string> kvp in stack)
         {
            if (kvp.Value.CTStartsWith("#"))
            {
               clipDict.TryGetValue(kvp.Value, out AudioClip clip);

               if (clipDict.TryGetValue(kvp.Value, out clip))
               {
                  audioSource.clip = clip;
                  audioSource.Play();

                  do
                  {
                     yield return null;
                  } while (audioSource.isPlaying);
               }
               else
               {
                  Debug.LogWarning("Clip not found: " + kvp.Value, this);
               }
            }
            else
            {
               next = false;

               uid = mode == Model.Enum.SpeakMode.Speak
                  ? Speaker.Instance.Speak(kvp.Value, audioSource, voices.Voice, true, rate, pitch, volume)
                  : Speaker.Instance.SpeakNative(kvp.Value, voices.Voice, rate, pitch, volume);

               do
               {
                  yield return null;
               } while (!next);
            }
         }

         onComplete();
      }

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

      private void onSpeakComplete(Model.Wrapper wrapper)
      {
         if (wrapper.Uid.Equals(uid))
         {
            next = true;
         }
      }

      #endregion


      #region Event-trigger methods

      private void onStart()
      {
         if (Util.Config.DEBUG)
            Debug.Log("onStart", this);

         if (!Util.Helper.isEditorMode)
            OnStarted?.Invoke();

         OnParalanguageStart?.Invoke();
      }

      private void onComplete()
      {
         if (Util.Config.DEBUG)
            Debug.Log("onComplete", this);

         if (!Util.Helper.isEditorMode)
            OnCompleted?.Invoke();

         OnParalanguageComplete?.Invoke();
      }

      #endregion
   }
}
// © 2018-2021 crosstales LLC (https://www.crosstales.com)
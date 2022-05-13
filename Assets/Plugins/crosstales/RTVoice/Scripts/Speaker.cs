﻿using UnityEngine;
using System.Linq;
using UnityEngine.Serialization;

namespace Crosstales.RTVoice
{
   /// <summary>Main component of RT-Voice.</summary>
   [ExecuteInEditMode]
   [DisallowMultipleComponent]
   [RequireComponent(typeof(LiveSpeaker))]
   [HelpURL("https://www.crosstales.com/media/data/assets/rtvoice/api/class_crosstales_1_1_r_t_voice_1_1_speaker.html")]
   public class Speaker : Crosstales.Common.Util.Singleton<Speaker>
   {
      #region Variables

      [FormerlySerializedAs("CustomProvider")] [Header("Custom Provider"), Tooltip("Custom provider for RT-Voice."), SerializeField]
      private Crosstales.RTVoice.Provider.BaseCustomVoiceProvider customProvider;

      [FormerlySerializedAs("CustomMode")] [Tooltip("Enable or disable the custom provider (default: false)."), SerializeField]
      private bool customMode;

      [FormerlySerializedAs("ESpeakMode")] [Header("eSpeak Settings"), Tooltip("Enable or disable eSpeak for standalone platforms (default: false)."), SerializeField]
      private bool eSpeakMode;

      [Tooltip("eSpeak application name/path (default: 'espeak')."), SerializeField] private string eSpeakApplication = "espeak";

      [Tooltip("eSpeak application data path (default: empty)."), SerializeField] private string eSpeakDataPath = string.Empty;

      [FormerlySerializedAs("ESpeakModifier")] [Tooltip("Active modifier for all eSpeak voices (default: none, m1-m6 = male, f1-f4 = female)."), SerializeField]
      private Crosstales.RTVoice.Model.Enum.ESpeakModifiers eSpeakModifier = Crosstales.RTVoice.Model.Enum.ESpeakModifiers.none;


      [FormerlySerializedAs("AndroidEngine")] [Header("Android Settings"), Tooltip("Active speech engine under Android (default: empty)."), SerializeField]
      private string androidEngine = string.Empty;


      [Header("Windows Settings"), Tooltip("Force 32bit under Windows standalone (default: false)."), SerializeField]
      private bool windowsForce32bit;


      [FormerlySerializedAs("AutoClearTags")] [Header("Advanced Settings"), Tooltip("Automatically clear tags from speeches depending on the capabilities of the current TTS-system (default: false)."), SerializeField]
      private bool autoClearTags;

      [FormerlySerializedAs("Caching"), Tooltip("Enable or disable the caching of generated speeches (default: true)."), SerializeField]
      private bool caching = true;


      [FormerlySerializedAs("SilenceOnDisable")] [Header("Behaviour Settings"), Tooltip("Silence any speeches if this component gets disabled (default: false)."), SerializeField]
      private bool silenceOnDisable;

      [FormerlySerializedAs("SilenceOnFocusLost")] [FormerlySerializedAs("SilenceOnFocustLost")] [Tooltip("Silence any speeches if the application loses the focus. Otherwise the speeches are paused and unpaused (default: false)."), SerializeField]
      private bool silenceOnFocusLost;

      [Tooltip("Starts and stops the Speaker depending on the focus and running state (default: true)."), SerializeField]
      private bool handleFocus = true;

      /*
      /// <summary>Files to delete at the application end.</summary>
      public static readonly System.Collections.Generic.List<string> FilesToDelete = new System.Collections.Generic.List<string>();
      */

      private float cleanUpTimer;

      private Crosstales.RTVoice.Provider.IVoiceProvider voiceProvider;
      private Crosstales.RTVoice.Provider.MainVoiceProvider mainVoiceProvider;
      private Crosstales.RTVoice.Provider.BaseCustomVoiceProvider customVoiceProvider;
      private readonly System.Collections.Generic.Dictionary<string, AudioSource> genericSources = new System.Collections.Generic.Dictionary<string, AudioSource>();
      private readonly System.Collections.Generic.Dictionary<string, AudioSource> providedSources = new System.Collections.Generic.Dictionary<string, AudioSource>();

      private int speechCount;
      private int busyCount;
      private bool deleted; //ignore in reset!

      private static readonly char[] splitCharWords = { ' ' };
      private const float cleanUpTime = 5f; //in seconds

      private static bool loggedVPIsNull;
#if (!UNITY_WSA && !UNITY_XBOXONE && !UNITY_WEBGL) || UNITY_EDITOR
      private System.Threading.Thread deleteWorker;
#endif
#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
      private static string currentTextToSpeak;
      private static Crosstales.RTVoice.Model.Wrapper currentWrapper;
      private const float delayPause = 0.5f;
      private static float lastTimePaused = 0;
#endif

      #endregion


      #region Properties

      /// <summary>Custom provider for RT-Voice.</summary>
      public Crosstales.RTVoice.Provider.BaseCustomVoiceProvider CustomProvider
      {
         get => customProvider;
         set
         {
            if (customProvider == value) return;

            customProvider = value;

            ReloadProvider();
         }
      }

      /// <summary>Enables or disables the custom provider.</summary>
      public bool CustomMode
      {
         get => customMode;
         set
         {
            if (customMode == value) return;

            customMode = value;

            ReloadProvider();
         }
      }

      /// <summary>Enable or disable eSpeak for standalone platforms.</summary>
      public bool ESpeakMode
      {
         get => eSpeakMode;
         set
         {
            if (eSpeakMode == value) return;

            eSpeakMode = value;

            ReloadProvider();
         }
      }

      /// <summary>eSpeak application name/path.</summary>
      public string ESpeakApplication
      {
         get => eSpeakApplication;
         set => eSpeakApplication = value;
      }

      /// <summary>eSpeak application data path.</summary>
      public string ESpeakDataPath
      {
         get => eSpeakDataPath;
         set => eSpeakDataPath = value;
      }

      /// <summary>Active modifier for all eSpeak voices.</summary>
      public Crosstales.RTVoice.Model.Enum.ESpeakModifiers ESpeakModifier
      {
         get => eSpeakModifier;
         set => eSpeakModifier = value;
      }

      /// <summary>
      /// Active speech engine under Android.
      /// Note: the default Google Engine is "com.google.android.tts"
      /// </summary>
      public string AndroidEngine
      {
         get => androidEngine;
         set
         {
            if (androidEngine == value || !Crosstales.RTVoice.Util.Helper.isAndroidPlatform) return;

            androidEngine = value;

            ReloadProvider();
         }
      }

      /// <summary>
      /// Force 32bit under Windows standalone
      /// </summary>
      public bool WindowsForce32bit
      {
         get => windowsForce32bit;
         set
         {
            if (windowsForce32bit == value || !Crosstales.RTVoice.Util.Helper.isWindowsPlatform) return;

            windowsForce32bit = value;

            ReloadProvider();
         }
      }

      /// <summary>Automatically clear tags from speeches depending on the capabilities of the current TTS-system.</summary>
      public bool AutoClearTags
      {
         get => autoClearTags;
         set => autoClearTags = value;
      }

      /// <summary>Enable or disable the caching of generated speeches.</summary>
      public bool Caching
      {
         get => caching;
         set => caching = value;
      }

      /// <summary>Silence any speeches if this component gets disabled.</summary>
      public bool SilenceOnDisable
      {
         get => silenceOnDisable;
         set => silenceOnDisable = value;
      }

      /// <summary>Silence any speeches if the application loses the focus.</summary>
      public bool SilenceOnFocusLost
      {
         get => silenceOnFocusLost;
         set => silenceOnFocusLost = value;
      }

      /// <summary>Starts and stops the Speaker depending on the focus and running state.</summary>
      public bool HandleFocus
      {
         get => handleFocus;
         set => handleFocus = value;
      }

/*
      /// <summary>Don't destroy gameobject during scene switches.</summary>
      public bool DontDestroy
      {
         get => dontDestroy;
         set => dontDestroy = value;
      }
*/
      /// <summary>Number of active speeches.</summary>
      public int SpeechCount
      {
         get => speechCount;
         private set => speechCount = value < 0 ? 0 : value;
      }

      /// <summary>Number of active calls.</summary>
      public int BusyCount
      {
         get => busyCount;
         private set => busyCount = value < 0 ? 0 : value;
      }

      /// <summary>Are all voices ready to speak?</summary>
      public bool areVoicesReady { get; private set; }

      /// <summary>Checks if TTS is available on this system.</summary>
      /// <returns>True if TTS is available on this system.</returns>
      public bool isTTSAvailable
      {
         get
         {
            if (voiceProvider != null)
               return voiceProvider.Voices.Count > 0;

            logVPIsNull();

            return false;
         }
      }

      /// <summary>Checks if RT-Voice is speaking on this system.</summary>
      /// <returns>True if RT-Voice is speaking on this system.</returns>
      public bool isSpeaking => SpeechCount > 0;

      /// <summary>Checks if RT-Voice is busy on this system.</summary>
      /// <returns>True if RT-Voice is busy on this system.</returns>
      public bool isBusy => BusyCount > 0;

      /// <summary>Is standalone TTS enforced?</summary>
      public bool enforcedStandaloneTTS { get; private set; }

      /// <summary>Is RT-Voice paused?</summary>
      public bool isPaused { get; private set; }

      /// <summary>Is RT-Voice muted?</summary>
      public bool isMuted { get; private set; }


      #region Provider delegates

      /// <summary>Returns the extension of the generated audio files.</summary>
      /// <returns>Extension of the generated audio files.</returns>
      public string AudioFileExtension
      {
         get
         {
            if (voiceProvider != null)
               return voiceProvider.AudioFileExtension;

            logVPIsNull();

            return ".wav"; //best guess
         }
      }

      /// <summary>Returns the default voice name of the current TTS-provider.</summary>
      /// <returns>Default voice name of the current TTS-provider.</returns>
      public string DefaultVoiceName
      {
         get
         {
            if (voiceProvider != null)
               return voiceProvider.DefaultVoiceName;

            logVPIsNull();

            return string.Empty;
         }
      }

      /// <summary>Get all available voices from the current TTS-system.</summary>
      /// <returns>All available voices (alphabetically ordered by 'Name') as a list.</returns>
      public System.Collections.Generic.List<Crosstales.RTVoice.Model.Voice> Voices
      {
         get
         {
            //Debug.Log($"Voices: {voiceProvider}");
            if (voiceProvider != null)
               return voiceProvider.Voices;

            logVPIsNull();

            return new System.Collections.Generic.List<Crosstales.RTVoice.Model.Voice>();
         }
      }

      /// <summary>Indicates if this TTS-system is working directly inside the Unity Editor (without 'Play'-mode).</summary>
      /// <returns>True if this TTS-system is working directly inside the Unity Editor.</returns>
      public bool isWorkingInEditor
      {
         get
         {
            if (voiceProvider != null)
               return voiceProvider.isWorkingInEditor;

            logVPIsNull();

            return false;
         }
      }

      /// <summary>Indicates if this TTS-system is working with 'Play'-mode inside the Unity Editor.</summary>
      /// <returns>True if this TTS-system is working with 'Play'-mode inside the Unity Editor.</returns>
      public bool isWorkingInPlaymode
      {
         get
         {
            if (voiceProvider != null)
               return voiceProvider.isWorkingInPlaymode;

            logVPIsNull();

            return false;
         }
      }

      /// <summary>Maximal length of the speech text (in characters) for the current TTS-system.</summary>
      /// <returns>The maximal length of the speech text.</returns>
      public int MaxTextLength
      {
         get
         {
            if (voiceProvider != null)
               return voiceProvider.MaxTextLength;

            logVPIsNull();

            return 3999; //minimum (Android)
         }
      }

      /// <summary>Indicates if this TTS-system is supporting SpeakNative.</summary>
      /// <returns>True if this TTS-system supports SpeakNative.</returns>
      public bool isSpeakNativeSupported
      {
         get
         {
            if (voiceProvider != null)
               return voiceProvider.isSpeakNativeSupported;

            logVPIsNull();

            return false;
         }
      }

      /// <summary>Indicates if this TTS-system is supporting Speak.</summary>
      /// <returns>True if this TTS-system supports Speak.</returns>
      public bool isSpeakSupported
      {
         get
         {
            if (voiceProvider != null)
               return voiceProvider.isSpeakSupported;

            logVPIsNull();

            return false;
         }
      }

      /// <summary>Indicates if this TTS-system is supporting the current platform.</summary>
      /// <returns>True if this TTS-system supports current platform.</returns>
      public bool isPlatformSupported => voiceProvider?.isPlatformSupported == true;

      /// <summary>Indicates if this TTS-system is supporting SSML.</summary>
      /// <returns>True if this TTS-system supports SSML.</returns>
      public bool isSSMLSupported
      {
         get
         {
            if (voiceProvider != null)
               return voiceProvider.isSSMLSupported;

            logVPIsNull();

            return false;
         }
      }

      /// <summary>Indicates if this TTS-system is an online service like MaryTTS or AWS Polly.</summary>
      /// <returns>True if this TTS-system is an online service.</returns>
      public bool isOnlineService
      {
         get
         {
            if (voiceProvider != null)
               return voiceProvider.isOnlineService;

            logVPIsNull();

            return false;
         }
      }

      /// <summary>Indicates if this TTS-system uses co-routines.</summary>
      /// <returns>True if this TTS-system uses co-routines.</returns>
      public bool hasCoRoutines
      {
         get
         {
            if (voiceProvider != null)
               return voiceProvider.hasCoRoutines;

            logVPIsNull();

            return true;
         }
      }

      /// <summary>Indicates if this TTS-system is supporting IL2CPP.</summary>
      /// <returns>True if this TTS-system supports IL2CPP.</returns>
      public bool isIL2CPPSupported
      {
         get
         {
            if (voiceProvider != null)
               return voiceProvider.isIL2CPPSupported;

            logVPIsNull();

            return true;
         }
      }

      /// <summary>Indicates if this provider returns voices in the Editor mode.</summary>
      /// <returns>True if this provider returns voices in the Editor mode.</returns>
      public bool hasVoicesInEditor
      {
         get
         {
            if (voiceProvider != null)
               return voiceProvider.hasVoicesInEditor;

            logVPIsNull();

            return false;
         }
      }

      /// <summary>Get all available cultures from the current TTS-system (ISO 639-1).</summary>
      /// <returns>All available cultures (alphabetically ordered by 'Culture') as a list.</returns>
      public System.Collections.Generic.List<string> Cultures
      {
         get
         {
            if (voiceProvider != null)
               return voiceProvider.Cultures;

            logVPIsNull();

            return new System.Collections.Generic.List<string>();
         }
      }

      /// <summary>Get all available languages from the current TTS-system.</summary>
      /// <returns>All available languages as a list.</returns>
      public System.Collections.Generic.List<SystemLanguage> Languages
      {
         get
         {
            System.Collections.Generic.List<SystemLanguage> result = new System.Collections.Generic.List<SystemLanguage>();

            if (voiceProvider != null)
            {
               foreach (string code in voiceProvider.Cultures)
               {
                  SystemLanguage lang = Crosstales.RTVoice.Util.Helper.ISO639ToLanguage(code);

                  if (!result.Contains(lang))
                     result.Add(lang);
               }
            }
            else
            {
               logVPIsNull();
            }

            return result;
         }
      }

      /// <summary>Get all available speech engines (works only for Android).</summary>
      /// <returns>All available speech engines as a list.</returns>
      public System.Collections.Generic.List<string> Engines
      {
         get
         {
#if UNITY_ANDROID || UNITY_EDITOR
            if (voiceProvider is Crosstales.RTVoice.Provider.VoiceProviderAndroid android)
               return android.Engines;

            logVPIsNull();
#endif
            return new System.Collections.Generic.List<string>();
         }
      }

      #endregion

      #endregion


      #region Events

      //[Header("Events")]
      public VoicesReadyEvent OnReady;
      public SpeakStartEvent OnSpeakStarted;
      public SpeakCompleteEvent OnSpeakCompleted;
      public ProviderChangeEvent OnProviderChanged;
      public ErrorEvent OnError;


      /// <summary>An event triggered whenever the voices of a provider are ready.</summary>
      public event VoicesReady OnVoicesReady;

      /// <summary>An event triggered whenever a speak is started.</summary>
      public event SpeakStart OnSpeakStart;

      /// <summary>An event triggered whenever a speak is completed.</summary>
      public event SpeakComplete OnSpeakComplete;

      /// <summary>An event triggered whenever a new word is spoken (native, Windows and iOS only).</summary>
      public event SpeakCurrentWord OnSpeakCurrentWord;

      /// <summary>An event triggered whenever a new word is spoken (native, Windows and iOS only).</summary>
      public event SpeakCurrentWordString OnSpeakCurrentWordString;

      /// <summary>An event triggered whenever a new phoneme is spoken (native, Windows only).</summary>
      public event SpeakCurrentPhoneme OnSpeakCurrentPhoneme;

      /// <summary>An event triggered whenever a new viseme is spoken (native, Windows only).</summary>
      public event SpeakCurrentViseme OnSpeakCurrentViseme;

      /// <summary>An event triggered whenever a speak audio generation is started.</summary>
      public event SpeakAudioGenerationStart OnSpeakAudioGenerationStart;

      /// <summary>An event triggered whenever a speak audio generation is completed.</summary>
      public event SpeakAudioGenerationComplete OnSpeakAudioGenerationComplete;

      /// <summary>An event triggered whenever a provider changes (e.g. Windows to MaryTTS).</summary>
      public event ProviderChange OnProviderChange;

      /// <summary>An event triggered whenever an error occurs.</summary>
      public event ErrorInfo OnErrorInfo;

      #endregion


      #region MonoBehaviour methods

      protected override void Awake()
      {
         base.Awake();

         if (instance == this)
         {
            if (!deleted)
            {
               deleted = true;

               //if (Util.Helper.isWindowsPlatform && Util.Config.AUDIOFILE_AUTOMATIC_DELETE) //only delete files under Windows
               if (Crosstales.RTVoice.Util.Config.AUDIOFILE_AUTOMATIC_DELETE)
                  DeleteAudioFiles();
            }

            if (Crosstales.RTVoice.Util.Helper.isLinuxPlatform)
               eSpeakMode = true;

            /*
            if (isESpeakMode && !Util.Helper.isStandalonePlatform)
                ESpeakMode = false;
                */
            initProvider();
         }
      }

      private void Update()
      {
         cleanUpTimer += Time.deltaTime;

         if (cleanUpTimer > cleanUpTime)
         {
            cleanUpTimer = 0f;

            if (genericSources.Count > 0)
            {
               //Debug.Log(genericSources.Count);

               System.Collections.Generic.KeyValuePair<string, AudioSource>[] sources = genericSources.Where(source => source.Value != null && source.Value.clip != null && !source.Value.CTHasActiveClip()).ToArray();
               foreach (System.Collections.Generic.KeyValuePair<string, AudioSource> source in sources)
               {
                  genericSources.Remove(source.Key);
                  Destroy(source.Value);
               }
            }

            if (providedSources.Count > 0)
            {
               System.Collections.Generic.KeyValuePair<string, AudioSource>[] sources = providedSources.Where(source => source.Value != null && source.Value.clip != null && !source.Value.CTHasActiveClip()).ToArray();

               foreach (System.Collections.Generic.KeyValuePair<string, AudioSource> source in sources)
               {
                  //source.Value.clip = null; //remove clip
                  providedSources.Remove(source.Key);
               }
            }
         }
      }

      private void OnDisable()
      {
         if (silenceOnDisable)
            Silence();
      }

      protected override void OnDestroy()
      {
         Silence();

         if (instance == this)
         {
            unsubscribeEvents();
            unsubscribeCustomEvents();
         }

         base.OnDestroy();
      }

      protected override void OnApplicationQuit()
      {
         Silence();

#if UNITY_ANDROID || UNITY_EDITOR
         if (voiceProvider is Crosstales.RTVoice.Provider.VoiceProviderAndroid)
            Crosstales.RTVoice.Provider.VoiceProviderAndroid.ShutdownTTS();
#endif

         /*
         if (!Util.Helper.isEditorMode)
         {
             foreach (string outputFile in FilesToDelete)
             {
                 if (System.IO.File.Exists(outputFile))
                 {
                     try
                     {
                         System.IO.File.Delete(outputFile);
                     }
                     catch (System.Exception ex)
                     {
                         string errorMessage = "Could not delete file '" + outputFile + "'!" + System.Environment.NewLine + ex;
                         Debug.LogError(errorMessage, this);
                     }
                 }
             }
         }
         */

#if (!UNITY_WSA && !UNITY_XBOXONE && !UNITY_WEBGL) || UNITY_EDITOR
         if (deleteWorker?.IsAlive == true)
         {
            if (Crosstales.RTVoice.Util.Constants.DEV_DEBUG)
               Debug.Log("Killing worker", this);

            deleteWorker.Abort(); //TODO dangerous - find a better solution!
         }
#endif

         base.OnApplicationQuit();
      }

      private void OnApplicationFocus(bool hasFocus)
      {
         if (Crosstales.RTVoice.Util.Helper.isMobilePlatform || !Application.runInBackground)
         {
#if UNITY_ANDROID || UNITY_IOS
            if (!TouchScreenKeyboard.isSupported || !TouchScreenKeyboard.visible)
            {
#endif
            if (silenceOnFocusLost)
            {
               if (!hasFocus)
                  Silence();
            }
            else
            {
               if (handleFocus)
               {
                  if (hasFocus)
                  {
                     UnPause();
                  }
                  else
                  {
                     Pause();
                  }
               }
            }
#if UNITY_ANDROID || UNITY_IOS
            }
#endif
         }
      }

      #endregion


      #region Public methods

      /// <summary>Resets this object.</summary>
      //[RuntimeInitializeOnLoadMethod]
      public static void ResetObject()
      {
         DeleteInstance();
         loggedVPIsNull = false;
      }

      /// <summary>
      /// Approximates the speech length in seconds of a given text and rate.
      /// Note: This is an experimental method and doesn't provide an exact value; +/- 15% is "normal"!
      /// </summary>
      /// <param name="text">Text for the length approximation.</param>
      /// <param name="rate">Speech rate of the speaker in percent for the length approximation (1 = 100%, default: 1, optional).</param>
      /// <param name="wordsPerMinute">Words per minute (default: 175, optional).</param>
      /// <param name="timeFactor">Time factor for the calculated value (default: 0.9, optional).</param>
      /// <returns>Approximated speech length in seconds of the given text and rate.</returns>
      public float ApproximateSpeechLength(string text, float rate = 1f, float wordsPerMinute = 175f, float timeFactor = 0.9f)
      {
         float words = text.Split(splitCharWords, System.StringSplitOptions.RemoveEmptyEntries).Length;
         float characters = text.Length - words + 1;
         float ratio = characters / words;

         if (Common.Util.BaseHelper.isWindowsPlatform && !ESpeakMode && !CustomMode)
         {
            if (Mathf.Abs(rate - 1f) > Crosstales.Common.Util.BaseConstants.FLOAT_TOLERANCE)
            {
               //relevant?
               if (rate > 1f)
               {
                  //larger than 1
                  if (rate >= 2.75f)
                  {
                     rate = 2.78f;
                  }
                  else if (rate >= 2.6f && rate < 2.75f)
                  {
                     rate = 2.6f;
                  }
                  else if (rate >= 2.35f && rate < 2.6f)
                  {
                     rate = 2.39f;
                  }
                  else if (rate >= 2.2f && rate < 2.35f)
                  {
                     rate = 2.2f;
                  }
                  else if (rate >= 2f && rate < 2.2f)
                  {
                     rate = 2f;
                  }
                  else if (rate >= 1.8f && rate < 2f)
                  {
                     rate = 1.8f;
                  }
                  else if (rate >= 1.6f && rate < 1.8f)
                  {
                     rate = 1.6f;
                  }
                  else if (rate >= 1.4f && rate < 1.6f)
                  {
                     rate = 1.45f;
                  }
                  else if (rate >= 1.2f && rate < 1.4f)
                  {
                     rate = 1.28f;
                  }
                  else if (rate > 1f && rate < 1.2f)
                  {
                     rate = 1.14f;
                  }
               }
               else
               {
                  //smaller than 1
                  if (rate <= 0.3f)
                  {
                     rate = 0.33f;
                  }
                  else if (rate > 0.3 && rate <= 0.4f)
                  {
                     rate = 0.375f;
                  }
                  else if (rate > 0.4 && rate <= 0.45f)
                  {
                     rate = 0.42f;
                  }
                  else if (rate > 0.45 && rate <= 0.5f)
                  {
                     rate = 0.47f;
                  }
                  else if (rate > 0.5 && rate <= 0.55f)
                  {
                     rate = 0.525f;
                  }
                  else if (rate > 0.55 && rate <= 0.6f)
                  {
                     rate = 0.585f;
                  }
                  else if (rate > 0.6 && rate <= 0.7f)
                  {
                     rate = 0.655f;
                  }
                  else if (rate > 0.7 && rate <= 0.8f)
                  {
                     rate = 0.732f;
                  }
                  else if (rate > 0.8 && rate <= 0.9f)
                  {
                     rate = 0.82f;
                  }
                  else if (rate > 0.9 && rate < 1f)
                  {
                     rate = 0.92f;
                  }
               }
            }
         }

         float speechLength = words / (wordsPerMinute / 60 * rate);

         if (ratio < 2)
         {
            speechLength *= 1f;
         }
         else if (ratio >= 2f && ratio < 3f)
         {
            speechLength *= 1.05f;
         }
         else if (ratio >= 3f && ratio < 3.5f)
         {
            speechLength *= 1.15f;
         }
         else if (ratio >= 3.5f && ratio < 4f)
         {
            speechLength *= 1.2f;
         }
         else if (ratio >= 4f && ratio < 4.5f)
         {
            speechLength *= 1.25f;
         }
         else if (ratio >= 4.5f && ratio < 5f)
         {
            speechLength *= 1.3f;
         }
         else if (ratio >= 5f && ratio < 5.5f)
         {
            speechLength *= 1.4f;
         }
         else if (ratio >= 5.5f && ratio < 6f)
         {
            speechLength *= 1.45f;
         }
         else if (ratio >= 6f && ratio < 6.5f)
         {
            speechLength *= 1.5f;
         }
         else if (ratio >= 6.5f && ratio < 7f)
         {
            speechLength *= 1.6f;
         }
         else if (ratio >= 7f && ratio < 8f)
         {
            speechLength *= 1.7f;
         }
         else if (ratio >= 8f && ratio < 9f)
         {
            speechLength *= 1.8f;
         }
         else
         {
            speechLength *= ratio * (ratio / 100f + 0.02f) + 1f;
         }

         if (speechLength < 0.8f)
            speechLength += 0.6f;

         return speechLength * timeFactor;
      }

      /// <summary>Is a voice available for a given gender and optional culture from the current TTS-system?</summary>
      /// <param name="gender">Gender of the voice</param>
      /// <param name="culture">Culture of the voice (e.g. "en", optional)</param>
      /// <returns>True if a voice is available for a given gender and culture.</returns>
      public bool isVoiceForGenderAvailable(Crosstales.RTVoice.Model.Enum.Gender gender, string culture = "")
      {
         return VoicesForGender(gender, culture).Count > 0;
      }

      /// <summary>Is a voice available for a given gender and language  from the current TTS-system?</summary>
      /// <param name="gender">Gender of the voice</param>
      /// <param name="language">Language of the voice</param>
      /// <returns>True if a voice is available for a given gender and language.</returns>
      public bool isVoiceForGenderAvailable(Crosstales.RTVoice.Model.Enum.Gender gender, SystemLanguage language)
      {
         return isVoiceForGenderAvailable(gender, Crosstales.RTVoice.Util.Helper.LanguageToISO639(language));
      }

      /// <summary>Get all available voices for a given gender and optional culture from the current TTS-system.</summary>
      /// <param name="gender">Gender of the voice</param>
      /// <param name="culture">Culture of the voice (e.g. "en", optional)</param>
      /// <param name="isFuzzy">Always returns voices if there is no match with the gender and/or culture (default: false, optional)</param>
      /// <returns>All available voices (alphabetically ordered by 'Name') for a given gender and culture as a list.</returns>
      public System.Collections.Generic.List<Crosstales.RTVoice.Model.Voice> VoicesForGender(Crosstales.RTVoice.Model.Enum.Gender gender, string culture = "", bool isFuzzy = false)
      {
         System.Collections.Generic.List<Crosstales.RTVoice.Model.Voice> voices = new System.Collections.Generic.List<Crosstales.RTVoice.Model.Voice>(Voices.Count);

         if (string.IsNullOrEmpty(culture))
         {
            if (Crosstales.RTVoice.Model.Enum.Gender.UNKNOWN == gender)
               return Voices;

            voices.AddRange(Voices.Where(voice => voice.Gender == gender));
         }
         else
         {
            if (Crosstales.RTVoice.Model.Enum.Gender.UNKNOWN == gender)
               return VoicesForCulture(culture, isFuzzy);

            voices.AddRange(VoicesForCulture(culture, isFuzzy).Where(voice => voice.Gender == gender));

            if (voices.Count == 0)
               return VoicesForCulture(culture, isFuzzy);
         }

         return voices;
      }

      /// <summary>Get all available voices for a given gender and language from the current TTS-system.</summary>
      /// <param name="gender">Gender of the voice</param>
      /// <param name="language">Language of the voice</param>
      /// <param name="isFuzzy">Always returns voices if there is no match with the gender and/or language (default: false, optional)</param>
      /// <returns>All available voices (alphabetically ordered by 'Name') for a given gender and language as a list.</returns>
      public System.Collections.Generic.List<Crosstales.RTVoice.Model.Voice> VoicesForGender(Crosstales.RTVoice.Model.Enum.Gender gender, SystemLanguage language, bool isFuzzy = false)
      {
         return VoicesForGender(gender, Crosstales.RTVoice.Util.Helper.LanguageToISO639(language), isFuzzy);
      }

      /// <summary>Get a voice from for a given gender, optional culture and optional index from the current TTS-system.</summary>
      /// <param name="gender">Gender of the voice</param>
      /// <param name="culture">Culture of the voice (e.g. "en", optional)</param>
      /// <param name="index">Index of the voice (default: 0, optional)</param>
      /// <param name="fallbackCulture">Fallback culture of the voice (default "en", optional)</param>
      /// <param name="isFuzzy">Always returns voices if there is no match with the gender and/or culture (default: false, optional)</param>
      /// <returns>Voice for the given gender, culture and index.</returns>
      public Crosstales.RTVoice.Model.Voice VoiceForGender(Crosstales.RTVoice.Model.Enum.Gender gender, string culture = "", int index = 0, string fallbackCulture = "en", bool isFuzzy = false)
      {
         Crosstales.RTVoice.Model.Voice result = null;

         System.Collections.Generic.List<Crosstales.RTVoice.Model.Voice> voices = VoicesForGender(gender, culture, isFuzzy);

         if (voices.Count > 0)
         {
            if (voices.Count - 1 >= index && index >= 0)
            {
               result = voices[index];
            }
            else
            {
               //use the default voice
               //result = voices[0];
               Debug.LogWarning($"No voice for gender '{gender}' and culture '{culture}' with index {index} found! Speaking with the default voice!", this);
            }
         }
         else
         {
            voices = VoicesForGender(gender, fallbackCulture, isFuzzy);

            if (voices.Count > 0)
            {
               result = voices[0];
               Debug.LogWarning($"No voice for gender '{gender}' and culture '{culture}' found! Speaking with the fallback culture: '{fallbackCulture}'", this);
            }
            else
            {
               //use the default voice
               Debug.LogWarning($"No voice for gender '{gender}' and culture '{culture}' found! Speaking with the default voice!", this);
            }
         }

         return result;
      }

      /// <summary>Get a voice from for a given gender, language and index from the current TTS-system.</summary>
      /// <param name="gender">Gender of the voice</param>
      /// <param name="language">Language of the voice</param>
      /// <param name="index">Index of the voice (default: 0, optional)</param>
      /// <param name="isFuzzy">Always returns voices if there is no match with the gender and/or language (default: false, optional)</param>
      /// <returns>Voice for the given gender, language and index.</returns>
      public Crosstales.RTVoice.Model.Voice VoiceForGender(Crosstales.RTVoice.Model.Enum.Gender gender, SystemLanguage language, int index = 0, bool isFuzzy = false)
      {
         return VoiceForGender(gender, Crosstales.RTVoice.Util.Helper.LanguageToISO639(language), index, "en", isFuzzy);
      }

      /// <summary>Is a voice available for a given culture from the current TTS-system?</summary>
      /// <param name="culture">Culture of the voice (e.g. "en")</param>
      /// <returns>True if a voice is available for a given culture.</returns>
      public bool isVoiceForCultureAvailable(string culture)
      {
         return VoicesForCulture(culture).Count > 0;
      }

      /// <summary>Is a voice available for a given language from the current TTS-system?</summary>
      /// <param name="language">Language of the voice</param>
      /// <returns>True if a voice is available for a given language.</returns>
      public bool isVoiceForLanguageAvailable(SystemLanguage language)
      {
         return isVoiceForCultureAvailable(Crosstales.RTVoice.Util.Helper.LanguageToISO639(language));
      }

      /// <summary>Get all available voices for a given culture from the current TTS-system.</summary>
      /// <param name="culture">Culture of the voice (e.g. "en")</param>
      /// <param name="isFuzzy">Always returns voices if there is no match with the culture (default: false, optional)</param>
      /// <returns>All available voices (alphabetically ordered by 'Name') for a given culture as a list.</returns>
      public System.Collections.Generic.List<Crosstales.RTVoice.Model.Voice> VoicesForCulture(string culture, bool isFuzzy = false)
      {
         if (string.IsNullOrEmpty(culture))
         {
            if (Crosstales.RTVoice.Util.Config.DEBUG)
               Debug.LogWarning("The given 'culture' is null or empty! Returning all available voices.", this);

            return Voices;
         }

         string _culture = culture.Trim().Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty);
#if UNITY_WSA || UNITY_XBOXONE
         System.Collections.Generic.List<Crosstales.RTVoice.Model.Voice> voices = Voices.Where(s => s.SimplifiedCulture.StartsWith(_culture, System.StringComparison.OrdinalIgnoreCase)).OrderBy(s => s.Name).ToList();
#else
         System.Collections.Generic.List<Crosstales.RTVoice.Model.Voice> voices = Voices.Where(s => s.SimplifiedCulture.StartsWith(_culture, System.StringComparison.InvariantCultureIgnoreCase)).OrderBy(s => s.Name).ToList();
#endif
         if (voices.Count == 0 && isFuzzy)
         {
            return Voices;
         }

         return voices;
      }

      /// <summary>Get all available voices for a given language from the current TTS-system.</summary>
      /// <param name="language">Language of the voice</param>
      /// <param name="isFuzzy">Always returns voices if there is no match with the language (default: false, optional)</param>
      /// <returns>All available voices (alphabetically ordered by 'Name') for a given language as a list.</returns>
      public System.Collections.Generic.List<Crosstales.RTVoice.Model.Voice> VoicesForLanguage(SystemLanguage language, bool isFuzzy = false)
      {
         return VoicesForCulture(Crosstales.RTVoice.Util.Helper.LanguageToISO639(language), isFuzzy);
      }

      /// <summary>Get a voice from for a given culture and optional index from the current TTS-system.</summary>
      /// <param name="culture">Culture of the voice (e.g. "en")</param>
      /// <param name="index">Index of the voice (default: 0, optional)</param>
      /// <param name="fallbackCulture">Fallback culture of the voice (default "en", optional)</param>
      /// <param name="isFuzzy">Always returns voices if there is no match with the culture (default: false, optional)</param>
      /// <returns>Voice for the given culture and index.</returns>
      public Crosstales.RTVoice.Model.Voice VoiceForCulture(string culture, int index = 0, string fallbackCulture = "en", bool isFuzzy = false)
      {
         Crosstales.RTVoice.Model.Voice result = null;

         if (!string.IsNullOrEmpty(culture))
         {
            System.Collections.Generic.List<Crosstales.RTVoice.Model.Voice> voices = VoicesForCulture(culture, isFuzzy);

            if (voices.Count > 0)
            {
               if (voices.Count - 1 >= index && index >= 0)
               {
                  result = voices[index];
               }
               else
               {
                  //use the default voice
                  //result = voices[0];
                  Debug.LogWarning($"No voices for culture '{culture}' with index {index} found! Speaking with the default voice!", this);
               }
            }
            else
            {
               voices = VoicesForCulture(fallbackCulture, isFuzzy);

               if (voices.Count > 0)
               {
                  result = voices[0];
                  Debug.LogWarning($"No voices for culture '{culture}' found! Speaking with the fallback culture: '{fallbackCulture}'", this);
               }
               else
               {
                  //use the default voice
                  Debug.LogWarning($"No voices for culture '{culture}' found! Speaking with the default voice!", this);
               }
            }
         }

         return result;
      }

      /// <summary>Get a voice from for a given language and optional index from the current TTS-system.</summary>
      /// <param name="language">language of the voice</param>
      /// <param name="index">Index of the voice (default: 0, optional)</param>
      /// <param name="isFuzzy">Always returns voices if there is no match with the language (default: false, optional)</param>
      /// <returns>Voice for the given language and index.</returns>
      public Crosstales.RTVoice.Model.Voice VoiceForLanguage(SystemLanguage language, int index = 0, bool isFuzzy = false)
      {
         return VoiceForCulture(Crosstales.RTVoice.Util.Helper.LanguageToISO639(language), index, "en", isFuzzy);
      }

      /// <summary>Is a voice available for a given name from the current TTS-system?</summary>
      /// <param name="_name">Name of the voice (e.g. "Alex")</param>
      /// <param name="isExact">Exact match for the voice name (default: false, optional)</param>
      /// <returns>True if a voice is available for a given name.</returns>
      public bool isVoiceForNameAvailable(string _name, bool isExact = false)
      {
         return VoiceForName(_name, isExact) != null;
      }

      /// <summary>Get a voice for a given name from the current TTS-system.</summary>
      /// <param name="_name">Name of the voice (e.g. "Alex")</param>
      /// <param name="isExact">Exact match for the voice name (default: false, optional)</param>
      /// <returns>Voice for the given name or null if not found.</returns>
      public Crosstales.RTVoice.Model.Voice VoiceForName(string _name, bool isExact = false)
      {
         Crosstales.RTVoice.Model.Voice result = null;

         if (string.IsNullOrEmpty(_name))
         {
            if (Crosstales.RTVoice.Util.Config.DEBUG)
               Debug.LogWarning("The given 'name' is null or empty! Returning null.", this);
         }
         else
         {
            result = isExact ? Voices.FirstOrDefault(voice => voice.Name.CTEquals(_name)) : Voices.FirstOrDefault(voice => voice.Name.CTContains(_name));

            if (result == null)
            {
               //use the default voice
               Debug.LogWarning("No voice for name '" + _name + "' found! Speaking with the default voice!", this);
            }
         }

         return result;
      }

      /// <summary>Speaks a text with a given voice (native mode).</summary>
      /// <param name="text">Text to speak.</param>
      /// <param name="voice">Voice to speak (optional).</param>
      /// <param name="rate">Speech rate of the speaker in percent (1 = 100%, values: 0.01-3, default: 1, optional).</param>
      /// <param name="pitch">Pitch of the speech in percent (1 = 100%, values: 0-2, default: 1, optional).</param>
      /// <param name="volume">Volume of the speaker in percent (1 = 100%, values: 0.01-1, default: 1, optional).</param>
      /// <param name="forceSSML">Force SSML on supported platforms (default: true, optional).</param>
      /// <returns>UID of the speaker.</returns>
      public string SpeakNative(string text, Crosstales.RTVoice.Model.Voice voice = null, float rate = 1f, float pitch = 1f, float volume = 1f, bool forceSSML = true)
      {
         if (this != null && !isActiveAndEnabled)
            return "disabled";

         Crosstales.RTVoice.Model.Wrapper wrapper = new Crosstales.RTVoice.Model.Wrapper(text, voice, rate, pitch, volume, forceSSML);

         SpeakNativeWithUID(wrapper);

         return wrapper.Uid;
      }

      /// <summary>Speaks a text with a given voice (native mode).</summary>
      /// <param name="wrapper">Speak wrapper.</param>
      public void SpeakNativeWithUID(Crosstales.RTVoice.Model.Wrapper wrapper)
      {
         if (this != null && !isActiveAndEnabled)
            return;

         if (Crosstales.RTVoice.Util.Constants.DEV_DEBUG)
            Debug.LogWarning($"SpeakNativeWithUID called: {wrapper}", this);
         if (wrapper != null)
         {
            if (Crosstales.RTVoice.Util.Helper.isEditorMode)
            {
#if UNITY_EDITOR
               speakNativeInEditor(wrapper);
#endif
            }
            else
            {
               if (voiceProvider != null)
               {
                  if (string.IsNullOrEmpty(wrapper.Text))
                  {
                     Debug.LogWarning("'wrapper.Text' is null or empty!", this);
                  }
                  else
                  {
                     BusyCount++;

                     if (!voiceProvider.isSpeakNativeSupported) //add an AudioSource for providers without native support
                     {
                        if (wrapper.Source == null)
                        {
                           wrapper.Source = gameObject.AddComponent<AudioSource>();
                           genericSources.Add(wrapper.Uid, wrapper.Source);
                        }
                        else
                        {
                           if (!providedSources.ContainsKey(wrapper.Uid))
                              providedSources.Add(wrapper.Uid, wrapper.Source);
                        }

                        wrapper.SpeakImmediately = true; //must always speak immediately
                     }

                     StartCoroutine(voiceProvider.SpeakNative(wrapper));
                  }
               }
               else
               {
                  logVPIsNull();
               }
            }
         }
         else
         {
            logWrapperIsNull();
         }
      }

      /// <summary>Speaks a text with a given wrapper (native mode).</summary>
      /// <param name="wrapper">Speak wrapper.</param>
      /// <returns>UID of the speaker.</returns>
      public string SpeakNative(Crosstales.RTVoice.Model.Wrapper wrapper)
      {
         if (this != null && !isActiveAndEnabled)
            return "disabled";

         if (wrapper != null)
         {
            SpeakNativeWithUID(wrapper);

            return wrapper.Uid;
         }

         logWrapperIsNull();

         return string.Empty;
      }

      /// <summary>Speaks a text with a given voice.</summary>
      /// <param name="text">Text to speak.</param>
      /// <param name="source">AudioSource for the output (optional).</param>
      /// <param name="voice">Voice to speak (optional).</param>
      /// <param name="speakImmediately">Speak the text immediately (default: true). Only works if 'Source' is not null.</param>
      /// <param name="rate">Speech rate of the speaker in percent (1 = 100%, values: 0.01-3, default: 1, optional).</param>
      /// <param name="pitch">Pitch of the speech in percent (1 = 100%, values: 0-2, default: 1, optional).</param>
      /// <param name="volume">Volume of the speaker in percent (1 = 100%, values: 0.01-1, default: 1, optional).</param>
      /// <param name="outputFile">Saves the generated audio to an output file (without extension, optional).</param>
      /// <param name="forceSSML">Force SSML on supported platforms (default: true, optional).</param>
      /// <returns>UID of the speaker.</returns>
      public string Speak(string text, AudioSource source = null, Crosstales.RTVoice.Model.Voice voice = null, bool speakImmediately = true, float rate = 1f, float pitch = 1f, float volume = 1f, string outputFile = "", bool forceSSML = true)
      {
         if (this != null && !isActiveAndEnabled)
            return "disabled";

         Crosstales.RTVoice.Model.Wrapper wrapper = new Crosstales.RTVoice.Model.Wrapper(text, voice, rate, pitch, volume, source, speakImmediately, outputFile, forceSSML);

         SpeakWithUID(wrapper);

         return wrapper.Uid;
      }

      /// <summary>Speaks a text with a given voice.</summary>
      /// <param name="wrapper">Speak wrapper.</param>
      public void SpeakWithUID(Crosstales.RTVoice.Model.Wrapper wrapper)
      {
         if (this != null && !isActiveAndEnabled)
            return;

         if (Crosstales.RTVoice.Util.Constants.DEV_DEBUG)
            Debug.LogWarning($"SpeakWithUID called: {wrapper}", this);

         if (wrapper != null)
         {
            if (Crosstales.RTVoice.Util.Helper.isEditorMode)
            {
#if UNITY_EDITOR
               speakNativeInEditor(wrapper);
#endif
            }
            else
            {
               if (voiceProvider != null)
               {
                  if (string.IsNullOrEmpty(wrapper.Text))
                  {
                     Debug.LogWarning("'wrapper.Text' is null or empty!", this);
                  }
                  else
                  {
                     BusyCount++;

                     if (voiceProvider.isSpeakSupported) //audio file generation possible
                     {
                        if (wrapper.Source == null)
                        {
                           wrapper.Source = gameObject.AddComponent<AudioSource>();

                           genericSources.Add(wrapper.Uid, wrapper.Source);

                           if (string.IsNullOrEmpty(wrapper.OutputFile))
                              wrapper.SpeakImmediately = true; //must always speak immediately (since there is no AudioSource given and no output file wanted)
                        }
                        else
                        {
                           if (!providedSources.ContainsKey(wrapper.Uid))
                              providedSources.Add(wrapper.Uid, wrapper.Source);
                        }

                        wrapper.Source.mute = isMuted;

                        //TODO activate in providers (waiting for it)
                        //if (isPaused)
                        //    wrapper.Source.Pause();
                     }

                     if (Caching && GlobalCache.Instance.Clips.ContainsKey(wrapper))
                     {
                        if (Crosstales.RTVoice.Util.Config.DEBUG)
                           Debug.Log($"Wrapper CACHED: {wrapper}", this);

                        Crosstales.RTVoice.Util.Context.NumberOfCachedSpeeches++;

                        StartCoroutine(voiceProvider.SpeakWithClip(wrapper, GlobalCache.Instance.GetClip(wrapper)));
                     }
                     else
                     {
                        if (Crosstales.RTVoice.Util.Config.DEBUG)
                           Debug.Log($"Wrapper NOT cached: {wrapper}", this);

                        Crosstales.RTVoice.Util.Context.NumberOfNonCachedSpeeches++;

                        StartCoroutine(voiceProvider.Speak(wrapper));
                     }
                  }
               }
               else
               {
                  logVPIsNull();
               }
            }
         }
         else
         {
            logWrapperIsNull();
         }
      }

      /// <summary>Speaks a text with a given wrapper.</summary>
      /// <param name="wrapper">Speak wrapper.</param>
      /// <returns>UID of the speaker.</returns>
      public string Speak(Crosstales.RTVoice.Model.Wrapper wrapper)
      {
         if (this != null && !isActiveAndEnabled)
            return "disabled";

         if (wrapper != null)
         {
            SpeakWithUID(wrapper);

            return wrapper.Uid;
         }

         logWrapperIsNull();

         return string.Empty;
      }

      /// <summary>Speaks and marks a text with a given wrapper.</summary>
      /// <param name="wrapper">Speak wrapper.</param>
      public void SpeakMarkedWordsWithUID(Crosstales.RTVoice.Model.Wrapper wrapper)
      {
         if (this != null && !isActiveAndEnabled)
            return;

         if (Crosstales.RTVoice.Util.Constants.DEV_DEBUG)
            Debug.LogWarning($"SpeakMarkedWordsWithUID called: {wrapper}", this);

         if (voiceProvider != null)
         {
            if (string.IsNullOrEmpty(wrapper.Text))
            {
               Debug.LogWarning("'wrapper.Text' is null or empty!", this);
            }
            else
            {
               if (wrapper.Source == null || wrapper.Source.clip == null)
               {
                  Debug.LogError("'wrapper.Source' must be a valid AudioSource with a clip! Use 'Speak()' before!", this);
               }
               else
               {
                  BusyCount++;

                  wrapper.SpeakImmediately = true;

                  //TODO improve the detection for supported providers
                  if (!Crosstales.RTVoice.Util.Helper.isMacOSPlatform && !Crosstales.RTVoice.Util.Helper.isWSABasedPlatform && !CustomMode) //prevent "double-speak"
                  {
                     wrapper.Volume = 0f;
                     wrapper.Source.PlayDelayed(0.1f);
                  }

                  SpeakNativeWithUID(wrapper);
               }
            }
         }
         else
         {
            logVPIsNull();
         }
      }


      /// <summary>Speaks and marks a text with a given voice and tracks the word position.</summary>
      /// <param name="uid">UID of the speaker</param>
      /// <param name="text">Text to speak.</param>
      /// <param name="source">AudioSource for the output.</param>
      /// <param name="voice">Voice to speak (optional).</param>
      /// <param name="rate">Speech rate of the speaker in percent (1 = 100%, values: 0.01-3, default: 1, optional).</param>
      /// <param name="pitch">Pitch of the speech in percent (1 = 100%, values: 0-2, default: 1, optional).</param>
      /// <param name="forceSSML">Force SSML on supported platforms (default: true, optional).</param>
      public void SpeakMarkedWordsWithUID(string uid, string text, AudioSource source, Crosstales.RTVoice.Model.Voice voice = null, float rate = 1f, float pitch = 1f, bool forceSSML = true)
      {
         SpeakMarkedWordsWithUID(new Crosstales.RTVoice.Model.Wrapper(uid, text, voice, rate, pitch, 0, source, true, "", forceSSML));
      }

      //      /// <summary>
      //      /// Speaks a text with a given voice and tracks the word position.
      //      /// </summary>
      //      public static Guid SpeakMarkedWords(string text, AudioSource source = null, Voice voice = null, int rate = 1, int volume = 100) {
      //         Guid result = Guid.NewGuid();
      //
      //         SpeakMarkedWordsWithUID(result, text, source, voice, rate, volume);
      //
      //         return result;
      //      }

      /// <summary>Generates an audio file from a given wrapper.</summary>
      /// <param name="wrapper">Speak wrapper.</param>
      /// <returns>UID of the generator.</returns>
      public string Generate(Crosstales.RTVoice.Model.Wrapper wrapper)
      {
         if (this != null && !isActiveAndEnabled)
            return "disabled";

         if (wrapper != null)
         {
            if (Crosstales.RTVoice.Util.Helper.isEditorMode)
            {
#if UNITY_EDITOR
               generateInEditor(wrapper);
#endif
            }
            else
            {
               if (voiceProvider != null)
               {
                  if (string.IsNullOrEmpty(wrapper.Text))
                  {
                     Debug.LogWarning("'wrapper.Text' is null or empty! Can't generate audio file.", this);
                  }
                  else
                  {
                     if (string.IsNullOrEmpty(wrapper.OutputFile))
                     {
                        Debug.LogWarning("'wrapper.OutputFile' is null or empty! Can't generate audio file.", this);
                     }
                     else
                     {
                        StartCoroutine(voiceProvider.Generate(wrapper));
                     }
                  }

                  return wrapper.Uid;
               }

               logVPIsNull();
            }
         }
         else
         {
            logWrapperIsNull();
         }

         return string.Empty;
      }


      /// <summary>Generates an audio file from a text with a given voice.</summary>
      /// <param name="text">Text to generate.</param>
      /// <param name="outputFile">Saves the generated audio to an output file (without extension).</param>
      /// <param name="voice">Voice to speak (optional).</param>
      /// <param name="rate">Speech rate of the speaker in percent (1 = 100%, values: 0.01-3, default: 1, optional).</param>
      /// <param name="pitch">Pitch of the speech in percent (1 = 100%, values: 0-2, default: 1, optional).</param>
      /// <param name="volume">Volume of the speaker in percent (1 = 100%, values: 0.01-1, default: 1, optional).</param>
      /// <param name="forceSSML">Force SSML on supported platforms (default: true, optional).</param>
      /// <returns>UID of the generator.</returns>
      public string Generate(string text, string outputFile, Crosstales.RTVoice.Model.Voice voice = null, float rate = 1f, float pitch = 1f, float volume = 1f, bool forceSSML = true)
      {
         if (this != null && !isActiveAndEnabled)
            return "disabled";

         Crosstales.RTVoice.Model.Wrapper wrapper = new Crosstales.RTVoice.Model.Wrapper(text, voice, rate, pitch, volume, null, false, outputFile, forceSSML);

         return Generate(wrapper);
      }

      /// <summary>Silence all active TTS-voices (optional with a UID).</summary>
      /// <param name="uid">UID of the speaker (optional)</param>
      public void Silence(string uid = null)
      {
         if (this != null && !isActiveAndEnabled)
            return;

         if (Crosstales.RTVoice.Util.Constants.DEV_DEBUG)
            Debug.Log($"Silence called: {uid}", this);

         if (voiceProvider != null)
         {
            if (string.IsNullOrEmpty(uid))
            {
               silence();
            }
            else
            {
               if (genericSources.ContainsKey(uid))
               {
                  if (genericSources.TryGetValue(uid, out AudioSource source))
                     source.Stop();
               }
               else if (providedSources.ContainsKey(uid))
               {
                  if (providedSources.TryGetValue(uid, out AudioSource source))
                     source.Stop();
               }
               else
               {
                  voiceProvider.Silence(uid);
               }
            }
         }
         else
         {
            logVPIsNull();
         }

         //SpeechCount--;
      }

      /// <summary>Pause all active TTS-voices (optional with a UID, only for 'Speak'-calls).</summary>
      /// <param name="uid">UID of the speaker (optional)</param>
      public void Pause(string uid = null)
      {
         if (this != null && !isActiveAndEnabled)
            return;

         if (Crosstales.RTVoice.Util.Constants.DEV_DEBUG)
            Debug.LogWarning($"Pause called: {uid}", this);

         isPaused = true;

#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
         if (voiceProvider?.GetType() == typeof(Provider.VoiceProviderIOS))
         {
            float currentTime = Time.realtimeSinceStartup;

            if (lastTimePaused + delayPause < currentTime)
            {
               lastTimePaused = currentTime;

               ((Provider.VoiceProviderIOS)voiceProvider).Pause();
            }
            else
            {
               Debug.LogWarning("'Pause' is called too fast - please slow down!", this);
            }
         }
         else
         {
#endif
         if (voiceProvider != null)
         {
            if (!string.IsNullOrEmpty(uid))
            {
               if (genericSources.ContainsKey(uid))
               {
                  if (genericSources.TryGetValue(uid, out AudioSource source))
                     source.Pause();
               }
               else if (providedSources.ContainsKey(uid))
               {
                  if (providedSources.TryGetValue(uid, out AudioSource source))
                     source.Pause();
               }
               else
               {
                  Debug.Log($"No AudioSource for uid found: {uid}", this);
               }
            }
            else
            {
               foreach (System.Collections.Generic.KeyValuePair<string, AudioSource> source in genericSources.Where(source => source.Value != null))
               {
                  source.Value.Pause();
               }

               foreach (System.Collections.Generic.KeyValuePair<string, AudioSource> source in providedSources.Where(source => source.Value != null))
               {
                  source.Value.Pause();
               }
            }
         }
         else
         {
            logVPIsNull();
         }
#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
         }
#endif
      }

      /// <summary>Un-Pause all active TTS-voices (optional with a UID, only for 'Speak'-calls).</summary>
      /// <param name="uid">UID of the speaker (optional)</param>
      public void UnPause(string uid = null)
      {
         if (this != null && !isActiveAndEnabled)
            return;

         if (Crosstales.RTVoice.Util.Constants.DEV_DEBUG)
            Debug.LogWarning($"UnPause called: {uid}", this);

         isPaused = false;

#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
         if (voiceProvider?.GetType() == typeof(Provider.VoiceProviderIOS))
         {
            float currentTime = Time.realtimeSinceStartup;

            if (lastTimePaused + delayPause < currentTime)
            {
               lastTimePaused = currentTime;

               if (currentWrapper != null)
               {
                  currentWrapper.Text = currentTextToSpeak;
                  SpeakNative(currentWrapper);
                  //voiceProvider.SpeakNative(currentTextToSpeak);
               }
            }
            else
            {
               Debug.LogWarning("'UnPause' is called too fast - please slow down!", this);
            }
         }
         else
         {
#endif
         if (voiceProvider != null)
         {
            if (!string.IsNullOrEmpty(uid))
            {
               if (genericSources.ContainsKey(uid))
               {
                  if (genericSources.TryGetValue(uid, out AudioSource source))
                     source.UnPause();
               }
               else if (providedSources.ContainsKey(uid))
               {
                  if (providedSources.TryGetValue(uid, out AudioSource source))
                     source.UnPause();
               }
               else
               {
                  Debug.Log($"No AudioSource for uid found: {uid}", this);
               }
            }
            else
            {
               foreach (System.Collections.Generic.KeyValuePair<string, AudioSource> source in genericSources.Where(source => source.Value != null))
               {
                  source.Value.UnPause();
               }

               foreach (System.Collections.Generic.KeyValuePair<string, AudioSource> source in providedSources.Where(source => source.Value != null))
               {
                  source.Value.UnPause();
               }
            }
         }

         else
         {
            logVPIsNull();
         }
#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
         }
#endif
      }

      /// <summary>Pause or unpause all active TTS-voices (optional with a UID, only for 'Speak'-calls).</summary>
      /// <param name="uid">UID of the speaker (optional)</param>
      public void PauseOrUnPause(string uid = null)
      {
         if (this != null && !isActiveAndEnabled)
            return;

         if (isPaused)
         {
            UnPause(uid);
         }
         else
         {
            Pause(uid);
         }
      }

      /// <summary>Mute all active TTS-voices (optional with a UID, only for 'Speak'-calls).</summary>
      /// <param name="uid">UID of the speaker (optional)</param>
      public void Mute(string uid = null)
      {
         if (this != null && !isActiveAndEnabled)
            return;

         if (Crosstales.RTVoice.Util.Constants.DEV_DEBUG)
            Debug.LogWarning($"Mute called: {uid}", this);

         isMuted = true;

         if (voiceProvider != null)
         {
            if (!string.IsNullOrEmpty(uid))
            {
               if (genericSources.ContainsKey(uid))
               {
                  if (genericSources.TryGetValue(uid, out AudioSource source))
                     source.mute = true;
               }
               else if (providedSources.ContainsKey(uid))
               {
                  if (providedSources.TryGetValue(uid, out AudioSource source))
                     source.mute = true;
               }
               else
               {
                  Debug.Log($"No AudioSource for uid found: {uid}", this);
               }
            }
            else
            {
               foreach (System.Collections.Generic.KeyValuePair<string, AudioSource> source in genericSources.Where(source => source.Value != null))
               {
                  source.Value.mute = true;
               }

               foreach (System.Collections.Generic.KeyValuePair<string, AudioSource> source in providedSources.Where(source => source.Value != null))
               {
                  source.Value.mute = true;
               }
            }
         }
         else
         {
            logVPIsNull();
         }
      }

      /// <summary>Un-mute all active TTS-voices (optional with a UID, only for 'Speak'-calls).</summary>
      /// <param name="uid">UID of the speaker (optional)</param>
      public void UnMute(string uid = null)
      {
         if (this != null && !isActiveAndEnabled)
            return;

         if (Crosstales.RTVoice.Util.Constants.DEV_DEBUG)
            Debug.LogWarning($"UnMute called: {uid}", this);

         isMuted = false;

         if (voiceProvider != null)
         {
            if (!string.IsNullOrEmpty(uid))
            {
               if (genericSources.ContainsKey(uid))
               {
                  if (genericSources.TryGetValue(uid, out AudioSource source))
                     source.mute = false;
               }
               else if (providedSources.ContainsKey(uid))
               {
                  if (providedSources.TryGetValue(uid, out AudioSource source))
                     source.mute = false;
               }
               else
               {
                  Debug.Log($"No AudioSource for uid found: {uid}", this);
               }
            }
            else
            {
               foreach (System.Collections.Generic.KeyValuePair<string, AudioSource> source in genericSources.Where(source => source.Value != null))
               {
                  source.Value.mute = false;
               }

               foreach (System.Collections.Generic.KeyValuePair<string, AudioSource> source in providedSources.Where(source => source.Value != null))
               {
                  source.Value.mute = false;
               }
            }
         }
         else
         {
            logVPIsNull();
         }
      }

      /// <summary>Mute or unmute all active TTS-voices (optional with a UID, only for 'Speak'-calls).</summary>
      /// <param name="uid">UID of the speaker (optional)</param>
      public void MuteOrUnMute(string uid = null)
      {
         if (isMuted)
         {
            UnMute(uid);
         }
         else
         {
            Mute(uid);
         }
      }

      /// <summary>Reloads the provider.</summary>
      public void ReloadProvider()
      {
         if (this != null && !isActiveAndEnabled)
            return;

         Silence();
         initProvider();
      }

      /// <summary>Deletes all generated audio files.</summary>
      public void DeleteAudioFiles()
      {
#if (!UNITY_XBOXONE && !UNITY_WEBGL) || UNITY_EDITOR
         if (!Crosstales.RTVoice.Util.Helper.isWebPlatform)
         {
            //string path = Application.persistentDataPath;
            string path = Application.temporaryCachePath;

#if !UNITY_WSA || UNITY_EDITOR
            if (deleteWorker?.IsAlive == true)
            {
               if (Crosstales.RTVoice.Util.Constants.DEV_DEBUG)
                  Debug.Log("Killing worker", this);

               deleteWorker.Abort(); //TODO dangerous - find a better solution!
            }

            deleteWorker = new System.Threading.Thread(() => deleteAudioFiles(path));
            deleteWorker.Start();
#else
            deleteAudioFiles(path);
#endif
         }
#endif
      }

      #endregion


      #region Private methods

      private void silence()
      {
         if (Crosstales.RTVoice.Util.Constants.DEV_DEBUG)
            Debug.Log("Silence called", this);

         if (voiceProvider != null)
         {
            voiceProvider.Silence();

            /*
            if (instance != null && voiceProvider.hasCoRoutines)
                StopAllCoroutines();
            */

            foreach (System.Collections.Generic.KeyValuePair<string, AudioSource> source in genericSources.Where(source => source.Value != null))
            {
               source.Value.Stop();
               Destroy(source.Value, 0.1f);
            }

            genericSources.Clear();

            foreach (System.Collections.Generic.KeyValuePair<string, AudioSource> source in providedSources.Where(source => source.Value != null))
            {
               source.Value.Stop();
            }
         }
         else
         {
            providedSources.Clear();

            if (!Common.Util.BaseHelper.isEditorMode)
               logVPIsNull();
         }

         SpeechCount = 0;
         BusyCount = 0;
      }

      private void deleteAudioFiles(string audioDataPath)
      {
         try
         {
            System.Random rnd = new System.Random();
            string filesToDelete = Crosstales.RTVoice.Util.Constants.AUDIOFILE_PREFIX + "*"; // + AudioFileExtension;
            string path = Crosstales.RTVoice.Util.Helper.isAndroidPlatform || Crosstales.RTVoice.Util.Helper.isWSABasedPlatform ? Crosstales.Common.Util.FileHelper.ValidatePath(audioDataPath) : Crosstales.RTVoice.Util.Config.AUDIOFILE_PATH;
            string[] fileList = System.IO.Directory.GetFiles(path, filesToDelete);

            foreach (string file in fileList)
            {
               try
               {
#if !UNITY_WSA || UNITY_EDITOR
                  if (Crosstales.RTVoice.Util.Helper.isWindowsPlatform /* && ii % 10 == 0 */) //only for Windows to prevent issues with AV
                  {
                     System.Threading.Thread.Sleep(rnd.Next(1200, 1800));
                  }
#endif
                  System.IO.File.Delete(file);
               }
               catch (System.Exception ex)
               {
                  if (!Crosstales.RTVoice.Util.Helper.isEditor)
                     Debug.LogWarning($"Could not delete the file '{file}': {ex}", this);
               }
            }
         }
         catch (System.Exception ex)
         {
            if (!Crosstales.RTVoice.Util.Helper.isEditor)
               Debug.LogWarning($"Could not scan the path for files: {ex}", this);
         }
      }

      private void initProvider()
      {
         unsubscribeEvents();

         areVoicesReady = false;
         enforcedStandaloneTTS = false;

         bool useCustom = CustomProvider != null && CustomMode && CustomProvider.enabled;

         if (useCustom)
         {
            if (CustomProvider.isPlatformSupported)
            {
               subscribeCustomEvents();
               voiceProvider = customVoiceProvider = CustomProvider;
               mainVoiceProvider = null;

               CustomProvider.Load();

               //Debug.Log($"Load custom: {voiceProvider}");
            }
            else
            {
               Debug.LogWarning("'Custom Provider' does not support the current platform!", this);
               useCustom = false;

               //if (!Util.Helper.isEditorMode)
               //    CustomMode = false;
            }
         }

         if (!useCustom)
         {
            unsubscribeCustomEvents();
            customVoiceProvider = null;
            initOSProvider();

            subscribeEvents();
            voiceProvider?.Load();
            onProviderChange();

            //Debug.Log("Use internal voice provider.");
         }
      }

      private void initOSProvider()
      {
         if (!Crosstales.RTVoice.Util.Helper.isMacOSEditor && !Crosstales.RTVoice.Util.Helper.isLinuxEditor && Crosstales.RTVoice.Util.Helper.isWindowsPlatform && !eSpeakMode || Crosstales.RTVoice.Util.Helper.isWindowsEditor && Crosstales.RTVoice.Util.Config.ENFORCE_STANDALONE_TTS && !eSpeakMode)
         {
            enforcedStandaloneTTS = !Crosstales.RTVoice.Util.Helper.isWindowsPlatform && Crosstales.RTVoice.Util.Helper.isWindowsEditor && Crosstales.RTVoice.Util.Config.ENFORCE_STANDALONE_TTS;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            voiceProvider = mainVoiceProvider = Crosstales.RTVoice.Provider.VoiceProviderWindows.Instance;
#endif
         }
         else if (!Crosstales.RTVoice.Util.Helper.isWindowsEditor && !Crosstales.RTVoice.Util.Helper.isLinuxEditor && Crosstales.RTVoice.Util.Helper.isMacOSPlatform && !eSpeakMode || Crosstales.RTVoice.Util.Helper.isMacOSEditor && Crosstales.RTVoice.Util.Config.ENFORCE_STANDALONE_TTS && !eSpeakMode)
         {
            enforcedStandaloneTTS = !Crosstales.RTVoice.Util.Helper.isMacOSPlatform && Crosstales.RTVoice.Util.Helper.isMacOSEditor && Crosstales.RTVoice.Util.Config.ENFORCE_STANDALONE_TTS;
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX //|| CT_DEVELOP
            voiceProvider = mainVoiceProvider = Provider.VoiceProviderMacOS.Instance;
#endif
         }
#if UNITY_STANDALONE || UNITY_EDITOR
         else if (eSpeakMode && Crosstales.RTVoice.Provider.VoiceProviderLinux.isSupported)
         {
            voiceProvider = mainVoiceProvider = Crosstales.RTVoice.Provider.VoiceProviderLinux.Instance;
         }
#endif
         else if (Crosstales.RTVoice.Util.Helper.isAndroidPlatform)
         {
#if UNITY_ANDROID || UNITY_EDITOR
            voiceProvider = mainVoiceProvider = Crosstales.RTVoice.Provider.VoiceProviderAndroid.Instance;
#endif
         }
         else if (Crosstales.RTVoice.Util.Helper.isIOSBasedPlatform)
         {
#if UNITY_IOS || UNITY_TVOS || UNITY_EDITOR
            voiceProvider = mainVoiceProvider = Crosstales.RTVoice.Provider.VoiceProviderIOS.Instance;
#endif
         }
#if ((UNITY_WSA || UNITY_XBOXONE) && !UNITY_EDITOR) && ENABLE_WINMD_SUPPORT //|| CT_DEVELOP
         else if (Util.Helper.isWSABasedPlatform)
         {
            voiceProvider = mainVoiceProvider = Provider.VoiceProviderWSA.Instance;
         }
#endif
         else
         {
            Debug.LogError("No valid TTS provider found!", this);
            voiceProvider = mainVoiceProvider = null;
            //voiceProvider = new Provider.VoiceProviderLinux(); // always add a default provider
         }

         //Debug.Log("VP: " + voiceProvider);
         //voiceProvider?.Load();
      }

      private void logWrapperIsNull()
      {
         const string errorMessage = "'wrapper' is null!";

         onErrorInfo(null, errorMessage);

         Debug.LogError(errorMessage, this);
      }

      private void logVPIsNull()
      {
         string errorMessage = "'voiceProvider' is null!" + System.Environment.NewLine + "Did you add the 'RTVoice'-prefab to the current scene?";

         onErrorInfo(null, errorMessage);

         if (!loggedVPIsNull && !Common.Util.BaseHelper.isEditorMode)
         {
            Debug.LogWarning(errorMessage, this);
            loggedVPIsNull = true;
         }
      }

      private void subscribeCustomEvents()
      {
         if (CustomProvider != null)
         {
            CustomProvider.isActive = true;
            CustomProvider.OnVoicesReady += onVoicesReady;
            CustomProvider.OnSpeakStart += onSpeakStart;
            CustomProvider.OnSpeakComplete += onSpeakComplete;
            CustomProvider.OnSpeakCurrentWord += onSpeakCurrentWord;
            CustomProvider.OnSpeakCurrentWordString += onSpeakCurrentWordString;
            CustomProvider.OnSpeakCurrentPhoneme += onSpeakCurrentPhoneme;
            CustomProvider.OnSpeakCurrentViseme += onSpeakCurrentViseme;
            CustomProvider.OnSpeakAudioGenerationStart += onSpeakAudioGenerationStart;
            CustomProvider.OnSpeakAudioGenerationComplete += onSpeakAudioGenerationComplete;
            CustomProvider.OnErrorInfo += onErrorInfo;
         }
      }

      private void unsubscribeCustomEvents()
      {
         if (CustomProvider != null)
         {
            CustomProvider.isActive = false;
            CustomProvider.OnVoicesReady -= onVoicesReady;
            CustomProvider.OnSpeakStart -= onSpeakStart;
            CustomProvider.OnSpeakComplete -= onSpeakComplete;
            CustomProvider.OnSpeakCurrentWord -= onSpeakCurrentWord;
            CustomProvider.OnSpeakCurrentWordString -= onSpeakCurrentWordString;
            CustomProvider.OnSpeakCurrentPhoneme -= onSpeakCurrentPhoneme;
            CustomProvider.OnSpeakCurrentViseme -= onSpeakCurrentViseme;
            CustomProvider.OnSpeakAudioGenerationStart -= onSpeakAudioGenerationStart;
            CustomProvider.OnSpeakAudioGenerationComplete -= onSpeakAudioGenerationComplete;
            CustomProvider.OnErrorInfo -= onErrorInfo;
         }
      }

      private void subscribeEvents()
      {
         if (mainVoiceProvider != null)
         {
            mainVoiceProvider.OnVoicesReady += onVoicesReady;
            mainVoiceProvider.OnSpeakStart += onSpeakStart;
            mainVoiceProvider.OnSpeakComplete += onSpeakComplete;
            mainVoiceProvider.OnSpeakCurrentWord += onSpeakCurrentWord;
            mainVoiceProvider.OnSpeakCurrentWordString += onSpeakCurrentWordString;
            mainVoiceProvider.OnSpeakCurrentPhoneme += onSpeakCurrentPhoneme;
            mainVoiceProvider.OnSpeakCurrentViseme += onSpeakCurrentViseme;
            mainVoiceProvider.OnSpeakAudioGenerationStart += onSpeakAudioGenerationStart;
            mainVoiceProvider.OnSpeakAudioGenerationComplete += onSpeakAudioGenerationComplete;
            mainVoiceProvider.OnErrorInfo += onErrorInfo;
         }

         if (customVoiceProvider != null)
         {
            customVoiceProvider.OnVoicesReady += onVoicesReady;
            customVoiceProvider.OnSpeakStart += onSpeakStart;
            customVoiceProvider.OnSpeakComplete += onSpeakComplete;
            customVoiceProvider.OnSpeakCurrentWord += onSpeakCurrentWord;
            customVoiceProvider.OnSpeakCurrentWordString += onSpeakCurrentWordString;
            customVoiceProvider.OnSpeakCurrentPhoneme += onSpeakCurrentPhoneme;
            customVoiceProvider.OnSpeakCurrentViseme += onSpeakCurrentViseme;
            customVoiceProvider.OnSpeakAudioGenerationStart += onSpeakAudioGenerationStart;
            customVoiceProvider.OnSpeakAudioGenerationComplete += onSpeakAudioGenerationComplete;
            customVoiceProvider.OnErrorInfo += onErrorInfo;
         }
      }

      private void unsubscribeEvents()
      {
         if (mainVoiceProvider != null)
         {
            mainVoiceProvider.OnVoicesReady -= onVoicesReady;
            mainVoiceProvider.OnSpeakStart -= onSpeakStart;
            mainVoiceProvider.OnSpeakComplete -= onSpeakComplete;
            mainVoiceProvider.OnSpeakCurrentWord -= onSpeakCurrentWord;
            mainVoiceProvider.OnSpeakCurrentWordString -= onSpeakCurrentWordString;
            mainVoiceProvider.OnSpeakCurrentPhoneme -= onSpeakCurrentPhoneme;
            mainVoiceProvider.OnSpeakCurrentViseme -= onSpeakCurrentViseme;
            mainVoiceProvider.OnSpeakAudioGenerationStart -= onSpeakAudioGenerationStart;
            mainVoiceProvider.OnSpeakAudioGenerationComplete -= onSpeakAudioGenerationComplete;
            mainVoiceProvider.OnErrorInfo -= onErrorInfo;
         }

         if (customVoiceProvider != null)
         {
            customVoiceProvider.OnVoicesReady -= onVoicesReady;
            customVoiceProvider.OnSpeakStart -= onSpeakStart;
            customVoiceProvider.OnSpeakComplete -= onSpeakComplete;
            customVoiceProvider.OnSpeakCurrentWord -= onSpeakCurrentWord;
            customVoiceProvider.OnSpeakCurrentWordString -= onSpeakCurrentWordString;
            customVoiceProvider.OnSpeakCurrentPhoneme -= onSpeakCurrentPhoneme;
            customVoiceProvider.OnSpeakCurrentViseme -= onSpeakCurrentViseme;
            customVoiceProvider.OnSpeakAudioGenerationStart -= onSpeakAudioGenerationStart;
            customVoiceProvider.OnSpeakAudioGenerationComplete -= onSpeakAudioGenerationComplete;
            customVoiceProvider.OnErrorInfo -= onErrorInfo;
         }
      }

      #endregion


      #region Event-trigger methods

      private void onVoicesReady()
      {
         areVoicesReady = true;

         if (!Crosstales.RTVoice.Util.Helper.isEditorMode)
            OnReady?.Invoke();

         OnVoicesReady?.Invoke();
      }

      private void onProviderChange()
      {
         if (!Crosstales.RTVoice.Util.Helper.isEditorMode)
            OnProviderChanged?.Invoke(voiceProvider?.GetType().ToString());

         OnProviderChange?.Invoke(voiceProvider?.GetType().ToString());
      }

      private void onSpeakStart(Crosstales.RTVoice.Model.Wrapper wrapper)
      {
         if (!Crosstales.RTVoice.Util.Helper.isEditorMode)
            OnSpeakStarted?.Invoke(wrapper?.Uid);

         OnSpeakStart?.Invoke(wrapper);

         SpeechCount++;
      }

      private void onSpeakComplete(Crosstales.RTVoice.Model.Wrapper wrapper)
      {
#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
         currentTextToSpeak = null;
         currentWrapper = null;
#endif
         if (!Crosstales.RTVoice.Util.Helper.isEditorMode)
            OnSpeakCompleted?.Invoke(wrapper?.Uid);

         OnSpeakComplete?.Invoke(wrapper);

         SpeechCount--;
         BusyCount--;
         Crosstales.RTVoice.Util.Context.NumberOfSpeeches++;

         //if (wrapper.isNative)
         Crosstales.RTVoice.Util.Context.TotalSpeechLength += wrapper.SpeechTime;
         Crosstales.RTVoice.Util.Context.NumberOfCharacters += wrapper.Text.Length;
      }

      private void onSpeakCurrentWord(Crosstales.RTVoice.Model.Wrapper wrapper, string[] speechTextArray, int wordIndex)
      {
#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
         if (voiceProvider?.GetType() == typeof(Provider.VoiceProviderIOS))
         {
            //currentTextToSpeak = string.Join(" ", speechTextArray, wordIndex + 1, speechTextArray.Length - wordIndex - 1);
            currentTextToSpeak = string.Join(" ", speechTextArray, wordIndex, speechTextArray.Length - wordIndex);
            currentWrapper = wrapper;
            currentWrapper.isPartial = true;
         }
#endif
         OnSpeakCurrentWord?.Invoke(wrapper, speechTextArray, wordIndex);
      }

      private void onSpeakCurrentWordString(Crosstales.RTVoice.Model.Wrapper wrapper, string word)
      {
         OnSpeakCurrentWordString?.Invoke(wrapper, word);
      }

      private void onSpeakCurrentPhoneme(Crosstales.RTVoice.Model.Wrapper wrapper, string phoneme)
      {
         OnSpeakCurrentPhoneme?.Invoke(wrapper, phoneme);
      }

      private void onSpeakCurrentViseme(Crosstales.RTVoice.Model.Wrapper wrapper, string viseme)
      {
         OnSpeakCurrentViseme?.Invoke(wrapper, viseme);
      }

      private void onSpeakAudioGenerationStart(Crosstales.RTVoice.Model.Wrapper wrapper)
      {
         OnSpeakAudioGenerationStart?.Invoke(wrapper);
      }

      private void onSpeakAudioGenerationComplete(Crosstales.RTVoice.Model.Wrapper wrapper)
      {
         OnSpeakAudioGenerationComplete?.Invoke(wrapper);

         Crosstales.RTVoice.Util.Context.NumberOfAudioFiles++;
         Crosstales.RTVoice.Util.Context.TotalSpeechLength += wrapper.SpeechTime;
         Crosstales.RTVoice.Util.Context.NumberOfCharacters += wrapper.Text.Length;
      }

      private void onErrorInfo(Crosstales.RTVoice.Model.Wrapper wrapper, string errorInfo)
      {
         if (!Crosstales.RTVoice.Util.Helper.isEditorMode)
            OnError?.Invoke(wrapper?.Uid, errorInfo);

         OnErrorInfo?.Invoke(wrapper, errorInfo);
      }

      #endregion


      #region Editor-only methods

#if UNITY_EDITOR

      private void speakNativeInEditor(Crosstales.RTVoice.Model.Wrapper wrapper)
      {
         if (Crosstales.RTVoice.Util.Helper.isEditorMode)
         {
            if (voiceProvider != null)
            {
               if (string.IsNullOrEmpty(wrapper.Text))
               {
                  Debug.LogWarning("'wrapper.Text' is null or empty!", this);
               }
               else
               {
                  System.Threading.Thread worker = new System.Threading.Thread(() => voiceProvider.SpeakNativeInEditor(wrapper));
                  worker.Start();
               }

               //return wrapper.Uid;
            }
            else
            {
               logVPIsNull();
            }
         }
         else
         {
            Debug.LogWarning("'SpeakNativeInEditor()' works only inside the Unity Editor!", this);
         }

         //return string.Empty;
      }

      private void generateInEditor(Crosstales.RTVoice.Model.Wrapper wrapper)
      {
         if (Crosstales.RTVoice.Util.Helper.isEditorMode)
         {
            if (voiceProvider != null)
            {
               if (string.IsNullOrEmpty(wrapper.Text))
               {
                  Debug.LogWarning("'wrapper.Text' is null or empty!", this);
               }
               else
               {
                  System.Threading.Thread worker = new System.Threading.Thread(() => voiceProvider.GenerateInEditor(wrapper));
                  worker.Start();
               }

               //return wrapper.Uid;
            }

            logVPIsNull();
         }
         else
         {
            Debug.LogWarning("'GenerateInEditor()' works only inside the Unity Editor!", this);
         }

         //return string.Empty;
      }

#endif

      #endregion


      #region iOS

#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
        /// <summary>Sets all voices from iOS.</summary>
        /// <param name="voices">All voices from iOS.</param>
        public void SetVoices(string voices)
        {
            Provider.VoiceProviderIOS.SetVoices(voices);
        }

        /// <summary>The current spoken word from iOS.</summary>
        /// <param name="voices">Current spoken word from iOS.</param>
        public void WordSpoken(string word)
        {
            Provider.VoiceProviderIOS.WordSpoken(word);
        }

        /// <summary>Sets the state from iOS.</summary>
        /// <param name="voices">State from iOS.</param>
        public void SetState(string state)
        {
            Provider.VoiceProviderIOS.SetState(state);
        }
#endif

      #endregion
   }
}
// © 2015-2022 crosstales LLC (https://www.crosstales.com)
#if UNITY_ANDROID || UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Linq;

namespace Crosstales.RTVoice.Provider
{
   /// <summary>Android voice provider.</summary>
   public class VoiceProviderAndroid : BaseVoiceProvider<VoiceProviderAndroid>
   {
      #region Variables

      private static string lastEngine;
      private static bool isInitialized;
      private static AndroidJavaObject ttsHandler;

      private static bool isSSML;

      private readonly WaitForSeconds wfs = new WaitForSeconds(0.1f);

      private System.Collections.Generic.List<string> cachedEngines = new System.Collections.Generic.List<string>();
      private bool isLoading;

      #endregion


      #region Properties

/*
      /// <summary>Returns the singleton instance of this class.</summary>
      /// <returns>Singleton instance of this class.</returns>
      public static VoiceProviderAndroid Instance => instance ?? (instance = new VoiceProviderAndroid());
*/
      public override string AudioFileExtension => ".wav";

      public override AudioType AudioFileType => AudioType.WAV;

      public override string DefaultVoiceName => "English (United States)";

      public override bool isWorkingInEditor => false;

      public override bool isWorkingInPlaymode => false;

      public override int MaxTextLength => 3999;

      public override bool isSpeakNativeSupported => true;

      public override bool isSpeakSupported => true;

      public override bool isPlatformSupported => Util.Helper.isAndroidPlatform;

      public override bool isSSMLSupported => isSSML;

      public override bool isOnlineService => false;

      public override bool hasCoRoutines => true;

      public override bool isIL2CPPSupported => true;

      public override bool hasVoicesInEditor => false;

      /// <summary> Returns all installed TTS engines on Android.</summary>
      public System.Collections.Generic.List<string> Engines => cachedEngines;

      #endregion


      #region Constructor

      public VoiceProviderAndroid()
      {
#if !UNITY_EDITOR || CT_DEVELOP
         if (!isInitialized)
            initializeTTS();
#endif
      }

      #endregion


      #region Implemented methods

      public override void Load(bool forceReload = false)
      {
#if !UNITY_EDITOR || CT_DEVELOP
         bool _forceReload = forceReload;

         if (lastEngine != Speaker.Instance.AndroidEngine)
         {
            instance = this;
            isInitialized = false;
            _forceReload = true;
         }

         if (!isInitialized)
            initializeTTS();

         if (cachedVoices?.Count == 0 || _forceReload)
         {
            if (!isLoading)
            {
               isLoading = true;
               Speaker.Instance.StartCoroutine(getVoices());
            }
         }
         else
         {
            onVoicesReady();
         }
#endif
      }

      public override IEnumerator SpeakNative(Model.Wrapper wrapper)
      {
#if !UNITY_EDITOR || CT_DEVELOP
         if (wrapper == null)
         {
            Debug.LogWarning("'wrapper' is null!");
         }
         else
         {
            if (string.IsNullOrEmpty(wrapper.Text))
            {
               Debug.LogWarning("'wrapper.Text' is null or empty!");
            }
            else
            {
               yield return null; //return to the main process (uid)

               if (!isInitialized)
               {
                  do
                  {
                     // waiting...
                     yield return wfs;
                  } while (!(isInitialized = ttsHandler.CallStatic<bool>("isInitialized")));
               }

               string voiceName = getVoiceName(wrapper);
               silence = false;
               onSpeakStart(wrapper);

               ttsHandler.CallStatic("SpeakNative", prepareText(wrapper), wrapper.Rate, wrapper.Pitch, wrapper.Volume, voiceName);

               do
               {
                  yield return wfs;
               } while (!silence && ttsHandler.CallStatic<bool>("isWorking"));

               if (Util.Config.DEBUG)
                  Debug.Log("Text spoken: " + wrapper.Text);

               onSpeakComplete(wrapper);
            }
         }
#else
            yield return null;
#endif
      }

      public override IEnumerator Speak(Model.Wrapper wrapper)
      {
#if !UNITY_EDITOR || CT_DEVELOP
         if (wrapper == null)
         {
            Debug.LogWarning("'wrapper' is null!");
         }
         else
         {
            if (string.IsNullOrEmpty(wrapper.Text))
            {
               Debug.LogWarning("'wrapper.Text' is null or empty: " + wrapper);
            }
            else
            {
               if (wrapper.Source == null)
               {
                  Debug.LogWarning("'wrapper.Source' is null: " + wrapper);
               }
               else
               {
                  yield return null; //return to the main process (uid)

                  if (!isInitialized)
                  {
                     do
                     {
                        // waiting...
                        yield return wfs;
                     } while (!(isInitialized = ttsHandler.CallStatic<bool>("isInitialized")));
                  }

                  string voiceName = getVoiceName(wrapper);
                  string outputFile = getOutputFile(wrapper.Uid, true);

                  ttsHandler.CallStatic<string>("Speak", prepareText(wrapper), wrapper.Rate, wrapper.Pitch, voiceName, outputFile);

                  silence = false;
                  onSpeakAudioGenerationStart(wrapper);

                  do
                  {
                     yield return wfs;
                  } while (!silence && ttsHandler.CallStatic<bool>("isWorking"));

                  yield return playAudioFile(wrapper, Util.Helper.ValidURLFromFilePath(outputFile), outputFile);
               }
            }
         }
#else
            yield return null;
#endif
      }

      public override IEnumerator Generate(Model.Wrapper wrapper)
      {
#if !UNITY_EDITOR || CT_DEVELOP
         if (wrapper == null)
         {
            Debug.LogWarning("'wrapper' is null!");
         }
         else
         {
            if (string.IsNullOrEmpty(wrapper.Text))
            {
               Debug.LogWarning("'wrapper.Text' is null or empty: " + wrapper);
            }
            else
            {
               yield return null; //return to the main process (uid)

               if (!isInitialized)
               {
                  do
                  {
                     // waiting...
                     yield return wfs;
                  } while (!(isInitialized = ttsHandler.CallStatic<bool>("isInitialized")));
               }

               string voiceName = getVoiceName(wrapper);
               string outputFile = getOutputFile(wrapper.Uid, true);

               ttsHandler.CallStatic<string>("Speak", prepareText(wrapper), wrapper.Rate, wrapper.Pitch, voiceName, outputFile);

               silence = false;
               onSpeakAudioGenerationStart(wrapper);

               do
               {
                  yield return wfs;
               } while (!silence && ttsHandler.CallStatic<bool>("isWorking"));

               processAudioFile(wrapper, outputFile);
            }
         }
#else
            yield return null;
#endif
      }

#if !UNITY_EDITOR || CT_DEVELOP
      public override void Silence()
      {
         ttsHandler.CallStatic("StopNative");

         base.Silence();
      }
#endif

      #endregion


      #region Public methods

      public static void ShutdownTTS()
      {
#if !UNITY_EDITOR || CT_DEVELOP
         ttsHandler.CallStatic("Shutdown");
#endif
      }

      #endregion


      #region Private methods

#if !UNITY_EDITOR || CT_DEVELOP
      private IEnumerator getVoices()
      {
         yield return null;

         if (!isInitialized)
         {
            do
            {
               yield return wfs;
            } while (!(isInitialized = ttsHandler.CallStatic<bool>("isInitialized")));
         }

         string[] stringVoices = null;
         bool success = false;

         try
         {
            stringVoices = ttsHandler.CallStatic<string[]>("GetVoices");
            success = true;
         }
         catch (System.Exception ex)
         {
            string errorMessage = "Could not get any voices!" + System.Environment.NewLine + ex;
            Debug.LogError(errorMessage);
            onErrorInfo(null, errorMessage);
         }

         if (success)
         {
            System.Collections.Generic.List<Model.Voice> voices = new System.Collections.Generic.List<Model.Voice>(350);

            foreach (string voice in stringVoices)
            {
               string[] currentVoiceData = voice.Split(';');

               if (!currentVoiceData[0].CTContains("network")) //ignore network-voices
               {
                  Model.Enum.Gender gender = Model.Enum.Gender.UNKNOWN;

                  if (currentVoiceData[0].CTContains("#male"))
                  {
                     gender = Model.Enum.Gender.MALE;
                  }
                  else if (currentVoiceData[0].CTContains("#female"))
                  {
                     gender = Model.Enum.Gender.FEMALE;
                  }

                  Model.Voice newVoice = new Model.Voice(currentVoiceData[0], "Android voice: " + voice, gender, "unknown", currentVoiceData[1]);
                  voices.Add(newVoice);
               }
            }

            cachedVoices = voices.OrderBy(s => s.Name).ToList();

            if (Util.Constants.DEV_DEBUG)
               Debug.Log("Voices read: " + cachedVoices.CTDump());
         }

         yield return getEngines();

         isLoading = false;

         onVoicesReady();
      }

      private IEnumerator getEngines()
      {
         string[] stringEngines = null;
         bool success = false;

         try
         {
            stringEngines = ttsHandler.CallStatic<string[]>("GetEngines");
            success = true;
         }
         catch (System.Exception ex)
         {
            string errorMessage = "Could not get any engines!" + System.Environment.NewLine + ex;
            Debug.LogWarning(errorMessage);
            onErrorInfo(null, errorMessage);
         }

         if (success)
         {
            yield return null;

            System.Collections.Generic.List<string> engines = stringEngines.Select(voice => voice.Split(';')).Select(currentEngineData => currentEngineData[0]).ToList();

            cachedEngines = engines.OrderBy(s => s).ToList();

            if (Util.Constants.DEV_DEBUG)
               Debug.Log("Engines read: " + cachedEngines.CTDump());
         }
      }

      private void initializeTTS()
      {
         AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
         AndroidJavaObject jo = jc.GetStatic<AndroidJavaObject>("currentActivity");
         ttsHandler = new AndroidJavaObject("com.crosstales.RTVoice.RTVoiceAndroidBridge", jo);
         ttsHandler.CallStatic("SetupEngine", Speaker.Instance.AndroidEngine);

         lastEngine = Speaker.Instance.AndroidEngine;

         isSSML = ttsHandler.CallStatic<bool>("isSSMLSupported");
      }

      private static string prepareText(Model.Wrapper wrapper)
      {
         //TEST
         //wrapper.ForceSSML = false;

         if (wrapper.ForceSSML && !Speaker.Instance.AutoClearTags)
         {
            System.Text.StringBuilder sbXML = new System.Text.StringBuilder();

            //sbXML.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>");
            //sbXML.Append("<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"");
            //sbXML.Append(wrapper.Voice == null ? "en-US" : wrapper.Voice.Culture);
            //sbXML.Append("\">");

            sbXML.Append("<speak>");
/*
            //Volume seems to have no effect!
            
            if (wrapper.Volume < 1f)
            {
               sbXML.Append("<prosody volume='");

               sbXML.Append((1 - wrapper.Volume).ToString("-#0%", Util.Helper.BaseCulture));

               sbXML.Append("'>");
            }
*/
            sbXML.Append(wrapper.Text);
/*
            if (wrapper.Volume < 1f)
               sbXML.Append("</prosody>");
*/
            sbXML.Append("</speak>");

            return getValidXML(sbXML.ToString().Replace('"', '\''));
         }

         return wrapper.Text.Replace('"', '\'');
      }
#endif

      #endregion


      #region Editor-only methods

#if UNITY_EDITOR

      public override void GenerateInEditor(Model.Wrapper wrapper)
      {
         Debug.LogError("'GenerateInEditor' is not supported for Android!");
      }

      public override void SpeakNativeInEditor(Model.Wrapper wrapper)
      {
         Debug.LogError("'SpeakNativeInEditor' is not supported for Android!");
      }

#endif

      #endregion
   }
}
#endif
// © 2016-2021 crosstales LLC (https://www.crosstales.com)
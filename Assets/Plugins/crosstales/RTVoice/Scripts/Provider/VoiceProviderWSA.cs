#if ((UNITY_WSA || UNITY_XBOXONE) && !UNITY_EDITOR) && ENABLE_WINMD_SUPPORT //|| CT_DEVELOP
using UnityEngine;
using System.Collections;
using System.Linq;

namespace Crosstales.RTVoice.Provider
{
   /// <summary>WSA (UWP) voice provider.</summary>
   public class VoiceProviderWSA : BaseVoiceProvider<VoiceProviderWSA>
   {
      #region Variables

      private static RTVoiceUWPBridge ttsHandler;

      private bool isLoading;

      #endregion


      #region Properties

      public override string AudioFileExtension => ".wav";

      public override AudioType AudioFileType => AudioType.WAV;

      public override string DefaultVoiceName => "Microsoft David";

      public override bool isWorkingInEditor => false;

      public override bool isWorkingInPlaymode => false;

      public override int MaxTextLength => 64000;

      public override bool isSpeakNativeSupported => false;

      public override bool isSpeakSupported => true;

      public override bool isPlatformSupported => Util.Helper.isWSABasedPlatform;

      public override bool isSSMLSupported => true;

      public override bool isOnlineService => false;

      public override bool hasCoRoutines => true;

      public override bool isIL2CPPSupported => true;

      public override bool hasVoicesInEditor => false;

      #endregion


      #region Constructor

      /// <summary>Constructor for VoiceProviderWSA.</summary>
      public VoiceProviderWSA()
      {
         //Util.Config.DEBUG = true; //only for tests

         Load();
      }

      #endregion


      #region Implemented methods

      public override void Load(bool forceReload = false)
      {
         if (cachedVoices?.Count == 0 || forceReload)
         {
            if (!isLoading)
            {
               isLoading = true;

               if (ttsHandler == null)
               {
                  if (Util.Constants.DEV_DEBUG)
                     Debug.Log("Initializing TTS...");

                  ttsHandler = new RTVoiceUWPBridge();
               }

               Speaker.Instance.StartCoroutine(getVoices());
            }
         }
         else
         {
            onVoicesReady();
         }
      }

      public override IEnumerator SpeakNative(Model.Wrapper wrapper)
      {
         yield return speak(wrapper, true);
      }

      public override IEnumerator Speak(Model.Wrapper wrapper)
      {
         yield return speak(wrapper, false);
      }

      public override IEnumerator Generate(Model.Wrapper wrapper)
      {
#if UNITY_WSA
         if (wrapper == null)
         {
            Debug.LogWarning("'wrapper' is null!");
         }
         else
         {
            if (string.IsNullOrEmpty(wrapper.Text))
            {
               Debug.LogWarning("'wrapper.Text' is null or empty: " + wrapper);
               yield return null;
            }
            else
            {
               yield return null; //return to the main process (uid)

               ttsHandler.isBusy = true;

               string voiceName = getVoiceName(wrapper);
               string outputFile = getOutputFile(wrapper.Uid, true);
               //string path = Application.persistentDataPath.Replace('/', '\\');
               string path = Application.temporaryCachePath.Replace('/', '\\');

               //ttsHandler.SynthesizeToFile(prepareText(wrapper), Application.persistentDataPath.Replace('/', '\\'), Util.Constants.AUDIOFILE_PREFIX + wrapper.Uid + AudioFileExtension, voiceName);
               UnityEngine.WSA.Application.InvokeOnUIThread(() => { ttsHandler.SynthesizeToFile(prepareText(wrapper), path, Util.Constants.AUDIOFILE_PREFIX + wrapper.Uid + AudioFileExtension, voiceName); }, false);
               //UnityEngine.WSA.Application.InvokeOnAppThread(() => { ttsHandler.SynthesizeToFile(prepareText(wrapper), path, Util.Constants.AUDIOFILE_PREFIX + wrapper.Uid + AudioFileExtension, voiceName); }, false);

               silence = false;

               onSpeakAudioGenerationStart(wrapper);

               do
               {
                  yield return null;
               } while (!silence && ttsHandler.isBusy);

               //Debug.Log("FILE: " + "file://" + outputFile + "/" + wrapper.Uid + extension);

               processAudioFile(wrapper, outputFile);
            }
         }
#else
         Debug.LogWarning("Generate is not supported for XBox!");
         yield return null;
#endif
      }

      #endregion


      #region Private methods

      private IEnumerator getVoices()
      {
         try
         {
            System.Collections.Generic.List<Model.Voice> voices = new System.Collections.Generic.List<Model.Voice>(70);
            string[] myStringVoices = ttsHandler.Voices;
            string name;

            foreach (string voice in myStringVoices)
            {
               string[] currentVoiceData = voice.Split(';');
               name = currentVoiceData[0];
               Model.Voice newVoice = new Model.Voice(name, "UWP voice: " + voice, Util.Helper.WSAVoiceNameToGender(name), "unknown", currentVoiceData[1]);
               voices.Add(newVoice);
            }

            cachedVoices = voices.OrderBy(s => s.Name).ToList();

            if (Util.Constants.DEV_DEBUG)
               Debug.Log("Voices read: " + cachedVoices.CTDump());
         }
         catch (System.Exception ex)
         {
            string errorMessage = "Could not get any voices!" + System.Environment.NewLine + ex;
            Debug.LogError(errorMessage);
            onErrorInfo(null, errorMessage);
         }

         yield return null;

         isLoading = false;

         onVoicesReady();
      }

      private IEnumerator speak(Model.Wrapper wrapper, bool isNative)
      {
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

                  ttsHandler.isBusy = true;

                  string voiceName = getVoiceName(wrapper);
#if UNITY_WSA
                  string outputFile = getOutputFile(wrapper.Uid, true);
                  //string path = Application.persistentDataPath.Replace('/', '\\');
                  string path = Application.temporaryCachePath.Replace('/', '\\');

                  //ttsHandler.SynthesizeToFile(prepareText(wrapper), path, Util.Constants.AUDIOFILE_PREFIX + wrapper.Uid + AudioFileExtension, voiceName);
                  UnityEngine.WSA.Application.InvokeOnUIThread(() => { ttsHandler.SynthesizeToFile(prepareText(wrapper), path, Util.Constants.AUDIOFILE_PREFIX + wrapper.Uid + AudioFileExtension, voiceName); }, false);
                  //UnityEngine.WSA.Application.InvokeOnAppThread(() => { ttsHandler.SynthesizeToFile(prepareText(wrapper), path, Util.Constants.AUDIOFILE_PREFIX + wrapper.Uid + AudioFileExtension, voiceName); }, false);
#else
                  System.Threading.Tasks.Task.Run(() => { ttsHandler.SynthesizeToMemory(prepareText(wrapper), voiceName); });
#endif
                  silence = false;

                  if (!isNative)
                  {
                     onSpeakAudioGenerationStart(wrapper);
                  }

                  do
                  {
                     yield return null;
                  } while (!silence && ttsHandler.isBusy);
#if UNITY_WSA
                  yield return playAudioFile(wrapper, Util.Constants.PREFIX_FILE + outputFile, outputFile, AudioFileType, isNative);
#else
                  yield return playAudioFile(wrapper, ttsHandler.AudioData, AudioFileType, isNative);
#endif
               }
            }
         }
      }

      private IEnumerator playAudioFile(Model.Wrapper wrapper, byte[] data, AudioType type = AudioType.WAV, bool isNative = false)
      {
         if (wrapper != null && wrapper.Source != null)
         {
            if (type == AudioType.WAV)
            {
               AudioClip ac = Common.Audio.WavMaster.ToAudioClip(data);
               if (ac != null && ac.loadState == AudioDataLoadState.Loaded)
               {
                  wrapper.Source.clip = ac;

                  if (Util.Config.DEBUG)
                     Debug.Log($"Text generated: {wrapper.Text}");

                  //copyAudioFile(wrapper, outputFile, isLocalFile, www.downloadHandler.data);

                  if (!isNative)
                     onSpeakAudioGenerationComplete(wrapper);

                  if (ac != null && Speaker.Instance.Caching)
                  {
                     if (Util.Config.DEBUG)
                        Debug.Log($"Adding wrapper to clips-cache: {wrapper}");

                     GlobalCache.Instance.AddClip(wrapper, ac);
                  }

                  if ((isNative || wrapper.SpeakImmediately) && wrapper.Source != null)
                  {
                     wrapper.Source.Play();
                     onSpeakStart(wrapper);

                     do
                     {
                        yield return null;
                     } while (!silence && Util.Helper.hasActiveClip(wrapper.Source));

                     if (Util.Config.DEBUG)
                        Debug.Log($"Text spoken: {wrapper.Text}");

                     onSpeakComplete(wrapper);

                     if (ac != null && !Speaker.Instance.Caching)
                        AudioClip.Destroy(ac);
                  }
               }
               else
               {
                  string errorMessage = $"Could not load the audio file from the speech: {wrapper}";
                  Debug.LogError(errorMessage);
                  onErrorInfo(wrapper, errorMessage);
               }
            }
            else
            {
               string errorMessage = $"WebGL supports only WAV files: {wrapper}";
               Debug.LogError(errorMessage);
               onErrorInfo(wrapper, errorMessage);
            }
         }
         else
         {
            string errorMessage = $"'Source' is null: {wrapper}";
            Debug.LogError(errorMessage);
            onErrorInfo(wrapper, errorMessage);
         }
      }

      private static string prepareText(Model.Wrapper wrapper)
      {
         //TEST
         //wrapper.ForceSSML = false;

         if (wrapper.ForceSSML && !Speaker.Instance.AutoClearTags)
         {
            System.Text.StringBuilder sbXML = new System.Text.StringBuilder();
            sbXML.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>");
            sbXML.Append("<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"");
            sbXML.Append(wrapper.Voice == null ? "en-US" : wrapper.Voice.Culture);
            sbXML.Append("\">");
            if (Mathf.Abs(wrapper.Rate - 1f) > Common.Util.BaseConstants.FLOAT_TOLERANCE ||
                Mathf.Abs(wrapper.Pitch - 1f) > Common.Util.BaseConstants.FLOAT_TOLERANCE ||
                Mathf.Abs(wrapper.Volume - 1f) > Common.Util.BaseConstants.FLOAT_TOLERANCE)
            {
               sbXML.Append("<prosody");

               if (Mathf.Abs(wrapper.Rate - 1f) > Common.Util.BaseConstants.FLOAT_TOLERANCE)
               {
                  float _rate = wrapper.Rate > 1 ? (wrapper.Rate - 1f) * 0.5f : wrapper.Rate - 1f;

                  sbXML.Append(" rate=\"");
                  sbXML.Append(_rate >= 0f
                     ? _rate.ToString("+#0%", Util.Helper.BaseCulture)
                     : _rate.ToString("#0%", Util.Helper.BaseCulture));

                  sbXML.Append("\"");
               }

               if (Mathf.Abs(wrapper.Pitch - 1f) > Common.Util.BaseConstants.FLOAT_TOLERANCE)
               {
                  float _pitch = wrapper.Pitch - 1f;

                  sbXML.Append(" pitch=\"");
                  sbXML.Append(_pitch >= 0f
                     ? _pitch.ToString("+#0%", Util.Helper.BaseCulture)
                     : _pitch.ToString("#0%", Util.Helper.BaseCulture));

                  sbXML.Append("\"");
               }

               if (Mathf.Abs(wrapper.Volume - 1f) > Common.Util.BaseConstants.FLOAT_TOLERANCE)
               {
                  sbXML.Append(" volume=\"");
                  sbXML.Append((100 * wrapper.Volume).ToString("#0", Util.Helper.BaseCulture));

                  sbXML.Append("\"");
               }

               sbXML.Append(">");

               sbXML.Append(wrapper.Text);

               sbXML.Append("</prosody>");
            }
            else
            {
               sbXML.Append(wrapper.Text);
            }

            sbXML.Append("</speak>");
            return getValidXML(sbXML.ToString());
         }

         return wrapper.Text;
      }

      #endregion


      #region Editor-only methods

#if UNITY_EDITOR

      public override void GenerateInEditor(Model.Wrapper wrapper)
      {
         Debug.LogError("'GenerateInEditor' is not supported for UWP (WSA)!");
      }

      public override void SpeakNativeInEditor(Model.Wrapper wrapper)
      {
         Debug.LogError("'SpeakNativeInEditor' is not supported for UWP (WSA)!");
      }

#endif

      #endregion
   }
}
#endif
// © 2016-2021 crosstales LLC (https://www.crosstales.com)
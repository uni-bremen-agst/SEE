using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Linq;

namespace Crosstales.RTVoice.Provider
{
   /// <summary>Base class for voice providers.</summary>
   public abstract class BaseVoiceProvider<T> : MainVoiceProvider where T : new()
   {
      #region Variables

      protected static T instance;

      #endregion


      #region Properties

      /// <summary>Returns the singleton instance of this class.</summary>
      /// <returns>Singleton instance of this class.</returns>
      public static T Instance => instance == null ? instance = new T() : instance;

      #endregion
   }

   /// <summary>Main class for voice providers.</summary>
   public abstract class MainVoiceProvider : IVoiceProvider
   {
      #region Variables

      protected System.Collections.Generic.List<Model.Voice> cachedVoices = new System.Collections.Generic.List<Model.Voice>();

      private System.Collections.Generic.List<string> cachedCultures;

#if (!UNITY_WSA && !UNITY_XBOXONE) || UNITY_EDITOR
      protected readonly System.Collections.Generic.Dictionary<string, System.Diagnostics.Process> processes =
         new System.Collections.Generic.Dictionary<string, System.Diagnostics.Process>();
#endif

      protected bool silence;

      protected static readonly char[] splitCharWords = {' '};

      #endregion


      #region Events

      /// <summary>An event triggered whenever the voices of a provider are ready.</summary>
      public event VoicesReady OnVoicesReady;

      /// <summary>An event triggered whenever a speak is started.</summary>
      public event SpeakStart OnSpeakStart;

      /// <summary>An event triggered whenever a speak is completed.</summary>
      public event SpeakComplete OnSpeakComplete;

      /// <summary>An event triggered whenever a new word is spoken (native, Windows and iOS only).</summary>
      public event SpeakCurrentWord OnSpeakCurrentWord;

      /// <summary>An event triggered whenever a new phoneme is spoken (native mode, Windows only).</summary>
      public event SpeakCurrentPhoneme OnSpeakCurrentPhoneme;

      /// <summary>An event triggered whenever a new viseme is spoken (native mode, Windows only).</summary>
      public event SpeakCurrentViseme OnSpeakCurrentViseme;

      /// <summary>An event triggered whenever a speak audio generation is started.</summary>
      public event SpeakAudioGenerationStart OnSpeakAudioGenerationStart;

      /// <summary>An event triggered whenever a speak audio generation is completed.</summary>
      public event SpeakAudioGenerationComplete OnSpeakAudioGenerationComplete;

      /// <summary>An event triggered whenever an error occurs.</summary>
      public event ErrorInfo OnErrorInfo;

      #endregion

/*
      #region Constructor

      /// <summary>
      /// Constructor for a voice provider.
      /// </summary>
      public MainVoiceProvider()
      {
         //Load();
      }

      #endregion
*/

      #region Implemented methods

      public abstract string AudioFileExtension { get; }

      public abstract AudioType AudioFileType { get; }

      public abstract string DefaultVoiceName { get; }

      public virtual System.Collections.Generic.List<Model.Voice> Voices => cachedVoices;

      public abstract bool isWorkingInEditor { get; }

      public abstract bool isWorkingInPlaymode { get; }

      public abstract int MaxTextLength { get; }

      public abstract bool isSpeakNativeSupported { get; }

      public abstract bool isSpeakSupported { get; }

      public abstract bool isPlatformSupported { get; }

      public abstract bool isSSMLSupported { get; }

      public abstract bool isOnlineService { get; }

      public abstract bool hasCoRoutines { get; }

      public abstract bool isIL2CPPSupported { get; }

      public abstract bool hasVoicesInEditor { get; }

      public System.Collections.Generic.List<string> Cultures
      {
         get
         {
            if (cachedCultures == null || cachedCultures.Count == 0)
            {
               cachedCultures = new System.Collections.Generic.List<string>();

               System.Collections.Generic.IEnumerable<Model.Voice> cultures = Voices.GroupBy(cul => cul.Culture)
                  .Select(grp => grp.First()).OrderBy(s => s.Culture).ToList();

               foreach (Model.Voice voice in cultures)
               {
                  cachedCultures.Add(voice.Culture);
               }
            }

            return cachedCultures;
         }
      }

      public virtual void Silence()
      {
         silence = true;

#if UNITY_STANDALONE || UNITY_EDITOR
         foreach (System.Collections.Generic.KeyValuePair<string, System.Diagnostics.Process> kvp in processes.Where(kvp => !kvp.Value.HasExited))
         {
            kvp.Value.Kill();
         }

         processes.Clear();
#endif
      }

      public virtual void Silence(string uid)
      {
#if UNITY_STANDALONE || UNITY_EDITOR
         if (!string.IsNullOrEmpty(uid))
         {
            if (processes.ContainsKey(uid))
            {
               if (!processes[uid].HasExited)
                  processes[uid].Kill();

               processes.Remove(uid);
            }
         }
#endif
      }

      public abstract IEnumerator SpeakNative(Model.Wrapper wrapper);

      public abstract IEnumerator Speak(Model.Wrapper wrapper);

      public abstract IEnumerator Generate(Model.Wrapper wrapper);

      public virtual IEnumerator SpeakWithClip(Model.Wrapper wrapper, AudioClip clip)
      {
         if (wrapper != null && wrapper.Source != null)
         {
            silence = false;
            onSpeakAudioGenerationStart(wrapper);

            wrapper.Source.clip = clip;

            yield return null;

            if (Util.Config.DEBUG)
               Debug.Log("Text generated: " + wrapper.Text);

            onSpeakAudioGenerationComplete(wrapper);

            yield return null;

            if (wrapper.SpeakImmediately && wrapper.Source != null)
            {
               wrapper.Source.Play();
               onSpeakStart(wrapper);

               do
               {
                  yield return null;
               } while (!silence && Util.Helper.hasActiveClip(wrapper.Source));

               if (Util.Config.DEBUG)
                  Debug.Log("Text spoken: " + wrapper.Text);

               onSpeakComplete(wrapper);
            }
         }
         else
         {
            string errorMessage = "'Source' is null: " + wrapper;
            Debug.LogError(errorMessage);
            onErrorInfo(wrapper, errorMessage);
         }
      }

      public abstract void Load(bool forceReload = false);

      #endregion


      #region Protected methods

#if UNITY_STANDALONE || UNITY_EDITOR
#if ENABLE_IL2CPP
      protected void startProcess(Common.Util.CTProcess process, int timeout = 0, bool eventOutputData =
         false, bool eventErrorData = false, bool redirectOutputData = true, bool redirectErrorData = true)
#else
      protected static void startProcess(System.Diagnostics.Process process, int timeout = 0,
         bool eventOutputData = false, bool eventErrorData = false, bool redirectOutputData = true,
         bool redirectErrorData = true)
#endif
      {
         try
         {
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = redirectOutputData;
            process.StartInfo.RedirectStandardError = redirectErrorData;
            process.StartInfo.UseShellExecute = false;

            if (redirectOutputData)
               process.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;

            if (redirectErrorData)
               process.StartInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;

            process.Start();

            if (eventOutputData)
               process.BeginOutputReadLine();

            if (eventErrorData)
               process.BeginErrorReadLine();

            if (timeout > 0)
            {
               process.WaitForExit(timeout);
            }
            else
            {
               process.WaitForExit(); //TODO good idea?
            }
         }
         catch (System.Threading.ThreadAbortException)
         {
            //Debug.LogError("Thread aborted: " + ex);
         }
         catch (System.Exception ex)
         {
            Debug.LogError("Could not start process: " + ex);
         }
      }
#endif

      protected virtual string getOutputFile(string uid, bool isPersistentData = false /*, bool createFile = false*/)
      {
         string filename = Util.Constants.AUDIOFILE_PREFIX + uid + AudioFileExtension;
         string outputFile;

         if (isPersistentData)
         {
            //outputFile = Util.Helper.ValidatePath(Application.persistentDataPath) + filename;
            outputFile = Util.Helper.ValidatePath(Application.temporaryCachePath) + filename;
         }
         else
         {
            outputFile = Util.Config.AUDIOFILE_PATH + filename;
         }

         /*
         if (createFile)
         {
             try
             {
                 System.IO.File.Create(outputFile).Dispose(); //to reduce AV-problems
             }
             catch (System.Exception ex)
             {
                 Debug.LogWarning("Could not create file: " + ex);
             }
         }
         */

         return outputFile;
      }

      protected virtual IEnumerator playAudioFile(Model.Wrapper wrapper, string url, string outputFile,
         AudioType type = AudioType.WAV, bool isNative = false, bool isLocalFile = true,
         System.Collections.Generic.Dictionary<string, string> headers = null)
      {
         if (wrapper != null && wrapper.Source != null)
         {
            if (!isLocalFile || isLocalFile && (System.IO.File.Exists(outputFile) && new System.IO.FileInfo(outputFile).Length > 1024))
            {
               if (Util.Helper.isStandalonePlatform && type == AudioType.MPEG)
               {
                  Debug.LogWarning("MP3 is not supported under the current platform!");
                  //TODO add support for DJ!
               }
               else
               {
                  using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url.Trim(), type))
                  {
                     if (headers != null)
                     {
                        foreach (System.Collections.Generic.KeyValuePair<string, string> kvp in headers)
                        {
                           www.SetRequestHeader(kvp.Key, kvp.Value);
                        }
                     }

                     yield return www.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
                  if (www.result != UnityWebRequest.Result.ProtocolError && www.result != UnityWebRequest.Result.ConnectionError)
#else
                     if (!www.isHttpError && !www.isNetworkError)
#endif
                     {
                        //just for testing!
                        //string outputFile = Util.Config.AUDIOFILE_PATH + wrapper.Uid + extension;
                        //System.IO.File.WriteAllBytes(outputFile, www.bytes);
#if UNITY_WEBGL
                     if (type == AudioType.WAV)
                     {
                        AudioClip ac = Common.Audio.WavMaster.ToAudioClip(www.downloadHandler.data);
#else
                        AudioClip ac = DownloadHandlerAudioClip.GetContent(www);

                        do
                        {
                           yield return ac;
                        } while (ac != null && ac.loadState == AudioDataLoadState.Loading);
#endif
                        if (ac != null && ac.loadState == AudioDataLoadState.Loaded)
                        {
                           wrapper.Source.clip = ac;

                           if (Util.Config.DEBUG)
                              Debug.Log($"Text generated: {wrapper.Text}");

                           copyAudioFile(wrapper, outputFile, isLocalFile, www.downloadHandler.data);

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
#if UNITY_WEBGL
                     }
                     else
                     {
                        string errorMessage = $"WebGL supports only WAV files: {wrapper}";
                        Debug.LogError(errorMessage);
                        onErrorInfo(wrapper, errorMessage);
                     }
#endif
                     }
                     else
                     {
                        string errorMessage = $"Could not generate the speech: {wrapper} ({www.error})";
                        Debug.LogError(errorMessage);
                        onErrorInfo(wrapper, errorMessage);
                     }
                  }
               }
            }
            else
            {
               string errorMessage = $"The generated audio file is invalid: {wrapper}";
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

      protected virtual void copyAudioFile(Model.Wrapper wrapper, string outputFile, bool isLocalFile = true, byte[] data = null)
      {
         if (wrapper != null)
         {
            if (!string.IsNullOrEmpty(wrapper.OutputFile))
            {
               wrapper.OutputFile += AudioFileExtension;

               if (isLocalFile)
               {
                  Util.Helper.CopyFile(outputFile, wrapper.OutputFile, Util.Config.AUDIOFILE_AUTOMATIC_DELETE);
               }
               else
               {
                  if (data != null)
                  {
                     try
                     {
                        System.IO.File.WriteAllBytes(wrapper.OutputFile, data); //TODO write AudioClip
                     }
                     catch (System.Exception ex)
                     {
                        Debug.LogError("Could not write audio file!" + System.Environment.NewLine + ex);
                     }
                  }
               }
            }

            if (Util.Config.AUDIOFILE_AUTOMATIC_DELETE && !Util.Helper.isWindowsPlatform) //only delete files when not under Windows
            {
               //if (!Util.Helper.isEditorMode)
               //{
               //    Speaker.FilesToDelete.Add(outputFile);
               //}
               //else
               //{
               try
               {
                  if (System.IO.File.Exists(outputFile))
                     System.IO.File.Delete(outputFile);
               }
               catch (System.Exception ex)
               {
                  string errorMessage = "Could not delete file '" + outputFile + "'!" + System.Environment.NewLine + ex;
                  Debug.LogError(errorMessage);
                  onErrorInfo(wrapper, errorMessage);
               }

               //}
            }
            else
            {
               if (string.IsNullOrEmpty(wrapper.OutputFile))
               {
                  wrapper.OutputFile = outputFile;
               }
            }
         }
         else
         {
            const string errorMessage = "'wrapper' is null!";
            Debug.LogError(errorMessage);
            onErrorInfo(null, errorMessage);
         }
      }

      protected virtual void processAudioFile(Model.Wrapper wrapper, string outputFile, bool isLocalFile = true, byte[] data = null)
      {
         if (wrapper != null)
         {
            if (!isLocalFile || isLocalFile && (System.IO.File.Exists(outputFile) && new System.IO.FileInfo(outputFile).Length > 1024))
            {
               if (Util.Config.DEBUG)
                  Debug.Log("Text generated: " + wrapper.Text);

               copyAudioFile(wrapper, outputFile, isLocalFile, data);

               onSpeakAudioGenerationComplete(wrapper);
            }
            else
            {
               const string errorMessage = "The generated audio file is invalid!";
               Debug.LogError(errorMessage);
               onErrorInfo(wrapper, errorMessage);
            }
         }
         else
         {
            const string errorMessage = "'wrapper' is null!";
            Debug.LogError(errorMessage);
            onErrorInfo(null, errorMessage);
         }
      }

      protected virtual string getVoiceName(Model.Wrapper wrapper)
      {
         if (wrapper != null && string.IsNullOrEmpty(wrapper.Voice?.Name))
         {
            if (Util.Config.DEBUG)
               Debug.LogWarning(
                  "'wrapper.Voice' or 'wrapper.Voice.Name' is null! Using the providers 'default' voice.");

            return DefaultVoiceName;
         }

         return wrapper != null ? wrapper.Voice?.Name : DefaultVoiceName;
      }

      protected static string getValidXML(string xml)
      {
         return !string.IsNullOrEmpty(xml)
            ? xml.Replace(" & ", " &amp; ").Replace(" < ", " &lt; ").Replace(" > ", " &gt; ")
            : xml;
      }

      #endregion

      /*
      private string authenticate(string username, string password)
      {
          string auth = username + ":" + password;
          auth = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(auth));
          auth = "Basic " + auth;
          return auth;
      }
      */

      #region Event-trigger methods

      protected void onVoicesReady()
      {
         if (Util.Config.DEBUG)
            Debug.Log("onVoicesReady");

         OnVoicesReady?.Invoke();
      }

      protected void onSpeakStart(Model.Wrapper wrapper)
      {
         if (Util.Config.DEBUG)
            Debug.Log("onSpeakStart: " + wrapper);

         OnSpeakStart?.Invoke(wrapper);
      }

      protected void onSpeakComplete(Model.Wrapper wrapper)
      {
         if (Util.Config.DEBUG)
            Debug.Log("onSpeakComplete: " + wrapper);

         OnSpeakComplete?.Invoke(wrapper);
      }

      protected void onSpeakCurrentWord(Model.Wrapper wrapper, string[] speechTextArray, int wordIndex)
      {
         if (wordIndex < speechTextArray.Length)
         {
            if (Util.Config.DEBUG)
               Debug.Log(
                  "onSpeakCurrentWord: " + speechTextArray[wordIndex] + System.Environment.NewLine + wrapper);

            OnSpeakCurrentWord?.Invoke(wrapper, speechTextArray, wordIndex);
         }
         else
         {
            Debug.LogWarning("Word index is larger than the speech text word count: " + wordIndex + "/" +
                             speechTextArray.Length);
         }
      }

      protected void onSpeakCurrentPhoneme(Model.Wrapper wrapper, string phoneme)
      {
         if (Util.Config.DEBUG)
            Debug.Log("onSpeakCurrentPhoneme: " + phoneme + System.Environment.NewLine + wrapper);

         OnSpeakCurrentPhoneme?.Invoke(wrapper, phoneme);
      }

      protected void onSpeakCurrentViseme(Model.Wrapper wrapper, string viseme)
      {
         if (Util.Config.DEBUG)
            Debug.Log("onSpeakCurrentViseme: " + viseme + System.Environment.NewLine + wrapper);

         OnSpeakCurrentViseme?.Invoke(wrapper, viseme);
      }

      protected void onSpeakAudioGenerationStart(Model.Wrapper wrapper)
      {
         if (Util.Config.DEBUG)
            Debug.Log("onSpeakAudioGenerationStart: " + wrapper);

         OnSpeakAudioGenerationStart?.Invoke(wrapper);
      }

      protected void onSpeakAudioGenerationComplete(Model.Wrapper wrapper)
      {
         if (Util.Config.DEBUG)
            Debug.Log("onSpeakAudioGenerationComplete: " + wrapper);

         OnSpeakAudioGenerationComplete?.Invoke(wrapper);
      }

      protected void onErrorInfo(Model.Wrapper wrapper, string info)
      {
         if (Util.Config.DEBUG)
            Debug.Log("onErrorInfo: " + info);

         OnErrorInfo?.Invoke(wrapper, info);
      }

      #endregion


      #region Editor-only methods

#if UNITY_EDITOR

      public abstract void SpeakNativeInEditor(Model.Wrapper wrapper);

      public abstract void GenerateInEditor(Model.Wrapper wrapper);

#endif

      #endregion
   }
}
// © 2015-2021 crosstales LLC (https://www.crosstales.com)
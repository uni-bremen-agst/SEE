#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN //|| CT_DEVELOP
using UnityEngine;
using System.Collections;
using System.Linq;

namespace Crosstales.RTVoice.Provider
{
   /// <summary>Windows voice provider.</summary>
   public class VoiceProviderWindows : BaseVoiceProvider<VoiceProviderWindows>
   {
      #region Variables

#if ENABLE_IL2CPP
      private const bool useVisemesAndPhonemesIL2CPP = false;
#endif
      private readonly string dataPath = Util.Helper.ValidatePath(Application.temporaryCachePath);

      private const string idVoice = "@VOICE:";
      private const string idSpeak = "@SPEAK";
      private const string idWord = "@WORD";
      private const string idPhoneme = "@PHONEME:";
      private const string idViseme = "@VISEME:";
      private const string idStart = "@STARTED";

      private static readonly char[] splitChar = {':'};

#if ENABLE_IL2CPP
      private System.Collections.Generic.Dictionary<string, Common.Util.CTProcess> processCreators = new System.Collections.Generic.Dictionary<string, Common.Util.CTProcess>();
#endif
      private bool isLoading;

      #endregion


      #region Properties

/*
      /// <summary>Returns the singleton instance of this class.</summary>
      /// <returns>Singleton instance of this class.</returns>
      public static VoiceProviderWindows Instance => instance ?? (instance = new VoiceProviderWindows());
*/
      public override string AudioFileExtension => ".wav";

      public override AudioType AudioFileType => AudioType.WAV;

      public override string DefaultVoiceName => "Microsoft David Desktop";

      public override bool isWorkingInEditor => Util.Helper.isWindowsEditor;

      public override bool isWorkingInPlaymode => Util.Helper.isWindowsEditor;

      public override int MaxTextLength => 32000;

      public override bool isSpeakNativeSupported => true;

      public override bool isSpeakSupported => true;

      public override bool isPlatformSupported => Util.Helper.isWindowsPlatform;

      public override bool isSSMLSupported => true;

      public override bool isOnlineService => false;

      public override bool hasCoRoutines => true;

      public override bool isIL2CPPSupported => true;

      public override bool hasVoicesInEditor => true;

      #endregion

/*
      #region Constructor

      /// <summary>
      /// Constructor for VoiceProviderWindows.
      /// </summary>
      public VoiceProviderWindows()
      {
         dataPath = Util.Helper.ValidatePath(Application.temporaryCachePath);
      }

      #endregion
*/

      #region Implemented methods

      public override void Load(bool forceReload = false)
      {
         if (cachedVoices?.Count == 0 || forceReload)
         {
            if (Util.Helper.isEditorMode)
            {
#if UNITY_EDITOR
               getVoicesInEditor();
#endif
            }
            else
            {
               if (!isLoading)
               {
                  isLoading = true;
                  Speaker.Instance.StartCoroutine(getVoices());
               }
            }
         }
         else
         {
            onVoicesReady();
         }
      }

      public override IEnumerator SpeakNative(Model.Wrapper wrapper)
      {
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

               if (System.IO.File.Exists(applicationName))
               {
                  string voiceName = getVoiceName(wrapper);
                  int calculatedRate = calculateRate(wrapper.Rate);
                  int calculatedVolume = calculateVolume(wrapper.Volume);

                  string args = "--speak" +
                                $" -text \"{prepareText(wrapper)}\"" +
                                $" -rate {calculatedRate}" +
                                $" -volume {calculatedVolume}" +
                                $" -voice \"{voiceName.Replace('"', '\'')}\"";

                  if (Util.Config.DEBUG)
                     Debug.Log("Process arguments: " + args);
#if ENABLE_IL2CPP
                  using (Common.Util.CTProcess process = new Common.Util.CTProcess())
#else
                  using (System.Diagnostics.Process process = new System.Diagnostics.Process())
#endif
                  {
                     //speakProcess.StartInfo.FileName = System.IO.Path.GetFileName(application);
                     //speakProcess.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(application);
                     process.StartInfo.FileName = applicationName;
                     process.StartInfo.Arguments = args;

                     string[] speechTextArray = Util.Helper.CleanText(wrapper.Text, false)
                        .Split(splitCharWords, System.StringSplitOptions.RemoveEmptyEntries);
                     int wordIndex = 0;
                     int wordIndexCompare = 0;
                     string phoneme = string.Empty;
                     string viseme = string.Empty;
                     bool start = false;
#if ENABLE_IL2CPP
                     System.Threading.Thread worker = new System.Threading.Thread(() => readSpeakNativeStream(process, ref speechTextArray, out wordIndex, out phoneme, out viseme, out start, useVisemesAndPhonemesIL2CPP, useVisemesAndPhonemesIL2CPP)) {Name = wrapper.Uid};
#else
                     System.Threading.Thread worker = new System.Threading.Thread(() => readSpeakNativeStream(process, ref speechTextArray, out wordIndex, out phoneme, out viseme, out start)) {Name = wrapper.Uid};
#endif
                     worker.Start();

                     silence = false;
#if ENABLE_IL2CPP
                     processCreators.Add(wrapper.Uid, process);
#else
                     processes.Add(wrapper.Uid, process);
#endif
                     do
                     {
                        yield return null;

                        if (wordIndex != wordIndexCompare)
                        {
                           onSpeakCurrentWord(wrapper, speechTextArray, wordIndex - 1);

                           wordIndexCompare = wordIndex;
                        }

                        if (!string.IsNullOrEmpty(phoneme))
                        {
                           onSpeakCurrentPhoneme(wrapper, phoneme);

                           phoneme = string.Empty;
                        }

                        if (!string.IsNullOrEmpty(viseme))
                        {
                           onSpeakCurrentViseme(wrapper, viseme);

                           viseme = string.Empty;
                        }

                        if (start)
                        {
                           onSpeakStart(wrapper);

                           start = false;
                        }
                     } while (worker.IsAlive || !process.HasExited);

                     // clear output
                     onSpeakCurrentPhoneme(wrapper, string.Empty);
                     onSpeakCurrentViseme(wrapper, string.Empty);
#if ENABLE_IL2CPP
                     if (process.ExitCode == 0 || process.ExitCode == 123456) //123456 = Killed
#else
                     if (process.ExitCode == 0 || process.ExitCode == -1) //-1 = Killed
#endif
                     {
                        if (Util.Config.DEBUG)
                           Debug.Log("Text spoken: " + wrapper.Text);

                        onSpeakComplete(wrapper);
                     }
                     else
                     {
                        using (System.IO.StreamReader sr = process.StandardError)
                        {
                           string errorMessage = "Could not speak the text: " + wrapper +
                                                 System.Environment.NewLine + "Exit code: " + process.ExitCode +
                                                 System.Environment.NewLine + sr.ReadToEnd();
                           Debug.LogError(errorMessage);
                           onErrorInfo(wrapper, errorMessage);
                        }
                     }
#if ENABLE_IL2CPP
                     processCreators.Remove(wrapper.Uid);
#else
                     processes.Remove(wrapper.Uid);
#endif
                  }
               }
               else
               {
                  string errorMessage = "Could not find the TTS-wrapper: '" + applicationName + "'";
                  Debug.LogError(errorMessage);
                  onErrorInfo(wrapper, errorMessage);
               }
            }
         }
      }


      public override IEnumerator Speak(Model.Wrapper wrapper)
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

                  if (System.IO.File.Exists(applicationName))
                  {
                     string voiceName = getVoiceName(wrapper);
                     int calculatedRate = calculateRate(wrapper.Rate);
                     int calculatedVolume = calculateVolume(wrapper.Volume);

                     string outputFile = getOutputFile(wrapper.Uid);

                     //string args = $"--speakToFile -text \"{prepareText(wrapper)}\" -file \"{outputFile.Replace('"', '\'')}\" -rate {calculatedRate} -volume {calculatedVolume} -voice \"{voiceName.Replace('"', '\'')}\"";
                     string args = "--speakToFile" +
                                   $" -text \"{prepareText(wrapper)}\"" +
                                   $" -file \"{outputFile.Replace('"', '\'')}\"" +
                                   $" -rate {calculatedRate}" +
                                   $" -volume {calculatedVolume}" +
                                   $" -voice \"{voiceName.Replace('"', '\'')}\"";

                     if (Util.Config.DEBUG)
                        Debug.Log("Process arguments: " + args);
#if ENABLE_IL2CPP
                     using (Common.Util.CTProcess process = new Common.Util.CTProcess())
#else
                     using (System.Diagnostics.Process process = new System.Diagnostics.Process())
#endif
                     {
                        process.StartInfo.FileName = applicationName;
                        process.StartInfo.Arguments = args;

                        System.Threading.Thread worker = new System.Threading.Thread(() => startProcess(process, 0, false, false, false)) {Name = wrapper.Uid};
                        worker.Start();

                        silence = false;
#if ENABLE_IL2CPP
                        processCreators.Add(wrapper.Uid, process);
#else
                        processes.Add(wrapper.Uid, process);
#endif
                        onSpeakAudioGenerationStart(wrapper);

                        do
                        {
                           yield return null;
                           //Debug.Log(worker.IsAlive + " - " + !process.HasExited);
                        } while (worker.IsAlive || !process.HasExited);

                        if (process.ExitCode == 0)
                        {
                           yield return playAudioFile(wrapper, Util.Helper.ValidURLFromFilePath(outputFile), outputFile);
                        }
                        else
                        {
                           using (System.IO.StreamReader sr = process.StandardError)
                           {
                              string errorMessage =
                                 "Could not speak the text: " + wrapper + System.Environment.NewLine +
                                 "Exit code: " + process.ExitCode + System.Environment.NewLine + sr.ReadToEnd();
                              Debug.LogError(errorMessage);
                              onErrorInfo(wrapper, errorMessage);
                           }
                        }
#if ENABLE_IL2CPP
                        processCreators.Remove(wrapper.Uid);
#else
                        processes.Remove(wrapper.Uid);
#endif
                     }
                  }
                  else
                  {
                     string errorMessage = "Could not find the TTS-wrapper: '" + applicationName + "'";
                     Debug.LogError(errorMessage);
                     onErrorInfo(wrapper, errorMessage);
                  }
               }
            }
         }
      }

      public override IEnumerator Generate(Model.Wrapper wrapper)
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
               yield return null; //return to the main process (uid)

               if (System.IO.File.Exists(applicationName))
               {
                  string voiceName = getVoiceName(wrapper);
                  int calculatedRate = calculateRate(wrapper.Rate);
                  int calculatedVolume = calculateVolume(wrapper.Volume);

                  string outputFile = getOutputFile(wrapper.Uid);

                  string args = "--speakToFile" +
                                $" -text \"{prepareText(wrapper)}\"" +
                                $" -file \"{outputFile.Replace('"', '\'')}\"" +
                                $" -rate {calculatedRate}" +
                                $" -volume {calculatedVolume}" +
                                $" -voice \"{voiceName.Replace('"', '\'')}\"";

                  if (Util.Config.DEBUG)
                     Debug.Log("Process arguments: " + args);
#if ENABLE_IL2CPP
                  using (Common.Util.CTProcess process = new Common.Util.CTProcess())
#else
                  using (System.Diagnostics.Process process = new System.Diagnostics.Process())
#endif
                  {
                     process.StartInfo.FileName = applicationName;
                     process.StartInfo.Arguments = args;

                     System.Threading.Thread worker = new System.Threading.Thread(() => startProcess(process, 0, false, false, false)) {Name = wrapper.Uid};
                     worker.Start();

                     silence = false;
#if ENABLE_IL2CPP
                     processCreators.Add(wrapper.Uid, process);
#else
                     processes.Add(wrapper.Uid, process);
#endif
                     onSpeakAudioGenerationStart(wrapper);

                     do
                     {
                        yield return null;
                     } while (worker.IsAlive || !process.HasExited);

                     if (process.ExitCode == 0)
                     {
                        processAudioFile(wrapper, outputFile);
                     }
                     else
                     {
                        using (System.IO.StreamReader sr = process.StandardError)
                        {
                           string errorMessage = "Could not generate the text: " + wrapper +
                                                 System.Environment.NewLine + "Exit code: " + process.ExitCode +
                                                 System.Environment.NewLine + sr.ReadToEnd();
                           Debug.LogError(errorMessage);
                           onErrorInfo(wrapper, errorMessage);
                        }
                     }
#if ENABLE_IL2CPP
                     processCreators.Remove(wrapper.Uid);
#else
                     processes.Remove(wrapper.Uid);
#endif
                  }
               }
               else
               {
                  string errorMessage = "Could not find the TTS-wrapper: '" + applicationName + "'";
                  Debug.LogError(errorMessage);
                  onErrorInfo(wrapper, errorMessage);
               }
            }
         }
      }

      public override void Silence()
      {
         base.Silence();
#if ENABLE_IL2CPP
         foreach (var kvp in processCreators.Where(kvp => kvp.Value.isBusy))
         {
             kvp.Value.Kill();
         }

         processCreators.Clear();
#endif
      }

      public override void Silence(string uid)
      {
         base.Silence(uid);
#if ENABLE_IL2CPP
         if (!string.IsNullOrEmpty(uid))
         {
             if (processCreators.ContainsKey(uid))
             {
                 if (processCreators[uid].isBusy)
                     processCreators[uid].Kill();

                 processCreators.Remove(uid);
             }
         }
#endif
      }

      #endregion


      #region Private methods

      private IEnumerator getVoices()
      {
         if (System.IO.File.Exists(applicationName))
         {
            System.Collections.Generic.List<Model.Voice> voices = new System.Collections.Generic.List<Model.Voice>();
#if ENABLE_IL2CPP
            using (Common.Util.CTProcess process = new Common.Util.CTProcess())
#else
            using (System.Diagnostics.Process process = new System.Diagnostics.Process())
#endif
            {
               process.StartInfo.FileName = applicationName;
               process.StartInfo.Arguments = "--voices";

               System.Threading.Thread worker = new System.Threading.Thread(() => startProcess(process, Util.Constants.DEFAULT_TTS_KILL_TIME));
               worker.Start();

               do
               {
                  yield return null;
               } while (worker.IsAlive || !process.HasExited);

               if (process.ExitCode == 0)
               {
                  using (System.IO.StreamReader streamReader = process.StandardOutput)
                  {
                     while (!streamReader.EndOfStream)
                     {
                        string reply = streamReader.ReadLine();

                        if (Util.Config.DEBUG)
                           Debug.Log("reply: " + reply);

                        if (!string.IsNullOrEmpty(reply))
                        {
                           if (reply.CTStartsWith(idVoice))
                           {
                              string[] splittedString = reply.Split(splitChar,
                                 System.StringSplitOptions.RemoveEmptyEntries);

                              if (splittedString.Length == 6)
                              {
                                 //if (!splittedString[1].CTContains("espeak")) //ignore eSpeak voices
                                 //{
                                 voices.Add(new Model.Voice(splittedString[1], splittedString[2],
                                    Util.Helper.StringToGender(splittedString[3]), splittedString[4],
                                    splittedString[5]));
                                 //}
                              }
                              else
                              {
                                 Debug.LogWarning("Voice is invalid: " + reply);
                              }
                           }
                        }
                     }
                  }
               }
               else
               {
                  using (System.IO.StreamReader sr = process.StandardError)
                  {
                     string errorMessage = "Could not get any voices: " + process.ExitCode +
                                           System.Environment.NewLine + sr.ReadToEnd();
                     Debug.LogError(errorMessage);
                     onErrorInfo(null, errorMessage);
                  }
               }
            }

            cachedVoices = voices.OrderBy(s => s.Name).ToList();

            if (Util.Constants.DEV_DEBUG)
               Debug.Log("Voices read: " + cachedVoices.CTDump());
         }
         else
         {
            string errorMessage = "Could not find the TTS-wrapper: '" + applicationName + "'";
            Debug.LogError(errorMessage);
            onErrorInfo(null, errorMessage);
         }

         isLoading = false;

         onVoicesReady();
      }

#if ENABLE_IL2CPP
      private void readSpeakNativeStream(Common.Util.CTProcess process, ref string[] speechTextArray, out int wordIndex, out string phoneme, out string viseme, out bool start, bool redirectOutputData = true, bool redirectErrorData = true)
#else
      private static void readSpeakNativeStream(System.Diagnostics.Process process, ref string[] speechTextArray, out int wordIndex, out string phoneme, out string viseme, out bool start, bool redirectOutputData = true, bool redirectErrorData = true)
#endif
      {
         wordIndex = 0;
         phoneme = string.Empty;
         viseme = string.Empty;
         start = false;

         process.StartInfo.CreateNoWindow = true;
         process.StartInfo.RedirectStandardOutput = redirectOutputData;
         process.StartInfo.RedirectStandardError = redirectErrorData;
         process.StartInfo.UseShellExecute = false;
         process.StartInfo.StandardErrorEncoding = process.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;

         try
         {
            process.Start();

            using (System.IO.StreamReader streamReader = process.StandardOutput)
            {
               string reply = streamReader.ReadLine();
               if (!string.IsNullOrEmpty(reply) && idSpeak.Equals(reply))
               {
                  while (!process.HasExited)
                  {
                     reply = streamReader.ReadLine();

                     if (!string.IsNullOrEmpty(reply))
                     {
                        if (reply.CTStartsWith(idWord))
                        {
                           if (wordIndex < speechTextArray.Length)
                           {
                              if (speechTextArray[wordIndex].Equals("-"))
                              {
                                 wordIndex++;
                              }

                              wordIndex++;
                           }
                        }
                        else if (reply.CTStartsWith(idPhoneme))
                        {
                           string[] splittedString = reply.Split(splitChar,
                              System.StringSplitOptions.RemoveEmptyEntries);

                           if (splittedString.Length > 1)
                           {
                              phoneme = splittedString[1];
                           }
                        }
                        else if (reply.CTStartsWith(idViseme))
                        {
                           string[] splittedString = reply.Split(splitChar,
                              System.StringSplitOptions.RemoveEmptyEntries);

                           if (splittedString.Length > 1)
                           {
                              viseme = splittedString[1];
                           }
                        }
                        else if (reply.Equals(idStart))
                        {
                           start = true;
                        }
                     }
                  }
               }
               else
               {
                  if (process.StartInfo.RedirectStandardOutput)
                     Debug.LogError("Unexpected process output: " + reply + System.Environment.NewLine +
                                    streamReader.ReadToEnd());
               }
            }
         }
         catch (System.Exception ex)
         {
            Debug.LogError("Could not speak: " + ex);
         }
      }

      private string applicationName
      {
         get
         {
            string appName;

/*            
            if (Util.Helper.isEditor)
            {
               if (System.IntPtr.Size == 4)
               {
                  appName = dataPath + Util.Constants.TTS_WINDOWS_SUBPATH;
               }
               else
               {
                  appName = dataPath + Util.Constants.TTS_WINDOWS_SUBPATH;
               }
            }
            else
            {
               appName = dataPath + Util.Config.TTS_WINDOWS_BUILD;
            }
*/
            appName = System.IntPtr.Size == 4 ? dataPath + Util.Constants.TTS_WINDOWS_x86_SUBPATH : dataPath + Util.Constants.TTS_WINDOWS_SUBPATH;

            if (appName.Contains("'"))
            {
               Debug.LogError("The path to the application contains an apostrophe and the TTS-wrapper will therefore not work: " + appName);
            }
            else
            {
               try
               {
                  if (!System.IO.File.Exists(appName))
                  {
                     System.IO.File.WriteAllBytes(appName, System.IntPtr.Size == 4 ? System.Convert.FromBase64String(bin32) : System.Convert.FromBase64String(bin64));
                  }
               }
               catch (System.Exception ex)
               {
                  Debug.LogError("Could not write the TTS-wrapper to the destination: " + ex);
               }
            }

            return appName;
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

            float _pitch = wrapper.Pitch - 1f;

            if (Mathf.Abs(_pitch) > Common.Util.BaseConstants.FLOAT_TOLERANCE)
            {
               sbXML.Append("<prosody pitch='");

               sbXML.Append(_pitch > 0f
                  ? _pitch.ToString("+#0%", Util.Helper.BaseCulture)
                  : _pitch.ToString("#0%", Util.Helper.BaseCulture));

               sbXML.Append("'>");
            }

            sbXML.Append(wrapper.Text);

            if (Mathf.Abs(_pitch) > Common.Util.BaseConstants.FLOAT_TOLERANCE)
               sbXML.Append("</prosody>");

            sbXML.Append("</speak>");

            return getValidXML(sbXML.ToString().Replace('"', '\''));
         }

         return wrapper.Text.Replace('"', '\'');
      }

      private static int calculateVolume(float volume)
      {
         return Mathf.Clamp((int)(100 * volume), 0, 100);
      }

      private static int calculateRate(float rate)
      {
         //allowed range: 0 - 3f - all other values were cropped
         int result = 0;

         if (Mathf.Abs(rate - 1f) > Common.Util.BaseConstants.FLOAT_TOLERANCE)
         {
            //relevant?
            if (rate > 1f)
            {
               //larger than 1
               if (rate >= 2.75f)
               {
                  result = 10; //2.78
               }
               else if (rate >= 2.6f && rate < 2.75f)
               {
                  result = 9; //2.6
               }
               else if (rate >= 2.35f && rate < 2.6f)
               {
                  result = 8; //2.39
               }
               else if (rate >= 2.2f && rate < 2.35f)
               {
                  result = 7; //2.2
               }
               else if (rate >= 2f && rate < 2.2f)
               {
                  result = 6; //2
               }
               else if (rate >= 1.8f && rate < 2f)
               {
                  result = 5; //1.8
               }
               else if (rate >= 1.6f && rate < 1.8f)
               {
                  result = 4; //1.6
               }
               else if (rate >= 1.4f && rate < 1.6f)
               {
                  result = 3; //1.45
               }
               else if (rate >= 1.2f && rate < 1.4f)
               {
                  result = 2; //1.28
               }
               else if (rate > 1f && rate < 1.2f)
               {
                  result = 1; //1.14
               }
            }
            else
            {
               //smaller than 1
               if (rate <= 0.3f)
               {
                  result = -10; //0.33
               }
               else if (rate > 0.3 && rate <= 0.4f)
               {
                  result = -9; //0.375
               }
               else if (rate > 0.4 && rate <= 0.45f)
               {
                  result = -8; //0.42
               }
               else if (rate > 0.45 && rate <= 0.5f)
               {
                  result = -7; //0.47
               }
               else if (rate > 0.5 && rate <= 0.55f)
               {
                  result = -6; //0.525
               }
               else if (rate > 0.55 && rate <= 0.6f)
               {
                  result = -5; //0.585
               }
               else if (rate > 0.6 && rate <= 0.7f)
               {
                  result = -4; //0.655
               }
               else if (rate > 0.7 && rate <= 0.8f)
               {
                  result = -3; //0.732
               }
               else if (rate > 0.8 && rate <= 0.9f)
               {
                  result = -2; //0.82
               }
               else if (rate > 0.9 && rate < 1f)
               {
                  result = -1; //0.92
               }
            }
         }

         if (Util.Constants.DEV_DEBUG)
            Debug.Log("calculateRate: " + result + " - " + rate);

         return result;
      }

      #endregion


      #region Editor-only methods

#if UNITY_EDITOR

      public override void GenerateInEditor(Model.Wrapper wrapper)
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
               if (System.IO.File.Exists(applicationName))
               {
                  string voiceName = getVoiceName(wrapper);
                  int calculatedRate = calculateRate(wrapper.Rate);
                  int calculatedVolume = calculateVolume(wrapper.Volume);

                  string outputFile = getOutputFile(wrapper.Uid);

                  string args = "--speakToFile" +
                                $" -text \"{prepareText(wrapper)}\"" +
                                $" -file \"{outputFile.Replace('"', '\'')}\"" +
                                $" -rate {calculatedRate}" +
                                $" -volume {calculatedVolume}" +
                                $" -voice \"{voiceName.Replace('"', '\'')}\"";

                  if (Util.Config.DEBUG)
                     Debug.Log("Process arguments: " + args);
#if ENABLE_IL2CPP
                  using (Common.Util.CTProcess process = new Common.Util.CTProcess())
#else
                  using (System.Diagnostics.Process process = new System.Diagnostics.Process())
#endif
                  {
                     process.StartInfo.FileName = applicationName;
                     process.StartInfo.Arguments = args;

                     System.Threading.Thread worker = new System.Threading.Thread(() => startProcess(process, 0, false, false, false)) {Name = wrapper.Uid};
                     worker.Start();

                     silence = false;
                     onSpeakAudioGenerationStart(wrapper);

                     do
                     {
                        System.Threading.Thread.Sleep(50);
                     } while (worker.IsAlive || !process.HasExited);

                     if (process.ExitCode == 0)
                     {
                        processAudioFile(wrapper, outputFile);
                     }
                     else
                     {
                        using (System.IO.StreamReader sr = process.StandardError)
                        {
                           string errorMessage = "Could not generate the text: " + wrapper +
                                                 System.Environment.NewLine + "Exit code: " + process.ExitCode +
                                                 System.Environment.NewLine + sr.ReadToEnd();
                           Debug.LogError(errorMessage);
                           onErrorInfo(wrapper, errorMessage);
                        }
                     }
                  }
               }
               else
               {
                  string errorMessage = "Could not find the TTS-wrapper: '" + applicationName + "'";
                  Debug.LogError(errorMessage);
                  onErrorInfo(wrapper, errorMessage);
               }
            }
         }
      }

      public override void SpeakNativeInEditor(Model.Wrapper wrapper)
      {
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
               if (System.IO.File.Exists(applicationName))
               {
                  string voiceName = getVoiceName(wrapper);
                  int calculatedRate = calculateRate(wrapper.Rate);
                  int calculatedVolume = calculateVolume(wrapper.Volume);

                  string args = "--speak" +
                                $" -text \"{prepareText(wrapper)}\"" +
                                $" -rate {calculatedRate}" +
                                $" -volume {calculatedVolume}" +
                                $" -voice \"{voiceName.Replace('"', '\'')}\"";

                  if (Util.Config.DEBUG)
                     Debug.Log("Process arguments: " + args);
#if ENABLE_IL2CPP
                  using (Common.Util.CTProcess process = new Common.Util.CTProcess())
#else
                  using (System.Diagnostics.Process process = new System.Diagnostics.Process())
#endif
                  {
                     process.StartInfo.FileName = applicationName;
                     process.StartInfo.Arguments = args;

                     System.Threading.Thread worker = new System.Threading.Thread(() => startProcess(process, 0, false, false, false)) {Name = wrapper.Uid};
                     worker.Start();

                     silence = false;
                     onSpeakStart(wrapper);

                     do
                     {
                        System.Threading.Thread.Sleep(50);

                        if (silence && !process.HasExited)
                        {
                           process.Kill();
                        }
                     } while (worker.IsAlive || !process.HasExited);
#if ENABLE_IL2CPP
                  if (process.ExitCode == 0 || process.ExitCode == 123456) //123456 = Killed
#else
                     if (process.ExitCode == 0 || process.ExitCode == -1) //-1 = Killed
#endif
                     {
                        if (Util.Config.DEBUG)
                           Debug.Log("Text spoken: " + wrapper.Text);

                        onSpeakComplete(wrapper);
                     }
                     else
                     {
                        using (System.IO.StreamReader sr = process.StandardError)
                        {
                           Debug.LogError("Could not speak the text: " + process.ExitCode +
                                          System.Environment.NewLine + sr.ReadToEnd());
                        }
                     }
                  }
               }
               else
               {
                  string errorMessage = "Could not find the TTS-wrapper: '" + applicationName + "'";
                  Debug.LogError(errorMessage);
                  onErrorInfo(wrapper, errorMessage);
               }
            }
         }
      }

      private void getVoicesInEditor()
      {
         if (System.IO.File.Exists(applicationName))
         {
            System.Collections.Generic.List<Model.Voice> voices = new System.Collections.Generic.List<Model.Voice>();
#if ENABLE_IL2CPP
            using (Common.Util.CTProcess process = new Common.Util.CTProcess())
#else
            using (System.Diagnostics.Process process = new System.Diagnostics.Process())
#endif
            {
               process.StartInfo.FileName = applicationName;
               process.StartInfo.Arguments = "--voices";

               try
               {
                  System.Threading.Thread voiceWorker = new System.Threading.Thread(() => startProcess(process, Util.Constants.DEFAULT_TTS_KILL_TIME));
                  voiceWorker.Start();

                  do
                  {
                     System.Threading.Thread.Sleep(50);
                  } while (voiceWorker.IsAlive || !process.HasExited);

                  if (Util.Constants.DEV_DEBUG)
                     Debug.Log("Finished after: " + (process.ExitTime - process.StartTime).Seconds);

                  if (process.ExitCode == 0)
                  {
                     using (System.IO.StreamReader streamReader = process.StandardOutput)
                     {
                        while (!streamReader.EndOfStream)
                        {
                           string reply = streamReader.ReadLine();

                           if (!string.IsNullOrEmpty(reply))
                           {
                              if (reply.CTStartsWith(idVoice))
                              {
                                 string[] splittedString = reply.Split(splitChar,
                                    System.StringSplitOptions.RemoveEmptyEntries);

                                 if (splittedString.Length == 6)
                                 {
                                    //if (!splittedString[1].CTContains("espeak")) //ignore eSpeak voices
                                    //{
                                    voices.Add(new Model.Voice(splittedString[1], splittedString[2],
                                       Util.Helper.StringToGender(splittedString[3]), splittedString[4],
                                       splittedString[5]));
                                    //}
                                 }
                                 else
                                 {
                                    Debug.LogWarning("Voice is invalid: " + reply);
                                 }
                              }
                           }
                        }
                     }
                  }
                  else
                  {
                     using (System.IO.StreamReader sr = process.StandardError)
                     {
                        string errorMessage = "Could not get any voices: " + process.ExitCode +
                                              System.Environment.NewLine + sr.ReadToEnd();
                        Debug.LogError(errorMessage);
                     }
                  }
               }
               catch (System.Exception ex)
               {
                  string errorMessage = "Could not get any voices!" + System.Environment.NewLine + ex;
                  Debug.LogError(errorMessage);
               }
            }

            cachedVoices = voices.OrderBy(s => s.Name).ToList();

            if (Util.Constants.DEV_DEBUG)
               Debug.Log("Voices read: " + cachedVoices.CTDump());
         }
         else
         {
            string errorMessage = "Could not find the TTS-wrapper: '" + applicationName + "'";
            Debug.LogError(errorMessage);
         }

         onVoicesReady();
      }
#endif

      #endregion

      private const string bin32 = "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BACmAQ4ACADVAQIANQDmAQIAPQDmAS4ACwBEAi4AEwBNAi4AGwBsAi4AIwB1Ai4AKwCSAi4AMwC6Ai4AOwDAAi4AQwDUAi4ASwDrAi4AUwC6Ai4AWwAPAy4AYwAdAxUAUgBdAMEAXQFnAXMBfQF5AJMAzwDoAPsADgEhATQBRwEEgAAA5AcCAN4c3QEAAAAAAAA5AAAABAAAAAAAAAAAAAAAlAFuAAAAAAAEAAAAAAAAAAAAAACdAX8EAAAAAAAAAFJlYWRPbmx5Q29sbGVjdGlvbmAxAEV2ZW50SGFuZGxlcmAxAElFbnVtZXJhdG9yYDEASW50MzIAUlRWb2ljZVRUU1dyYXBwZXJfeDg2AGdldF9VVEY4ADxNb2R1bGU+AFN5c3RlbS5JTwByYQBtc2NvcmxpYgBTeXN0ZW0uQ29sbGVjdGlvbnMuR2VuZXJpYwBhZGRfU3RhdGVDaGFuZ2VkAHN5bnRoU3RhdGVDaGFuZ2VkAGFkZF9QaG9uZW1lUmVhY2hlZABzeW50aFBob25lbWVSZWFjaGVkAGFkZF9WaXNlbWVSZWFjaGVkAHN5bnRoVmlzZW1lUmVhY2hlZABnZXRfRW5hYmxlZABhZGRfU3BlYWtDb21wbGV0ZWQAc3ludGhTcGVha0NvbXBsZXRlZABhZGRfU3BlYWtTdGFydGVkAHN5bnRoU3BlYWtTdGFydGVkAGdldF9TdGFja1RyYWNlAENyb3NzdGFsZXMuUlRWb2ljZQBnZXRfVm9pY2UASW5zdGFsbGVkVm9pY2UAU2VsZWN0Vm9pY2UAc2VsZWN0Vm9pY2UAU2V0T3V0cHV0VG9EZWZhdWx0QXVkaW9EZXZpY2UAcmV0dXJuQ29kZQBnZXRfQWdlAFZvaWNlQWdlAGdldF9NZXNzYWdlAGFkZF9Wb2ljZUNoYW5nZQBzeW50aFZvaWNlQ2hhbmdlAElEaXNwb3NhYmxlAFNldE91dHB1dFRvV2F2ZUZpbGUAc3BlYWtUb0ZpbGUAQ29uc29sZQBnZXRfTmFtZQB2b2ljZU5hbWUAbmFtZQBnZXRfUGhvbmVtZQBnZXRfVmlzZW1lAHNldF9Wb2x1bWUAZ2V0Vm9sdW1lAFdyaXRlTGluZQBnZXRfTmV3TGluZQBnZXRfQ3VsdHVyZQBEaXNwb3NlAFRyeVBhcnNlAHNldF9SYXRlAGdldFJhdGUAZ2V0X1N0YXRlAFN5bnRoZXNpemVyU3RhdGUARGVidWdnYWJsZUF0dHJpYnV0ZQBBc3NlbWJseVRpdGxlQXR0cmlidXRlAEFzc2VtYmx5VHJhZGVtYXJrQXR0cmlidXRlAFRhcmdldEZyYW1ld29ya0F0dHJpYnV0ZQBBc3NlbWJseUZpbGVWZXJzaW9uQXR0cmlidXRlAEFzc2VtYmx5Q29uZmlndXJhdGlvbkF0dHJpYnV0ZQBBc3NlbWJseURlc2NyaXB0aW9uQXR0cmlidXRlAENvbXBpbGF0aW9uUmVsYXhhdGlvbnNBdHRyaWJ1dGUAQXNzZW1ibHlQcm9kdWN0QXR0cmlidXRlAEFzc2VtYmx5Q29weXJpZ2h0QXR0cmlidXRlAEFzc2VtYmx5Q29tcGFueUF0dHJpYnV0ZQBSdW50aW1lQ29tcGF0aWJpbGl0eUF0dHJpYnV0ZQB2YWx1ZQBSVFZvaWNlVFRTV3JhcHBlcl94ODYuZXhlAHNldF9PdXRwdXRFbmNvZGluZwBTeXN0ZW0uUnVudGltZS5WZXJzaW9uaW5nAFN0cmluZwBTeXN0ZW0uU3BlZWNoAFNwZWFrAHNwZWFrAFN5c3RlbS5Db2xsZWN0aW9ucy5PYmplY3RNb2RlbABTcGVha1NzbWwAdm9sAFN5c3RlbQBNYWluAG1pbgBKb2luAHZlcnNpb24AU3lzdGVtLkdsb2JhbGl6YXRpb24AU3lzdGVtLlJlZmxlY3Rpb24AQXJndW1lbnROdWxsRXhjZXB0aW9uAGdldF9EZXNjcmlwdGlvbgBTdHJpbmdDb21wYXJpc29uAGdldF9Wb2ljZUluZm8AQ3VsdHVyZUluZm8AY2xhbXAAZ2V0X0dlbmRlcgBWb2ljZUdlbmRlcgBzZW5kZXIAVFRTV3JhcHBlcgBUZXh0V3JpdGVyAFNwZWVjaFN5bnRoZXNpemVyAHNwZWVjaFN5bnRoZXNpemVyAGdldF9FcnJvcgBJRW51bWVyYXRvcgBHZXRFbnVtZXJhdG9yAC5jdG9yAC5jY3RvcgB3cml0ZUVycgBTeXN0ZW0uRGlhZ25vc3RpY3MAR2V0SW5zdGFsbGVkVm9pY2VzAHZvaWNlcwBTeXN0ZW0uUnVudGltZS5Db21waWxlclNlcnZpY2VzAERlYnVnZ2luZ01vZGVzAEdldENvbW1hbmRMaW5lQXJncwBTdGF0ZUNoYW5nZWRFdmVudEFyZ3MAUGhvbmVtZVJlYWNoZWRFdmVudEFyZ3MAVmlzZW1lUmVhY2hlZEV2ZW50QXJncwBTcGVha0NvbXBsZXRlZEV2ZW50QXJncwBTcGVha1N0YXJ0ZWRFdmVudEFyZ3MAVm9pY2VDaGFuZ2VFdmVudEFyZ3MAU3BlYWtQcm9ncmVzc0V2ZW50QXJncwBhcmdzAFN5c3RlbS5TcGVlY2guU3ludGhlc2lzAEVxdWFscwBDb250YWlucwBTeXN0ZW0uQ29sbGVjdGlvbnMAYWRkX1NwZWFrUHJvZ3Jlc3MAc3ludGhTcGVha1Byb2dyZXNzAENvbmNhdABPYmplY3QARW52aXJvbm1lbnQAZ2V0Q0xJQXJndW1lbnQAZ2V0X0N1cnJlbnQAZ2V0X0NvdW50AHdyaXRlT3V0AE1vdmVOZXh0AFN5c3RlbS5UZXh0AGdldF9UZXh0AHNwZWVjaFRleHQAdGV4dABtYXgAd3JpdGVMb2dPbmx5AElzTnVsbE9yRW1wdHkAAAB3TgBvACAAYQByAGcAdQBtAGUAbgB0AHMAIQAgAFUAcwBlACAAJwAtAC0AaABlAGwAcAAnACAAYQBzACAAYQByAGcAdQBtAGUAbgB0ACAAdABvACAAcwBlAGUAIABtAG8AcgBlACAAZABlAHQAYQBpAGwAcwAuAAEXQQByAGcAdQBtAGUAbgB0AHMAOgAgAAADIAAAES0ALQB2AG8AaQBjAGUAcwABDy0ALQBzAHAAZQBhAGsAARstAC0AcwBwAGUAYQBrAFQAbwBGAGkAbABlAAETLQAtAHYAZQByAHMAaQBvAG4AAUVSAFQAVgBvAGkAYwBlAFQAVABTAFcAcgBhAHAAcABlAHIAIAAoAHgAOAA2ACkAIAAtACAAMgAwADIAMAAuADIALgAwAAENLQAtAGgAZQBsAHAAARVBAHIAZwB1AG0AZQBuAHQAcwA6AAAVLQAtAC0ALQAtAC0ALQAtAC0ALQABbS0ALQB2AG8AaQBjAGUAcwAgACAAIAAgACAAIAAgACAAIAAgACAAIAAgACAAIAAgACAAUgBlAHQAdQByAG4AcwAgAGEAbABsACAAYQB2AGEAaQBsAGEAYgBsAGUAIAB2AG8AaQBjAGUAcwAuAAGAny0ALQBzAHAAZQBhAGsAIAAgACAAIAAgACAAIAAgACAAIAAgACAAIAAgACAAIAAgACAAUwBwAGUAYQBrAHMAIABhACAAdABlAHgAdAAgAHcAaQB0AGgAIABhAG4AIABvAHAAdABpAG8AbgBhAGwAIAByAGEAdABlACwAIAB2AG8AbAB1AG0AZQAgAGEAbgBkACAAdgBvAGkAYwBlAC4AAU0gACAALQB0AGUAeAB0ACAAPAB0AGUAeAB0AD4AIAAgACAAIAAgACAAIAAgACAAIAAgAFQAZQB4AHQAIAB0AG8AIABzAHAAZQBhAGsAAYCfIAAgAC0AcgBhAHQAZQAgADwAcgBhAHQAZQA+ACAAIAAgACAAIAAgACAAIAAgACAAIABTAHAAZQBlAGQAIAByAGEAdABlACAAYgBlAHQAdwBlAGUAbgAgAC0AMQAwACAALQAgADEAMAAgAG8AZgAgAHQAaABlACAAcwBwAGUAYQBrAGUAcgAgACgAbwBwAHQAaQBvAG4AYQBsACkALgABgJUgACAALQB2AG8AbAB1AG0AZQAgADwAdgBvAGwAdQBtAGUAPgAgACAAIAAgACAAIAAgAFYAbwBsAHUAbQBlACAAYgBlAHQAdwBlAGUAbgAgADAAIAAtACAAMQAwADAAIABvAGYAIAB0AGgAZQAgAHMAcABlAGEAawBlAHIAIAAoAG8AcAB0AGkAbwBuAGEAbAApAC4AAYCLIAAgAC0AdgBvAGkAYwBlACAAPAB2AG8AaQBjAGUATgBhAG0AZQA+ACAAIAAgACAAIABOAGEAbQBlACAAbwBmACAAdABoAGUAIAB2AG8AaQBjAGUAIABmAG8AcgAgAHQAaABlACAAcwBwAGUAZQBjAGgAIAAoAG8AcAB0AGkAbwBuAGEAbAApAC4AAYCzLQAtAHMAcABlAGEAawBUAG8ARgBpAGwAZQAgACAAIAAgACAAIAAgACAAIAAgACAAIABTAHAAZQBhAGsAcwAgAGEAIAB0AGUAeAB0ACAAdABvACAAYQAgAGYAaQBsAGUAIAB3AGkAdABoACAAYQBuACAAbwBwAHQAaQBvAG4AYQBsACAAcgBhAHQAZQAsACAAdgBvAGwAdQBtAGUAIABhAG4AZAAgAHYAbwBpAGMAZQAuAAFbIAAgAC0AZgBpAGwAZQAgADwAZgBpAGwAZQBQAGEAdABoAD4AIAAgACAAIAAgACAAIABOAGEAbQBlACAAbwBmACAAbwB1AHQAcAB1AHQAIABmAGkAbABlAC4AAWstAC0AdgBlAHIAcwBpAG8AbgAgACAAIAAgACAAIAAgACAAIAAgACAAIAAgACAAIAAgAFYAZQByAHMAaQBvAG4AIABvAGYAIAB0AGgAaQBzACAAYQBwAHAAbABpAGMAYQB0AGkAbwBuAC4AAVUtAC0AaABlAGwAcAAgACAAIAAgACAAIAAgACAAIAAgACAAIAAgACAAIAAgACAAIAAgAFQAaABpAHMAIABpAG4AZgBvAHIAbQBhAHQAaQBvAG4ALgABaVYAaQBzAGkAdAAgACcAaAB0AHQAcABzADoALwAvAHcAdwB3AC4AYwByAG8AcwBzAHQAYQBsAGUAcwAuAGMAbwBtACcAIABmAG8AcgAgAG0AbwByAGUAIABkAGUAdABhAGkAbABzAC4AASNVAG4AawBuAG8AdwBuACAAYwBvAG0AbQBhAG4AZAA6ACAAAA9AAFYATwBJAEMARQBTAAAlTgB1AG0AYgBlAHIAIABvAGYAIAB2AG8AaQBjAGUAcwA6ACAAAA9AAFYATwBJAEMARQA6AAADOgAAOVcAQQBSAE4ASQBOAEcAOgAgAFYAbwBpAGMAZQAgAGkAcwAgAGQAaQBzAGEAYgBsAGUAZAA6ACAAAAtAAEQATwBOAEUAAAstAHQAZQB4AHQAAUVBAHIAZwB1AG0AZQBuAHQAIAAnAC0AdABlAHgAdAAnACAAaQBzACAAbgB1AGwAbAAgAG8AcgAgAGUAbQBwAHQAeQAhAAELLQByAGEAdABlAAEPLQB2AG8AbAB1AG0AZQABDS0AdgBvAGkAYwBlAAENQABTAFAARQBBAEsAAAstAGYAaQBsAGUAAUVBAHIAZwB1AG0AZQBuAHQAIAAnAC0AZgBpAGwAZQAnACAAaQBzACAAbgB1AGwAbAAgAG8AcgAgAGUAbQBwAHQAeQAhAAEZQABTAFAARQBBAEsAVABPAEYASQBMAEUAABE8AC8AcwBwAGUAYQBrAD4AACdTAHAAZQBlAGMAaAAgAFMAUwBNAEwAIAB0AGUAeAB0ADoAIAAnAAEDJwABHVMAcABlAGUAYwBoACAAdABlAHgAdAA6ACAAJwABOVYAbwBpAGMAZQAg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AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAQAAADAAAAAQ3AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==";
      private const string bin64 = "TVqQAAMAAAAEAAAA//8AALgAAAAAAAAAQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgAAAAA4fug4AtAnNIbgBTM0hVGhpcyBwcm9ncmFtIGNhbm5vdCBiZSBydW4gaW4gRE9TIG1vZGUuDQ0KJAAAAAAAAABQRQAATAEDAGPme14AAAAAAAAAAOAAIgALATAAACgAAAAKAAAAAAAAukYAAAAgAAAAYAAAAABAAAAgAAAAAgAABAAAAAAAAAAGAAAAAAAAAACgAAAAAgAAAAAAAAMAYIUAABAAABAAAAAAEAAAEAAAAAAAABAAAAAAAAAAAAAAAGhGAABPAAAAAGAAAHwGAAAAAAAAAAAAAAAAAAAAAAAAAIAAAAwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIAAACAAAAAAAAAAAAAAACCAAAEgAAAAAAAAAAAAAAC50ZXh0AAAAwCYAAAAgAAAAKAAAAAIAAAAAAAAAAAAAAAAAACAAAGAucnNyYwAAAHwGAAAAYAAAAAgAAAAqAAAAAAAAAAAAAAAAAABAAABALnJlbG9jAAAMAAAAAIAAAAACAAAAMgAAAAAAAAAAAAAAAAAAQAAAQgAAAAAAAAAAAAAAAAAAAACcRgAAAAAAAEgAAAACAAUAMCoAADgcAAABAAAAAQAABgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABswAwBXAgAAAQAAESgNAAAKKA4AAAoCji0WcgEAAHAWKAwAAAYXgAMAAAQ4LQIAAHJ5AABwcpEAAHACKA8AAAooEAAAChcoCwAABgIWmgoGcpUAAHAZbxEAAAosMygDAAAG3fYBAAALB28SAAAKKBMAAAoHbxQAAAooFQAAChYoDAAABh9kgAMAAATdzQEAAAZypwAAcBlvEQAACiw2KAQAAAbdtQEAAAwIbxIAAAooEwAACghvFAAACigVAAAKFigMAAAGIMgAAACAAwAABN2JAQAABnK3AABwGW8RAAAKLDYoBQAABt1xAQAADQlvEgAACigTAAAKCW8UAAAKKBUAAAoWKAwAAAYgLAEAAIADAAAE3UUBAAAGctMAAHAZbxEAAAosD3LnAABwKBYAAAo4KAEAAAZyIQEAcBlvEQAACjn8AAAAcucAAHAoFgAACnKRAABwKBYAAApyLwEAcCgWAAAKckUBAHAoFgAACnJbAQBwKBYAAApykQAAcCgWAAAKcskBAHAoFgAACnJqAgBwKBYAAApyuAIAcCgWAAAKclkDAHAoFgAACnLwAwBwKBYAAApykQAAcCgWAAAKcn0EAHAoFgAACnJqAgBwKBYAAApyMgUAcCgWAAAKcrgCAHAoFgAACnJZAwBwKBYAAApy8AMAcCgWAAAKcpEAAHAoFgAACnKOBQBwKBYAAApykQAAcCgWAAAKcvoFAHAoFgAACnKRAABwKBYAAApykQAAcCgWAAAKclAGAHAoFgAACisbcroGAHAGKBAAAAoWKAwAAAYg9AEAAIADAAAEfgMAAAQqAAEoAAAAAFEAClsAKQ8AAAEAAJIACpwALA8AAAEAANYACuAALA8AAAETMAMAMAAAAAIAABEoFwAACgoWCyseAgYHmhtvEQAACiwOBo5pBxdYMQYGBxdYmioHF1gLBwaOaTLcFCobMAQAmQEAAAMAABFy3gYAcBYoCwAABnMYAAAKCgZvGQAACgty7gYAcAdvGgAACowhAAABKBsAAAoXKAsAAAYHbxwAAAoMOC0BAAAIbx0AAAoNCW8eAAAKOZAAAAAfCo0OAAABJRZyFAcAcKIlFwlvHwAACm8gAAAKoiUYciQHAHCiJRkJbx8AAApvIQAACqIlGnIkBwBwoiUbCW8fAAAKbyIAAAqMIwAAAaIlHHIkBwBwoiUdCW8fAAAKbyMAAAqMJAAAAaIlHnIkBwBwoiUfCQlvHwAACm8kAAAKoiglAAAKFigLAAAGOIsAAAAfCo0OAAABJRZyKAcAcKIlFwlvHwAACm8gAAAKoiUYciQHAHCiJRkJbx8AAApvIQAACqIlGnIkBwBwoiUbCW8fAAAKbyIAAAqMIwAAAaIlHHIkBwBwoiUdCW8fAAAKbyMAAAqMJAAAAaIlHnIkBwBwoiUfCQlvHwAACm8kAAAKoiglAAAKFigLAAAGCG8mAAAKOsj+///eFAgsBghvJwAACtwGLAYGbycAAArccmIHAHAWKAsAAAYqAAAAQTQAAAIAAAA6AAAAPwEAAHkBAAAKAAAAAAAAAAIAAAARAAAAcgEAAIMBAAAKAAAAAAAAABswAwA7AQAABAAAEXJuBwBwKAIAAAYKBigoAAAKLBZyegcAcBYoDAAABiDJAAAAgAMAAAQqcsAHAHAoAgAABgtyzAcAcCgCAAAGDHLcBwBwKAIAAAYNcuoHAHAWKAsAAAZzGAAAChMEEQQU/gYPAAAGcykAAApvKgAAChEEFP4GDQAABnMrAAAKbywAAAoRBBT+Bg4AAAZzLQAACm8uAAAKEQQU/gYQAAAGcy8AAApvMAAAChEEFP4GEQAABnMxAAAKbzIAAAoRBBT+BhIAAAZzMwAACm80AAAKEQQU/gYTAAAGczUAAApvNgAACgkoKAAACi0DCSsFfjcAAAoRBCgHAAAGEQRvOAAAChEEBygJAAAGbzkAAAoRBAgoCgAABm86AAAKBhEEKAYAAAbeDBEELAcRBG8nAAAK3HJiBwBwFigLAAAGKgABEAAAAgBcAMcjAQwAAAAAGzADADYBAAAFAAARcm4HAHAoAgAABgoGKCgAAAosFnJ6BwBwFigMAAAGIC0BAACAAwAABCpy+AcAcCgCAAAGCwcoKAAACiwWcgQIAHAWKAwAAAYgLgEAAIADAAAEKnLABwBwKAIAAAYMcswHAHAoAgAABg1y3AcAcCgCAAAGEwRySggAcBYoCwAABnMYAAAKEwURBRT+BhAAAAZzLwAACm8wAAAKEQUU/gYRAAAGczEAAApvMgAAChEFFP4GEgAABnMzAAAKbzQAAAoRBRT+BhMAAAZzNQAACm82AAAKEQQoKAAACi0EEQQrBX43AAAKEQUoBwAABhEFB287AAAKEQUIKAkAAAZvOQAAChEFCSgKAAAGbzoAAAoGEQUoBgAABt4MEQUsBxEFbycAAArccmIHAHAWKAsAAAYHFigLAAAGKgAAARAAAAIAhgCRFwEMAAAAABswAwB+AAAABgAAEQJyZAgAcG88AAAKLB9ydggAcAJynggAcCgVAAAKFigLAAAGAwJvPQAACisdcqIIAHACcp4IAHAoFQAAChYoCwAABgMCbz4AAAreMgpywAgAcAYoGwAAChYoDAAABt4eC3L6CABwBygbAAAKFigMAAAGIJABAACAAwAABN4AKgAAARwAAAAAAABLSwAUFAAAAQAAAABLXwAeDwAAARswAwBpAAAABwAAERYKAigoAAAKLUUDbxkAAApvHAAACgsrIwdvHQAACm8fAAAKbyAAAAoCbz8AAAosCwMCb0AAAAoXCt4UB28mAAAKLdXeCgcsBgdvJwAACtwGLRZyHgkAcAJynggAcCgVAAAKFigLAAAGKgAAAAEQAAACABYAL0UACgAAAAA6AgMyCAIEMAICKgQqAyoAEzADADsAAAAIAAARFwoCKCgAAAotLwISAChBAAAKLA0GH/YfCigIAAAGCisYclIJAHACcp4IAHAoFQAAChYoCwAABhcKBioAEzADADwAAAAIAAARH2QKAigoAAAKLS8CEgAoQQAACiwMBhYfZCgIAAAGCisZcqgJAHACcp4IAHAoFQAAChYoCwAABh9kCgYqKgMtBgIoFgAACio+Ay0LKEIAAAoCb0MAAAoqXnIGCgBwA29EAAAKKBAAAAoWKAsAAAYqcnIaCgBwA29FAAAKjCEAAAEoGwAAChYoCwAABipeciwKAHADb0YAAAooEAAAChYoCwAABioycjoKAHAWKAsAAAYqMnJMCgBwFigLAAAGKnJyYgoAcANvRwAACowqAAABKBsAAAoXKAsAAAYqcnKoCgBwA29IAAAKbyAAAAooEAAAChcoCwAABioGKgAAQlNKQgEAAQAAAAAADAAAAHY0LjAuMzAzMTkAAAAABQBsAAAAuAUAACN+AAAkBgAA6AcAACNTdHJpbmdzAAAAAAwOAADYCgAAI1VTAOQYAAAQAAAAI0dVSUQAAAD0GAAARAMAACNCbG9iAAAAAAAAAAIAAAFXHQIICQAAAAD6ATMAFgAAAQAAACoAAAACAAAAAwAAABQAAAAdAAAASAAAAAQAAAAMAAAACAAAAAkAAAABAAAAAgAAAAAAGwQBAAAAAAAGAIoDDQYGAPcDDQYGANIC4AUPAC0GAAAGAOYC3gQGAG0D3gQGAE4D3gQGAN4D3gQGAKoD3gQGAMMD3gQGAP0C3gQGADED3gQGABgDRAQGAFoHrAQGAPwErAQKAIIF8wYGAAEAfwQKAG0B8wYGACUAYQAGAPAErAQKAGUG8wYKAH0G8wYKANcG8wYKAKwG8wYKAJQG8wYKAE8G8wYKAMIG8wYGADsEpAcGACcCrAQGAF4ErAQGABYFrAQGAGEHrAQGADMArAQKACsF8wYKAFIF8wYKAMUB8wYGADUFyQQGALAFGwcGAPsBrAQGABYArAQGAHcFSwAKAMEC8wYAAAAAQgAAAAAAAQABAIEBEABsBVABOQABAAEAUYBCAloBUYDBBFoBEQCyAdwBUCAAAAAAlgCzBN8BAQDcIgAAAACRAG0H5QECABgjAAAAAJEABgbqAQMA9CQAAAAAkQB5BOoBAwBMJgAAAACRABsC6gEDAKAnAAAAAJEAeQTuAQMASCgAAAAAkQCIAe4BBQDQKAAAAACRAEEF9QEHAOAoAAAAAJEArwL8AQoAKCkAAAAAkQBpAvwBCwBwKQAAAACRAJIHAQIMAHspAAAAAJEA1wUBAg4AiykAAAAAkQCyAAcCEACjKQAAAACRANgADgISAMApAAAAAJEAQAcVAhQA2CkAAAAAkQAvARwCFgDlKQAAAACRAAoBIwIYAPIpAAAAAJEAjQAqAhoADyoAAAAAkQDqATECHAAsKgAAAACRGNAF6gEeAAAAAQDuBgAAAQBCAgAAAQC5BwAAAgCUBQAAAQA4AgAAAgCUBQAAAQAVBAAAAgC4BAAAAwDJBwAAAQBVAAAAAQCoBAAAAQDEBxAQAgDNBwAAAQDEBxAQAgDNBwAAAQBeBQAAAgAvBAAAAQBeBQAAAgAvBAAAAQBeBQAAAgAvBAAAAQBeBQAAAgAvBAAAAQBeBQAAAgAvBAAAAQBeBQAAAgAvBAAAAQBeBQAAAgAvBAkAygUBABEAygUGABkAygUKACkAygUQADEAygUQADkAygUQAEEAygUQAEkAygUQAFEAygUQAFkAygUQAGEAygUQAGkAygUQAOEAOQAfAOkAMQQkAPEAvAQqAPEAUwcxAPEACwc3AHkAzgE+AAEBfQJCAHkAQQE+APEAUwdGAOkAcwJNAAEBPAZYAIEAygUGAIEA8wVwAAwAiAeAAPEAUweEAAwAvAWKABQAfAeaAJEA6wCfAJEAJwWjABEBLwI+ABEBBgU+ABEBRwWpABEBvQGvABEBiQK1APEAUwe7ADEBmwefADkBlQIGAPEA2gfKABwAygXXAIEALgfdACQAygXXAIEAnwDwACwAygXXAIEAxgADATQAygXXAIEAHgEWATwAygXXAIEA9wApAUQAygXXAIEAfAA8AUwAygXXAIEA2gFPAfEA4gdaAYEAlAEGAIEApgIBAIEAXgIBAIEABwIQAPEAEgduAYEAngQQAIEAcwQQAPEACwduAYEAfAEQAAkBnQKBAekApgWIAUkBcwIQAKkARwI+ALEAUwKAALkAsAc+ANEAtwKOAdkAYwGjAA4ABACmAQ4ACADJAQIANQDaAQIAPQDaAS4ACwA4Ai4AEwBBAi4AGwBgAi4AIwBpAi4AKwCAAi4AMwCoAi4AOwCuAi4AQwBpAi4ASwDCAi4AUwCoAi4AWwDmAi4AYwD0AhUAUgBdAMEAXQFnAXMBfQF5AJMAzwDoAPsADgEhATQBRwEEgAAA5AcCAN4c+QEAAAAAAABlBQAABAAAAAAAAAAAAAAAlAFYAAAAAAAEAAAAAAAAAAAAAACdAWUEAAAAAAAAAFJlYWRPbmx5Q29sbGVjdGlvbmAxAEV2ZW50SGFuZGxlcmAxAElFbnVtZXJhdG9yYDEASW50MzIAZ2V0X1VURjgAPE1vZHVsZT4AU3lzdGVtLklPAHJhAG1zY29ybGliAFN5c3RlbS5Db2xsZWN0aW9ucy5HZW5lcmljAGFkZF9TdGF0ZUNoYW5nZWQAc3ludGhTdGF0ZUNoYW5nZWQAYWRkX1Bob25lbWVSZWFjaGVkAHN5bnRoUGhvbmVtZVJlYWNoZWQAYWRkX1Zpc2VtZVJlYWNoZWQAc3ludGhWaXNlbWVSZWFjaGVkAGdldF9FbmFibGVkAGFkZF9TcGVha0NvbXBsZXRlZABzeW50aFNwZWFrQ29tcGxldGVkAGFkZF9TcGVha1N0YXJ0ZWQAc3ludGhTcGVha1N0YXJ0ZWQAZ2V0X1N0YWNrVHJhY2UAQ3Jvc3N0YWxlcy5SVFZvaWNlAGdldF9Wb2ljZQBJbnN0YWxsZWRWb2ljZQBTZWxlY3RWb2ljZQBzZWxlY3RWb2ljZQBTZXRPdXRwdXRUb0RlZmF1bHRBdWRpb0RldmljZQByZXR1cm5Db2RlAGdldF9BZ2UAVm9pY2VBZ2UAZ2V0X01lc3NhZ2UAYWRkX1ZvaWNlQ2hhbmdlAHN5bnRoVm9pY2VDaGFuZ2UASURpc3Bvc2FibGUAU2V0T3V0cHV0VG9XYXZlRmlsZQBzcGVha1RvRmlsZQBDb25zb2xlAGdldF9OYW1lAHZvaWNlTmFtZQBuYW1lAGdldF9QaG9uZW1lAGdldF9WaXNlbWUAc2V0X1ZvbHVtZQBnZXRWb2x1bWUAV3JpdGVMaW5lAGdldF9OZXdMaW5lAGdldF9DdWx0dXJlAERpc3Bvc2UAVHJ5UGFyc2UAc2V0X1JhdGUAZ2V0UmF0ZQBnZXRfU3RhdGUAU3ludGhlc2l6ZXJTdGF0ZQBEZWJ1Z2dhYmxlQXR0cmlidXRlAEFzc2VtYmx5VGl0bGVBdHRyaWJ1dGUAQXNzZW1ibHlUcmFkZW1hcmtBdHRyaWJ1dGUAVGFyZ2V0RnJhbWV3b3JrQXR0cmlidXRlAEFzc2VtYmx5RmlsZVZlcnNpb25BdHRyaWJ1dGUAQXNzZW1ibHlDb25maWd1cmF0aW9uQXR0cmlidXRlAEFzc2VtYmx5RGVzY3JpcHRpb25BdHRyaWJ1dGUAQ29tcGlsYXRpb25SZWxheGF0aW9uc0F0dHJpYnV0ZQBBc3NlbWJseVByb2R1Y3RBdHRyaWJ1dGUAQXNzZW1ibHlDb3B5cmlnaHRBdHRyaWJ1dGUAQXNzZW1ibHlDb21wYW55QXR0cmlidXRlAFJ1bnRpbWVDb21wYXRpYmlsaXR5QXR0cmlidXRlAHZhbHVlAFJUVm9pY2VUVFNXcmFwcGVyLmV4ZQBzZXRfT3V0cHV0RW5jb2RpbmcAU3lzdGVtLlJ1bnRpbWUuVmVyc2lvbmluZwBTdHJpbmcAU3lzdGVtLlNwZWVjaABTcGVhawBzcGVhawBTeXN0ZW0uQ29sbGVjdGlvbnMuT2JqZWN0TW9kZWwAU3BlYWtTc21sAHZvbABTeXN0ZW0ATWFpbgBtaW4ASm9pbgB2ZXJzaW9uAFN5c3RlbS5HbG9iYWxpemF0aW9uAFN5c3RlbS5SZWZsZWN0aW9uAEFyZ3VtZW50TnVsbEV4Y2VwdGlvbgBnZXRfRGVzY3JpcHRpb24AU3RyaW5nQ29tcGFyaXNvbgBnZXRfVm9pY2VJbmZvAEN1bHR1cmVJbmZvAGNsYW1wAGdldF9HZW5kZXIAVm9pY2VHZW5kZXIAc2VuZGVyAFJUVm9pY2VUVFNXcmFwcGVyAFRleHRXcml0ZXIAU3BlZWNoU3ludGhlc2l6ZXIAc3BlZWNoU3ludGhlc2l6ZXIAZ2V0X0Vycm9yAElFbnVtZXJhdG9yAEdldEVudW1lcmF0b3IALmN0b3IALmNjdG9yAHdyaXRlRXJyAFN5c3RlbS5EaWFnbm9zdGljcwBHZXRJbnN0YWxsZWRWb2ljZXMAdm9pY2VzAFN5c3RlbS5SdW50aW1lLkNvbXBpbGVyU2VydmljZXMARGVidWdnaW5nTW9kZXMAR2V0Q29tbWFuZExpbmVBcmdzAFN0YXRlQ2hhbmdlZEV2ZW50QXJncwBQaG9uZW1lUmVhY2hlZEV2ZW50QXJncwBWaXNlbWVSZWFjaGVkRXZlbnRBcmdzAFNwZWFrQ29tcGxldGVkRXZlbnRBcmdzAFNwZWFrU3RhcnRlZEV2ZW50QXJncwBWb2ljZUNoYW5nZUV2ZW50QXJncwBTcGVha1Byb2dyZXNzRXZlbnRBcmdzAGFyZ3MAU3lzdGVtLlNwZWVjaC5TeW50aGVzaXMARXF1YWxzAENvbnRhaW5zAFN5c3RlbS5Db2xsZWN0aW9ucwBhZGRfU3BlYWtQcm9ncmVzcwBzeW50aFNwZWFrUHJvZ3Jlc3MAQ29uY2F0AE9iamVjdABFbnZpcm9ubWVudABnZXRDTElBcmd1bWVudABnZXRfQ3VycmVudABnZXRfQ291bnQAd3JpdGVPdXQATW92ZU5leHQAU3lzdGVtLlRleHQAZ2V0X1RleHQAc3BlZWNoVGV4dAB0ZXh0AG1heAB3cml0ZUxvZ09ubHkASXNOdWxsT3JFbXB0eQAAd04AbwAgAGEAcgBnAHUAbQBlAG4AdABzACEAIABVAHMAZQAgACcALQAtAGgAZQBsAHAAJwAgAGEAcwAgAGEAcgBnAHUAbQBlAG4AdAAgAHQAbwAgAHMAZQBlACAAbQBvAHIAZQAgAGQAZQB0AGEAaQBsAHMALgABF0EAcgBnAHUAbQBlAG4AdABzADoAIAAAAyAAABEtAC0AdgBvAGkAYwBlAHMAAQ8tAC0AcwBwAGUAYQBrAAEbLQAtAHMAcABlAGEAawBUAG8ARgBpAGwAZQABEy0ALQB2AGUAcgBzAGkAbwBuAAE5UgBUAFYAbwBpAGMAZQBUAFQAUwBXAHIAYQBwAHAAZQByACAALQAgADIAMAAyADAALgAyAC4AMAABDS0ALQBoAGUAbABwAAEVQQByAGcAdQBtAGUAbgB0AHMAOgAAFS0ALQAtAC0ALQAtAC0ALQAtAC0AAW0tAC0AdgBvAGkAYwBlAHMAIAAgACAAIAAgACAAIAAgACAAIAAgACAAIAAgACAAIAAgAFIAZQB0AHUAcgBuAHMAIABhAGwAbAAgAGEAdgBhAGkAbABhAGIAbABlACAAdgBvAGkAYwBlAHMALgABgJ8tAC0AcwBwAGUAYQBrACAAIAAgACAAIAAgACAAIAAgACAAIAAgACAAIAAgACAAIAAgAFMAcABlAGEAawBzACAAYQAgAHQAZQB4AHQAIAB3AGkAdABoACAAYQBuACAAbwBwAHQAaQBvAG4AYQBsACAAcgBhAHQAZQAsACAAdgBvAGwAdQBtAGUAIABhAG4AZAAgAHYAbwBpAGMAZQAuAAFNIAAgAC0AdABlAHgAdAAgADwAdABlAHgAdAA+ACAAIAAgACAAIAAgACAAIAAgACAAIABUAGUAeAB0ACAAdABvACAAcwBwAGUAYQBrAAGAnyAAIAAtAHIAYQB0AGUAIAA8AHIAYQB0AGUAPgAgACAAIAAgACAAIAAgACAAIAAgACAAUwBwAGUAZQBkACAAcgBhAHQAZQAgAGIAZQB0AHcAZQBlAG4AIAAtADEAMAAgAC0AIAAxADAAIABvAGYAIAB0AGgAZQAgAHMAcABlAGEAawBlAHIAIAAoAG8AcAB0AGkAbwBuAGEAbAApAC4AAYCVIAAgAC0AdgBvAGwAdQBtAGUAIAA8AHYAbwBsAHUAbQBlAD4AIAAgACAAIAAgACAAIABWAG8AbAB1AG0AZQAgAGIAZQB0AHcAZQBlAG4AIAAwACAALQAgADEAMAAwACAAbwBmACAAdABoAGUAIABzAHAAZQBhAGsAZQByACAAKABvAHAAdABpAG8AbgBhAGwAKQAuAAGAiyAAIAAtAHYAbwBpAGMAZQAgADwAdgBvAGkAYwBlAE4AYQBtAGUAPgAgACAAIAAgACAATgBhAG0AZQAgAG8AZgAgAHQAaABlACAAdgBvAGkAYwBlACAAZgBvAHIAIAB0AGgAZQAgAHMAcABlAGUAYwBoACAAKABvAHAAdABpAG8AbgBhAGwAKQAuAAGAsy0ALQBzAHAAZQBhAGsAVABvAEYAaQBsAGUAIAAgACAAIAAgACAAIAAgACAAIAAgACAAUwBwAGUAYQBrAHMAIABhACAAdABlAHgAdAAgAHQAbwAgAGEAIABmAGkAbABlACAAdwBpAHQAaAAgAGEAbgAgAG8AcAB0AGkAbwBuAGEAbAAgAHIAYQB0AGUALAAgAHYAbwBsAHUAbQBlACAAYQBuAGQAIAB2AG8AaQBjAGUALgABWyAAIAAtAGYAaQBsAGUAIAA8AGYAaQBsAGUAUABhAHQAaAA+ACAAIAAgACAAIAAgACAATgBhAG0AZQAgAG8AZgAgAG8AdQB0AHAAdQB0ACAAZgBpAGwAZQAuAAFrLQAtAHYAZQByAHMAaQBvAG4AIAAgACAAIAAgACAAIAAgACAAIAAgACAAIAAgACAAIABWAGUAcgBzAGkAbwBuACAAbwBmACAAdABoAGkAcwAgAGEAcABwAGwAaQBjAGEAdABpAG8AbgAuAAFVLQAtAGgAZQBsAHAAIAAgACAAIAAgACAAIAAgACAAIAAgACAAIAAgACAAIAAgACAAIABUAGgAaQBzACAAaQBuAGYAbwByAG0AYQB0AGkAbwBuAC4AAWlWAGkAcwBpAHQAIAAnAGgAdAB0AHAAcwA6AC8ALwB3AHcAdwAuAGMAcgBvAHMAcwB0AGEAbABlAHMALgBjAG8AbQAnACAAZgBvAHIAIABtAG8AcgBlACAAZABlAHQAYQBpAGwAcwAuAAEjVQBuAGsAbgBvAHcAbgAgAGMAbwBtAG0AYQBuAGQAOgAgAAAPQABWAE8ASQBDAEUAUwAAJU4AdQBtAGIAZQByACAAbwBmACAAdgBvAGkAYwBlAHMAOgAgAAAPQABWAE8ASQBDAEUAOgAAAzoAADlXAEEAUgBOAEkATgBHADoAIABWAG8AaQBjAGUAIABpAHMAIABkAGkAcwBhAGIAbABlAGQAOgAgAAALQABEAE8ATgBFAAALLQB0AGUAeAB0AAFFQQByAGcAdQBtAGUAbgB0ACAAJwAtAHQAZQB4AHQAJwAgAGkAcwAgAG4AdQBsAGwAIABvAHIAIABlAG0AcAB0AHkAIQABCy0AcgBhAHQAZQABDy0AdgBvAGwAdQBtAGUAAQ0tAHYAbwBpAGMAZQABDUAAUwBQAEUAQQBLAAALLQBmAGkAbABlAAFFQQByAGcAdQBtAGUAbgB0ACAAJwAtAGYAaQBsAGUAJwAgAGkAcwAgAG4AdQBsAGwAIABvAHIAIABlAG0AcAB0AHkAIQABGUAAUwBQAEUAQQBLAFQATwBGAEkATABFAAARPAAvAHMAcABlAGEAawA+AAAnUwBwAGUAZQBjAGgAIABTAFMATQBMACAAdABlAHgAdAA6ACAAJwABAycAAR1TAHAAZQBlAGMAaAAgAHQAZQB4AHQAOgAgACcAATlWAG8AaQBjAGUAIABoAGEAZAAgAGkAbgB2AGEAbABpAGQAIABzAGUAdAB0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AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAQAAADAAAALw2AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==";
   }
}
#endif
// © 2015-2021 crosstales LLC (https://www.crosstales.com)
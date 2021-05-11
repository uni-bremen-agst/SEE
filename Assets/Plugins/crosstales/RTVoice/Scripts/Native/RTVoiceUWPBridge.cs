#if ((UNITY_WSA || UNITY_XBOXONE) && !UNITY_EDITOR) && ENABLE_WINMD_SUPPORT //|| CT_DEVELOP
using UnityEngine;
using System;
using System.Threading.Tasks;
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Crosstales.RTVoice
{
   /// <summary>WSA (UWP) TTS bridge.</summary>
   public sealed class RTVoiceUWPBridge : IDisposable
   {
      #region Variables

      private static SpeechSynthesizer tts = new SpeechSynthesizer();

#if UNITY_WSA
      private static StorageFolder targetFolder = ApplicationData.Current.LocalFolder;
      //private static StorageFolder logFolder = ApplicationData.Current.LocalFolder;
      //private static StorageFile logFile;
      //private System.Collections.Generic.List<MediaElement> mediaElements = new System.Collections.Generic.List<MediaElement>();
#endif

      #endregion


      #region Constructor

      public RTVoiceUWPBridge()
      {
         tts = new SpeechSynthesizer();
      }

      #endregion


      #region Properties

      /// <summary>
      /// Indicates if the TTS-Engine is currently busy.
      /// </summary>
      /// <returns>True if the TTS-Engine is currently busy.</returns>
      public bool isBusy { get; set; }

#if UNITY_WSA
      /// <summary>
      /// Returns the target folder of the last Speak call.
      /// If there hasn't been a Speak call so far, returns ApplicationData.Current.LocalFolder.
      /// </summary>
      /// <returns>The target folder of the last Speak call.</returns>
      public static string TargetFolder
      {
         get
         {
            /*
            if (targetFolder == null)
            {
                targetFolder = ApplicationData.Current.LocalFolder;
            }
            */

            return targetFolder.Path;
         }
      }
#endif

      /// <summary>Returns the audio data of the last Speak call.</summary>
      /// <returns>The audio data of the last Speak call.</returns>
      public byte[] AudioData { get; private set; }

      /// <summary>
      /// Returns the available voices.
      /// </summary>
      /// <returns>Available voices as string-array. Format: DisplayName;Language</string></returns>
      public string[] Voices
      {
         get
         {
            System.Collections.Generic.List<string> result = new System.Collections.Generic.List<string>();

            foreach (VoiceInformation Voice in SpeechSynthesizer.AllVoices)
            {
               result.Add(Voice.DisplayName + ";" + Voice.Language);
            }

            return result.ToArray();
         }
      }

      #endregion


      #region Public Methods

#if UNITY_WSA
      /// <summary>
      /// Use the TTS engine to write the voice clip into a pre-defined Folder.
      /// </summary>
      /// <param name="text">Spoken text</param>
      /// <param name="path">Target folder</param>
      /// <param name="fileName">File name</param>
      /// <param name="voice">Desired voice</param>
      public async void SynthesizeToFile(string text, string path, string fileName, string voice)
      {
         isBusy = true;

         if (Util.Config.DEBUG)
            Debug.Log($"SynthesizeToFile: {text} - {path}");

         try
         {
            targetFolder = await StorageFolder.GetFolderFromPathAsync(path);

            SpeechSynthesisStream stream = await synthesizeText(text, voice);

            StorageFile outputFile = await targetFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

            using (var reader = new DataReader(stream))
            {
               await reader.LoadAsync((uint)stream.Size);

               IBuffer buffer = reader.ReadBuffer((uint)stream.Size);

               await FileIO.WriteBufferAsync(outputFile, buffer);
            }
         }
         catch (Exception ex)
         {
            Debug.LogError("Could not synthesize to file: " + ex);
         }

         isBusy = false;
      }
#endif

      /// <summary>
      /// Use the TTS engine to write the voice clip into a pre-defined Folder.
      /// </summary>
      /// <param name="text">Spoken text</param>
      /// <param name="path">Target folder</param>
      /// <param name="fileName">File name</param>
      /// <param name="voice">Desired voice</param>
      public async void SynthesizeToMemory(string text, string voice)
      {
         isBusy = true;

         if (Util.Config.DEBUG)
            Debug.Log($"SynthesizeToMemory: {text}");

         try
         {
            SpeechSynthesisStream stream = await synthesizeText(text, voice);

            using (var reader = new DataReader(stream))
            {
               await reader.LoadAsync((uint)stream.Size);

               IBuffer buffer = reader.ReadBuffer((uint)stream.Size);

               //AudioData = System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(buffer);
               AudioData = buffer.ToArray();
            }
         }
         catch (Exception ex)
         {
            Debug.LogError("Could not synthesize to memory: " + ex);
         }

         isBusy = false;
      }

      public void Dispose()
      {
         tts.Dispose();
      }

      #endregion


      #region Private Methods

      private async Task<SpeechSynthesisStream> synthesizeText(string inputText, string inputVoice)
      {
         if (!tts.Voice.DisplayName.Equals(inputVoice))
         {
            tts.Voice = SpeechSynthesizer.AllVoices[0];

            foreach (VoiceInformation Voice in SpeechSynthesizer.AllVoices)
            {
               if (Voice.DisplayName.Equals(inputVoice))
               {
                  tts.Voice = Voice;
                  break;
               }
            }
         }

         if (inputText.Contains("</speak>"))
            return await tts.SynthesizeSsmlToStreamAsync(inputText);

         return await tts.SynthesizeTextToStreamAsync(inputText);
      }

      #endregion
   }
}
#endif
// © 2016-2021 crosstales LLC (https://www.crosstales.com)
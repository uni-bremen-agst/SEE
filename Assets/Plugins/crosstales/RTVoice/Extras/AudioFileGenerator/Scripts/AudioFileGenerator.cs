using UnityEngine;
using System.Collections;

namespace Crosstales.RTVoice.Tool
{
   /// <summary>Process files with configured speeches.</summary>
   [ExecuteInEditMode]
   [HelpURL("https://crosstales.com/media/data/assets/rtvoice/api/class_crosstales_1_1_r_t_voice_1_1_tool_1_1_audio_file_generator.html")]
   public class AudioFileGenerator : MonoBehaviour
   {
      #region Variables

      [Header("Configuration")]
      [UnityEngine.Serialization.FormerlySerializedAsAttribute("TextFiles")] [Tooltip("Text files to generate."), SerializeField]
      private TextAsset[] textFiles;

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("FileInsideAssets")] [Tooltip("Are the specified file paths inside the Assets-folder (current project)? If this option is enabled, it prefixes the path with 'Application.dataPath' (default: true)."), SerializeField]
      private bool fileInsideAssets = true;

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("SampleRate")] [Header("Windows Settings"), Tooltip("Set the sample rate of the WAV files (default: 48000). Note: this works only under Windows standalone."), SerializeField]
      private Common.Model.Enum.SampleRate sampleRate = Common.Model.Enum.SampleRate._48000Hz;

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("BitsPerSample")] [HideInInspector, Tooltip("Set the bits per sample of the WAV files (default: 16). Note: this works only under Windows standalone."), SerializeField]
      private int bitsPerSample = 16;

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("Channels")] [Tooltip("Set the channels of the WAV files (default: 1). Note: this works only under Windows standalone."), Range(1, 2), SerializeField]
      private int channels = 2;

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("CreateCopy")] [Tooltip("Creates a copy of the downsampled WAV file and leaves the original intact (default: false). Note: this works only under Windows standalone."), SerializeField]
      private bool createCopy;

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("isNormalize")] [Tooltip("Normalize the volume of the WAV files (default: false). Note: this works only under Windows standalone."), SerializeField]
      private bool _isNormalize;

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("GenerateOnStart")] [Header("Behaviour Settings"), Tooltip("Enable generating of the texts on start (default: false)."), SerializeField]
      private bool generateOnStart;

      private static readonly char[] splitChar = {';'};

      private string lastUid = "crosstales";

      private bool isGenerate;

      #endregion


      #region Properties

      /// <summary>Text files to generate.</summary>
      public TextAsset[] TextFiles
      {
         get => textFiles;
         set => textFiles = value;
      }

      /// <summary>Are the specified file paths inside the Assets-folder (current project)? If this option is enabled, it prefixes the path with 'Application.dataPath'.</summary>
      public bool FileInsideAssets
      {
         get => fileInsideAssets;
         set => fileInsideAssets = value;
      }

      /// <summary>Set the sample rate of the WAV files. Note: this works only under Windows standalone.</summary>
      public Common.Model.Enum.SampleRate SampleRate
      {
         get => sampleRate;
         set => sampleRate = value;
      }

/*
      /// <summary>Set the bits per sample of the WAV files (default: 16). Note: this works only under Windows standalone.</summary>
      public int BitsPerSample
      {
         get => bitsPerSample;
         set => bitsPerSample = value;
      }
*/
      /// <summary>Set the channels of the WAV files. Note: this works only under Windows standalone.</summary>
      public int Channels
      {
         get => channels;
         set => channels = Mathf.Clamp(value, 1, 2);
      }

      /// <summary>Creates a copy of the downsampled WAV file and leaves the original intact. Note: this works only under Windows standalone.</summary>
      public bool CreateCopy
      {
         get => createCopy;
         set => createCopy = value;
      }

      /// <summary>Normalize the volume of the WAV files. Note: this works only under Windows standalone.</summary>
      public bool isNormalize
      {
         get => _isNormalize;
         set => _isNormalize = value;
      }

      /// <summary>Enable generating of the texts on start.</summary>
      public bool GenerateOnStart
      {
         get => generateOnStart;
         set => generateOnStart = value;
      }

      #endregion


      #region Events

      [Header("Events")] public AudioFileGeneratorStartEvent OnStarted;
      public AudioFileGeneratorCompleteEvent OnCompleted;

      /// <summary>An event triggered whenever a AudioFileGenerator 'Generate' is started.</summary>
      public event AudioFileGeneratorStart OnAudioFileGeneratorStart;

      /// <summary>An event triggered whenever a AudioFileGenerator 'Generate' is completed.</summary>
      public event AudioFileGeneratorComplete OnAudioFileGeneratorComplete;

      #endregion


      #region MonoBehaviour methods

      private void Start()
      {
         Speaker.Instance.OnSpeakAudioGenerationComplete += onSpeakAudioGenerationComplete;
         Speaker.Instance.OnVoicesReady += onVoicesReady;
      }

      private void OnDestroy()
      {
         if (!Util.Helper.isEditorMode && Speaker.Instance != null)
         {
            Speaker.Instance.OnSpeakAudioGenerationComplete -= onSpeakAudioGenerationComplete;
            Speaker.Instance.OnVoicesReady -= onVoicesReady;
         }
      }

      private void OnValidate()
      {
         if (bitsPerSample < 15)
         {
            bitsPerSample = 8;
         }
         else if (bitsPerSample < 31)
         {
            bitsPerSample = 16;
         }
         else
         {
            bitsPerSample = 32;
         }

         channels = channels <= 1 ? 1 : 2;
      }

      #endregion


      #region Public methods

      /// <summary>Generate the audio files from the text files.</summary>
      public void Generate()
      {
         if (!isGenerate)
         {
            isGenerate = true;

            if (Util.Helper.isEditorMode)
            {
#if UNITY_EDITOR
               generateInEditor();
#endif
            }
            else
            {
               StartCoroutine(generate());
            }
         }
      }

      #endregion


      #region Private methods

#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && !UNITY_EDITOR_OSX && !UNITY_EDITOR_LINUX
      private void convert(string outputFile)
      {
         string tmpFile = outputFile.Substring(0, outputFile.Length - 4) + "_" + sampleRate +
                          Speaker.Instance.AudioFileExtension;
         bool converted = false;

         try
         {
            using (NAudio.Wave.WaveFileReader reader = new NAudio.Wave.WaveFileReader(outputFile))
            {
               if (reader.WaveFormat.SampleRate != (int)sampleRate)
               {
                  NAudio.Wave.WaveFormat newFormat = new NAudio.Wave.WaveFormat((int)sampleRate, bitsPerSample, channels);

                  using (NAudio.Wave.WaveFormatConversionStream conversionStream =
                     new NAudio.Wave.WaveFormatConversionStream(newFormat, reader))
                  {
                     NAudio.Wave.WaveFileWriter.CreateWaveFile(tmpFile, conversionStream);
                  }

                  converted = true;
               }
            }
         }
         catch (System.Exception ex)
         {
            Debug.LogError("Could not convert audio file: " + ex, this);
         }

         if (converted)
         {
            try
            {
               if (!createCopy)
               {
                  System.IO.File.Delete(outputFile);

                  System.IO.File.Move(tmpFile, outputFile);
               }
            }
            catch (System.Exception ex)
            {
               Debug.LogError("Could not delete and move audio files: " + ex, this);
            }
         }
      }

      private void normalizeWAV(string inputFile)
      {
         string tmpFile = inputFile.Substring(0, inputFile.Length - 4) + "_normalized" + Speaker.Instance.AudioFileExtension;

         try
         {
            //float max = 0;

            using (NAudio.Wave.AudioFileReader reader = new NAudio.Wave.AudioFileReader(inputFile))
            {
               float max = getMaxPeak(inputFile);

               if (Mathf.Abs(max) < Common.Util.BaseConstants.FLOAT_TOLERANCE || max > 1f)
               {
                  Debug.LogWarning("File cannot be normalized!", this);
               }
               else
               {
                  // rewind and amplify
                  reader.Position = 0;
                  reader.Volume = 1f / max;

                  // write out to a new WAV file
                  //NAudio.Wave.WaveFileWriter.CreateWaveFile16(inputFile, reader);
                  NAudio.Wave.WaveFileWriter.CreateWaveFile16(tmpFile, reader);
               }
            }

            //System.IO.File.Delete(tmpFile);
         }
         catch (System.Exception ex)
         {
            Debug.LogError("Could not normalize audio file: " + ex, this);
         }
      }

      private float getMaxPeak(string inputFile)
      {
         float max = 0;

         try
         {
            using (NAudio.Wave.AudioFileReader reader = new NAudio.Wave.AudioFileReader(inputFile))
            {
               // find the max peak
               float[] buffer = new float[reader.WaveFormat.SampleRate];
               int read;

               do
               {
                  read = reader.Read(buffer, 0, buffer.Length);
                  for (int ii = 0; ii < read; ii++)
                  {
                     float abs = Mathf.Abs(buffer[ii]);
                     if (abs > max) max = abs;
                  }
               } while (read > 0);
            }
         }
         catch (System.Exception ex)
         {
            Debug.LogError("Could not find the max peak in audio file: " + ex, this);
         }

         return max;
      }
#endif
      private IEnumerator generate()
      {
         onStart();

         foreach (TextAsset textFile in textFiles)
         {
            if (textFile != null)
            {
               System.Collections.Generic.List<string> speeches = Util.Helper.SplitStringToLines(textFile.text);

               foreach (string speech in speeches)
               {
                  if (!speech.CTStartsWith("#"))
                  {
                     string[] args = speech.Split(splitChar, System.StringSplitOptions.RemoveEmptyEntries);

                     if (args.Length >= 2)
                     {
                        Model.Wrapper wrapper = prepare(args, speech);
                        string uid = Speaker.Instance.Generate(wrapper);

                        do
                        {
                           yield return null;
                        } while (!uid.Equals(lastUid));

#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && !UNITY_EDITOR_OSX && !UNITY_EDITOR_LINUX
                        convert(wrapper.OutputFile);

                        if (_isNormalize)
                           normalizeWAV(wrapper.OutputFile);
#endif
                     }
                     else
                     {
                        Debug.LogWarning("Invalid speech: " + speech, this);
                     }
                  }
               }
            }
         }

         if (Util.Config.DEBUG)
            Debug.Log("Generate finished!", this);

         onComplete();

         isGenerate = false;
      }

      private Model.Wrapper prepare(string[] args, string speech)
      {
         Model.Wrapper wrapper = new Model.Wrapper {Text = args[0]};

         if (fileInsideAssets)
         {
            wrapper.OutputFile = Application.dataPath + @"/" + args[1];
         }
         else
         {
            wrapper.OutputFile = args[1];
         }

         if (args.Length >= 3)
         {
            wrapper.Voice = Speaker.Instance.VoiceForName(args[2]);
         }

         if (args.Length >= 4)
         {
            if (!float.TryParse(args[3], out float rate))
            {
               Debug.LogWarning("Rate was invalid: " + speech, this);
            }
            else
            {
               wrapper.Rate = rate;
            }
         }

         if (args.Length >= 5)
         {
            if (!float.TryParse(args[4], out float pitch))
            {
               Debug.LogWarning("Pitch was invalid: " + speech, this);
            }
            else
            {
               wrapper.Pitch = pitch;
            }
         }

         if (args.Length >= 6)
         {
            if (!float.TryParse(args[5], out float volume))
            {
               Debug.LogWarning("Volume was invalid: " + speech, this);
            }
            else
            {
               wrapper.Volume = volume;
            }
         }

         return wrapper;
      }

      #endregion


      #region Callbacks

      private void onVoicesReady()
      {
         if (generateOnStart)
            Generate();
      }

      private void onSpeakAudioGenerationComplete(Model.Wrapper wrapper)
      {
         lastUid = wrapper.Uid;

         if (Util.Config.DEBUG)
            Debug.Log("Speech generated: " + wrapper, this);
      }

      #endregion


      #region Event-trigger methods

      private void onStart()
      {
         if (Util.Config.DEBUG)
            Debug.Log("onStart", this);

         if (!Util.Helper.isEditorMode)
            OnStarted?.Invoke();

         OnAudioFileGeneratorStart?.Invoke();
      }

      private void onComplete()
      {
         if (Util.Config.DEBUG)
            Debug.Log("onComplete", this);

         if (!Util.Helper.isEditorMode)
            OnCompleted?.Invoke();

         OnAudioFileGeneratorComplete?.Invoke();
      }

      #endregion


      #region Editor-only methods

#if UNITY_EDITOR
      private void generateInEditor()
      {
         foreach (TextAsset textFile in textFiles)
         {
            if (textFile != null)
            {
               System.Collections.Generic.List<string> speeches = Util.Helper.SplitStringToLines(textFile.text);

               foreach (string speech in speeches)
               {
                  if (!speech.CTStartsWith("#"))
                  {
                     string[] args = speech.Split(splitChar, System.StringSplitOptions.RemoveEmptyEntries);

                     if (args.Length >= 2)
                     {
                        Model.Wrapper wrapper = prepare(args, speech);
                        Speaker.Instance.Generate(wrapper);

#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && !UNITY_EDITOR_OSX && !UNITY_EDITOR_LINUX
                        if (_isNormalize)
                           Debug.LogWarning("Normalization is only supported in Play-mode!", this);
#endif
/*
                        string uid = Speaker.Generate(wrapper);
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                        do
                        {
                           //Debug.Log("Wait...: " + uid + " - " + lastUid);
                           System.Threading.Thread.Sleep(50);
                        } while (!uid.Equals(lastUid));

                        Debug.Log(wrapper);
                        convert(wrapper.OutputFile);

                        if (isNormalize)
                           normalizeWAV(wrapper.OutputFile);
#endif
*/
                     }
                     else
                     {
                        Debug.LogWarning("Invalid speech: " + speech, this);
                     }
                  }
               }
            }
         }

         if (Util.Config.DEBUG)
            Debug.Log("Generate finished!", this);

#if UNITY_EDITOR
         if (fileInsideAssets)
            UnityEditor.AssetDatabase.Refresh();
#endif

         isGenerate = false;
      }

#endif

      #endregion
   }
}
// © 2017-2021 crosstales LLC (https://www.crosstales.com)
using UnityEngine;

namespace Crosstales.RTVoice.Model
{
   /// <summary>Wrapper for "Speak"-function calls.</summary>
   [System.Serializable]
   public class Wrapper
   {
      #region Variables

      [Tooltip("Text for the speech."), TextArea(1, 5), SerializeField] private string text = string.Empty;

      [Tooltip("AudioSource for the speech."), SerializeField] private AudioSource source;

      [Tooltip("Voice for the speech."), SerializeField] private Voice voice;

      [Tooltip("Speak immediately after the audio generation. Only works if 'Source' is not null."), SerializeField]
      private bool speakImmediately = true;

      [Tooltip("Speech rate of the speaker in percent (1 = 100%, default: 1, optional)."), Range(0.01f, 3f), SerializeField]
      private float rate = 1f;

      [Tooltip("Speech pitch of the speaker in percent (1 = 100%, default: 1, optional)."), Range(0f, 2f), SerializeField]
      private float pitch = 1f;

      [Tooltip("Volume of the speaker in percent (1 = 100%, default: 1, optional)."), Range(0f, 1f), SerializeField]
      private float volume = 1f;

      [Tooltip("Output file (without extension) for the generated audio."), SerializeField] private string outputFile;

      [Tooltip("Force SSML on supported platforms."), SerializeField] private bool forceSSML = true;

      [Tooltip("Is the current wrapper just a part of a speech (only used in iOS)."), SerializeField] private bool _isPartial;

      private string uid;

      //private string cachedString;
      private readonly System.DateTime created = System.DateTime.Now;

      #endregion


      #region Properties

      /// <summary>Text for the speech.</summary>
      public string Text
      {
         get
         {
            //if (cachedString == null)
            //{
            string result /*= cachedString*/ = Util.Helper.CleanText(text, Speaker.Instance.AutoClearTags || !ForceSSML /*&& !(Speaker.isMaryMode /* || Util.Helper.isWindowsPlatform )*/);

            if (result.Length > Speaker.Instance.MaxTextLength)
            {
               Debug.LogWarning("Text is too long! It will be shortened to " + Speaker.Instance.MaxTextLength + " characters: " + this);

               return result.Substring(0, Speaker.Instance.MaxTextLength);
               //cachedString = result.Substring(0, Speaker.Instance.MaxTextLength);
            }
            //}

            return result;
            //return cachedString;
         }

         set
         {
            //cachedString = null;
            text = value;
         }
      }

      /// <summary>AudioSource for the speech.</summary>
      public AudioSource Source
      {
         get => source;
         set => source = value;
      }

      /// <summary>Voice for the speech.</summary>
      public Voice Voice
      {
         get => voice;
         set => voice = value;
      }

      /// <summary>Speak immediately after the audio generation. Only works if 'Source' is not null.</summary>
      public bool SpeakImmediately
      {
         get => speakImmediately;
         set => speakImmediately = value;
      }

      /// <summary>Rate of the speech (range: 0.01-3).</summary>
      public float Rate
      {
         get => rate;
         set => rate = Mathf.Clamp(value, 0.01f, 3f);
      }

      /// <summary>Pitch of the speech (range: 0-2).</summary>
      public float Pitch
      {
         get => pitch;
         set => pitch = Mathf.Clamp(value, 0f, 2f);
      }

      /// <summary>Volume of the speech (range: 0.01-1).</summary>
      public float Volume
      {
         get => volume;
         set => volume = Mathf.Clamp(value, 0.01f, 1f);
      }

      /// <summary>Output file (without extension) for the generated audio.</summary>
      public string OutputFile
      {
         get => outputFile;
         set => outputFile = value;
      }

      /// <summary>Force SSML on supported platforms.</summary>
      public bool ForceSSML
      {
         get => forceSSML;
         set => forceSSML = value;
      }

      /// <summary>Is the current wrapper just a part of a speech (only used in iOS).</summary>
      public bool isPartial
      {
         get => _isPartial;
         set => _isPartial = value;
      }

      /// <summary>UID of the speech.</summary>
      public string Uid
      {
         get => uid;
         private set => uid = value;
      }

      /// <summary>Returns the creation time of the Wrapper.</summary>
      /// <returns>Creation time of the Wrapper.</returns>
      public System.DateTime Created => created;

      /// <summary>Returns the speech time in seconds (0: no audio file was generated).</summary>
      /// <returns>Speech time in seconds.</returns>
      public float SpeechTime
      {
         get
         {
            if (!Util.Helper.isEditorMode && source != null && source.clip != null)
            {
               return source.clip.length;
            }

            return 0f;
         }
      }

      #endregion


      #region Constructors

      /// <summary>Default.</summary>
      public Wrapper()
      {
         uid = System.Guid.NewGuid().ToString();
      }

      /// <summary>Instantiate the class.</summary>
      /// <param name="text">Text for the speech.</param>
      /// <param name="voice">Voice for the speech (default: null, optional).</param>
      /// <param name="rate">Rate of the speech (values: 0-3, default: 1, optional).</param>
      /// <param name="pitch">Pitch of the speech (values: 0-2, default: 1, optional).</param>
      /// <param name="volume">Volume of the speech (values: 0-1, default: 1, optional).</param>
      /// <param name="forceSSML">Force SSML on supported platforms (default: true, optional).</param>
      public Wrapper(string text, Voice voice = null, float rate = 1f, float pitch = 1f, float volume = 1f, bool forceSSML = true)
      {
         uid = System.Guid.NewGuid().ToString();
         Text = text;
         this.voice = voice;
         Rate = rate;
         Pitch = pitch;
         Volume = volume;
         this.forceSSML = forceSSML;
      }

      /// <summary>Instantiate the class.</summary>
      /// <param name="text">Text for the speech.</param>
      /// <param name="voice">Voice for the speech (default: null, optional).</param>
      /// <param name="rate">Rate of the speech (values: 0-3, default: 1, optional).</param>
      /// <param name="pitch">Pitch of the speech (values: 0-2, default: 1, optional).</param>
      /// <param name="volume">Volume of the speech (values: 0-1, default: 1, optional).</param>
      /// <param name="source">AudioSource for the speech (default: null, optional).</param>
      /// <param name="speakImmediately">Speak immediately after the audio generation. Only works if 'Source' is not null (default: true, optional).</param>
      /// <param name="outputFile">Output file (without extension) for the generated audio (default: empty, optional).</param>
      /// <param name="forceSSML">Force SSML on supported platforms (default: true, optional).</param>
      public Wrapper(string text, Voice voice = null, float rate = 1f, float pitch = 1f, float volume = 1f, AudioSource source = null, bool speakImmediately = true, string outputFile = "", bool forceSSML = true)
      {
         uid = System.Guid.NewGuid().ToString();
         Text = text;
         this.source = source;
         this.voice = voice;
         this.speakImmediately = speakImmediately;
         Rate = rate;
         Pitch = pitch;
         Volume = volume;
         this.outputFile = outputFile;
         this.forceSSML = forceSSML;
      }

      /// <summary>Instantiate the class.</summary>
      /// <param name="uid">UID of the speech.</param>
      /// <param name="text">Text for the speech.</param>
      /// <param name="voice">Voice for the speech (default: null, optional).</param>
      /// <param name="rate">Rate of the speech (values: 0-3, default: 1, optional).</param>
      /// <param name="pitch">Pitch of the speech (values: 0-2, default: 1, optional).</param>
      /// <param name="volume">Volume of the speech (values: 0-1, default: 1, optional).</param>
      /// <param name="source">AudioSource for the speech (default: null, optional).</param>
      /// <param name="speakImmediately">Speak immediately after the audio generation. Only works if 'Source' is not null (default: true, optional).</param>
      /// <param name="outputFile">Output file (without extension) for the generated audio (default: empty, optional).</param>
      /// <param name="forceSSML">Force SSML on supported platforms (default: true, optional).</param>
      public Wrapper(string uid, string text, Voice voice = null, float rate = 1f, float pitch = 1f, float volume = 1f, AudioSource source = null, bool speakImmediately = true, string outputFile = "", bool forceSSML = true) : this(text, voice, rate, pitch, volume, source, speakImmediately, outputFile, forceSSML)
      {
         this.uid = uid;
      }

      #endregion


      #region Overridden methods

      public override string ToString()
      {
         System.Text.StringBuilder result = new System.Text.StringBuilder();

         result.Append(GetType().Name);
         result.Append(Util.Constants.TEXT_TOSTRING_START);

         result.Append("Uid='");
         result.Append(uid);
         result.Append(Util.Constants.TEXT_TOSTRING_DELIMITER);

         result.Append("Text='");
         result.Append(text);
         result.Append(Util.Constants.TEXT_TOSTRING_DELIMITER);

         result.Append("Source='");
         result.Append(source);
         result.Append(Util.Constants.TEXT_TOSTRING_DELIMITER);

         result.Append("Voice='");
         result.Append(voice);
         result.Append(Util.Constants.TEXT_TOSTRING_DELIMITER);

         result.Append("SpeakImmediately='");
         result.Append(speakImmediately);
         result.Append(Util.Constants.TEXT_TOSTRING_DELIMITER);

         result.Append("Rate='");
         result.Append(rate);
         result.Append(Util.Constants.TEXT_TOSTRING_DELIMITER);

         result.Append("Pitch='");
         result.Append(pitch);
         result.Append(Util.Constants.TEXT_TOSTRING_DELIMITER);

         result.Append("Volume='");
         result.Append(volume);
         result.Append(Util.Constants.TEXT_TOSTRING_DELIMITER);

         result.Append("OutputFile='");
         result.Append(outputFile);
         result.Append(Util.Constants.TEXT_TOSTRING_DELIMITER);

         result.Append("ForceSSML='");
         result.Append(forceSSML);
         result.Append(Util.Constants.TEXT_TOSTRING_DELIMITER);

         result.Append("isPartial='");
         result.Append(isPartial);
         result.Append(Util.Constants.TEXT_TOSTRING_DELIMITER);

         result.Append("Created='");
         result.Append(Created);
         result.Append(Util.Constants.TEXT_TOSTRING_DELIMITER_END);

         result.Append(Util.Constants.TEXT_TOSTRING_END);

         return result.ToString();
      }

      public override bool Equals(object obj)
      {
         if (obj == null || GetType() != obj.GetType())
            return false;

         Wrapper o = (Wrapper)obj;

         bool result = Text == o.Text &&
                       voice == o.voice &&
                       System.Math.Abs(Rate - o.Rate) < Util.Constants.FLOAT_TOLERANCE &&
                       System.Math.Abs(Pitch - o.Pitch) < Util.Constants.FLOAT_TOLERANCE &&
                       System.Math.Abs(Volume - o.Volume) < Util.Constants.FLOAT_TOLERANCE;

         return result;
      }

      public override int GetHashCode()
      {
         int hash = 0;

         if (Text != null)
            hash += Text.GetHashCode();
         if (voice != null)
            hash += voice.GetHashCode();
         hash += (int)(Rate * 100) * 17;
         hash += (int)(Pitch * 100) * 17;
         hash += (int)(Volume * 100) * 17;

         return hash;
      }

      #endregion
   }
}
// © 2015-2021 crosstales LLC (https://www.crosstales.com)
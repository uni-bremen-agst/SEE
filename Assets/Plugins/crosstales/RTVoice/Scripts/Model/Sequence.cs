using UnityEngine;

namespace Crosstales.RTVoice.Model
{
   /// <summary>Model for a sequence.</summary>
   [System.Serializable]
   public class Sequence
   {
      #region Variables

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("Text")] [Tooltip("Text to speak."), TextArea(1, 5), SerializeField]
      private string text = string.Empty;

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("Voices")] [Tooltip("Voices for the speech."), SerializeField]
      private VoiceAlias voices;

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("Mode")] [Tooltip("Speak mode (default: 'Speak')."), SerializeField]
      private Enum.SpeakMode mode = Enum.SpeakMode.Speak;


      [UnityEngine.Serialization.FormerlySerializedAsAttribute("Source")] [Header("Optional Settings")] [Tooltip("AudioSource for the output (optional)."), SerializeField, System.Xml.Serialization.XmlIgnore]
      private AudioSource source;

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("Rate")] [Tooltip("Speech rate of the speaker in percent (1 = 100%, default: 1, optional)."), Range(0.01f, 3f), SerializeField]
      private float rate = 1f;

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("Pitch")] [Tooltip("Speech pitch of the speaker in percent (1 = 100%, default: 1, optional)."), Range(0f, 2f), SerializeField]
      private float pitch = 1f;

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("Volume")] [Tooltip("Volume of the speaker in percent (1 = 100%, default: 1, optional)."), Range(0f, 1f), SerializeField]
      private float volume = 1f;

      private bool initialized;

      #endregion


      #region Properties

      /// <summary>Text to speak.</summary>
      public string Text
      {
         get => text;
         set => text = value;
      }

      /// <summary>Voices for the speech.</summary>
      public VoiceAlias Voices
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

      /// <summary>AudioSource for the output.</summary>
      public AudioSource Source
      {
         get => source;
         set => source = value;
      }

      /// <summary>Speech rate of the speaker in percent (1 = 100%, range: 0.01-3).</summary>
      public float Rate
      {
         get => rate;
         set => rate = Mathf.Clamp(value, 0.01f, 3f);
      }

      /// <summary>Speech pitch of the speaker in percent (1 = 100%, range: 0-2).</summary>
      public float Pitch
      {
         get => pitch;
         set => pitch = Mathf.Clamp(value, 0f, 2f);
      }

      /// <summary>Volume of the speaker in percent (1 = 100%, range: 0-1).</summary>
      public float Volume
      {
         get => volume;
         set => volume = Mathf.Clamp01(value);
      }

      public bool Initialized
      {
         get => initialized;
         set => initialized = value;
      }

      #endregion


      #region Overridden methods

      public override string ToString()
      {
         System.Text.StringBuilder result = new System.Text.StringBuilder();

         result.Append(GetType().Name);
         result.Append(Util.Constants.TEXT_TOSTRING_START);

         result.Append("Text='");
         result.Append(text);
         result.Append(Util.Constants.TEXT_TOSTRING_DELIMITER);

         result.Append("Voices='");
         result.Append(voices);
         result.Append(Util.Constants.TEXT_TOSTRING_DELIMITER);

         result.Append("Source='");
         result.Append(source);
         result.Append(Util.Constants.TEXT_TOSTRING_DELIMITER);

         result.Append("Rate='");
         result.Append(rate);
         result.Append(Util.Constants.TEXT_TOSTRING_DELIMITER);

         result.Append("Pitch='");
         result.Append(pitch);
         result.Append(Util.Constants.TEXT_TOSTRING_DELIMITER);

         result.Append("Volume='");
         result.Append(volume);
         result.Append(Util.Constants.TEXT_TOSTRING_DELIMITER_END);

         result.Append(Util.Constants.TEXT_TOSTRING_END);

         return result.ToString();
      }

      public override bool Equals(object obj)
      {
         if (obj == null || GetType() != obj.GetType())
            return false;

         Sequence o = (Sequence)obj;

         return text == o.text &&
                voices == o.voices &&
                //Source == o.Source &&
                System.Math.Abs(Rate - o.Rate) < Util.Constants.FLOAT_TOLERANCE &&
                System.Math.Abs(Pitch - o.Pitch) < Util.Constants.FLOAT_TOLERANCE &&
                System.Math.Abs(Volume - o.Volume) < Util.Constants.FLOAT_TOLERANCE;
      }

      public override int GetHashCode()
      {
         int hash = 0;

         if (text != null)
            hash += text.GetHashCode();
         if (voices != null)
            hash += voices.GetHashCode();
         hash += (int)(rate * 100) * 17;
         hash += (int)(pitch * 100) * 17;
         hash += (int)(volume * 100) * 17;

         return hash;
      }

      #endregion
   }
}
// © 2016-2021 crosstales LLC (https://www.crosstales.com)
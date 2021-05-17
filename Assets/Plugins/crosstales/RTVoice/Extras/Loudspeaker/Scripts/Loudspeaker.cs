using UnityEngine;

namespace Crosstales.RTVoice.Tool
{
   /// <summary>Loudspeaker for an AudioSource.</summary>
   [RequireComponent(typeof(AudioSource))]
   [HelpURL("https://www.crosstales.com/media/data/assets/rtvoice/api/class_crosstales_1_1_r_t_voice_1_1_tool_1_1_loudspeaker.html")]
   public class Loudspeaker : MonoBehaviour
   {
      #region Variables

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("Source")] [Tooltip("Origin AudioSource."), SerializeField] private AudioSource source;

      [UnityEngine.Serialization.FormerlySerializedAsAttribute("SilenceSource")] [Tooltip("Silence the origin (default: true)."), SerializeField]
      private bool silenceSource = true;

      private AudioSource audioSource;

      private bool stopped = true;

      #endregion


      #region Properties

      /// <summary>Origin AudioSource.</summary>
      public AudioSource Source
      {
         get => source;
         set => source = value;
      }

      /// <summary>Silence the origin.</summary>
      public bool SilenceSource
      {
         get => silenceSource;
         set => silenceSource = value;
      }

      #endregion


      #region MonoBehaviour methods

      private void Awake()
      {
         audioSource = GetComponent<AudioSource>();
         audioSource.playOnAwake = false;
         audioSource.loop = false;
         audioSource.Stop(); //always stop the AudioSource at startup
      }

      private void Start()
      {
         if (source == null)
            Debug.LogWarning("No 'Source' added to the Loudspeaker!", this);
      }

      private void Update()
      {
         if (Util.Helper.hasActiveClip(source))
         {
            if (stopped)
            {
               audioSource.loop = source.loop;
               audioSource.clip = source.clip;

               audioSource.Play();

               stopped = false;

               if (silenceSource)
                  source.volume = 0f;
            }
         }
         else
         {
            if (!stopped)
            {
               audioSource.Stop();
               audioSource.clip = null;
               stopped = true;
            }
         }
      }

      public void OnDisable()
      {
         audioSource.Stop();
         audioSource.clip = null;
         stopped = true;
      }

      #endregion
   }
}
// © 2016-2021 crosstales LLC (https://www.crosstales.com)
using UnityEngine;
using System.Collections;
using System.Linq;

namespace Crosstales.RTVoice.Tool
{
   /// <summary>Simple sequencer for dialogues.</summary>
   [HelpURL("https://www.crosstales.com/media/data/assets/rtvoice/api/class_crosstales_1_1_r_t_voice_1_1_tool_1_1_sequencer.html")]
   public class Sequencer : MonoBehaviour
   {
      #region Variables

      /// <summary>All available sequences.</summary>
      [Header("Configuration")]
      [Tooltip("All available sequences.")] public Model.Sequence[] Sequences;

      /// <summary>Delay in seconds before the Sequencer starts processing (default: 0).</summary>
      [Tooltip("Delay in seconds before the Sequencer starts processing (default: 0).")] public float Delay;


      /// <summary>Enable the Sequencer on start (default: false).</summary>
      [Header("Behaviour Settings")] [Tooltip("Enable the Sequencer on start (default: false).")] public bool PlayOnStart;

      private int currentIndex;

      private string uidCurrentSpeaker;

      private bool playAllSequences;

      private bool played;

      #endregion


      #region Properties

      /// <summary>Returns the current Sequence.</summary>
      /// <returns>The current Sequence.</returns>
      public Model.Sequence CurrentSequence => Sequences[currentIndex];

      #endregion


      #region MonoBehaviour methods

      private void Start()
      {
         Speaker.Instance.OnVoicesReady += onVoicesReady;
         Speaker.Instance.OnSpeakComplete += speakCompleteMethod;

         play();
      }

      private void OnDestroy()
      {
         if (Speaker.Instance != null)
         {
            Speaker.Instance.OnSpeakComplete -= speakCompleteMethod;
            Speaker.Instance.OnVoicesReady -= onVoicesReady;
         }
      }

      private void OnValidate()
      {
         if (Delay < 0f)
            Delay = 0f;

         foreach (Model.Sequence seq in Sequences.Where(seq => !seq.Initialized))
         {
            seq.Rate = 1f;
            seq.Pitch = 1f;
            seq.Volume = 1f;

            seq.Initialized = true;
         }
      }

      #endregion


      #region Public methods

      /// <summary>Plays a Sequence with a given index.</summary>
      /// <param name="index">Index of the Sequence (default: 0, optional).</param>
      public void PlaySequence(int index = 0)
      {
         if (Sequences != null)
         {
            if (index >= 0 && index < Sequences.Length)
            {
               StartCoroutine(playMe(Sequences[index]));

               currentIndex = index + 1;
            }
            else
            {
               Debug.LogWarning("The given index is outside the range of Sequences: " + index, this);
            }
         }
         else
         {
            Debug.LogWarning("Sequences is null!", this);
         }
      }

      /// <summary>Plays the next Sequence in the array.</summary>
      public void PlayNextSequence()
      {
         PlaySequence(currentIndex);
      }

      /// <summary>Plays all Sequences.</summary>
      public void PlayAllSequences()
      {
         StopAllSequences();

         playAllSequences = true;

         PlaySequence();
      }

      /// <summary>Stops and silences all active Sequences.</summary>
      public void StopAllSequences()
      {
         StopAllCoroutines();
         Speaker.Instance.Silence();
         playAllSequences = false;
      }

      #endregion


      #region Callback methods

      private void speakCompleteMethod(Model.Wrapper wrapper)
      {
         if (playAllSequences)
         {
            if (wrapper.Uid.Equals(uidCurrentSpeaker) && currentIndex < Sequences.Length)
            {
               PlayNextSequence();
            }
            else
            {
               StopAllSequences();
            }
         }
      }

      private void onVoicesReady()
      {
         play();
      }

      #endregion


      #region Private methods

      private void play()
      {
         if (PlayOnStart && !played && Speaker.Instance.Voices.Count > 0)
         {
            played = true;

            PlayAllSequences();
         }
      }

      private IEnumerator playMe(Model.Sequence seq)
      {
         yield return new WaitForSeconds(Delay);

         uidCurrentSpeaker = seq.Mode == Model.Enum.SpeakMode.Speak
            ? Speaker.Instance.Speak(seq.Text, seq.Source, seq.Voices.Voice, true, seq.Rate, seq.Pitch, seq.Volume)
            : Speaker.Instance.SpeakNative(seq.Text, seq.Voices.Voice, seq.Rate, seq.Pitch, seq.Volume);
      }

      #endregion
   }
}
// © 2016-2021 crosstales LLC (https://www.crosstales.com)
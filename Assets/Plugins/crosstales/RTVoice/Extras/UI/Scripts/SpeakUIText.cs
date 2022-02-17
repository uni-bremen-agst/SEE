using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Crosstales.RTVoice.UI
{
   /// <summary>Speaks a Text.</summary>
   [HelpURL("https://crosstales.com/media/data/assets/rtvoice/api/class_crosstales_1_1_r_t_voice_1_1_u_i_1_1_speak_u_i_text.html")]
   public class SpeakUIText : SpeakUIBase
   {
      #region Variables

      public bool ChangeColor = true;
      public Color TextColor = Color.green;
      public bool ClearTags = true;

      public Text TextComponent;

      private Color originalColor;

      #endregion


      #region MonoBehaviour methods

      private void Awake()
      {
         if (TextComponent == null)
            TextComponent = GetComponent<Text>();

         originalColor = TextComponent.color;
      }

      private void Update()
      {
         if (isInside)
         {
            elapsedTime += Time.deltaTime;

            if (elapsedTime > Delay && uid == null && (!SpeakOnlyOnce || !spoken))
            {
               if (ChangeColor)
                  TextComponent.color = TextColor;

               uid = speak(ClearTags ? TextComponent.text.CTClearTags() : TextComponent.text);
               elapsedTime = 0f;
            }
         }
         else
         {
            elapsedTime = 0f;
         }
      }

      #endregion


      #region Overridden methods

      public override void OnPointerExit(PointerEventData eventData)
      {
         base.OnPointerExit(eventData);

         TextComponent.color = originalColor;
      }

      protected override void onSpeakComplete(Model.Wrapper wrapper)
      {
         if (wrapper.Uid == uid)
         {
            base.onSpeakComplete(wrapper);

            TextComponent.color = originalColor;
         }
      }

      #endregion
   }
}
// © 2021 crosstales LLC (https://www.crosstales.com)
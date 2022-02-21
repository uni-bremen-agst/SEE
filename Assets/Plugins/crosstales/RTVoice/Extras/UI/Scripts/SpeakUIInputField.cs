using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Crosstales.RTVoice.UI
{
   /// <summary>Speaks an InputField.</summary>
   [HelpURL("https://crosstales.com/media/data/assets/rtvoice/api/class_crosstales_1_1_r_t_voice_1_1_u_i_1_1_speak_u_i_input_field.html")]
   public class SpeakUIInputField : SpeakUIBase
   {
      #region Variables

      public bool ChangeColor = true;
      public Color TextColor = Color.green;
      public bool ClearTags = true;

      public InputField InputComponent;

      private Color originalColor;
      private Color originalPHColor;

      #endregion


      #region MonoBehaviour methods

      private void Awake()
      {
         InputComponent = GetComponent<InputField>();
         originalColor = InputComponent.textComponent.color;
         originalPHColor = InputComponent.placeholder.color;
      }

      private void Update()
      {
         if (isInside)
         {
            elapsedTime += Time.deltaTime;

            if (elapsedTime > Delay && uid == null && (!SpeakOnlyOnce || !spoken))
            {
               string text;
               if (!string.IsNullOrEmpty(InputComponent.textComponent.text))
               {
                  if (ChangeColor)
                     InputComponent.textComponent.color = TextColor;
                  text = InputComponent.textComponent.text;
               }
               else
               {
                  if (ChangeColor)
                     InputComponent.placeholder.color = TextColor;
                  text = InputComponent.placeholder.GetComponent<Text>().text;
               }

               uid = speak(ClearTags ? text.CTClearTags() : text);
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

         InputComponent.textComponent.color = originalColor;
         InputComponent.placeholder.color = originalPHColor;
      }

      protected override void onSpeakComplete(Model.Wrapper wrapper)
      {
         if (wrapper.Uid == uid)
         {
            base.onSpeakComplete(wrapper);

            InputComponent.textComponent.color = originalColor;
            InputComponent.placeholder.color = originalPHColor;
         }
      }

      #endregion
   }
}
// © 2021 crosstales LLC (https://www.crosstales.com)
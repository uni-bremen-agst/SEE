using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Crosstales.RTVoice.UI
{
   /// <summary>Speaks an InputField.</summary>
   [RequireComponent(typeof(InputField))]
   [HelpURL("https://crosstales.com/media/data/assets/rtvoice/api/class_crosstales_1_1_r_t_voice_1_1_u_i_1_1_speak_u_i_input_field.html")]
   public class SpeakUIInputField : SpeakUIBase
   {
      #region Variables

      public bool ChangeColor = true;
      public Color TextColor = Color.green;
      public bool ClearTags = true;

      protected InputField inputComponent;
      private Color originalColor;
      private Color originalPHColor;

      #endregion


      #region MonoBehaviour methods

      private void Awake()
      {
         inputComponent = GetComponent<InputField>();
         originalColor = inputComponent.textComponent.color;
         originalPHColor = inputComponent.placeholder.color;
      }

      private void Update()
      {
         if (isInside)
         {
            elapsedTime += Time.deltaTime;

            if (elapsedTime > Delay && uid == null && (!SpeakOnlyOnce || !spoken))
            {
               string text;
               if (!string.IsNullOrEmpty(inputComponent.textComponent.text))
               {
                  if (ChangeColor)
                     inputComponent.textComponent.color = TextColor;
                  text = inputComponent.textComponent.text;
               }
               else
               {
                  if (ChangeColor)
                     inputComponent.placeholder.color = TextColor;
                  text = inputComponent.placeholder.GetComponent<Text>().text;
               }

               uid = speak(ClearTags ? Util.Helper.ClearTags(text) : text);
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

         inputComponent.textComponent.color = originalColor;
         inputComponent.placeholder.color = originalPHColor;
      }

      protected override void onSpeakComplete(Model.Wrapper wrapper)
      {
         if (wrapper.Uid == uid)
         {
            base.onSpeakComplete(wrapper);

            inputComponent.textComponent.color = originalColor;
            inputComponent.placeholder.color = originalPHColor;
         }
      }

      #endregion
   }
}
// © 2021 crosstales LLC (https://www.crosstales.com)
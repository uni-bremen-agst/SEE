using UnityEngine;
using UnityEngine.EventSystems;

namespace Crosstales.RTVoice.UI
{
   /// <summary>Speaks a TextMesh Pro input field.</summary>
   [HelpURL("https://crosstales.com/media/data/assets/rtvoice/api/class_crosstales_1_1_r_t_voice_1_1_u_i_1_1_speak_u_i_t_m_p_input_field.html")]
   public class SpeakUITMPInputField : SpeakUIBase
   {
      //#region Variables

      public bool ChangeColor = true;
      public Color TextColor = Color.green;
      public bool ClearTags = true;

#if false || CT_DEVELOP //Change this to "true" is you have TextMesh Pro installed
      public TMPro.TMP_InputField InputComponent;

      private Color originalColor;
      private Color originalPHColor;

      //#endregion


      #region MonoBehaviour methods

      private void Awake()
      {
         if (InputComponent == null)
            InputComponent = GetComponent<TMPro.TMP_InputField>();

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
               string text = InputComponent.textComponent.text;
               if (!string.IsNullOrEmpty(text) && text.Length > 1)
               {
                  if (ChangeColor)
                     InputComponent.textComponent.color = TextColor;
               }
               else
               {
                  if (ChangeColor)
                     InputComponent.placeholder.color = TextColor;

                  text = InputComponent.placeholder.GetComponent<TMPro.TMP_Text>().text;
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
#else
      private void Awake()
      {
         Debug.LogWarning("Is TextMesh Pro installed? If so, please change line 16 of 'SpeakUITMPInputField.cs' to 'true'");
      }

#if UNITY_EDITOR
      [UnityEditor.CustomEditor(typeof(SpeakUITMPInputField))]
      public class CTHelperEditor : UnityEditor.Editor
      {
         public override void OnInspectorGUI()
         {
            UnityEditor.EditorGUILayout.HelpBox("Is TextMesh Pro installed? If so, please change line 16 of 'SpeakUITMPInputField.cs' to 'true'.", UnityEditor.MessageType.Warning);

            DrawDefaultInspector();
         }
      }
#endif
#endif
   }
}
// © 2021 crosstales LLC (https://www.crosstales.com)
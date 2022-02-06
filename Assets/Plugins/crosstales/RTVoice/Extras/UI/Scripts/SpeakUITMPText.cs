using UnityEngine;
using UnityEngine.EventSystems;

namespace Crosstales.RTVoice.UI
{
   /// <summary>Speaks a TextMesh Pro text.</summary>
   [HelpURL("https://crosstales.com/media/data/assets/rtvoice/api/class_crosstales_1_1_r_t_voice_1_1_u_i_1_1_speak_u_i_t_m_p_text.html")]
   public class SpeakUITMPText : SpeakUIBase
   {
      //#region Variables

      public bool ChangeColor = true;
      public Color TextColor = Color.green;
      public bool ClearTags = true;

#if false || CT_DEVELOP //Change this to "true" is you have TextMesh Pro installed
      public TMPro.TextMeshPro TextComponent;

      private Color originalColor;

      //#endregion


      #region MonoBehaviour methods

      private void Awake()
      {
         if (TextComponent == null)
            TextComponent = GetComponent<TMPro.TextMeshPro>();

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
#else
      private void Awake()
      {
         Debug.LogWarning("Is TextMesh Pro installed? If so, please change line 16 of 'SpeakUITMPText.cs' to 'true'");
      }

#if UNITY_EDITOR
      [UnityEditor.CustomEditor(typeof(SpeakUITMPText))]
      public class CTHelperEditor : UnityEditor.Editor
      {
         public override void OnInspectorGUI()
         {
            UnityEditor.EditorGUILayout.HelpBox("Is TextMesh Pro installed? If so, please change line 16 of 'SpeakUITMPText.cs' to 'true'.", UnityEditor.MessageType.Warning);

            DrawDefaultInspector();
         }
      }
#endif
#endif
   }
}
// © 2021 crosstales LLC (https://www.crosstales.com)
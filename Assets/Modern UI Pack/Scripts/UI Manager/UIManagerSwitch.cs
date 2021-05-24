using UnityEngine;
using UnityEngine.UI;

namespace Michsky.UI.ModernUIPack
{
    [ExecuteInEditMode]
    public class UIManagerSwitch : MonoBehaviour
    {
        [Header("Settings")]
        public UIManager UIManagerAsset;
        public bool webglMode = false;

        [Header("Resources")]
        public Image border;
        public Image background;
        public Image handleOn;
        public Image handleOff;

        void Awake()
        {
            if (Application.isPlaying && webglMode == true)
                return;

            try
            {
                if (UIManagerAsset == null)
                    UIManagerAsset = Resources.Load<UIManager>("MUIP Manager");

                this.enabled = true;

                if (UIManagerAsset.enableDynamicUpdate == false)
                {
                    UpdateSwitch();
                    this.enabled = false;
                }
            }

            catch { Debug.Log("<b>[Modern UI Pack]</b> No UI Manager found, assign it manually.", this); }
        }

        void LateUpdate()
        {
            if (UIManagerAsset == null)
                return;

            if (UIManagerAsset.enableDynamicUpdate == true)
                UpdateSwitch();
        }

        void UpdateSwitch()
        {
            if (Application.isPlaying && webglMode == true)
                return;

            try
            {
                border.color = new Color(UIManagerAsset.switchBorderColor.r, UIManagerAsset.switchBorderColor.g, UIManagerAsset.switchBorderColor.b, border.color.a);
                background.color = new Color(UIManagerAsset.switchBackgroundColor.r, UIManagerAsset.switchBackgroundColor.g, UIManagerAsset.switchBackgroundColor.b, background.color.a);
                handleOn.color = new Color(UIManagerAsset.switchHandleOnColor.r, UIManagerAsset.switchHandleOnColor.g, UIManagerAsset.switchHandleOnColor.b, handleOn.color.a);
                handleOff.color = new Color(UIManagerAsset.switchHandleOffColor.r, UIManagerAsset.switchHandleOffColor.g, UIManagerAsset.switchHandleOffColor.b, handleOff.color.a);
            }

            catch { }
        }
    }
}
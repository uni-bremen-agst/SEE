using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Michsky.UI.ModernUIPack
{
    [ExecuteInEditMode]
    public class UIManagerToggle : MonoBehaviour
    {
        [Header("Settings")]
        public UIManager UIManagerAsset;
        public bool webglMode = false;

        [Header("Resources")]
        public Image border;
        public Image background;
        public Image check;
        public TextMeshProUGUI onLabel;
        public TextMeshProUGUI offLabel;

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
                    UpdateToggle();
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
                UpdateToggle();
        }

        void UpdateToggle()
        {
            if (Application.isPlaying && webglMode == true)
                return;

            try
            {
                border.color = UIManagerAsset.toggleBorderColor;
                background.color = UIManagerAsset.toggleBackgroundColor;
                check.color = UIManagerAsset.toggleCheckColor;
                onLabel.color = new Color(UIManagerAsset.toggleTextColor.r, UIManagerAsset.toggleTextColor.g, UIManagerAsset.toggleTextColor.b, onLabel.color.a);
                onLabel.font = UIManagerAsset.toggleFont;
                onLabel.fontSize = UIManagerAsset.toggleFontSize;
                offLabel.color = new Color(UIManagerAsset.toggleTextColor.r, UIManagerAsset.toggleTextColor.g, UIManagerAsset.toggleTextColor.b, offLabel.color.a);
                offLabel.font = UIManagerAsset.toggleFont;
                offLabel.fontSize = UIManagerAsset.toggleFontSize;
            }

            catch { }
        }
    }
}
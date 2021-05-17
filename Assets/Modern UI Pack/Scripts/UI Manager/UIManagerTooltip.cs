using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Michsky.UI.ModernUIPack
{
    [ExecuteInEditMode]
    public class UIManagerTooltip : MonoBehaviour
    {
        [Header("Settings")]
        public UIManager UIManagerAsset;
        public bool webglMode = false;

        [Header("Resources")]
        public Image background;
        public TextMeshProUGUI text;

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
                    UpdateTooltip();
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
                UpdateTooltip();
        }

        void UpdateTooltip()
        {
            if (Application.isPlaying && webglMode == true)
                return;

            try
            {
                background.color = UIManagerAsset.tooltipBackgroundColor;
                text.color = UIManagerAsset.tooltipTextColor;
                text.font = UIManagerAsset.tooltipFont;
                text.fontSize = UIManagerAsset.tooltipFontSize;
            }

            catch { }
        }
    }
}
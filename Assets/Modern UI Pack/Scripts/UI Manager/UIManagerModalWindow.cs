using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Michsky.UI.ModernUIPack
{
    [ExecuteInEditMode]
    public class UIManagerModalWindow : MonoBehaviour
    {
        [Header("Settings")]
        public UIManager UIManagerAsset;
        public bool webglMode = false;

        [Header("Resources")]
        public Image background;
        public Image contentBackground;
        public Image icon;
        public TextMeshProUGUI title;
        public TextMeshProUGUI description;

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
                    UpdateModalWindow();
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
                UpdateModalWindow();
        }

        void UpdateModalWindow()
        {
            if (Application.isPlaying && webglMode == true)
                return;

            try
            {
                background.color = UIManagerAsset.modalWindowBackgroundColor;
                contentBackground.color = UIManagerAsset.modalWindowContentPanelColor;
                icon.color = UIManagerAsset.modalWindowIconColor;
                title.color = UIManagerAsset.modalWindowTitleColor;
                description.color = UIManagerAsset.modalWindowDescriptionColor;
                title.font = UIManagerAsset.modalWindowTitleFont;
                description.font = UIManagerAsset.modalWindowContentFont;
            }

            catch { }
        }
    }
}
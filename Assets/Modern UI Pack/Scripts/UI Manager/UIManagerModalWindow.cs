using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Michsky.UI.ModernUIPack
{
    [ExecuteInEditMode]
    public class UIManagerModalWindow : MonoBehaviour
    {
        [Header("SETTINGS")]
        public UIManager UIManagerAsset;

        [Header("RESOURCES")]
        public Image background;
        public Image contentBackground;
        public Image icon;
        public TextMeshProUGUI title;
        public TextMeshProUGUI description;

        void Awake()
        {
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

            catch
            {
                Debug.Log("<b>[Modern UI Pack]</b> No UI Manager found, assign it manually.", this);
            }
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
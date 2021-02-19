using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Michsky.UI.ModernUIPack
{
    [ExecuteInEditMode]
    public class UIManagerProgressBar : MonoBehaviour
    {
        [Header("SETTINGS")]
        public UIManager UIManagerAsset;

        [Header("RESOURCES")]
        public Image bar;
        public Image background;
        public TextMeshProUGUI label;

        bool dynamicUpdateEnabled;

        void Awake()
        {
            try
            {
                if (UIManagerAsset == null)
                    UIManagerAsset = Resources.Load<UIManager>("MUIP Manager");

                this.enabled = true;

                if (UIManagerAsset.enableDynamicUpdate == false)
                {
                    UpdateProgressBar();
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
                UpdateProgressBar();
        }

        void UpdateProgressBar()
        {
            try
            {
                bar.color = UIManagerAsset.progressBarColor;
                background.color = UIManagerAsset.progressBarBackgroundColor;
                label.color = UIManagerAsset.progressBarLabelColor;
                label.font = UIManagerAsset.progressBarLabelFont;
                label.fontSize = UIManagerAsset.progressBarLabelFontSize;
            }

            catch { }
        }
    }
}
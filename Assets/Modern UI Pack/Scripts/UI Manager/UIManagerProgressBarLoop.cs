using UnityEngine;
using UnityEngine.UI;

namespace Michsky.UI.ModernUIPack
{
    [ExecuteInEditMode]
    public class UIManagerProgressBarLoop : MonoBehaviour
    {
        [Header("SETTINGS")]
        public UIManager UIManagerAsset;
        public bool hasBackground;
        public bool useRegularBackground;

        [Header("RESOURCES")]
        public Image bar;
        [HideInInspector] public Image background;

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

                if (hasBackground == true)
                {
                    if (useRegularBackground == true)
                        background.color = UIManagerAsset.progressBarBackgroundColor;
                    else
                        background.color = UIManagerAsset.progressBarLoopBackgroundColor;
                }
            }

            catch { }
        }
    }
}
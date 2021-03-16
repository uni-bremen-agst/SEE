using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Michsky.UI.ModernUIPack
{
    [ExecuteInEditMode]
    public class UIManagerInputField : MonoBehaviour
    {
        [Header("SETTINGS")]
        public UIManager UIManagerAsset;

        [Header("RESOURCES")]
        public List<GameObject> images = new List<GameObject>();
        public List<GameObject> texts = new List<GameObject>();

        void Awake()
        {
            try
            {
                if (UIManagerAsset == null)
                    UIManagerAsset = Resources.Load<UIManager>("MUIP Manager");

                this.enabled = true;

                if (UIManagerAsset.enableDynamicUpdate == false)
                {
                    UpdateInputField();
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
                UpdateInputField();
        }

        void UpdateInputField()
        {
            for (int i = 0; i < images.Count; ++i)
            {
                Image currentImage = images[i].GetComponent<Image>();
                currentImage.color = new Color(UIManagerAsset.inputFieldColor.r, UIManagerAsset.inputFieldColor.g, UIManagerAsset.inputFieldColor.b, currentImage.color.a);
            }

            for (int i = 0; i < texts.Count; ++i)
            {
                TextMeshProUGUI currentText = texts[i].GetComponent<TextMeshProUGUI>();
                currentText.color = new Color(UIManagerAsset.inputFieldColor.r, UIManagerAsset.inputFieldColor.g, UIManagerAsset.inputFieldColor.b, currentText.color.a);
                currentText.font = UIManagerAsset.inputFieldFont;
                currentText.fontSize = UIManagerAsset.inputFieldFontSize;
            }
        }
    }
}
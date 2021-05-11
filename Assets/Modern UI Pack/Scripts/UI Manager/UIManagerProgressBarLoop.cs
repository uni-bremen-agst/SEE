﻿using UnityEngine;
using UnityEngine.UI;

namespace Michsky.UI.ModernUIPack
{
    [ExecuteInEditMode]
    public class UIManagerProgressBarLoop : MonoBehaviour
    {
        [Header("Settings")]
        public UIManager UIManagerAsset;
        public bool hasBackground;
        public bool useRegularBackground;
        public bool webglMode = false;

        [Header("Resources")]
        public Image bar;
        [HideInInspector] public Image background;

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
                    UpdateProgressBar();
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
                UpdateProgressBar();
        }

        void UpdateProgressBar()
        {
            if (Application.isPlaying && webglMode == true)
                return;

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
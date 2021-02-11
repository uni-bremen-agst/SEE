﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

namespace Michsky.UI.ModernUIPack
{
    public class NotificationManager : MonoBehaviour
    {
        // CONTENT
        public Sprite icon;
        public string title = "Notification Title";
        [TextArea] public string description = "Notification description";

        // Resources
        public Animator notificationAnimator;
        public Image iconObj;
        public TextMeshProUGUI titleObj;
        public TextMeshProUGUI descriptionObj;

        // Settings
        public bool enableTimer = true;
        public float timer = 3f;
        public bool useCustomContent = false;
        public bool useStacking = false;
        public bool destroyAfterPlaying = false;
        public NotificationStyle notificationStyle;

        public enum NotificationStyle
        {
            FADING,
            POPUP,
            SLIDING
        }

        void Start()
        {
            try
            {
                if (notificationAnimator == null)
                    notificationAnimator = gameObject.GetComponent<Animator>();

                if (useCustomContent == false)
                {
                    iconObj.sprite = icon;
                    titleObj.text = title;
                    descriptionObj.text = description;
                }
            }

            catch
            {
                Debug.LogError("Notification - Cannot initalize the object due to missing components.", this);
            }

            if (useStacking == true)
            {
                try
                {
                    NotificationStacking stacking = transform.GetComponentInParent<NotificationStacking>();
                    stacking.notifications.Add(this);
                    stacking.enableUpdating = true;
                    gameObject.SetActive(false);
                }

                catch { }
            }
        }

        IEnumerator StartTimer()
        {
            yield return new WaitForSeconds(timer);
            CloseNotification();
        }

        IEnumerator DestroyNotification()
        {
            yield return new WaitForSeconds(1f);
            Destroy(gameObject);
        }

        public void OpenNotification()
        {
            notificationAnimator.Play("In");

            if (enableTimer == true)
                StartCoroutine("StartTimer");
        }

        public void CloseNotification()
        {
            notificationAnimator.Play("Out");

            if (destroyAfterPlaying == true)
                StartCoroutine("DestroyNotification");
        }

        public void UpdateUI()
        {
            try
            {
                iconObj.sprite = icon;
                titleObj.text = title;
                descriptionObj.text = description;
            }

            catch
            {
                Debug.LogError("Notification - Cannot update the object due to missing components.", this);
            }
        }
    }
}
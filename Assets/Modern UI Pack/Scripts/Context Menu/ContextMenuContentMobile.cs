using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;

namespace Michsky.UI.ModernUIPack
{
    [AddComponentMenu("Modern UI Pack/Context Menu/Context Menu Content (Mobile)")]
    public class ContextMenuContentMobile : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Resources")]
        public ContextMenuManager contextManager;
        public Transform itemParent;

        [Header("Settings")]
        [Range(0.1f, 6)] public float holdToOpen = 0.75f;

        [Header("Items")]
        public List<ContextItem> contexItems = new List<ContextItem>();

        Animator contextAnimator;
        GameObject selectedItem;
        Image setItemImage;
        TextMeshProUGUI setItemText;
        Sprite imageHelper;
        string textHelper;
        float timer;
        bool timerEnabled;

        [System.Serializable]
        public class ContextItem
        {
            public string itemText = "Item Text";
            public Sprite itemIcon;
            public ContextItemType contextItemType;
            public UnityEvent onClickEvents;
        }

        public enum ContextItemType
        {
            BUTTON
            // SUB_MENU
        }

        void Start()
        {
            if (contextManager == null)
            {
                try
                {
                    contextManager = GameObject.Find("Context Menu").GetComponent<ContextMenuManager>();
                    itemParent = contextManager.transform.Find("Content/Item List").transform;
                }

                catch { Debug.Log("<b>[Context Menu]</b> No variable attached to Context Manager.", this); return; }
            }

            contextAnimator = contextManager.contextAnimator;

            foreach (Transform child in itemParent)
                Destroy(child.gameObject);
        }

        void Update()
        {
            if (timerEnabled == true)
            {
                timer += Time.deltaTime;

                if (timer >= holdToOpen)
                {
                    CheckForTimer();
                    timerEnabled = false;
                    timer = 0;
                }
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            timerEnabled = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            timerEnabled = false;
            timer = 0;
        }

        public void CheckForTimer()
        {
            if (timer <= holdToOpen)
                return;

            if (contextManager.isContextMenuOn == true)
            {
                contextAnimator.Play("Menu Out");
                contextManager.isContextMenuOn = false;
            }

            else if (contextManager.isContextMenuOn == false)
            {
                foreach (Transform child in itemParent)
                    Destroy(child.gameObject);

                for (int i = 0; i < contexItems.Count; ++i)
                {
                    if (contexItems[i].contextItemType == ContextItemType.BUTTON)
                        selectedItem = contextManager.contextButton;

                    GameObject go = Instantiate(selectedItem, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
                    go.transform.SetParent(itemParent, false);

                    setItemText = go.GetComponentInChildren<TextMeshProUGUI>();
                    textHelper = contexItems[i].itemText;
                    setItemText.text = textHelper;

                    Transform goImage;
                    goImage = go.gameObject.transform.Find("Icon");
                    setItemImage = goImage.GetComponent<Image>();
                    imageHelper = contexItems[i].itemIcon;
                    setItemImage.sprite = imageHelper;

                    Button itemButton;
                    itemButton = go.GetComponent<Button>();
                    itemButton.onClick.AddListener(contexItems[i].onClickEvents.Invoke);
                    itemButton.onClick.AddListener(CloseOnClick);
                    StartCoroutine(ExecuteAfterTime(0.01f));
                }

                contextManager.SetContextMenuPosition();
                contextAnimator.Play("Menu In");
                contextManager.isContextMenuOn = true;
                contextManager.SetContextMenuPosition();
            }
        }

        IEnumerator ExecuteAfterTime(float time)
        {
            yield return new WaitForSeconds(time);
            itemParent.gameObject.SetActive(false);
            itemParent.gameObject.SetActive(true);
            StopCoroutine(ExecuteAfterTime(0.01f));
            StopCoroutine("ExecuteAfterTime");
        }

        public void CloseOnClick()
        {
            contextAnimator.Play("Menu Out");
            contextManager.isContextMenuOn = false;
        }
    }
}
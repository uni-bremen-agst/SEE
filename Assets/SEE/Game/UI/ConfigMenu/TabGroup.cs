using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Game.UI.ConfigMenu
{
    public delegate void UpdateNotifier();

    public class TabGroup : MonoBehaviour
    {
        public List<TabButton> tabButtons = new List<TabButton>();
        public TabController tabController;

        private readonly List<UpdateNotifier> _updateSubscriber = new List<UpdateNotifier>();
        private TabButton _activeButton;

        void Start()
        {
            tabController = GetComponentInParent<TabController>();
            if (!tabController)
            {
                Debug.LogError("TabGroup needs to be inside a TabController.");
            }
        }

        public void Subscribe(TabButton button)
        {
            tabButtons.Add(button);
        }

        public void SubscribeToUpdates(UpdateNotifier notifier)
        {
            _updateSubscriber.Add(notifier);
        }

        public void OnTabSelected(TabButton button)
        {
            _activeButton = button;
            button.SetActive();
            ResetButtons();
            tabController.OnIndexUpdate(button.transform.GetSiblingIndex());
            foreach (var updateNotifier in _updateSubscriber)
            {
                updateNotifier();
            }
        }

        void ResetButtons()
        {
            foreach (var tabButton in tabButtons)
            {
                if (tabButton == _activeButton)
                {
                    continue;
                }

                tabButton.ResetStyles();
            }
        }
    }
}

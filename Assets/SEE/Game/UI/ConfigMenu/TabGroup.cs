using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.UI.ConfigMenu
{
    public delegate void UpdateNotifier();

    /// <summary>
    /// Controls all tab button in the sidebar.
    /// Tab buttons register themselves to this group.
    /// Other components can subscribe to listen for button clicks.
    /// </summary>
    public class TabGroup : MonoBehaviour
    {
        /// <summary>
        /// The list of all currently registered tab buttons.
        /// </summary>
        public List<TabButton> tabButtons = new List<TabButton>();

        /// <summary>
        /// The controller of all pages.
        /// </summary>
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

        /// <summary>
        /// Adds a new tab button the list of registered tab buttons.
        /// </summary>
        /// <param name="button">The buttons that wants to be registered.</param>
        public void Subscribe(TabButton button)
        {
            tabButtons.Add(button);
        }

        /// <summary>
        /// Subscribes the given component to updates of the button states.
        /// </summary>
        /// <param name="notifier">The component that wants to notified about updates.</param>
        public void SubscribeToUpdates(UpdateNotifier notifier)
        {
            _updateSubscriber.Add(notifier);
        }

        /// <summary>
        /// Sets a tab button as the currently active.
        /// </summary>
        /// <param name="button"></param>
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

        /// <summary>
        /// Rests all inactive buttons.
        /// </summary>
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

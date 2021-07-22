// Copyright 2021 Ruben Smidt
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
// IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

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
        public List<TabButton> TabButtons = new List<TabButton>();

        /// <summary>
        /// The controller of all pages.
        /// </summary>
        public TabController TabController;

        private readonly List<UpdateNotifier> updateSubscriber = new List<UpdateNotifier>();
        private TabButton activeButton;
        private void Start()
        {
            TabController = GetComponentInParent<TabController>();
            if (!TabController)
            {
                Debug.LogError("TabGroup needs to be inside a TabController.\n");
            }
        }

        /// <summary>
        /// Adds a new tab button the list of registered tab buttons.
        /// </summary>
        /// <param name="button">The buttons that wants to be registered.</param>
        public void Subscribe(TabButton button)
        {
            TabButtons.Add(button);
        }

        /// <summary>
        /// Subscribes the given component to updates of the button states.
        /// </summary>
        /// <param name="notifier">The component that wants to notified about updates.</param>
        public void SubscribeToUpdates(UpdateNotifier notifier)
        {
            updateSubscriber.Add(notifier);
        }

        /// <summary>
        /// Sets a tab button as the currently active.
        /// </summary>
        /// <param name="button"></param>
        public void OnTabSelected(TabButton button)
        {
            activeButton = button;
            button.SetActive();
            ResetButtons();
            TabController.OnIndexUpdate(button.transform.GetSiblingIndex());
            foreach (UpdateNotifier updateNotifier in updateSubscriber)
            {
                updateNotifier();
            }
        }

        /// <summary>
        /// Rests all inactive buttons.
        /// </summary>
        private void ResetButtons()
        {
            foreach (TabButton tabButton in TabButtons)
            {
                if (tabButton == activeButton)
                {
                    continue;
                }
                tabButton.ResetStyles();
            }
        }
    }
}

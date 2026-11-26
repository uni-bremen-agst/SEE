using System;
using System.Collections.Generic;
using System.Linq;
using DynamicPanels;
using SEE.Extensions;
using SEE.Utils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.UI;
using CollectionExtensions = SEE.Utils.CollectionExtensions;

namespace SEE.UI.Window
{
    /// <summary>
    /// Implements the desktop and VR presentation of a <see cref="WindowSpace"/>.
    /// Manages DynamicPanels panels and tabs, synchronizes their contents with
    /// the registered windows, and handles panel lifecycle events.
    /// </summary>
    public partial class WindowSpace
    {
        /// <summary>
        /// The panels containing the windows.
        /// </summary>
        private readonly Dictionary<Panel, List<BaseWindow>> panels = new();

        /// <summary>
        /// The dynamic canvas of the panel.
        /// This canvas will implement tabs, moving, tiling, and resizing for us.
        /// </summary>
        private DynamicPanelsCanvas panelsCanvas;

        /// <summary>
        /// Whether this window space has subscribed to DynamicPanels events.
        /// </summary>
        private bool panelEventsRegistered;

        /// <summary>
        /// Initializes the desktop window space by finding an existing
        /// window-space GameObject or instantiating its prefab.
        /// Ensures that the window space is active.
        /// </summary>
        protected override void StartDesktop()
        {
            // Add space game object if it doesn't exist yet
            space = Canvas.transform.Find(WindowSpaceName)?.gameObject;
            if (!space)
            {
                space = PrefabInstantiator.InstantiatePrefab(windowSpacePrefab, Canvas.transform, false);
                space.name = WindowSpaceName;
            }
            space.SetActive(true);
        }

        /// <summary>
        /// Configures the shared UI canvas for VR interaction,
        /// attaches it to the XR tablet, and initializes the
        /// window-space GameObject using the desktop setup.
        /// </summary>
        protected override void StartVR()
        {
            Canvas.MustGetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            Canvas.transform.SetParent(GameObject.Find("XRTabletCanvas(Clone)/Screen").transform, false);
            Canvas.AddComponent<TrackedDeviceGraphicRaycaster>();
            RectTransform canvasTransform = Canvas.GetComponent<RectTransform>();
            canvasTransform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
            canvasTransform.sizeDelta = new Vector2(950, 950);
            canvasTransform.localRotation = Quaternion.Euler(0, -90, 0);
            canvasTransform.localPosition = new Vector3(0.9f, 0, 0);
            StartDesktop();
        }

        /// <summary>
        /// Removes tabs with destroyed content from the given panel.
        /// Inactive invalid tabs are removed before the active tab to prevent
        /// DynamicPanels from activating another tab whose content has already
        /// been destroyed.
        /// </summary>
        /// <param name="panel">The panel whose invalid tabs shall be removed.</param>
        private static void RemoveDestroyedTabs(Panel panel)
        {
            if (panel == null)
            {
                return;
            }

            // Remove invalid inactive tabs first. Removing an inactive tab
            // does not require DynamicPanels to activate another tab.
            for (int index = panel.NumberOfTabs - 1; index >= 0; index--)
            {
                if (index == panel.ActiveTab)
                {
                    continue;
                }

                PanelTab tab = panel[index];

                if (tab != null && tab.Content == null)
                {
                    panel.RemoveTab(index);
                }
            }

            if (panel == null || panel.NumberOfTabs == 0)
            {
                return;
            }

            // Removing the active tab may cause DynamicPanels to activate
            // another tab. All remaining inactive tabs now have valid content.
            int activeIndex = panel.ActiveTab;

            if (activeIndex >= 0 && activeIndex < panel.NumberOfTabs)
            {
                PanelTab activeTab = panel[activeIndex];

                if (activeTab != null && activeTab.Content == null)
                {
                    panel.RemoveTab(activeIndex);
                }
            }
        }

        /// <summary>
        /// Activates the tab corresponding to <see cref="ActiveWindow"/>.
        /// Removes tabs whose content has already been destroyed before
        /// activating another tab, because removing such tabs may change
        /// the indices in the panel.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if neither the active window nor the previous active
        /// window belongs to the available windows.
        /// </exception>
        private void UpdateActiveTab()
        {
            if (panels.Count == 0 || ActiveWindow == null || ActiveWindow.Window == null)
            {
                return;
            }

            if (!windows.Contains(ActiveWindow))
            {
                Debug.LogWarning("Active window is not part of available windows. Resetting to previous entry.\n");

                if (ActiveWindow == currentActiveWindow)
                {
                    Debug.LogError("Neither active window, nor previous active window is "
                                   + "part of available windows. This component will now self-destruct.");
                    Destroyer.Destroy(this);
                    throw new InvalidOperationException();
                }

                ActiveWindow = currentActiveWindow;
                UpdateActiveTab();
                return;
            }

            BaseWindow requestedWindow = ActiveWindow;

            if (PanelTabForWindow(requestedWindow) is not ({ } panel, { } tab))
            {
                return;
            }

            if (panel == null)
            {
                return;
            }

            // Remove invalid inactive tabs before the active tab.
            // This prevents DynamicPanels from selecting destroyed content.
            RemoveDestroyedTabs(panel);

            if (panel == null || tab == null || tab.Content == null)
            {
                return;
            }

            // The tab index may have changed during cleanup.
            // Always retrieve it from the current panel state.
            int tabIndex = panel.GetTabIndex(tab);

            if (tabIndex < 0 || tabIndex >= panel.NumberOfTabs)
            {
                return;
            }

            panel.ActiveTab = tabIndex;

            // DynamicPanels may select another tab temporarily while
            // removing an invalid active tab. Preserve the window
            // explicitly requested by WindowSpace.
            if (requestedWindow != null
                && requestedWindow.Window != null
                && windows.Contains(requestedWindow))
            {
                ActiveWindow = requestedWindow;
            }
        }

        /// <summary>
        /// Returns the panel and tab for a given window.
        /// Also supports a destroyed window component if its managed
        /// reference to the UI GameObject is still available.
        /// Destroyed panels are ignored.
        /// </summary>
        /// <param name="window">The window to find the panel and tab for.</param>
        /// <returns>
        /// The panel and tab for the window, or null if no valid tab exists.
        /// </returns>
        private (Panel, PanelTab)? PanelTabForWindow(BaseWindow window)
        {
            if (ReferenceEquals(window, null) || window.Window == null)
            {
                return null;
            }

            RectTransform windowTransform = (RectTransform)window.Window.transform;

            return panels.Keys
                         .Where(panel => panel != null)
                         .Select(panel => (panel, panel.GetTab(windowTransform)))
                         .SingleOrDefault(entry => entry.Item2 != null);
        }

        /// <summary>
        /// Synchronizes the active windows with their DynamicPanels tabs.
        /// Removes obsolete window associations and tabs with destroyed
        /// content before deleting panels that are actually empty.
        /// </summary>
        protected override void UpdateDesktop()
        {
            bool activeWindowChanged = false;

            // Remove references to panels already destroyed by Unity.
            foreach (Panel destroyedPanel in panels.Keys.Where(panel => panel == null).ToList())
            {
                List<BaseWindow> affectedWindows = panels[destroyedPanel].ToList();
                panels.Remove(destroyedPanel);

                foreach (BaseWindow affectedWindow in affectedWindows)
                {
                    // A window moved to another panel must be preserved.
                    if (panels.Values.Any(panelWindows =>
                            panelWindows.Contains(affectedWindow)))
                    {
                        continue;
                    }

                    currentWindows.Remove(affectedWindow);

                    if (ReferenceEquals(currentActiveWindow, affectedWindow))
                    {
                        currentActiveWindow = null;
                    }

                    if (affectedWindow == null || affectedWindow.Window == null)
                    {
                        windows.Remove(affectedWindow);

                        if (ReferenceEquals(ActiveWindow, affectedWindow))
                        {
                            ActiveWindow = null;
                            activeWindowChanged = true;
                        }

                        Destroyer.Destroy(affectedWindow);
                    }
                }
            }

            // Remove destroyed window components.
            windows.RemoveAll(window => window == null);

            // Remove initialized windows whose UI content disappeared.
            // Newly added windows are still allowed to initialize.
            foreach (BaseWindow window in currentWindows
                         .Where(window => window != null && window.Window == null)
                         .ToList())
            {
                windows.Remove(window);
            }

            // Remove obsolete window registrations first.
            foreach (BaseWindow window in currentWindows.Except(windows).ToList())
            {
                if (PanelTabForWindow(window) is ({ } panel, { } tab)
                    && panel != null)
                {
                    panel.RemoveTab(tab);
                }

                foreach (List<BaseWindow> panelWindows in panels.Values)
                {
                    panelWindows.Remove(window);
                }

                currentWindows.Remove(window);

                if (ReferenceEquals(currentActiveWindow, window))
                {
                    currentActiveWindow = null;
                }

                Destroyer.Destroy(window);
            }

            // Remove tabs whose content GameObjects were destroyed.
            // Valid tabs, including those owned by other spaces, remain.
            foreach (Panel panel in panels.Keys.Where(panel => panel != null).ToList())
            {
                RemoveDestroyedTabs(panel);
            }

            // Select another valid window if the active one disappeared.
            if (ActiveWindow == null
                || ActiveWindow.Window == null
                || !windows.Contains(ActiveWindow))
            {
                BaseWindow replacement = windows.FirstOrDefault(
                    window => window != null && window.Window != null);

                if (!ReferenceEquals(ActiveWindow, replacement))
                {
                    ActiveWindow = replacement;
                    activeWindowChanged = true;
                }
            }

            // Delete panels only when both SEE and DynamicPanels
            // confirm that they contain no remaining windows or tabs.
            foreach (Panel emptyPanel in panels
                         .Where(entry => entry.Key != null
                                         && entry.Value.Count == 0
                                         && entry.Key.NumberOfTabs == 0)
                         .Select(entry => entry.Key)
                         .ToList())
            {
                panels.Remove(emptyPanel);

                if (emptyPanel != null)
                {
                    Destroyer.Destroy(emptyPanel.gameObject);
                }
            }

            if (activeWindowChanged)
            {
                OnActiveWindowChanged.Invoke();
            }

            if (panels.Count == 0)
            {
                if (windows.Any(window =>
                        window != null && window.Window != null))
                {
                    InitializePanel();
                }
                else
                {
                    return;
                }
            }

            if (panels.Count == 0)
            {
                return;
            }

            // Add newly initialized windows.
            foreach (BaseWindow window in windows.Except(currentWindows)
                         .Where(window => window != null && window.Window != null)
                         .ToList())
            {
                RectTransform rectTransform = (RectTransform)window.Window.transform;

                Panel targetPanel = panels.Keys.LastOrDefault(panel => panel != null);

                if (targetPanel == null)
                {
                    break;
                }

                panels[targetPanel].Add(window);

                PanelTab tab = targetPanel.AddTab(rectTransform);

                if (tab == null)
                {
                    panels[targetPanel].Remove(window);
                    continue;
                }

                tab.Label = window.Title;
                tab.Icon = null;
                currentWindows.Add(window);

                panelsCanvas.ForceRebuildLayoutImmediate();
                window.RebuildLayout();
            }

            if (currentActiveWindow != ActiveWindow && ActiveWindow)
            {
                UpdateActiveTab();
                currentActiveWindow = ActiveWindow;
            }
        }

        /// <summary>
        /// Synchronizes windows and panels in VR using the same
        /// lifecycle management as the desktop implementation.
        /// </summary>
        protected override void UpdateVR()
        {
            UpdateDesktop();
        }

        /// <summary>
        /// Registers the DynamicPanels event handlers once for this window space.
        /// </summary>
        private void RegisterPanelEvents()
        {
            if (panelEventsRegistered)
            {
                return;
            }

            PanelNotificationCenter.OnPanelCreated += HandleNewPanel;
            PanelNotificationCenter.OnPanelClosed += ClosePanel;
            PanelNotificationCenter.OnStoppedDraggingTab += HandleMovedTab;
            PanelNotificationCenter.OnActiveTabChanged += ChangeActiveTab;

            if (CanClose)
            {
                PanelNotificationCenter.OnTabClosed += CloseTab;
            }

            panelEventsRegistered = true;
        }

        /// <summary>
        /// Unregisters all DynamicPanels event handlers owned by this window space.
        /// </summary>
        private void UnregisterPanelEvents()
        {
            if (!panelEventsRegistered)
            {
                return;
            }

            PanelNotificationCenter.OnPanelCreated -= HandleNewPanel;
            PanelNotificationCenter.OnPanelClosed -= ClosePanel;
            PanelNotificationCenter.OnStoppedDraggingTab -= HandleMovedTab;
            PanelNotificationCenter.OnActiveTabChanged -= ChangeActiveTab;

            if (CanClose)
            {
                PanelNotificationCenter.OnTabClosed -= CloseTab;
            }

            panelEventsRegistered = false;
        }

        /// <summary>
        /// Unregisters the DynamicPanels event handlers before the window
        /// space is destroyed.
        /// </summary>
        protected override void OnDestroy()
        {
            UnregisterPanelEvents();
            base.OnDestroy();
        }

        /// <summary>
        /// Initializes a DynamicPanels panel for the first available window
        /// and registers the event handlers if necessary.
        /// </summary>
        private void InitializePanel()
        {
            if (!space.TryGetComponentOrLog(out panelsCanvas))
            {
                Destroyer.Destroy(this);
                return;
            }

            windows.RemoveAll(window => window == null);

            if (windows.Count == 0)
            {
                Destroyer.Destroy(this);
                return;
            }

            BaseWindow firstWindow = windows.FirstOrDefault(
                window => window != null && window.Window != null);

            if (firstWindow == null)
            {
                return;
            }

            Panel firstPanel = PanelUtils.CreatePanelFor((RectTransform)firstWindow.Window.transform, panelsCanvas);

            if (firstPanel == null)
            {
                return;
            }

            panels[firstPanel] = new List<BaseWindow>();

            RegisterPanelEvents();
        }

        /// <summary>
        /// Registers a newly created panel if it contains a window owned
        /// by this window space or is already managed by this space.
        /// Panels belonging exclusively to other window spaces are ignored.
        /// </summary>
        /// <param name="panel">The newly created panel.</param>
        private void HandleNewPanel(Panel panel)
        {
            if (panel == null
                || panelsCanvas == null
                || panel.Canvas != panelsCanvas)
            {
                return;
            }

            ISet<BaseWindow> panelWindows = CollectionExtensions.GetValueOrDefault(panels, panel, new List<BaseWindow>()).ToHashSet();

            for (int index = 0; index < panel.NumberOfTabs; index++)
            {
                PanelTab tab = panel[index];

                if (tab == null || tab.Content == null)
                {
                    continue;
                }

                BaseWindow window = windows.FirstOrDefault(
                    candidate => candidate != null
                                 && candidate.Window != null
                                 && candidate.Window == tab.Content.gameObject);

                if (window != null)
                {
                    panelWindows.Add(window);
                }
            }

            if (panelWindows.Count > 0 || panels.ContainsKey(panel))
            {
                panels[panel] = panelWindows.ToList();
            }
        }

        /// <summary>
        /// Updates the panel association when a window owned by this
        /// window space is moved to another DynamicPanels panel.
        /// </summary>
        /// <param name="tab">The moved tab.</param>
        private void HandleMovedTab(PanelTab tab)
        {
            if (tab == null || tab.Panel == null || tab.Content == null)
            {
                return;
            }

            BaseWindow movedWindow = windows.FirstOrDefault(
                window => window != null
                          && window.Window != null
                          && window.Window == tab.Content.gameObject);

            if (movedWindow == null)
            {
                return;
            }

            foreach (List<BaseWindow> panelWindows in panels.Values)
            {
                panelWindows.Remove(movedWindow);
            }

            List<BaseWindow> targetWindows = panels.GetOrAdd(tab.Panel, () => new List<BaseWindow>());

            if (!targetWindows.Contains(movedWindow))
            {
                targetWindows.Add(movedWindow);
            }
        }

        /// <summary>
        /// Updates the active window when the active tab changes in
        /// a panel managed by this window space.
        /// </summary>
        /// <param name="tab">The newly active tab.</param>
        private void ChangeActiveTab(PanelTab tab)
        {
            if (tab == null || tab.Panel == null || tab.Content == null)
            {
                return;
            }

            if (!panels.TryGetValue(tab.Panel, out List<BaseWindow> panelWindows))
            {
                return;
            }

            BaseWindow activeWindow = panelWindows.FirstOrDefault(
                window => window != null
                          && window.Window != null
                          && window.Window == tab.Content.gameObject);

            if (activeWindow != null)
            {
                ActiveWindow = activeWindow;
                OnActiveWindowChanged.Invoke();
            }
        }

        /// <summary>
        /// Closes the windows owned by this window space in the specified
        /// panel without removing unrelated tabs from other window spaces.
        /// </summary>
        /// <param name="panel">The panel whose close button was used.</param>
        private void ClosePanel(Panel panel)
        {
            if (panel == null
                || !panels.TryGetValue(panel, out List<BaseWindow> panelWindows))
            {
                return;
            }

            foreach (BaseWindow window in panelWindows.ToList())
            {
                if (window != null && window.Window != null)
                {
                    PanelTab tab = panel.GetTab((RectTransform)window.Window.transform);

                    if (tab != null)
                    {
                        panel.RemoveTab(tab);
                    }
                }

                windows.Remove(window);
                currentWindows.Remove(window);
                Destroyer.Destroy(window);
            }

            panels.Remove(panel);

            if (panel != null && panel.NumberOfTabs == 0)
            {
                Destroyer.Destroy(panel.gameObject);
            }

            if (ActiveWindow == null || !windows.Contains(ActiveWindow))
            {
                ActiveWindow = windows.FirstOrDefault(window => window != null && window.Window != null);
            }

            if (currentActiveWindow == null
                || !windows.Contains(currentActiveWindow))
            {
                currentActiveWindow = null;
            }

            OnActiveWindowChanged.Invoke();
        }

        /// <summary>
        /// Closes the window associated with a tab belonging to this
        /// window space. Tabs from unrelated panels are ignored.
        /// </summary>
        /// <param name="panelTab">The tab being closed.</param>
        private void CloseTab(PanelTab panelTab)
        {
            if (panelTab == null
                || panelTab.Panel == null
                || panelTab.Content == null)
            {
                return;
            }

            if (!panels.TryGetValue(panelTab.Panel, out List<BaseWindow> panelWindows))
            {
                return;
            }

            BaseWindow window = panelWindows.FirstOrDefault(
                entry => entry != null
                         && entry.Window != null
                         && entry.Window.GetInstanceID()
                            == panelTab.Content.gameObject.GetInstanceID());

            if (window != null)
            {
                CloseWindow(window);
            }

            if (panelTab.Panel.NumberOfTabs <= 1)
            {
                OnActiveWindowChanged.Invoke();
            }
        }
    }
}

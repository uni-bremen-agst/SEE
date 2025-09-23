using System;
using System.Collections.Generic;
using System.Linq;
using DynamicPanels;
using SEE.Controls;
using SEE.Controls.Actions;
using SEE.GO;
using SEE.Utils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.UI;
using CollectionExtensions = SEE.Utils.CollectionExtensions;

namespace SEE.UI.Window
{
    /// <summary>
    /// This part of the class contains the Desktop UI for a space.
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
        /// <p>
        /// Sets the active tab of the panel to the <see cref="ActiveWindow"/>.
        /// If <see cref="ActiveWindow"/> is not part of open <see cref="windows"/>, the previous active
        /// window will be restored and a warning will be logged. If the previous active window is not part
        /// of that list as well, this component will be destroyed and a <see cref="InvalidOperationException"/>
        /// may be thrown.
        /// </p>
        ///
        /// <p>
        /// If any of the following are <c>null</c>, calling the method will have no effect:
        /// <ul>
        /// <li><see cref="panel"/></li>
        /// <li><see cref="ActiveWindow"/></li>
        /// <li><c>ActiveWindow.window</c></li>
        /// </ul>
        /// </p>
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="ActiveWindow"/> is not
        /// part of the open windows, and the previous active window (<see cref="currentActiveWindow"/>)
        /// isn't part of the open windows either. Note that this may not be thrown at all, because
        /// <see cref="Destroy"/> is called beforehand.
        /// </exception>
        private void UpdateActiveTab()
        {
            if (panels.Count > 0 && ActiveWindow != null && ActiveWindow.Window != null)
            {
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

                    // Note: This is a tail-recursion, which would be great if C# had tail call optimization,
                    // but it doesn't. Your IDE may recommend to then use an iterative construct instead of the
                    // recursive one to be more efficient, but this will just "improve" an O(1) space complexity to
                    // O(1) space complexity, because the recursion will at most happen once per call.
                    // Additionally, the readability of the iterative version is (in my opinion) much worse, this is
                    // why I have left it the way it is.
                    UpdateActiveTab();
                    return;
                }
                if (PanelTabForWindow(ActiveWindow) is ({ } panel, { } tab))
                {
                    panel.ActiveTab = tab.Index;
                }
            }
        }

        /// <summary>
        /// Returns the panel and tab for a given window.
        /// </summary>
        /// <param name="window">The window to find the panel and tab for.</param>
        /// <returns>The panel and tab for the window, or <c>null</c> if the window is not part of the space.</returns>
        private (Panel, PanelTab)? PanelTabForWindow(BaseWindow window)
        {
            if (window == null || window.Window == null)
            {
                return null;
            }
            RectTransform windowTransform = (RectTransform)window.Window.transform;
            return panels.Keys.Select(x => (x, x.GetTab(windowTransform))).SingleOrDefault(x => x.Item2 != null);
        }

        protected override void UpdateDesktop()
        {
            switch (panels.Count)
            {
                case > 0:
                    // We need to destroy any empty panels now.
                    foreach (Panel emptyPanel in panels.Where(x => x.Value.Count == 0).Select(x => x.Key).ToList())
                    {
                        Destroyer.Destroy(emptyPanel);
                        panels.Remove(emptyPanel);
                    }
                    break;
                case 0 when windows.Any(x => x.Window):
                    // We need to Initialize at least one panel.
                    InitializePanel();
                    break;
                case 0: return; // If no window is initialized yet, there's nothing we can do.
            }

            // Now we need to detect changes in the open windows.
            // Unfortunately this adds an O(m+n) call to each frame, but considering how small m and n are likely to be,
            // this shouldn't be a problem.

            // First, close old windows that are not open anymore
            foreach (BaseWindow window in currentWindows.Except(windows).ToList())
            {
                if (PanelTabForWindow(window) is ({ } panel, { } tab))
                {
                    panel.RemoveTab(tab);
                    panels[panel].Remove(window);
                }
                currentWindows.Remove(window);
                Destroyer.Destroy(window);
            }

            // Then, add new tabs
            // We need to skip windows which weren't initialized yet
            foreach (BaseWindow window in windows.Except(currentWindows).Where(x => x.Window).ToList())
            {
                RectTransform rectTransform = (RectTransform)window.Window.transform;
                // Add the new window as a tab to the latest panel
                Panel targetPanel = panels.Keys.Last();
                panels[targetPanel].Add(window);
                PanelTab tab = targetPanel.AddTab(rectTransform);
                tab.Label = window.Title;
                tab.Icon = null;
                currentWindows.Add(window);

                // Allow closing the tab
                if (CanClose)
                {
                    // FIXME: Apparently, this is called twice. It seems like the problem is in PanelNotificationCenter.
                    PanelNotificationCenter.OnTabClosed += CloseTab;
                }

                // Rebuild layout
                panelsCanvas.ForceRebuildLayoutImmediate();
                window.RebuildLayout();
            }

            if (currentActiveWindow != ActiveWindow && ActiveWindow)
            {
                // Nominal active window has been changed, so we change the actual active window as well.
                UpdateActiveTab();

                // The window will only be actually changed when UpdateActiveTab() didn't throw an exception,
                // so currentActiveWindow is guaranteed to be part of windows.
                currentActiveWindow = ActiveWindow;
            }
            return;

            void CloseTab(PanelTab panelTab)
            {
                if (panels.TryGetValue(panelTab.Panel, out List<BaseWindow> panel))
                {
                    BaseWindow window = panel.FirstOrDefault(x => x.Window.GetInstanceID() == panelTab.Content.gameObject.GetInstanceID());
                    if (window != null)
                    {
                        CloseWindow(window);
                    }
                    if (panelTab.Panel.NumberOfTabs <= 1)
                    {
                        // All tabs were closed, so we send out an event
                        // (The PanelNotificationCenter won't trigger in this case)
                        OnActiveWindowChanged.Invoke();
                    }
                }
            }
        }

        protected override void UpdateVR()
        {
            UpdateDesktop();
        }

        /// <summary>
        /// Initializes the dynamic panel on the panel canvas.
        /// </summary>
        private void InitializePanel()
        {
            if (!space.TryGetComponentOrLog(out panelsCanvas))
            {
                Destroyer.Destroy(this);
            }

            windows.RemoveAll(x => x == null);
            if (windows.Count == 0)
            {
                Destroyer.Destroy(this);
                windows.Clear();
                return;
            }

            // Create the first panel.
            Panel firstPanel = PanelUtils.CreatePanelFor((RectTransform)windows[0].Window.transform, panelsCanvas);
            panels[firstPanel] = new();

            // The user may create panels themselves.
            PanelNotificationCenter.OnPanelCreated += HandleNewPanel;
            PanelNotificationCenter.OnPanelClosed += ClosePanel;
            PanelNotificationCenter.OnStoppedDraggingTab += HandleMovedTab;
            // When the active tab *on one of our panels* is changed, we invoke the corresponding event
            PanelNotificationCenter.OnActiveTabChanged += ChangeActiveTab;
            return;

            void HandleNewPanel(Panel panel)
            {
                // There may already be windows in the panel (if the user created it by dragging tabs into the void)
                ISet<BaseWindow> panelWindows = CollectionExtensions.GetValueOrDefault(panels, panel, new()).ToHashSet();
                for (int i = 0; i < panel.NumberOfTabs; i++)
                {
                    if (Windows.SingleOrDefault(w => w.Window == panel[i].Content.gameObject) is { } window)
                    {
                        panelWindows.Add(window);
                    }
                }
                panels[panel] = panelWindows.ToList();
            }

            void HandleMovedTab(PanelTab tab)
            {
                foreach ((Panel key, List<BaseWindow> value) in panels)
                {
                    foreach (BaseWindow baseWindow in value.Where(baseWindow => baseWindow.Window == tab.Content.gameObject))
                    {
                        panels[key].Remove(baseWindow);
                        panels.GetOrAdd(tab.Panel, () => new()).Add(baseWindow);
                        return;
                    }
                }
            }

            void ChangeActiveTab(PanelTab tab)
            {
                if (panels.TryGetValue(tab.Panel, out List<BaseWindow> panel))
                {
                    BaseWindow activePanel = panel.FirstOrDefault(x => x.Window == tab.Content.gameObject);
                    if (activePanel != null)
                    {
                        ActiveWindow = activePanel;
                        OnActiveWindowChanged.Invoke();
                    }
                }
            }

            void ClosePanel(Panel panel)
            {
                if (panels.ContainsKey(panel))
                {
                    // Close each tab in this panel.
                    foreach (BaseWindow window in panels[panel].Where(x => x.Window != null))
                    {
                        panel.RemoveTab(panel.GetTab((RectTransform)window.Window.transform));
                        Destroyer.Destroy(window);
                    }

                    windows.RemoveAll(panels[panel]);
                    panels.Remove(panel);
                    OnActiveWindowChanged.Invoke();
                    Destroyer.Destroy(panel);
                }
            }
        }
    }
}

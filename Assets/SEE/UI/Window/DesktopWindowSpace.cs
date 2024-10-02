using System;
using System.Linq;
using DynamicPanels;
using SEE.Controls;
using SEE.Controls.Actions;
using SEE.GO;
using SEE.Utils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace SEE.UI.Window
{
    /// <summary>
    /// This part of the class contains the Desktop UI for a space.
    /// </summary>
    public partial class WindowSpace
    {
        /// <summary>
        /// The <see cref="panel"/> containing the windows.
        /// </summary>
        private Panel panel;

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
            Canvas.transform.SetParent(GameObject.Find("XRTabletCanvas(Clone)").transform.Find("Screen").transform, false);
            Canvas.AddComponent<TrackedDeviceGraphicRaycaster>();
            Canvas.GetComponent<RectTransform>().localScale = new Vector3(0.001f, 0.001f, 0.001f);
            Canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(950, 950);
            Canvas.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0, -90, 0);
            Canvas.GetComponent<RectTransform>().localPosition = new Vector3(0.9f, 0, 0);
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
            if (panel != null && ActiveWindow != null && ActiveWindow.Window != null)
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
                    // why I have left it the way it is. The following disables this recommendation in some IDEs.
                    // ReSharper disable once TailRecursiveCall
                    UpdateActiveTab();
                    return;
                }
                panel.ActiveTab = panel.GetTabIndex((RectTransform)ActiveWindow.Window.transform);
            }
        }

        protected override void UpdateDesktop()
        {
            // In VR the TreeView could be open, while the user moves a Node. In this case, the TreeView
            // would update the entire time, the user is moving the Node, which is causing laggs. Thats
            // why we close it.
            if (GlobalActionHistory.Current() == ActionStateTypes.Move && XRSEEActions.CloseTreeView)
            {
                foreach (BaseWindow window in currentWindows)
                {
                    CloseWindow(window);
                }
                XRSEEActions.CloseTreeView = false;
            }
            if (panel && !windows.Any())
            {
                // We need to destroy the panel now
                Destroyer.Destroy(panel);
            }
            else if (!panel && windows.Any(x => x.Window))
            {
                InitializePanel();
            }
            else if (!panel)
            {
                // If no window is initialized yet, there's nothing we can do
                return;
            }

            if (currentActiveWindow != ActiveWindow)
            {
                // Nominal active window has been changed, so we change the actual active window as well.
                UpdateActiveTab();

                // The window will only be actually changed when UpdateActiveTab() didn't throw an exception,
                // so currentActiveWindow is guaranteed to be part of windows.
                currentActiveWindow = ActiveWindow;
            }

            // Now we need to detect changes in the open windows.
            // Unfortunately this adds an O(m+n) call to each frame, but considering how small m and n are likely to be,
            // this shouldn't be a problem.

            // First, close old windows that are not open anymore
            foreach (BaseWindow window in currentWindows.Except(windows).ToList())
            {
                panel.RemoveTab(panel.GetTab((RectTransform)window.Window.transform));
                currentWindows.Remove(window);
                Destroyer.Destroy(window);
            }

            // Then, add new tabs
            // We need to skip windows which weren't initialized yet
            foreach (BaseWindow window in windows.Except(currentWindows).Where(x => x.Window != null).ToList())
            {
                RectTransform rectTransform = (RectTransform)window.Window.transform;
                // Add the new window as a tab to our panel
                PanelTab tab = panel.AddTab(rectTransform);
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
                if (SceneSettings.InputType == PlayerInputType.DesktopPlayer)
                {
                    window.RebuildLayout();
                }
            }

            void CloseTab(PanelTab panelTab)
            {
                if (panelTab.Panel == panel)
                {
                    BaseWindow window = windows.FirstOrDefault(x => x.Window.GetInstanceID() == panelTab.Content.gameObject.GetInstanceID());
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
            panel = PanelUtils.CreatePanelFor((RectTransform)windows[0].Window.transform, panelsCanvas);
            // When the active tab *on this panel* is changed, we invoke the corresponding event
            PanelNotificationCenter.OnActiveTabChanged += ChangeActiveTab;
            PanelNotificationCenter.OnPanelClosed += ClosePanel;

            void ChangeActiveTab(PanelTab tab)
            {
                if (panel == tab.Panel)
                {
                    ActiveWindow = Windows.First(x => x.Window.GetInstanceID() == tab.Content.gameObject.GetInstanceID());
                    OnActiveWindowChanged.Invoke();
                }
            }

            void ClosePanel(Panel panel)
            {
                if (panel == this.panel)
                {
                    // Close each tab
                    foreach (BaseWindow window in windows)
                    {
                        this.panel.RemoveTab(this.panel.GetTab((RectTransform)window.Window.transform));
                        Destroyer.Destroy(window);
                    }

                    windows.Clear();
                    OnActiveWindowChanged.Invoke();
                    Destroyer.Destroy(this.panel);
                }
            }
        }
    }
}

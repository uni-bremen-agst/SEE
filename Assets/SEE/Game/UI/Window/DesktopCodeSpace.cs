using System;
using System.Linq;
using DynamicPanels;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.UI.Window
{
    /// <summary>
    /// This part of the class contains the Desktop UI for a space.
    /// </summary>
    public partial class WindowSpace
    {
        /// <summary>
        /// The <see cref="Panel"/> containing the windows.
        /// </summary>
        private Panel Panel;

        /// <summary>
        /// The dynamic canvas of the panel.
        /// This canvas will implement tabs, moving, tiling, and resizing for us.
        /// </summary>
        private DynamicPanelsCanvas PanelsCanvas;

        protected override void StartDesktop()
        {
            // Add space game object if it doesn't exist yet
            space = Canvas.transform.Find(WindowSpaceName)?.gameObject;
            if (!space)
            {
                space = PrefabInstantiator.InstantiatePrefab(WINDOW_SPACE_PREFAB, Canvas.transform, false);
                space.name = WindowSpaceName;
            }
            space.SetActive(true);
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
        /// <li><see cref="Panel"/></li>
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
            if (Panel != null && ActiveWindow != null && ActiveWindow.window != null)
            {
                if (!windows.Contains(ActiveWindow))
                {
                    Debug.LogWarning("Active window is not part of available windows. Resetting to previous entry.\n");
                    if (ActiveWindow == currentActiveWindow)
                    {
                        Debug.LogError("Neither active window, nor previous active window is "
                                       + "part of available windows. This component will now self-destruct.");
                        Destroy(this);
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
                Panel.ActiveTab = Panel.GetTabIndex((RectTransform) ActiveWindow.window.transform);
            }
        }

        protected override void UpdateDesktop()
        {
            if (Panel && !windows.Any())
            {
                // We need to destroy the panel now
                Destroy(Panel);
            } 
            else if (!Panel && windows.Any(x => x.window))
            {
                InitializePanel();
            } 
            else if (!Panel)
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
                Panel.RemoveTab(Panel.GetTab((RectTransform) window.window.transform));
                currentWindows.Remove(window);
                Destroy(window);
            }
            
            // Then, add new tabs 
            // We need to skip windows which weren't initialized yet
            foreach (BaseWindow window in windows.Except(currentWindows).Where(x => x.window != null).ToList())
            {
                RectTransform rectTransform = (RectTransform) window.window.transform;
                // Add the new window as a tab to our panel
                PanelTab tab = Panel.AddTab(rectTransform);
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
                PanelsCanvas.ForceRebuildLayoutImmediate();
                window.RebuildLayout();
            }

            void CloseTab(PanelTab panelTab)
            {
                if (panelTab.Panel == Panel)
                {
                    CloseWindow(windows.First(x => x.window.GetInstanceID() == panelTab.Content.gameObject.GetInstanceID()));
                    if (panelTab.Panel.NumberOfTabs <= 1)
                    {
                        // All tabs were closed, so we send out an event 
                        // (The PanelNotificationCenter won't trigger in this case)
                        OnActiveWindowChanged.Invoke();
                    }
                }
            }
        }

        /// <summary>
        /// Initializes the dynamic panel on the panel canvas.
        /// </summary>
        private void InitializePanel()
        {
            if (!space.TryGetComponentOrLog(out PanelsCanvas))
            {
                Destroy(this);
            }

            windows.RemoveAll(x => x == null);
            if (windows.Count == 0)
            {
                Destroy(this);
                windows.Clear();
                return;
            }
            Panel = PanelUtils.CreatePanelFor((RectTransform) windows[0].window.transform, PanelsCanvas);
            // When the active tab *on this panel* is changed, we invoke the corresponding event
            PanelNotificationCenter.OnActiveTabChanged += ChangeActiveTab;
            PanelNotificationCenter.OnPanelClosed += ClosePanel;
            
            void ChangeActiveTab(PanelTab tab)
            {
                if (Panel == tab.Panel)
                {
                    ActiveWindow = Windows.First(x => x.window.GetInstanceID() == tab.Content.gameObject.GetInstanceID());
                    OnActiveWindowChanged.Invoke();
                }
            }

            void ClosePanel(Panel panel)
            {
                if (panel == Panel)
                {
                    // Close each tab
                    foreach (BaseWindow window in windows)
                    {
                        Panel.RemoveTab(Panel.GetTab((RectTransform) window.window.transform));
                        Destroy(window);
                    }

                    windows.Clear();
                    OnActiveWindowChanged.Invoke();
                    Destroy(Panel);
                }
            }

        }
    }
}
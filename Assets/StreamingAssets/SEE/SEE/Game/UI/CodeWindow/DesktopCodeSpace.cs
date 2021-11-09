using System;
using System.Linq;
using DynamicPanels;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.UI.CodeWindow
{
    /// <summary>
    /// This part of the class contains the Desktop UI for a code space.
    /// </summary>
    public partial class CodeSpace
    {
        /// <summary>
        /// The <see cref="Panel"/> containing the code windows.
        /// </summary>
        private Panel Panel;

        /// <summary>
        /// The dynamic canvas of the panel.
        /// This canvas will implement tabs, moving, tiling, and resizing for us.
        /// </summary>
        private DynamicPanelsCanvas PanelsCanvas;

        protected override void StartDesktop()
        {
            // Add CodeSpace component if it doesn't exist yet
            space = Canvas.transform.Find(CodeSpaceName)?.gameObject;
            if (!space)
            {
                space = PrefabInstantiator.InstantiatePrefab(CODE_SPACE_PREFAB, Canvas.transform, false);
                space.name = CodeSpaceName;
            }
            space.SetActive(true);
        }

        /// <summary>
        /// <p>
        /// Sets the active tab of the panel to the <see cref="ActiveCodeWindow"/>.
        /// If <see cref="ActiveCodeWindow"/> is not part of open <see cref="codeWindows"/>, the previous active
        /// code window will be restored and a warning will be logged. If the previous active code window is not part
        /// of that list as well, this component will be destroyed and a <see cref="InvalidOperationException"/>
        /// may be thrown.
        /// </p>
        /// 
        /// <p>
        /// If any of the following are <c>null</c>, calling the method will have no effect:
        /// <ul>
        /// <li><see cref="Panel"/></li>
        /// <li><see cref="ActiveCodeWindow"/></li>
        /// <li><c>ActiveCodeWindow.codeWindow</c></li>
        /// </ul>
        /// </p>
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="ActiveCodeWindow"/> is not
        /// part of the open code windows, and the previous active code window (<see cref="currentActiveCodeWindow"/>)
        /// isn't part of the open code windows either. Note that this may not be thrown at all, because
        /// <see cref="Destroy"/> is called beforehand.
        /// </exception>
        private void UpdateActiveTab()
        {
            if (Panel != null && ActiveCodeWindow != null && ActiveCodeWindow.codeWindow != null)
            {
                if (!codeWindows.Contains(ActiveCodeWindow))
                {
                    Debug.LogWarning("Active code window is not part of available code windows. Resetting to previous entry.\n");
                    if (ActiveCodeWindow == currentActiveCodeWindow)
                    {
                        Debug.LogError("Neither active code window, nor previous active code window is "
                                       + "part of available code windows. This component will now self-destruct.");
                        Destroy(this);
                        throw new InvalidOperationException();
                    }
                    ActiveCodeWindow = currentActiveCodeWindow;
                    
                    // Note: This is a tail-recursion, which would be great if C# had tail call optimization, 
                    // which it doesn't. Your IDE may recommend to then use an iterative construct instead of the 
                    // recursive one to be more efficient, but this will just "improve" an O(1) space complexity to
                    // O(1) space complexity, because the recursion will at most happen once per call.
                    // Additionally, the readability of the iterative version is (in my opinion) much worse, this is
                    // why I have left it the way it is.
                    UpdateActiveTab();
                    return;
                }
                Panel.ActiveTab = Panel.GetTabIndex((RectTransform) ActiveCodeWindow.codeWindow.transform);
            }
        }

        protected override void UpdateDesktop()
        {
            if (Panel && !codeWindows.Any())
            {
                // We need to destroy the panel now
                Destroy(Panel);
            } 
            else if (!Panel && codeWindows.Any(x => x.codeWindow))
            {
                InitializePanel();
            } 
            else if (!Panel)
            {
                // If no code window is initialized yet, there's nothing we can do
                return;
            }
            
            if (currentActiveCodeWindow != ActiveCodeWindow)
            {
                // Nominal active code window has been changed, so we change the actual active code window as well.
                UpdateActiveTab();

                // The window will only be actually changed when UpdateActiveTab() didn't throw an exception,
                // so currentActiveCodeWindow is guaranteed to be part of codeWindows.
                currentActiveCodeWindow = ActiveCodeWindow;
            }
            
            // Now we need to detect changes in the open code windows.
            // Unfortunately this adds an O(m+n) call to each frame, but considering how small m and n are likely to be,
            // this shouldn't be a problem.
            
            // First, close old windows that are not open anymore
            foreach (CodeWindow codeWindow in currentCodeWindows.Except(codeWindows).ToList())
            {
                Panel.RemoveTab(Panel.GetTab((RectTransform) codeWindow.codeWindow.transform));
                currentCodeWindows.Remove(codeWindow);
                Destroy(codeWindow);
            }
            
            // Then, add new tabs 
            // We need to skip code windows who weren't initialized yet
            foreach (CodeWindow codeWindow in codeWindows.Except(currentCodeWindows).Where(x => x.codeWindow != null).ToList())
            {
                RectTransform rectTransform = (RectTransform) codeWindow.codeWindow.transform;
                // Add the new window as a tab to our panel
                PanelTab tab = Panel.AddTab(rectTransform);
                tab.Label = codeWindow.Title;
                tab.Icon = null;
                currentCodeWindows.Add(codeWindow);
                
                // Allow closing the tab
                if (CanClose)
                {
                    // FIXME: Apparently, this is called twice. It seems like the problem is in PanelNotificationCenter.
                    PanelNotificationCenter.OnTabClosed += CloseTab;
                }

                // Rebuild layout
                PanelsCanvas.ForceRebuildLayoutImmediate();
                codeWindow.RecalculateExcessLines();
            }

            void CloseTab(PanelTab panelTab)
            {
                if (panelTab.Panel == Panel)
                {
                    CloseCodeWindow(codeWindows.First(x => x.codeWindow.GetInstanceID() == panelTab.Content.gameObject.GetInstanceID()));
                    if (panelTab.Panel.NumberOfTabs <= 1)
                    {
                        // All tabs were closed, so we send out an event 
                        // (The PanelNotificationCenter won't trigger in this case)
                        OnActiveCodeWindowChanged.Invoke();
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

            codeWindows.RemoveAll(x => x == null);
            if (codeWindows.Count == 0)
            {
                Destroy(this);
                codeWindows.Clear();
                return;
            }
            Panel = PanelUtils.CreatePanelFor((RectTransform) codeWindows[0].codeWindow.transform, PanelsCanvas);
            // When the active tab *on this panel* is changed, we invoke the corresponding event
            PanelNotificationCenter.OnActiveTabChanged += ChangeActiveTab;
            PanelNotificationCenter.OnPanelClosed += ClosePanel;
            
            void ChangeActiveTab(PanelTab tab)
            {
                if (Panel == tab.Panel)
                {
                    ActiveCodeWindow = CodeWindows.First(x => x.codeWindow.GetInstanceID() == tab.Content.gameObject.GetInstanceID());
                    OnActiveCodeWindowChanged.Invoke();
                }
            }

            void ClosePanel(Panel panel)
            {
                if (panel == Panel)
                {
                    // Close each tab
                    foreach (CodeWindow codeWindow in codeWindows)
                    {
                        Panel.RemoveTab(Panel.GetTab((RectTransform) codeWindow.codeWindow.transform));
                        Destroy(codeWindow);
                    }

                    codeWindows.Clear();
                    OnActiveCodeWindowChanged.Invoke();
                    Destroy(Panel);
                }
            }

        }
    }
}
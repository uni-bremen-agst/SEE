using System;
using System.Linq;
using DynamicPanels;
using SEE.GO;
using UnityEngine;

namespace SEE.Game.UI.CodeWindow
{
    /// <summary>
    /// This part of the class contains the Desktop UI for a code space.
    /// </summary>
    public partial class CodeWindowSpace
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
            // Add CodeWindowSpace component if it doesn't exist yet
            space = Canvas.transform.Find(CodeWindowSpaceName)?.gameObject;
            if (!space)
            {
                GameObject spacePrefab = Resources.Load<GameObject>(CODE_WINDOW_SPACE_PREFAB);
                space = Instantiate(spacePrefab, Canvas.transform, false);
                space.name = CodeWindowSpaceName;
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
                    UpdateActiveTab();  // recursive call: ActiveCodeWindow has now been changed
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
                // We need to initialize the panel first
                if (!space.TryGetComponentOrLog(out PanelsCanvas))
                {
                    Destroy(this);
                }
                Panel = PanelUtils.CreatePanelFor((RectTransform) codeWindows[0].codeWindow.transform, PanelsCanvas);
                // When the active tab *on this panel* is changed, we invoke the corresponding event
                PanelNotificationCenter.OnActiveTabChanged += tab =>
                {
                    if (Panel == tab.Panel)
                    {
                        ActiveCodeWindow = CodeWindows.First(x => x.codeWindow.GetInstanceID() == tab.Content.gameObject.GetInstanceID());
                        OnActiveCodeWindowChanged.Invoke();
                    }
                };
            } else if (!Panel)
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
            
            // Now we need to detect changes in the open code windows
            // Unfortunately this adds an O(m+n) call to each frame, but considering how small m and n are likely to be,
            // this shouldn't be a problem.
            
            // First, close old windows that are not open anymore
            foreach (CodeWindow codeWindow in currentCodeWindows.Except(codeWindows).ToList())
            {
                Panel.RemoveTab(Panel.GetTab((RectTransform) codeWindow.codeWindow.transform));
                currentCodeWindows.Remove(codeWindow);
                // Otherwise, we'll just destroy the component
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
                    // FIXME: Apparently, this is called twice. It seems like the problem lies in PanelNotificationCenter.
                    PanelNotificationCenter.OnTabClosed += panelTab =>
                    {
                        if (panelTab.Panel == Panel)
                        {
                            CloseCodeWindow(codeWindows.First(x => x.codeWindow.GetInstanceID() == panelTab.Content.gameObject.GetInstanceID()));
                        }
                    };
                }

                // Rebuild layout
                PanelsCanvas.ForceRebuildLayoutImmediate();
                codeWindow.RecalculateExcessLines();
            }
        }
    }
}
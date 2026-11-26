
using System.Collections;
using System.Collections.Generic;
using DynamicPanels;
using NUnit.Framework;
using SEE.Utils;
using UnityEngine;
using UnityEngine.TestTools;

namespace SEE.UI.Window
{
    /// <summary>
    /// Tests the lifecycle of windows and their DynamicPanels containers.
    /// </summary>
    internal class TestWindowSpaceLifecycle : TestUI
    {
        /// <summary>
        /// Maximum number of frames allowed for window initialization.
        /// </summary>
        private const int MaximumInitializationFrames = 120;

        /// <summary>
        /// Game object containing the window components created by a test.
        /// </summary>
        private GameObject windowOwner;

        /// <summary>
        /// Canvases that existed before the test setup.
        /// </summary>
        private HashSet<Canvas> previousCanvases;

        /// <summary>
        /// Records the existing canvases so that the test can clean up
        /// only the UI objects it creates.
        /// </summary>
        /// <returns>An enumerator that completes immediately.</returns>
        [UnitySetUp]
        public IEnumerator RecordExistingCanvases()
        {
            previousCanvases = new HashSet<Canvas>(
                Object.FindObjectsByType<Canvas>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None));

            yield break;
        }

        /// <summary>
        /// Destroys the window components and canvases created by the test.
        /// </summary>
        /// <returns>An enumerator that waits for Unity destruction.</returns>
        [UnityTearDown]
        public IEnumerator CleanUpWindows()
        {
            if (windowOwner != null)
            {
                Destroyer.Destroy(windowOwner);
                windowOwner = null;
            }

            yield return null;

            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (!previousCanvases.Contains(canvas))
                {
                    Destroyer.Destroy(canvas.gameObject);
                }
            }

            yield return null;
        }

        /// <summary>
        /// Verifies that WindowSpace can open another window after
        /// the last DynamicPanels tab has been removed and the
        /// corresponding window has been closed.
        /// </summary>
        /// <returns>
        /// An enumerator that waits for the panel lifecycle.
        /// </returns>
        [UnityTest]
        [LoadScene]
        public IEnumerator ClosingWindowAfterLastTabRemovalAllowsReopening()
        {
            windowOwner = new GameObject("WindowSpace reopening test");

            WindowSpace windowSpace =
                windowOwner.AddComponent<WindowSpace>();

            WindowSpaceTestWindow firstWindow =
                windowOwner.AddComponent<WindowSpaceTestWindow>();
            firstWindow.Title = "First reopening test window";

            windowSpace.AddWindow(firstWindow);
            windowSpace.ActiveWindow = firstWindow;

            Panel firstPanel = null;
            PanelTab firstTab = null;

            for (int frame = 0;
                 frame < MaximumInitializationFrames && firstTab == null;
                 frame++)
            {
                if (firstWindow.Window != null)
                {
                    Panel[] availablePanels = Object.FindObjectsByType<Panel>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

                    foreach (Panel candidate in availablePanels)
                    {
                        if (candidate == null)
                        {
                            continue;
                        }

                        PanelTab candidateTab = candidate.GetTab(
                            (RectTransform)firstWindow.Window.transform);

                        if (candidateTab != null)
                        {
                            firstPanel = candidate;
                            firstTab = candidateTab;
                            break;
                        }
                    }
                }

                if (firstTab == null)
                {
                    yield return null;
                }
            }

            Assert.That(firstPanel, Is.Not.Null,
                        "The initial panel was not created.");
            Assert.That(firstTab, Is.Not.Null,
                        "The first window was not added to a panel.");
            Assert.That(windowSpace.Windows.Contains(firstWindow), Is.True);

            // Removing the last tab schedules destruction of its panel.
            firstPanel.RemoveTab(firstTab);

            // Explicitly remove the window from the nominal window list.
            Assert.That(windowSpace.CloseWindow(firstWindow), Is.True,
                        "The first window could not be closed.");

            yield return null;
            yield return null;

            Assert.That(firstPanel == null, Is.True,
                        "DynamicPanels did not destroy the empty panel.");

            Assert.That(windowSpace.Windows.Contains(firstWindow), Is.False,
                        "The closed window remains registered.");

            Assert.That(windowSpace, Is.Not.Null,
                        "WindowSpace was destroyed when its last panel disappeared.");

            // Open a new window using the same WindowSpace.
            WindowSpaceTestWindow secondWindow =
                windowOwner.AddComponent<WindowSpaceTestWindow>();
            secondWindow.Title = "Second reopening test window";

            windowSpace.AddWindow(secondWindow);
            windowSpace.ActiveWindow = secondWindow;

            PanelTab reopenedTab = null;

            for (int frame = 0;
                 frame < MaximumInitializationFrames && reopenedTab == null;
                 frame++)
            {
                if (secondWindow.Window != null)
                {
                    Panel[] availablePanels = Object.FindObjectsByType<Panel>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

                    foreach (Panel candidate in availablePanels)
                    {
                        if (candidate == null)
                        {
                            continue;
                        }

                        PanelTab candidateTab = candidate.GetTab(
                            (RectTransform)secondWindow.Window.transform);

                        if (candidateTab != null)
                        {
                            reopenedTab = candidateTab;
                            break;
                        }
                    }
                }

                if (reopenedTab == null)
                {
                    yield return null;
                }
            }

            Assert.That(windowSpace, Is.Not.Null,
                        "WindowSpace was destroyed unexpectedly.");
            Assert.That(windowSpace.Windows.Contains(secondWindow), Is.True,
                        "The reopened window is not registered.");
            Assert.That(reopenedTab, Is.Not.Null,
                        "The reopened window has no valid DynamicPanels tab.");
            Assert.That(windowSpace.ActiveWindow, Is.SameAs(secondWindow),
                        "The reopened window is not active.");
        }


        /// <summary>
        /// Verifies that a window can be reopened after its predecessor
        /// has been closed through the regular WindowSpace API.
        /// Unlike the other lifecycle tests, the panel is not
        /// explicitly modified by the test.
        /// </summary>
        /// <returns>
        /// An enumerator that waits for window initialization,
        /// removal, and reopening.
        /// </returns>
        [UnityTest]
        [LoadScene]
        public IEnumerator ClosingLastWindowNormallyAllowsReopening()
        {
            windowOwner = new GameObject("Normal window lifecycle test");

            WindowSpace windowSpace =
                windowOwner.AddComponent<WindowSpace>();

            WindowSpaceTestWindow firstWindow =
                windowOwner.AddComponent<WindowSpaceTestWindow>();
            firstWindow.Title = "First normal lifecycle window";

            windowSpace.AddWindow(firstWindow);
            windowSpace.ActiveWindow = firstWindow;

            Panel firstPanel = null;

            for (int frame = 0;
                 frame < MaximumInitializationFrames && firstPanel == null;
                 frame++)
            {
                if (firstWindow.Window != null)
                {
                    foreach (Panel candidate in Object.FindObjectsByType<Panel>(
                                 FindObjectsInactive.Include,
                                 FindObjectsSortMode.None))
                    {
                        if (candidate != null
                            && candidate.GetTab(
                                (RectTransform)firstWindow.Window.transform) != null)
                        {
                            firstPanel = candidate;
                            break;
                        }
                    }
                }

                if (firstPanel == null)
                {
                    yield return null;
                }
            }

            Assert.That(firstPanel, Is.Not.Null,
                        "The first window was not added to a panel.");

            // Close the window only through the public WindowSpace API.
            Assert.That(windowSpace.CloseWindow(firstWindow), Is.True);

            for (int frame = 0;
                 frame < MaximumInitializationFrames && firstPanel != null;
                 frame++)
            {
                yield return null;
            }

            Assert.That(firstPanel == null, Is.True,
                        "The final panel was not destroyed after closing the window.");
            Assert.That(windowSpace, Is.Not.Null,
                        "WindowSpace was destroyed during normal closing.");
            Assert.That(windowSpace.Windows.Contains(firstWindow), Is.False);

            WindowSpaceTestWindow secondWindow =
                windowOwner.AddComponent<WindowSpaceTestWindow>();
            secondWindow.Title = "Second normal lifecycle window";

            windowSpace.AddWindow(secondWindow);
            windowSpace.ActiveWindow = secondWindow;

            PanelTab reopenedTab = null;

            for (int frame = 0;
                 frame < MaximumInitializationFrames && reopenedTab == null;
                 frame++)
            {
                if (secondWindow.Window != null)
                {
                    foreach (Panel candidate in Object.FindObjectsByType<Panel>(
                                 FindObjectsInactive.Include,
                                 FindObjectsSortMode.None))
                    {
                        if (candidate == null)
                        {
                            continue;
                        }

                        reopenedTab = candidate.GetTab(
                            (RectTransform)secondWindow.Window.transform);

                        if (reopenedTab != null)
                        {
                            break;
                        }
                    }
                }

                if (reopenedTab == null)
                {
                    yield return null;
                }
            }

            Assert.That(windowSpace, Is.Not.Null,
                        "WindowSpace was destroyed unexpectedly.");
            Assert.That(windowSpace.Windows.Contains(secondWindow), Is.True);
            Assert.That(reopenedTab, Is.Not.Null,
                        "A normally reopened window has no DynamicPanels tab.");
            Assert.That(windowSpace.ActiveWindow, Is.SameAs(secondWindow));
        }

        /// <summary>
        /// Verifies that removing a tab directly through DynamicPanels
        /// does not invalidate another window in the same panel.
        /// The removed window is subsequently closed through WindowSpace.
        /// </summary>
        /// <returns>
        /// An enumerator that waits for window and panel updates.
        /// </returns>
        [UnityTest]
        [LoadScene]
        public IEnumerator RemovingOneTabKeepsOtherWindowUsable()
        {
            windowOwner = new GameObject("Multiple window lifecycle test");

            WindowSpace windowSpace =
                windowOwner.AddComponent<WindowSpace>();

            WindowSpaceTestWindow firstWindow =
                windowOwner.AddComponent<WindowSpaceTestWindow>();
            firstWindow.Title = "First multiple window test";

            WindowSpaceTestWindow secondWindow =
                windowOwner.AddComponent<WindowSpaceTestWindow>();
            secondWindow.Title = "Second multiple window test";

            windowSpace.AddWindow(firstWindow);
            windowSpace.AddWindow(secondWindow);
            windowSpace.ActiveWindow = secondWindow;

            Panel sharedPanel = null;
            PanelTab firstTab = null;
            PanelTab secondTab = null;

            for (int frame = 0;
                 frame < MaximumInitializationFrames && sharedPanel == null;
                 frame++)
            {
                if (firstWindow.Window != null && secondWindow.Window != null)
                {
                    Panel[] availablePanels = Object.FindObjectsByType<Panel>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

                    foreach (Panel candidate in availablePanels)
                    {
                        if (candidate == null)
                        {
                            continue;
                        }

                        PanelTab candidateFirstTab = candidate.GetTab(
                            (RectTransform)firstWindow.Window.transform);

                        PanelTab candidateSecondTab = candidate.GetTab(
                            (RectTransform)secondWindow.Window.transform);

                        if (candidateFirstTab != null
                            && candidateSecondTab != null)
                        {
                            sharedPanel = candidate;
                            firstTab = candidateFirstTab;
                            secondTab = candidateSecondTab;
                            break;
                        }
                    }
                }

                if (sharedPanel == null)
                {
                    yield return null;
                }
            }

            Assert.That(sharedPanel, Is.Not.Null,
                        "Both windows were not initialized in the same panel.");
            Assert.That(firstTab, Is.Not.Null);
            Assert.That(secondTab, Is.Not.Null);
            Assert.That(sharedPanel.NumberOfTabs, Is.EqualTo(2));
            Assert.That(windowSpace.Windows.Contains(firstWindow), Is.True);
            Assert.That(windowSpace.Windows.Contains(secondWindow), Is.True);

            // Simulate an externally removed tab.
            sharedPanel.RemoveTab(firstTab);

            Assert.That(windowSpace.CloseWindow(firstWindow), Is.True,
                        "The first window could not be closed.");

            yield return null;
            yield return null;

            Assert.That(windowSpace, Is.Not.Null,
                        "WindowSpace was destroyed unexpectedly.");

            Assert.That(sharedPanel, Is.Not.Null,
                        "The panel was destroyed despite containing another tab.");

            Assert.That(sharedPanel.NumberOfTabs, Is.EqualTo(1),
                        "The remaining tab was unexpectedly removed.");

            Assert.That(windowSpace.Windows.Contains(firstWindow), Is.False,
                        "The removed window is still registered.");

            Assert.That(windowSpace.Windows.Contains(secondWindow), Is.True,
                        "The remaining window was unexpectedly closed.");

            Assert.That(secondWindow, Is.Not.Null);
            Assert.That(secondWindow.Window, Is.Not.Null);

            PanelTab remainingTab = sharedPanel.GetTab(
                (RectTransform)secondWindow.Window.transform);

            Assert.That(remainingTab, Is.Not.Null,
                        "The remaining window lost its DynamicPanels tab.");

            Assert.That(windowSpace.ActiveWindow, Is.SameAs(secondWindow),
                        "The remaining window is no longer active.");
        }

        /// <summary>
        /// Verifies that destroying a window component removes its
        /// obsolete DynamicPanels panel and UI content. Reports the
        /// remaining window and tab state if cleanup fails.
        /// </summary>
        /// <returns>
        /// An enumerator that waits for window and panel destruction.
        /// </returns>
        [UnityTest]
        [LoadScene]
        public IEnumerator DestroyedWindowDoesNotLeaveOrphanedPanel()
        {
            windowOwner = new GameObject("Orphaned panel lifecycle test");

            WindowSpace windowSpace =
                windowOwner.AddComponent<WindowSpace>();

            WindowSpaceTestWindow firstWindow =
                windowOwner.AddComponent<WindowSpaceTestWindow>();
            firstWindow.Title = "Orphaned panel test window";

            windowSpace.AddWindow(firstWindow);
            windowSpace.ActiveWindow = firstWindow;

            Panel firstPanel = null;

            for (int frame = 0;
                 frame < MaximumInitializationFrames && firstPanel == null;
                 frame++)
            {
                if (firstWindow.Window != null)
                {
                    foreach (Panel candidate in Object.FindObjectsByType<Panel>(
                                 FindObjectsInactive.Include,
                                 FindObjectsSortMode.None))
                    {
                        if (candidate != null
                            && candidate.GetTab(
                                (RectTransform)firstWindow.Window.transform) != null)
                        {
                            firstPanel = candidate;
                            break;
                        }
                    }
                }

                if (firstPanel == null)
                {
                    yield return null;
                }
            }

            Assert.That(firstPanel, Is.Not.Null,
                        "The initial panel was not created.");

            GameObject panelObject = firstPanel.gameObject;
            GameObject windowObject = firstWindow.Window;

            Assert.That(panelObject, Is.Not.Null);
            Assert.That(windowObject, Is.Not.Null);
            Assert.That(windowSpace.Windows.Contains(firstWindow), Is.True);

            Destroyer.Destroy(firstWindow);

            for (int frame = 0;
                 frame < MaximumInitializationFrames && firstPanel != null;
                 frame++)
            {
                yield return null;
            }

            if (firstPanel != null)
            {
                int remainingTabCount = firstPanel.NumberOfTabs;

                bool remainingTabContentAlive = false;

                for (int i = 0; i < remainingTabCount; i++)
                {
                    PanelTab remainingTab = firstPanel[i];

                    if (remainingTab != null && remainingTab.Content != null)
                    {
                        remainingTabContentAlive = true;
                    }
                }

                bool originalTabPresent = false;

                if (windowObject != null)
                {
                    originalTabPresent = firstPanel.GetTab(
                        (RectTransform)windowObject.transform) != null;
                }

                Assert.Fail(
                    "WindowSpace did not remove the obsolete panel.\n"
                    + $"Window component destroyed: {firstWindow == null}\n"
                    + $"Window still registered: {windowSpace.Windows.Contains(firstWindow)}\n"
                    + $"Window UI GameObject alive: {windowObject != null}\n"
                    + $"Panel GameObject alive: {panelObject != null}\n"
                    + $"Remaining panel tabs: {remainingTabCount}\n"
                    + $"Remaining tab content alive: {remainingTabContentAlive}\n"
                    + $"Original window tab present: {originalTabPresent}");
            }

            Assert.That(panelObject == null, Is.True,
                        "The destroyed panel left its GameObject behind.");

            Assert.That(windowObject == null, Is.True,
                        "The destroyed window left its visual content behind.");

            Assert.That(windowSpace, Is.Not.Null,
                        "WindowSpace was unexpectedly destroyed.");
        }

        /// <summary>
        /// Verifies that closing a window belonging to one owner does
        /// not destroy another owner's window in the same panel.
        /// </summary>
        /// <returns>
        /// An enumerator that waits for initialization and closing.
        /// </returns>
        [UnityTest]
        [LoadScene]
        public IEnumerator ClosingOneOfTwoWindowsPreservesOtherWindow()
        {
            windowOwner = new GameObject("Shared panel lifecycle test");

            WindowSpace windowSpace =
                windowOwner.AddComponent<WindowSpace>();

            GameObject firstOwner = new("Code city A");
            firstOwner.transform.SetParent(windowOwner.transform, false);

            GameObject secondOwner = new("Code city B");
            secondOwner.transform.SetParent(windowOwner.transform, false);

            WindowSpaceTestWindow firstWindow =
                firstOwner.AddComponent<WindowSpaceTestWindow>();
            firstWindow.Title = "Window from city A";

            WindowSpaceTestWindow secondWindow =
                secondOwner.AddComponent<WindowSpaceTestWindow>();
            secondWindow.Title = "Window from city B";

            windowSpace.AddWindow(firstWindow);
            windowSpace.AddWindow(secondWindow);
            windowSpace.ActiveWindow = secondWindow;

            Panel sharedPanel = null;

            for (int frame = 0;
                 frame < MaximumInitializationFrames && sharedPanel == null;
                 frame++)
            {
                if (firstWindow.Window != null && secondWindow.Window != null)
                {
                    foreach (Panel candidate in Object.FindObjectsByType<Panel>(
                                 FindObjectsInactive.Include,
                                 FindObjectsSortMode.None))
                    {
                        if (candidate == null)
                        {
                            continue;
                        }

                        PanelTab firstTab = candidate.GetTab(
                            (RectTransform)firstWindow.Window.transform);

                        PanelTab secondTab = candidate.GetTab(
                            (RectTransform)secondWindow.Window.transform);

                        if (firstTab != null && secondTab != null)
                        {
                            sharedPanel = candidate;
                            break;
                        }
                    }
                }

                if (sharedPanel == null)
                {
                    yield return null;
                }
            }

            Assert.That(sharedPanel, Is.Not.Null,
                        "Both windows were not added to the same panel.");
            Assert.That(sharedPanel.NumberOfTabs, Is.EqualTo(2));

            GameObject panelObject = sharedPanel.gameObject;
            GameObject secondWindowObject = secondWindow.Window;

            Assert.That(windowSpace.CloseWindow(firstWindow), Is.True);

            yield return null;
            yield return null;
            yield return null;

            Assert.That(windowSpace, Is.Not.Null,
                        "WindowSpace was destroyed unexpectedly.");

            Assert.That(sharedPanel != null, Is.True,
                        "The shared panel was destroyed.");

            Assert.That(panelObject != null, Is.True,
                        "The shared panel GameObject was destroyed.");

            Assert.That(sharedPanel.NumberOfTabs, Is.EqualTo(1),
                        "The shared panel must retain exactly one tab.");

            Assert.That(windowSpace.Windows.Contains(firstWindow), Is.False,
                        "The closed window is still registered.");

            Assert.That(windowSpace.Windows.Contains(secondWindow), Is.True,
                        "The other owner's window was removed.");

            Assert.That(secondOwner != null, Is.True,
                        "The other window owner was destroyed.");

            Assert.That(secondWindow != null, Is.True,
                        "The other window component was destroyed.");

            Assert.That(secondWindowObject != null, Is.True,
                        "The other window's UI content was destroyed.");

            PanelTab remainingTab = sharedPanel.GetTab(
                (RectTransform)secondWindowObject.transform);

            Assert.That(remainingTab, Is.Not.Null,
                        "The other window lost its tab.");

            Assert.That(windowSpace.ActiveWindow, Is.SameAs(secondWindow),
                        "The other window is no longer active.");
        }


        /// <summary>
        /// Verifies that a panel remains alive while DynamicPanels still
        /// contains a tab, even if WindowSpace has no registered windows
        /// remaining in that panel.
        /// </summary>
        /// <returns>
        /// An enumerator that waits for window removal and panel updates.
        /// </returns>
        [UnityTest]
        [LoadScene]
        public IEnumerator UntrackedTabPreventsPanelDestruction()
        {
            windowOwner = new GameObject("Untracked tab lifecycle test");

            WindowSpace windowSpace =
                windowOwner.AddComponent<WindowSpace>();

            WindowSpaceTestWindow trackedWindow =
                windowOwner.AddComponent<WindowSpaceTestWindow>();
            trackedWindow.Title = "Tracked window";

            windowSpace.AddWindow(trackedWindow);
            windowSpace.ActiveWindow = trackedWindow;

            Panel panel = null;
            PanelTab trackedTab = null;

            for (int frame = 0;
                 frame < MaximumInitializationFrames && trackedTab == null;
                 frame++)
            {
                if (trackedWindow.Window != null)
                {
                    foreach (Panel candidate in Object.FindObjectsByType<Panel>(
                                 FindObjectsInactive.Include,
                                 FindObjectsSortMode.None))
                    {
                        if (candidate == null)
                        {
                            continue;
                        }

                        PanelTab candidateTab = candidate.GetTab(
                            (RectTransform)trackedWindow.Window.transform);

                        if (candidateTab != null)
                        {
                            panel = candidate;
                            trackedTab = candidateTab;
                            break;
                        }
                    }
                }

                if (trackedTab == null)
                {
                    yield return null;
                }
            }

            Assert.That(panel, Is.Not.Null,
                        "The initial panel was not created.");
            Assert.That(trackedTab, Is.Not.Null);

            // Simulate additional panel content not represented
            // in WindowSpace's registered BaseWindow collection.
            GameObject independentContent =
                new("Independent tab content", typeof(RectTransform));

            RectTransform independentTransform =
                (RectTransform)independentContent.transform;

            PanelTab independentTab = panel.AddTab(independentTransform);

            Assert.That(independentTab, Is.Not.Null,
                        "The independent tab could not be created.");
            Assert.That(panel.NumberOfTabs, Is.EqualTo(2));

            GameObject panelObject = panel.gameObject;

            // Closing the tracked window must not remove the
            // independent tab or its containing panel.
            Assert.That(windowSpace.CloseWindow(trackedWindow), Is.True);

            yield return null;
            yield return null;
            yield return null;

            Assert.That(windowSpace, Is.Not.Null,
                        "WindowSpace was destroyed unexpectedly.");

            Assert.That(panel != null, Is.True,
                        "A panel containing an independent tab was destroyed.");

            Assert.That(panelObject != null, Is.True,
                        "The panel GameObject was destroyed.");

            Assert.That(independentContent != null, Is.True,
                        "The independent tab content was destroyed.");

            Assert.That(panel.NumberOfTabs, Is.EqualTo(1),
                        "The independent tab was unexpectedly removed.");

            PanelTab remainingTab = panel.GetTab(independentTransform);

            Assert.That(remainingTab, Is.Not.Null,
                        "The independent tab is no longer registered.");

            Assert.That(windowSpace.Windows.Contains(trackedWindow), Is.False,
                        "The closed window is still registered.");
        }

        /// <summary>
        /// Verifies that destroying the active window and UI content
        /// of one city does not prevent activating another city's window.
        /// The first city's activation is synchronized with WindowSpace
        /// before its content is destroyed.
        /// </summary>
        /// <returns>
        /// An enumerator that waits for panel initialization,
        /// activation, destruction, and subsequent synchronization.
        /// </returns>
        [UnityTest]
        [LoadScene]
        public IEnumerator ResettingOneCityPreservesOtherCityActiveTab()
        {
            windowOwner = new GameObject("Multiple city reset test");

            WindowSpace windowSpace =
                windowOwner.AddComponent<WindowSpace>();

            GameObject firstCity = new("First code city");
            firstCity.transform.SetParent(windowOwner.transform, false);

            GameObject secondCity = new("Second code city");
            secondCity.transform.SetParent(windowOwner.transform, false);

            WindowSpaceTestWindow firstWindow =
                firstCity.AddComponent<WindowSpaceTestWindow>();
            firstWindow.Title = "First city window";

            WindowSpaceTestWindow secondWindow =
                secondCity.AddComponent<WindowSpaceTestWindow>();
            secondWindow.Title = "Second city window";

            windowSpace.AddWindow(firstWindow);
            windowSpace.AddWindow(secondWindow);
            windowSpace.ActiveWindow = firstWindow;

            Panel sharedPanel = null;
            PanelTab firstTab = null;
            PanelTab secondTab = null;

            for (int frame = 0;
                 frame < MaximumInitializationFrames && sharedPanel == null;
                 frame++)
            {
                if (firstWindow.Window != null && secondWindow.Window != null)
                {
                    foreach (Panel candidate in Object.FindObjectsByType<Panel>(
                                 FindObjectsInactive.Include,
                                 FindObjectsSortMode.None))
                    {
                        if (candidate == null)
                        {
                            continue;
                        }

                        PanelTab candidateFirstTab = candidate.GetTab(
                            (RectTransform)firstWindow.Window.transform);

                        PanelTab candidateSecondTab = candidate.GetTab(
                            (RectTransform)secondWindow.Window.transform);

                        if (candidateFirstTab != null
                            && candidateSecondTab != null)
                        {
                            sharedPanel = candidate;
                            firstTab = candidateFirstTab;
                            secondTab = candidateSecondTab;
                            break;
                        }
                    }
                }

                if (sharedPanel == null)
                {
                    yield return null;
                }
            }

            Assert.That(sharedPanel, Is.Not.Null,
                        "Both city windows were not initialized.");

            Assert.That(sharedPanel.NumberOfTabs, Is.EqualTo(2),
                        "Both city windows must initially have a tab.");

            int firstIndex = sharedPanel.GetTabIndex(firstTab);
            int secondIndex = sharedPanel.GetTabIndex(secondTab);

            Assert.That(firstIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(secondIndex, Is.GreaterThanOrEqualTo(0));

            // Activate the first city's window and allow WindowSpace
            // to synchronize its internal currentActiveWindow field.
            sharedPanel.ActiveTab = firstIndex;
            windowSpace.ActiveWindow = firstWindow;

            yield return null;

            Assert.That(windowSpace.ActiveWindow, Is.SameAs(firstWindow),
                        "The first city's window was not activated.");

            Assert.That(sharedPanel.ActiveTab, Is.EqualTo(firstIndex),
                        "The first city's panel tab was not activated.");

            // Simulate resetting the first city while its tab is active.
            Destroyer.Destroy(firstWindow.Window);
            Destroyer.Destroy(firstCity);

            yield return null;

            // The other city's window must remain usable.
            Assert.That(secondCity != null, Is.True,
                        "The second city was destroyed.");

            Assert.That(secondWindow != null, Is.True,
                        "The second city's window component was destroyed.");

            Assert.That(secondWindow.Window != null, Is.True,
                        "The second city's UI content was destroyed.");

            windowSpace.ActiveWindow = secondWindow;

            yield return null;
            yield return null;

            Assert.That(windowSpace, Is.Not.Null,
                        "WindowSpace was destroyed unexpectedly.");

            Assert.That(sharedPanel != null, Is.True,
                        "The panel containing the second city was destroyed.");

            Assert.That(windowSpace.Windows.Contains(secondWindow), Is.True,
                        "The second city's window is no longer registered.");

            PanelTab remainingTab = sharedPanel.GetTab(
                (RectTransform)secondWindow.Window.transform);

            Assert.That(remainingTab, Is.Not.Null,
                        "The second city's tab is missing.");

            int remainingIndex = sharedPanel.GetTabIndex(remainingTab);

            Assert.That(remainingIndex, Is.GreaterThanOrEqualTo(0),
                        "The remaining tab has an invalid index.");

            Assert.That(sharedPanel.ActiveTab, Is.EqualTo(remainingIndex),
                        "The second city's tab was not activated.");

            Assert.That(windowSpace.ActiveWindow, Is.SameAs(secondWindow),
                        "The second city's window is not active.");
        }


        /// <summary>
        /// Verifies that recreating a panel does not register additional
        /// active-tab event handlers. A subsequent tab change must emit
        /// exactly one active-window notification.
        /// </summary>
        /// <returns>
        /// An enumerator that waits for panel removal and recreation.
        /// </returns>
        [UnityTest]
        [LoadScene]
        public IEnumerator ReopeningPanelsDoesNotDuplicateActiveWindowEvents()
        {
            windowOwner = new GameObject("Panel event registration test");

            WindowSpace windowSpace =
                windowOwner.AddComponent<WindowSpace>();

            WindowSpaceTestWindow firstWindow =
                windowOwner.AddComponent<WindowSpaceTestWindow>();
            firstWindow.Title = "Initial event test window";

            windowSpace.AddWindow(firstWindow);
            windowSpace.ActiveWindow = firstWindow;

            Panel firstPanel = null;

            for (int frame = 0;
                 frame < MaximumInitializationFrames && firstPanel == null;
                 frame++)
            {
                if (firstWindow.Window != null)
                {
                    foreach (Panel candidate in Object.FindObjectsByType<Panel>(
                                 FindObjectsInactive.Include,
                                 FindObjectsSortMode.None))
                    {
                        if (candidate != null
                            && candidate.GetTab(
                                (RectTransform)firstWindow.Window.transform) != null)
                        {
                            firstPanel = candidate;
                            break;
                        }
                    }
                }

                if (firstPanel == null)
                {
                    yield return null;
                }
            }

            Assert.That(firstPanel, Is.Not.Null,
                        "The initial panel was not created.");

            Assert.That(windowSpace.CloseWindow(firstWindow), Is.True);

            for (int frame = 0;
                 frame < MaximumInitializationFrames && firstPanel != null;
                 frame++)
            {
                yield return null;
            }

            Assert.That(firstPanel == null, Is.True,
                        "The initial panel was not destroyed.");

            WindowSpaceTestWindow secondWindow =
                windowOwner.AddComponent<WindowSpaceTestWindow>();
            secondWindow.Title = "Second event test window";

            WindowSpaceTestWindow thirdWindow =
                windowOwner.AddComponent<WindowSpaceTestWindow>();
            thirdWindow.Title = "Third event test window";

            windowSpace.AddWindow(secondWindow);
            windowSpace.AddWindow(thirdWindow);
            windowSpace.ActiveWindow = secondWindow;

            Panel reopenedPanel = null;
            PanelTab secondTab = null;
            PanelTab thirdTab = null;

            for (int frame = 0;
                 frame < MaximumInitializationFrames && reopenedPanel == null;
                 frame++)
            {
                if (secondWindow.Window != null && thirdWindow.Window != null)
                {
                    foreach (Panel candidate in Object.FindObjectsByType<Panel>(
                                 FindObjectsInactive.Include,
                                 FindObjectsSortMode.None))
                    {
                        if (candidate == null)
                        {
                            continue;
                        }

                        PanelTab candidateSecondTab = candidate.GetTab(
                            (RectTransform)secondWindow.Window.transform);

                        PanelTab candidateThirdTab = candidate.GetTab(
                            (RectTransform)thirdWindow.Window.transform);

                        if (candidateSecondTab != null
                            && candidateThirdTab != null)
                        {
                            reopenedPanel = candidate;
                            secondTab = candidateSecondTab;
                            thirdTab = candidateThirdTab;
                            break;
                        }
                    }
                }

                if (reopenedPanel == null)
                {
                    yield return null;
                }
            }

            Assert.That(reopenedPanel, Is.Not.Null,
                        "The panel was not recreated.");

            yield return null;

            int notifications = 0;

            windowSpace.OnActiveWindowChanged.AddListener(
                () => notifications++);

            int secondIndex = reopenedPanel.GetTabIndex(secondTab);
            int thirdIndex = reopenedPanel.GetTabIndex(thirdTab);

            Assert.That(secondIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(thirdIndex, Is.GreaterThanOrEqualTo(0));

            int targetIndex = reopenedPanel.ActiveTab == secondIndex
                ? thirdIndex
                : secondIndex;

            reopenedPanel.ActiveTab = targetIndex;

            Assert.That(notifications, Is.EqualTo(1),
                        "A single tab change triggered multiple notifications.");
        }

        /// <summary>
        /// Verifies that a destroyed WindowSpace unregisters its
        /// DynamicPanels event handlers. Tab changes after destruction
        /// must not invoke events on the obsolete WindowSpace.
        /// </summary>
        /// <returns>
        /// An enumerator that waits for window initialization and
        /// WindowSpace destruction.
        /// </returns>
        [UnityTest]
        [LoadScene]
        public IEnumerator DestroyedWindowSpaceDoesNotReceivePanelEvents()
        {
            windowOwner = new GameObject("WindowSpace event cleanup test");

            WindowSpace windowSpace =
                windowOwner.AddComponent<WindowSpace>();

            WindowSpaceTestWindow firstWindow =
                windowOwner.AddComponent<WindowSpaceTestWindow>();
            firstWindow.Title = "First cleanup test window";

            WindowSpaceTestWindow secondWindow =
                windowOwner.AddComponent<WindowSpaceTestWindow>();
            secondWindow.Title = "Second cleanup test window";

            windowSpace.AddWindow(firstWindow);
            windowSpace.AddWindow(secondWindow);
            windowSpace.ActiveWindow = firstWindow;

            Panel sharedPanel = null;
            PanelTab firstTab = null;
            PanelTab secondTab = null;

            for (int frame = 0;
                 frame < MaximumInitializationFrames && sharedPanel == null;
                 frame++)
            {
                if (firstWindow.Window != null && secondWindow.Window != null)
                {
                    foreach (Panel candidate in Object.FindObjectsByType<Panel>(
                                 FindObjectsInactive.Include,
                                 FindObjectsSortMode.None))
                    {
                        if (candidate == null)
                        {
                            continue;
                        }

                        PanelTab candidateFirstTab = candidate.GetTab(
                            (RectTransform)firstWindow.Window.transform);

                        PanelTab candidateSecondTab = candidate.GetTab(
                            (RectTransform)secondWindow.Window.transform);

                        if (candidateFirstTab != null
                            && candidateSecondTab != null)
                        {
                            sharedPanel = candidate;
                            firstTab = candidateFirstTab;
                            secondTab = candidateSecondTab;
                            break;
                        }
                    }
                }

                if (sharedPanel == null)
                {
                    yield return null;
                }
            }

            Assert.That(sharedPanel, Is.Not.Null,
                        "Both windows were not initialized.");

            yield return null;

            int notifications = 0;

            windowSpace.OnActiveWindowChanged.AddListener(
                () => notifications++);

            Destroyer.Destroy(windowSpace);

            yield return null;

            Assert.That(windowSpace == null, Is.True,
                        "WindowSpace was not destroyed.");

            Assert.That(sharedPanel != null, Is.True,
                        "The panel was unexpectedly destroyed.");

            int firstIndex = sharedPanel.GetTabIndex(firstTab);
            int secondIndex = sharedPanel.GetTabIndex(secondTab);

            Assert.That(firstIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(secondIndex, Is.GreaterThanOrEqualTo(0));

            int targetIndex = sharedPanel.ActiveTab == firstIndex
                ? secondIndex
                : firstIndex;

            sharedPanel.ActiveTab = targetIndex;

            Assert.That(notifications, Is.Zero,
                        "A destroyed WindowSpace still receives tab events.");
        }


        /// <summary>
        /// Verifies that closing one tab destroys its UI content
        /// without affecting another window in the same panel.
        /// </summary>
        /// <returns>
        /// An enumerator that waits for initialization and cleanup.
        /// </returns>
        [UnityTest]
        [LoadScene]
        public IEnumerator ClosingOneTabDestroysContentAndPreservesOtherWindow()
        {
            windowOwner = new GameObject("Shared panel content cleanup test");

            WindowSpace windowSpace =
                windowOwner.AddComponent<WindowSpace>();

            WindowSpaceTestWindow firstWindow =
                windowOwner.AddComponent<WindowSpaceTestWindow>();
            firstWindow.Title = "Window to close";

            WindowSpaceTestWindow secondWindow =
                windowOwner.AddComponent<WindowSpaceTestWindow>();
            secondWindow.Title = "Window to preserve";

            windowSpace.AddWindow(firstWindow);
            windowSpace.AddWindow(secondWindow);
            windowSpace.ActiveWindow = secondWindow;

            Panel sharedPanel = null;

            for (int frame = 0;
                 frame < MaximumInitializationFrames && sharedPanel == null;
                 frame++)
            {
                if (firstWindow.Window != null && secondWindow.Window != null)
                {
                    foreach (Panel candidate in Object.FindObjectsByType<Panel>(
                                 FindObjectsInactive.Include,
                                 FindObjectsSortMode.None))
                    {
                        if (candidate == null)
                        {
                            continue;
                        }

                        PanelTab firstTab = candidate.GetTab(
                            (RectTransform)firstWindow.Window.transform);

                        PanelTab secondTab = candidate.GetTab(
                            (RectTransform)secondWindow.Window.transform);

                        if (firstTab != null && secondTab != null)
                        {
                            sharedPanel = candidate;
                            break;
                        }
                    }
                }

                if (sharedPanel == null)
                {
                    yield return null;
                }
            }

            Assert.That(sharedPanel, Is.Not.Null,
                        "Both windows were not added to the same panel.");

            Assert.That(sharedPanel.NumberOfTabs, Is.EqualTo(2));

            GameObject closedContent = firstWindow.Window;
            GameObject preservedContent = secondWindow.Window;

            Assert.That(windowSpace.CloseWindow(firstWindow), Is.True);

            yield return null;
            yield return null;
            yield return null;

            Assert.That(sharedPanel != null, Is.True,
                        "The panel was destroyed despite another open tab.");

            Assert.That(sharedPanel.NumberOfTabs, Is.EqualTo(1),
                        "The closed tab was not removed.");

            Assert.That(preservedContent != null, Is.True,
                        "The remaining window's content was destroyed.");

            Assert.That(windowSpace.Windows.Contains(secondWindow), Is.True,
                        "The remaining window was unregistered.");

            Assert.That(sharedPanel.GetTab(
                            (RectTransform)preservedContent.transform),
                        Is.Not.Null,
                        "The remaining window lost its tab.");

            Assert.That(closedContent == null, Is.True,
                        "The closed window's UI GameObject was not destroyed.");
        }


        /// <summary>
        /// Verifies that destroying multiple tab contents in the same frame
        /// does not cause DynamicPanels to activate another destroyed tab.
        /// The entire panel must be cleaned up without an exception.
        /// </summary>
        /// <returns>
        /// An enumerator that waits for window initialization, destruction,
        /// and subsequent panel cleanup.
        /// </returns>
        [UnityTest]
        [LoadScene]
        public IEnumerator DestroyingMultipleTabContentsInOneFrameDoesNotThrow()
        {
            windowOwner = new GameObject("Multiple destroyed tab contents test");

            WindowSpace windowSpace =
                windowOwner.AddComponent<WindowSpace>();

            WindowSpaceTestWindow firstWindow =
                windowOwner.AddComponent<WindowSpaceTestWindow>();
            firstWindow.Title = "First destroyed tab";

            WindowSpaceTestWindow secondWindow =
                windowOwner.AddComponent<WindowSpaceTestWindow>();
            secondWindow.Title = "Second destroyed tab";

            windowSpace.AddWindow(firstWindow);
            windowSpace.AddWindow(secondWindow);
            windowSpace.ActiveWindow = secondWindow;

            Panel sharedPanel = null;
            PanelTab secondTab = null;

            for (int frame = 0;
                 frame < MaximumInitializationFrames && sharedPanel == null;
                 frame++)
            {
                if (firstWindow.Window != null
                    && secondWindow.Window != null)
                {
                    foreach (Panel candidate in Object.FindObjectsByType<Panel>(
                                 FindObjectsInactive.Include,
                                 FindObjectsSortMode.None))
                    {
                        if (candidate == null)
                        {
                            continue;
                        }

                        PanelTab firstTab = candidate.GetTab(
                            (RectTransform)firstWindow.Window.transform);

                        PanelTab candidateSecondTab = candidate.GetTab(
                            (RectTransform)secondWindow.Window.transform);

                        if (firstTab != null && candidateSecondTab != null)
                        {
                            sharedPanel = candidate;
                            secondTab = candidateSecondTab;
                            break;
                        }
                    }
                }

                if (sharedPanel == null)
                {
                    yield return null;
                }
            }

            Assert.That(sharedPanel, Is.Not.Null,
                        "Both windows were not initialized in the same panel.");

            Assert.That(sharedPanel.NumberOfTabs, Is.EqualTo(2));

            int secondIndex = sharedPanel.GetTabIndex(secondTab);

            Assert.That(secondIndex, Is.GreaterThanOrEqualTo(0));

            sharedPanel.ActiveTab = secondIndex;
            windowSpace.ActiveWindow = secondWindow;

            yield return null;

            Assert.That(sharedPanel.ActiveTab,
                        Is.EqualTo(sharedPanel.GetTabIndex(secondTab)),
                        "The second tab is not active before destruction.");

            GameObject firstContent = firstWindow.Window;
            GameObject secondContent = secondWindow.Window;

            Assert.That(firstContent, Is.Not.Null);
            Assert.That(secondContent, Is.Not.Null);

            // Both UI contents disappear in the same frame.
            Destroyer.Destroy(firstContent);
            Destroyer.Destroy(secondContent);

            yield return null;
            yield return null;
            yield return null;

            Assert.That(windowSpace != null, Is.True,
                        "WindowSpace was unexpectedly destroyed.");

            Assert.That(windowSpace.Windows.Count, Is.Zero,
                        "Destroyed windows are still registered.");

            Assert.That(sharedPanel == null, Is.True,
                        "The panel containing destroyed tabs was not removed.");
        }


        /// <summary>
        /// Verifies that closing the final window notifies synchronization
        /// listeners with a serializable empty window space.
        /// </summary>
        /// <returns>
        /// An enumerator that waits for window initialization,
        /// closing, and synchronization notification.
        /// </returns>
        [UnityTest]
        [LoadScene]
        public IEnumerator ClosingLastWindowCanSynchronizeEmptyWindowSpace()
        {
            windowOwner = new GameObject("Empty window space synchronization test");

            WindowSpace windowSpace =
                windowOwner.AddComponent<WindowSpace>();

            WindowSpaceTestWindow firstWindow =
                windowOwner.AddComponent<WindowSpaceTestWindow>();
            firstWindow.Title = "Last synchronized window";

            windowSpace.AddWindow(firstWindow);
            windowSpace.ActiveWindow = firstWindow;

            Panel firstPanel = null;

            for (int frame = 0;
                 frame < MaximumInitializationFrames && firstPanel == null;
                 frame++)
            {
                if (firstWindow.Window != null)
                {
                    foreach (Panel candidate in Object.FindObjectsByType<Panel>(
                                 FindObjectsInactive.Include,
                                 FindObjectsSortMode.None))
                    {
                        if (candidate != null
                            && candidate.GetTab(
                                (RectTransform)firstWindow.Window.transform)
                               != null)
                        {
                            firstPanel = candidate;
                            break;
                        }
                    }
                }

                if (firstPanel == null)
                {
                    yield return null;
                }
            }

            Assert.That(firstPanel, Is.Not.Null,
                        "The initial window panel was not created.");

            int synchronizationNotifications = 0;
            bool emptyStateSerialized = false;

            windowSpace.OnActiveWindowChanged.AddListener(() =>
            {
                if (windowSpace.ActiveWindow != null)
                {
                    return;
                }

                synchronizationNotifications++;

                // This is the serialization operation also performed
                // by SyncWindowSpaceAction.UpdateSpace().
                WindowSpace.WindowSpaceValues values =
                    windowSpace.ToValueObject();

                emptyStateSerialized =
                    values.Windows != null && values.Windows.Count == 0;
            });

            Assert.That(windowSpace.CloseWindow(firstWindow), Is.True,
                        "The final window could not be closed.");

            yield return null;
            yield return null;
            yield return null;

            Assert.That(windowSpace.Windows.Count, Is.Zero,
                        "The closed window is still registered.");

            Assert.That(windowSpace.ActiveWindow == null, Is.True,
                        "The active window was not cleared.");

            Assert.That(synchronizationNotifications,
                        Is.GreaterThanOrEqualTo(1),
                        "No synchronization notification was raised.");

            Assert.That(emptyStateSerialized, Is.True,
                        "The empty window space could not be serialized.");
        }

        /// <summary>
        /// Verifies that an empty window space survives Unity serialization
        /// and can be restored without an active window.
        /// </summary>
        /// <returns>
        /// An enumerator that waits for initialization of the restored space.
        /// </returns>
        [UnityTest]
        [LoadScene]
        public IEnumerator EmptyWindowSpaceCanBeSerializedAndRestored()
        {
            windowOwner = new GameObject("Empty window space serialization test");

            WindowSpace originalSpace =
                windowOwner.AddComponent<WindowSpace>();
            originalSpace.WindowSpaceName = "Empty window space source";

            WindowSpace.WindowSpaceValues originalValues =
                originalSpace.ToValueObject();

            Assert.That(originalValues.HasActiveWindow, Is.False,
                        "An empty space must not have an active window.");

            Assert.That(originalValues.Windows, Is.Empty,
                        "An empty space must not contain window values.");

            // Exercise Unity serialization rather than passing only the
            // original in-memory value object.
            string serializedValues = JsonUtility.ToJson(originalValues);

            WindowSpace.WindowSpaceValues receivedValues =
                JsonUtility.FromJson<WindowSpace.WindowSpaceValues>(
                    serializedValues);

            Assert.That(receivedValues.HasActiveWindow, Is.False,
                        "The empty active-window state was not preserved.");

            Assert.That(receivedValues.Windows, Is.Not.Null,
                        "The received window list is missing.");

            Assert.That(receivedValues.Windows, Is.Empty,
                        "The received space unexpectedly contains windows.");

            GameObject receivingOwner =
                new GameObject("Empty window space receiver");

            receivingOwner.transform.SetParent(windowOwner.transform, false);

            WindowSpace restoredSpace =
                WindowSpace.FromValueObject(receivedValues, receivingOwner);

            restoredSpace.WindowSpaceName = "Empty window space receiver UI";

            Assert.That(restoredSpace.Windows, Is.Empty,
                        "The restored space unexpectedly contains windows.");

            Assert.That(restoredSpace.ActiveWindow, Is.Null,
                        "The restored space unexpectedly has an active window.");

            yield return null;

            Assert.That(restoredSpace, Is.Not.Null,
                        "The restored WindowSpace was destroyed.");
        }
    }
}

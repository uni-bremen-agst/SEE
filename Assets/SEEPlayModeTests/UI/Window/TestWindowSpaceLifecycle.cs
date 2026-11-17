
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
    }
}

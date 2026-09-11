using Michsky.UI.ModernUIPack;
using NUnit.Framework;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Utils;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using static SEE.Game.Drawable.ActionHelpers.LineCapPointsCalculator;
using static SEE.Game.Drawable.GameDrawer;

namespace SEE.UI.Menu.Drawable.Line
{
    /// <summary>
    /// Play-mode integration tests for <see cref="LineCapMenu"/>.
    /// </summary>
    /// <remarks>
    /// These tests are currently blocked by issue #983 because the play-mode
    /// test environment does not provide the SEE scene objects required by
    /// drawable menus.
    /// </remarks>
    [Ignore("Blocked by #983: required SEE UI scene objects are unavailable in play-mode tests.")]
    internal class TestLineCapMenu : TestUI
    {
        /// <summary>
        /// Path of the real line-menu prefab.
        /// </summary>
        private const string LineMenuPrefab =
            "Prefabs/UI/Drawable/LineMenu";

        /// <summary>
        /// Selector index of the main line.
        /// </summary>
        private const int MainIndex = 0;

        /// <summary>
        /// Selector index of the start cap.
        /// </summary>
        private const int StartCapIndex = 1;

        /// <summary>
        /// Selector index of the end cap.
        /// </summary>
        private const int EndCapIndex = 2;

        /// <summary>
        /// The instantiated line-menu prefab.
        /// </summary>
        private GameObject lineMenuObject;

        /// <summary>
        /// The line-cap menu being tested.
        /// </summary>
        private LineCapMenu lineCapMenu;

        /// <summary>
        /// The segment selector of the real prefab.
        /// </summary>
        private HorizontalSelector segmentSelector;

        /// <summary>
        /// The segment text object.
        /// </summary>
        private GameObject segmentText;

        /// <summary>
        /// The segment selection object.
        /// </summary>
        private GameObject segmentSelection;

        /// <summary>
        /// The line-cap text object.
        /// </summary>
        private GameObject lineCapText;

        /// <summary>
        /// The line-cap selection object.
        /// </summary>
        private GameObject lineCapSelection;

        /// <summary>
        /// Instantiates the real prefab and initializes a fresh
        /// <see cref="LineCapMenu"/>.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnitySetUp]
        public IEnumerator SetUpLineCapMenu()
        {
            lineMenuObject = Menu.InstantiatePrefab(LineMenuPrefab);

            lineCapMenu = new LineCapMenu(
                lineMenuObject,
                () => true);

            segmentText =
                GameFinder.FindAttachedOrLocalDescendant(
                    lineMenuObject,
                    "SegmentText");

            segmentSelection =
                GameFinder.FindAttachedOrLocalDescendant(
                    lineMenuObject,
                    "SegmentSelection");

            lineCapText =
                GameFinder.FindAttachedOrLocalDescendant(
                    lineMenuObject,
                    "LineCapText");

            lineCapSelection =
                GameFinder.FindAttachedOrLocalDescendant(
                    lineMenuObject,
                    "LineCapSelection");

            segmentSelector =
                segmentSelection.GetComponent<HorizontalSelector>();

            yield return null;
        }

        /// <summary>
        /// Removes listeners and destroys the test prefab.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTearDown]
        public IEnumerator TearDownLineCapMenu()
        {
            lineCapMenu?.RemoveListeners();

            if (lineMenuObject != null)
            {
                Destroyer.Destroy(lineMenuObject);
            }

            yield return null;
        }

        /// <summary>
        /// Verifies that a newly initialized line-cap menu starts with the main
        /// line selected.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTest]
        public IEnumerator TestInitialSegmentIsMain()
        {
            LineConf line = CreateLine();

            Assert.That(lineCapMenu.IsMainSelected, Is.True);
            Assert.That(lineCapMenu.IsStartCapSelected, Is.False);
            Assert.That(lineCapMenu.IsEndCapSelected, Is.False);
            Assert.That(lineCapMenu.GetSelectedCapConf(line), Is.Null);

            yield return null;
        }

        /// <summary>
        /// Verifies that segment-selection visibility can be controlled by
        /// <see cref="LineCapMenu"/>.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTest]
        public IEnumerator TestSegmentVisibility()
        {
            lineCapMenu.EnableSegment();

            Assert.That(segmentText.activeSelf, Is.True);
            Assert.That(segmentSelection.activeSelf, Is.True);

            lineCapMenu.DisableSegment();

            Assert.That(segmentText.activeSelf, Is.False);
            Assert.That(segmentSelection.activeSelf, Is.False);

            yield return null;
        }

        /// <summary>
        /// Verifies that line-cap-selection visibility can be controlled by
        /// <see cref="LineCapMenu"/>.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTest]
        public IEnumerator TestLineCapVisibility()
        {
            lineCapMenu.EnableLineCap();

            Assert.That(lineCapText.activeSelf, Is.True);
            Assert.That(lineCapSelection.activeSelf, Is.True);

            lineCapMenu.DisableLineCap();

            Assert.That(lineCapText.activeSelf, Is.False);
            Assert.That(lineCapSelection.activeSelf, Is.False);

            yield return null;
        }

        /// <summary>
        /// Verifies that selecting start and end segments exposes the
        /// corresponding cap configurations.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTest]
        public IEnumerator TestSegmentSelectionReturnsExpectedCap()
        {
            LineConf line = CreateLine();

            SetUpSegmentEditing(line);

            SelectSegment(StartCapIndex);

            Assert.That(lineCapMenu.IsStartCapSelected, Is.True);
            Assert.That(
                lineCapMenu.GetSelectedCapConf(line),
                Is.SameAs(line.LineCapStart));

            SelectSegment(EndCapIndex);

            Assert.That(lineCapMenu.IsEndCapSelected, Is.True);
            Assert.That(
                lineCapMenu.GetSelectedCapConf(line),
                Is.SameAs(line.LineCapEnd));

            yield return null;
        }

        /// <summary>
        /// Verifies that returning to the main line invokes the callback used
        /// to hide line-cap editing.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTest]
        public IEnumerator TestReturningToMainDisablesLineCapEditing()
        {
            LineConf line = CreateLine();
            int disableCalls = 0;

            lineCapMenu.BeginEditing(line);

            lineCapMenu.SetUpSegmentEditing(
                line,
                () => disableCalls++,
                selectedCap => { },
                () => { },
                () => { });

            SelectSegment(StartCapIndex);

            Assert.That(disableCalls, Is.EqualTo(0));

            SelectSegment(MainIndex);

            Assert.That(disableCalls, Is.EqualTo(1));
            Assert.That(lineCapMenu.IsMainSelected, Is.True);

            yield return null;
        }

        /// <summary>
        /// Verifies that changing a segment refreshes the editing UI and passes
        /// the selected cap kind to the line options.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTest]
        public IEnumerator TestSegmentSelectionInvokesCallbacks()
        {
            LineConf line = CreateLine();

            int refreshCalls = 0;
            int resetCalls = 0;
            LineCap selectedCap = LineCap.None;

            lineCapMenu.BeginEditing(line);

            lineCapMenu.SetUpSegmentEditing(
                line,
                () => { },
                cap => selectedCap = cap,
                () => refreshCalls++,
                () => resetCalls++);

            SelectSegment(StartCapIndex);

            Assert.That(selectedCap, Is.EqualTo(LineCap.Arrow));
            Assert.That(refreshCalls, Is.EqualTo(1));
            Assert.That(resetCalls, Is.EqualTo(1));

            SelectSegment(EndCapIndex);

            Assert.That(selectedCap, Is.EqualTo(LineCap.Aggregation));
            Assert.That(refreshCalls, Is.EqualTo(2));
            Assert.That(resetCalls, Is.EqualTo(2));

            yield return null;
        }

        /// <summary>
        /// Verifies that resetting the menu restores the main segment and its
        /// selector index.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTest]
        public IEnumerator TestResetRestoresMainSegment()
        {
            LineConf line = CreateLine();

            SetUpSegmentEditing(line);

            SelectSegment(EndCapIndex);

            Assert.That(lineCapMenu.IsEndCapSelected, Is.True);

            lineCapMenu.Reset();

            Assert.That(lineCapMenu.IsMainSelected, Is.True);
            Assert.That(segmentSelector.index, Is.EqualTo(MainIndex));

            yield return null;
        }

        /// <summary>
        /// Verifies that removing listeners prevents subsequent selector events
        /// from changing the selected segment.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTest]
        public IEnumerator TestRemoveListenersStopsSegmentChanges()
        {
            LineConf line = CreateLine();

            SetUpSegmentEditing(line);

            lineCapMenu.RemoveListeners();

            SelectSegment(StartCapIndex);

            Assert.That(lineCapMenu.IsMainSelected, Is.True);
            Assert.That(lineCapMenu.IsStartCapSelected, Is.False);

            yield return null;
        }

        /// <summary>
        /// Configures segment editing using no-op callbacks.
        /// </summary>
        /// <param name="line">The line configuration being edited.</param>
        private void SetUpSegmentEditing(LineConf line)
        {
            lineCapMenu.BeginEditing(line);

            lineCapMenu.SetUpSegmentEditing(
                line,
                () => lineCapMenu.DisableLineCap(),
                selectedCap => { },
                () => { },
                () => { });
        }

        /// <summary>
        /// Simulates selecting a segment through the real horizontal selector.
        /// </summary>
        /// <param name="index">The selector index to select.</param>
        private void SelectSegment(int index)
        {
            segmentSelector.index = index;
            segmentSelector.selectorEvent.Invoke(index);
        }

        /// <summary>
        /// Creates a line configuration containing distinct start and end caps.
        /// </summary>
        /// <returns>The created line configuration.</returns>
        private static LineConf CreateLine()
        {
            return new LineConf
            {
                PrimaryColor = Color.red,
                SecondaryColor = Color.blue,
                Thickness = 0.2f,
                LineKind = LineKind.Dashed,
                ColorKind = ColorKind.Gradient,
                Tiling = 2.0f,
                FillOutStatus = true,
                FillOutColor = Color.green,

                LineCapStart = CreateCap(
                    LineCap.Arrow,
                    Color.red),

                LineCapEnd = CreateCap(
                    LineCap.Aggregation,
                    Color.blue)
            };
        }

        /// <summary>
        /// Creates a line-cap configuration.
        /// </summary>
        /// <param name="capKind">The cap kind.</param>
        /// <param name="color">The primary cap color.</param>
        /// <returns>The created line-cap configuration.</returns>
        private static LineCapConf CreateCap(
            LineCap capKind,
            Color color)
        {
            return new LineCapConf
            {
                CapKind = capKind,
                ColorKind = ColorKind.Monochrome,
                PrimaryColor = color,
                SecondaryColor = Color.clear,
                Thickness = 0.15f,
                LineKind = LineKind.Solid,
                Tiling = 1.0f,
                FillOutStatus = false,
                FillOutColor = Color.clear,
                UseOwnVisuals = true
            };
        }
    }
}

using NUnit.Framework;
using SEE.Game.Drawable;
using SEE.Utils;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Play-mode integration tests for <see cref="LineMenuControls"/>.
    /// </summary>
    /// <remarks>
    /// These tests are currently blocked by issue #983 because the play-mode
    /// test environment does not provide the SEE scene objects required by
    /// drawable menus.
    /// </remarks>
    [Ignore("Blocked by #983: required SEE UI scene objects are unavailable in play-mode tests.")]
    internal class TestLineMenuControls : TestUI
    {
        /// <summary>
        /// Path of the real line-menu prefab.
        /// </summary>
        private const string LineMenuPrefab =
            "Prefabs/UI/Drawable/LineMenu";

        /// <summary>
        /// The instantiated line-menu prefab.
        /// </summary>
        private GameObject lineMenuObject;

        /// <summary>
        /// Instantiates the real line-menu prefab before every test.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnitySetUp]
        public IEnumerator SetUpLineMenu()
        {
            lineMenuObject = Menu.InstantiatePrefab(LineMenuPrefab);

            yield return null;
        }

        /// <summary>
        /// Destroys the instantiated line-menu prefab after every test.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTearDown]
        public IEnumerator TearDownLineMenu()
        {
            if (lineMenuObject != null)
            {
                Destroyer.Destroy(lineMenuObject);
            }

            yield return null;
        }

        /// <summary>
        /// Verifies that every hierarchy object required by
        /// <see cref="LineMenuControls"/> still exists in the real prefab.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTest]
        public IEnumerator TestRequiredPrefabObjectsExist()
        {
            string[] requiredObjects =
            {
                "LineKindSelection",
                "LineKindText",
                "ColorKindSelection",
                "Tiling",
                "Layer",
                "Thickness",
                "Loop",
                "PrimaryColorBtn",
                "SecondaryColorBtn",
                "ColorKindBtn",
                "FillOutBtn",
                "FillOut",
                "ColorAreaSelector",
                "ColorTypeSelector",
                "Picker 2.0",
                "ReturnBtn",
                "Dragger"
            };

            foreach (string objectName in requiredObjects)
            {
                GameObject result =
                    GameFinder.FindAttachedOrLocalDescendant(
                        lineMenuObject,
                        objectName);

                Assert.That(
                    result,
                    Is.Not.Null,
                    $"Required line-menu object '{objectName}' was not found.");
            }

            yield return null;
        }

        /// <summary>
        /// Verifies that all shared controls can be resolved from the real
        /// line-menu prefab.
        /// </summary>
        /// <returns>An enumerator used by the Unity test runner.</returns>
        [UnityTest]
        public IEnumerator TestSharedControlsCanBeResolved()
        {
            LineMenuControls controls =
                new LineMenuControls(lineMenuObject);

            Assert.That(controls.LineKindSelectionObject, Is.Not.Null);
            Assert.That(controls.LineKindTextObject, Is.Not.Null);
            Assert.That(controls.LineKindSelector, Is.Not.Null);

            Assert.That(controls.ColorKindSelectionObject, Is.Not.Null);
            Assert.That(controls.ColorKindSelector, Is.Not.Null);

            Assert.That(controls.TilingObject, Is.Not.Null);
            Assert.That(controls.TilingSlider, Is.Not.Null);

            Assert.That(controls.LayerObject, Is.Not.Null);
            Assert.That(controls.LayerSlider, Is.Not.Null);
            Assert.That(controls.LayerSliderController, Is.Not.Null);

            Assert.That(controls.ThicknessObject, Is.Not.Null);
            Assert.That(controls.ThicknessSlider, Is.Not.Null);

            Assert.That(controls.LoopObject, Is.Not.Null);
            Assert.That(controls.LoopManager, Is.Not.Null);

            Assert.That(controls.PrimaryColorButtonManager, Is.Not.Null);
            Assert.That(
                controls.PrimaryColorButtonManager.buttonVar,
                Is.Not.Null);

            Assert.That(controls.SecondaryColorButtonManager, Is.Not.Null);
            Assert.That(
                controls.SecondaryColorButtonManager.buttonVar,
                Is.Not.Null);

            Assert.That(controls.ColorKindButtonManager, Is.Not.Null);
            Assert.That(
                controls.ColorKindButtonManager.buttonVar,
                Is.Not.Null);

            Assert.That(controls.FillOutButtonManager, Is.Not.Null);
            Assert.That(
                controls.FillOutButtonManager.buttonVar,
                Is.Not.Null);

            Assert.That(controls.FillOutObject, Is.Not.Null);
            Assert.That(controls.FillOutManager, Is.Not.Null);

            Assert.That(controls.ColorAreaSelectorObject, Is.Not.Null);
            Assert.That(controls.ColorTypeSelectorObject, Is.Not.Null);

            Assert.That(controls.ColorPickerObject, Is.Not.Null);
            Assert.That(controls.ColorPicker, Is.Not.Null);

            Assert.That(controls.ReturnButtonObject, Is.Not.Null);
            Assert.That(controls.ReturnButtonManager, Is.Not.Null);

            Assert.That(controls.WindowDragger, Is.Not.Null);

            yield return null;
        }
    }
}

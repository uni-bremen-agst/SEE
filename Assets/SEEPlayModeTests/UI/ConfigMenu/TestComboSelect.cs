using Michsky.MUIP;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Tests for configuration-menu input controls.
/// </summary>
namespace SEE.UI.ConfigMenu
{
    /// <summary>
    /// Tests dropdown rebuilding and reactivation through the configuration control.
    /// </summary>
    [Category("ModernUIPack")]
    internal class TestComboSelect
    {
        /// <summary>
        /// Owns all objects created by a test.
        /// </summary>
        private GameObject root;

        /// <summary>
        /// Creates a canvas for the control.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            root = new("Combo selection test", typeof(Canvas));
        }

        /// <summary>
        /// Removes the control even when an assertion fails.
        /// </summary>
        /// <returns>An enumerator that waits for object destruction.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(root);
            yield return null;
        }

        /// <summary>
        /// Checks that shrinking or clearing options keeps the selected value readable.
        /// </summary>
        /// <returns>An enumerator that waits for queued value updates.</returns>
        /// <exception cref="AssertionException">An option rebuild leaves an invalid index or value.</exception>
        [UnityTest]
        public IEnumerator OptionsCanShrinkAndRecoverFromAnEmptyList()
        {
            ComboSelect combo = ComboSelectBuilder.Init(root.transform)
                .SetLabel("Selection")
                .SetComboSelectMode(ComboSelectMode.Restricted)
                .SetOnChangeHandler(_ => { })
                .SetAllowedValues(new[] { "First", "Second", "Third" })
                .SetDefaultValue("Third").Build();
            yield return null;
            yield return null;
            CustomDropdown dropdown = combo.GetComponentInChildren<CustomDropdown>(true);
            Assert.That(combo.Value, Is.EqualTo("Third"));
            combo.Values = new[] { "First" };
            combo.Value = "First";
            yield return null;
            Assert.That(combo.Value, Is.EqualTo("First"));
            combo.Values = System.Array.Empty<string>();
            yield return null;
            Assert.That(dropdown.items, Is.Empty);
            Assert.That(dropdown.selectedText.text, Is.Empty);
            Assert.That(combo.Value, Is.Empty);
            combo.Values = new[] { "Recovered" };
            combo.Value = "Recovered";
            yield return null;
            Assert.That(combo.Value, Is.EqualTo("Recovered"));
        }

        /// <summary>
        /// Checks that hiding an expanded control resets its list and outside-click trigger.
        /// </summary>
        /// <returns>An enumerator that waits for initialization and reactivation.</returns>
        /// <exception cref="AssertionException">The control keeps an open state or trigger after reactivation.</exception>
        [UnityTest]
        public IEnumerator ReopeningAnExpandedControlStartsClosed()
        {
            ComboSelect combo = ComboSelectBuilder.Init(root.transform)
                .SetLabel("Selection").SetOnChangeHandler(_ => { })
                .SetAllowedValues(new[] { "First", "Second" }).SetDefaultValue("First").Build();
            yield return null;
            yield return null;
            CustomDropdown dropdown = combo.GetComponentInChildren<CustomDropdown>(true);
            dropdown.Animate();
            Assert.That(dropdown.isOn, Is.True);
            combo.gameObject.SetActive(false);
            combo.gameObject.SetActive(true);
            yield return null;
            Assert.That(dropdown.isOn, Is.False);
            Assert.That(dropdown.triggerObject.activeSelf, Is.False);
            dropdown.Animate();
            Assert.That(dropdown.isOn, Is.True, "The first click after reopening must expand the list.");
        }
    }
}

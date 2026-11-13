
using NUnit.Framework;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using UnityEngine;

namespace SEE.Game.Drawable.Line
{
    /// <summary>
    /// Tests the extraction of line colors without implicitly
    /// instantiating renderer materials in EditMode.
    /// </summary>
    [TestFixture]
    public class TestLineVisualConfFactory
    {
        /// <summary>
        /// The temporary line object.
        /// </summary>
        private GameObject line;

        /// <summary>
        /// The line renderer whose configuration is read.
        /// </summary>
        private LineRenderer renderer;

        /// <summary>
        /// The first independently owned test material.
        /// </summary>
        private Material primaryMaterial;

        /// <summary>
        /// The second independently owned test material.
        /// </summary>
        private Material secondaryMaterial;

        /// <summary>
        /// Creates a minimal valid line and its test materials.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            line = new GameObject("LineVisualConfTestLine")
            {
                tag = Tags.Line
            };

            renderer = line.AddComponent<LineRenderer>();
            renderer.useWorldSpace = false;
            renderer.positionCount = 2;
            renderer.SetPosition(0, Vector3.zero);
            renderer.SetPosition(1, Vector3.right);

            line.AddComponent<LineValueHolder>();
            line.AddComponent<LineCapValueHolder>();
            line.AddComponent<LineAnchorValueHolder>();
            line.AddComponent<OrderInLayerValueHolder>();
            line.AddComponent<AssociatedPageHolder>();

            Shader shader = Shader.Find("Unlit/Color");

            Assert.That(shader, Is.Not.Null,
                "The built-in Unlit/Color shader is required.");

            primaryMaterial = new Material(shader)
            {
                color = Color.red
            };

            secondaryMaterial = new Material(shader)
            {
                color = Color.blue
            };
        }

        /// <summary>
        /// Destroys all temporary objects and owned materials.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (line != null)
            {
                Object.DestroyImmediate(line);
            }

            if (primaryMaterial != null)
            {
                Object.DestroyImmediate(primaryMaterial);
            }

            if (secondaryMaterial != null)
            {
                Object.DestroyImmediate(secondaryMaterial);
            }
        }

        /// <summary>
        /// Verifies that a monochrome line's edited material color
        /// is extracted without changing its material assignment.
        /// </summary>
        [Test]
        public void TestMonochromeExtractionPreservesColor()
        {
            ConfigureLine(ColorKind.Monochrome, LineKind.Solid);

            renderer.sharedMaterial = primaryMaterial;

            Color expected = new(0.2f, 0.6f, 0.9f, 1f);
            primaryMaterial.color = expected;

            int materialIdBefore =
                renderer.sharedMaterial.GetInstanceID();

            LineConf configuration = LineConf.GetLine(line);

            Assert.That(configuration, Is.Not.Null);
            Assert.That(configuration.ColorKind,
                Is.EqualTo(ColorKind.Monochrome));

            AssertColor(expected, configuration.PrimaryColor);
            AssertColor(Color.clear, configuration.SecondaryColor);

            Assert.That(
                renderer.sharedMaterial.GetInstanceID(),
                Is.EqualTo(materialIdBefore));
        }

        /// <summary>
        /// Verifies that both colors of a two-colored dashed
        /// line are extracted from its assigned materials.
        /// </summary>
        [Test]
        public void TestTwoDashedExtractionPreservesColors()
        {
            ConfigureLine(ColorKind.TwoDashed, LineKind.Dashed50);

            renderer.sharedMaterials = new[]
            {
                primaryMaterial,
                secondaryMaterial
            };

            Color primary = new(0.2f, 0.8f, 0.3f, 1f);
            Color secondary = new(0.8f, 0.2f, 0.7f, 1f);

            primaryMaterial.color = primary;
            secondaryMaterial.color = secondary;

            Material[] materialsBefore = renderer.sharedMaterials;

            int firstId = materialsBefore[0].GetInstanceID();
            int secondId = materialsBefore[1].GetInstanceID();

            LineConf configuration = LineConf.GetLine(line);

            Assert.That(configuration, Is.Not.Null);
            Assert.That(configuration.ColorKind,
                Is.EqualTo(ColorKind.TwoDashed));

            AssertColor(primary, configuration.PrimaryColor);
            AssertColor(secondary, configuration.SecondaryColor);

            Material[] materialsAfter = renderer.sharedMaterials;

            Assert.That(materialsAfter, Has.Length.EqualTo(2));
            Assert.That(materialsAfter[0].GetInstanceID(),
                Is.EqualTo(firstId));
            Assert.That(materialsAfter[1].GetInstanceID(),
                Is.EqualTo(secondId));
        }

        /// <summary>
        /// Verifies that gradient colors are extracted from the
        /// renderer's start and end color properties.
        /// </summary>
        [Test]
        public void TestGradientExtractionPreservesColors()
        {
            ConfigureLine(ColorKind.Gradient, LineKind.Solid);

            renderer.sharedMaterial = primaryMaterial;

            Color primary = new(0.3f, 0.7f, 0.2f, 1f);
            Color secondary = new(0.7f, 0.2f, 0.8f, 1f);

            renderer.startColor = primary;
            renderer.endColor = secondary;

            int materialIdBefore =
                renderer.sharedMaterial.GetInstanceID();

            LineConf configuration = LineConf.GetLine(line);

            Assert.That(configuration, Is.Not.Null);
            Assert.That(configuration.ColorKind,
                Is.EqualTo(ColorKind.Gradient));

            AssertColor(primary, configuration.PrimaryColor);
            AssertColor(secondary, configuration.SecondaryColor);

            Assert.That(
                renderer.sharedMaterial.GetInstanceID(),
                Is.EqualTo(materialIdBefore));
        }

        /// <summary>
        /// Sets the color and line kinds stored on the test object.
        /// </summary>
        /// <param name="colorKind">The selected color kind.</param>
        /// <param name="lineKind">The selected line kind.</param>
        private void ConfigureLine(ColorKind colorKind, LineKind lineKind)
        {
            LineValueHolder holder =
                line.GetComponent<LineValueHolder>();

            holder.ColorKind = colorKind;
            holder.LineKind = lineKind;
        }

        /// <summary>
        /// Compares all color components with a small tolerance.
        /// </summary>
        /// <param name="expected">The expected color.</param>
        /// <param name="actual">The extracted color.</param>
        private static void AssertColor(Color expected, Color actual)
        {
            Assert.That(actual.r,
                Is.EqualTo(expected.r).Within(0.005f));
            Assert.That(actual.g,
                Is.EqualTo(expected.g).Within(0.005f));
            Assert.That(actual.b,
                Is.EqualTo(expected.b).Within(0.005f));
            Assert.That(actual.a,
                Is.EqualTo(expected.a).Within(0.005f));
        }
    }
}

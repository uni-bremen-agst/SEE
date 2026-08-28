using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SEE.Game;
using SEE.Game.City;
using SEE.GraphProviders;
using SEE.Tools.RandomGraphs;
using SEE.Utils.Config;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Test cases for ConfigIO.
    /// </summary>
    internal class TestConfigIO : AbstractTestConfigIO
    {
        [Test]
        public void TestConfigParseInteger1()
        {
            Dictionary<string, object> expected = new()
            {
                { "label", 0 }
            };
            Assert.That(ConfigReader.Parse("label : 0;\n"), Is.EquivalentTo(expected));
        }

        [Test]
        public void TestConfigParseInteger2()
        {
            Dictionary<string, object> expected = new()
            {
                { "l", -1 }
            };
            Assert.That(ConfigReader.Parse("l : -1;"), Is.EquivalentTo(expected));
        }

        [Test]
        public void TestConfigParseInteger3()
        {
            Dictionary<string, object> expected = new()
            {
                { "label", 123 }
            };
            Assert.That(ConfigReader.Parse("label : +123;"), Is.EquivalentTo(expected));
        }

        [Test]
        public void TestConfigParseFloat1()
        {
            Dictionary<string, object> expected = new()
            {
                { "label", 123.0f }
            };
            Assert.That(ConfigReader.Parse("label: +123.0;"), Is.EquivalentTo(expected));
        }

        [Test]
        public void TestConfigParseFloat2()
        {
            Dictionary<string, object> expected = new()
            {
                { "label", -1234.0f }
            };
            Assert.That(ConfigReader.Parse("label : -1,234.00;"), Is.EquivalentTo(expected));
        }

        [Test]
        public void TestConfigParseFloat3()
        {
            Dictionary<string, object> expected = new()
            {
                { "label", 1.234567E-06f }
            };
            Assert.That(ConfigReader.Parse("label : 1.234567E-06 ;"), Is.EquivalentTo(expected));
        }

        [Test]
        public void TestConfigParseFloat4()
        {
            Dictionary<string, object> expected = new()
            {
                { "label", -1.234567e-1f }
            };
            Assert.That(ConfigReader.Parse("label\t: -1.234567e-1;\r"), Is.EquivalentTo(expected));
        }

        [Test]
        public void TestConfigParseInfinity()
        {
            const float value = float.PositiveInfinity;
            Dictionary<string, object> expected = new()
            {
                { "label", value }
            };
            Assert.That(ConfigReader.Parse($"label\t: {value.ToString("F8", System.Globalization.CultureInfo.InvariantCulture)};\r"), Is.EquivalentTo(expected));
        }

        [Test]
        public void TestConfigParseNegativeInfinity()
        {
            const float value = float.NegativeInfinity;
            Dictionary<string, object> expected = new()
            {
                { "label", value }
            };
            Assert.That(ConfigReader.Parse($"label\t: {value.ToString("F8", System.Globalization.CultureInfo.InvariantCulture)};\r"), Is.EquivalentTo(expected));
        }

        [Test]
        public void TestConfigParseString1()
        {
            Dictionary<string, object> expected = new()
            {
                { "label", "hello" }
            };
            Assert.That(ConfigReader.Parse("label : \"hello\";"), Is.EquivalentTo(expected));
        }

        [Test]
        public void TestConfigParseString3()
        {
            Dictionary<string, object> expected = new()
            {
                { "label", "" }
            };
            Assert.That(ConfigReader.Parse("label : \"\";"), Is.EquivalentTo(expected));
        }

        [Test]
        public void TestConfigParseString4()
        {
            Dictionary<string, object> expected = new()
            {
                { "label", "\"" }
            };
            Assert.That(ConfigReader.Parse("label : \"\"\"\";"), Is.EquivalentTo(expected));
        }

        [Test]
        public void TestConfigParseString2()
        {
            Dictionary<string, object> expected = new()
            {
                { "label", "\"hello, world\"" }
            };
            Assert.That(ConfigReader.Parse("label : \"\"\"hello, world\"\"\";"), Is.EquivalentTo(expected));
        }

        [Test]
        public void TestConfigParseTrue()
        {
            Dictionary<string, object> expected = new()
            {
                { "label", true }
            };
            Assert.That(ConfigReader.Parse("label : true;"), Is.EquivalentTo(expected));
        }

        [Test]
        public void TestConfigParseFalse()
        {
            Dictionary<string, object> expected = new()
            {
                { "label", false }
            };
            Assert.That(ConfigReader.Parse("label : false;"), Is.EquivalentTo(expected));
        }

        [Test]
        public void TestConfigParseAttribute1()
        {
            Dictionary<string, object> expected = new()
            {
                { "attr", new Dictionary<string, object>() { { "int", 1 } } }
            };
            Assert.That(ConfigReader.Parse("attr : { int: 1; };"), Is.EquivalentTo(expected));
        }

        [Test]
        public void TestConfigParseAttribute2()
        {
            Dictionary<string, object> expected = new()
            {
                { "attr", new Dictionary<string, object>() }
            };
            Dictionary<string, object> actual = ConfigReader.Parse("attr : { };");
            Assert.That(actual, Is.EquivalentTo(expected));
        }

        [Test]
        public void TestConfigParseAttribute3()
        {
            Dictionary<string, object> expected = new()
            {
                { "attr", new Dictionary<string, object>() { { "int", 1 }, { "x", "hello" } } }
            };
            Assert.That(ConfigReader.Parse("attr : { int: 1; x : \"hello\"; };"), Is.EquivalentTo(expected));
        }


        [Test]
        public void TestConfigParseAttribute4()
        {
            Dictionary<string, object> expected = new()
            {
                { "attr", new Dictionary<string, object>() { { "x", new Dictionary<string, object>() } } }
            };
            Assert.That(ConfigReader.Parse("attr : { x: {}; };"), Is.EquivalentTo(expected));
        }

        [Test]
        public void TestConfigParseAttribute5()
        {
            Dictionary<string, object> expected = new()
            {
                { "attr", new Dictionary<string, object>() { { "a", 1 }, { "b", 2 }, { "x", new Dictionary<string, object>() { { "y", true }, { "z", false } } } } }
            };
            Assert.That(ConfigReader.Parse("attr : { a: 1; b: 2; x: {y : true; z : false;}; };"), Is.EquivalentTo(expected));
        }

        [Test]
        public void TestConfigParseList1()
        {
            Dictionary<string, object> expected = new()
            {
                { "list", new List<object>() { } }
            };
            Assert.That(ConfigReader.Parse("list : [];"), Is.EquivalentTo(expected));
        }

        [Test]
        public void TestConfigParseList2()
        {
            Dictionary<string, object> expected = new()
            {
                { "list", new List<object>() { 1, 2, 3 } }
            };
            Assert.That(ConfigReader.Parse("list : [ 1; 2; 3;];"), Is.EquivalentTo(expected));
        }

        [Test]
        public void TestConfigParseList3()
        {
            Dictionary<string, object> expected = new()
            {
                { "list", new List<object>() { true} }
            };
            Assert.That(ConfigReader.Parse("list : [ true; ];"), Is.EquivalentTo(expected));
        }

        [Test]
        public void TestConfigParseList4()
        {
            Dictionary<string, object> expected = new()
            {
                { "list", new List<object>() { new List<object>(), new List<object>() { 1 }, new List<object>() { 1, 2 } } }
            };
            Assert.That(ConfigReader.Parse("list : [ []; [1;]; [1; 2;];];"), Is.EquivalentTo(expected));
        }

        /// <summary>
        /// Test for empty <see cref="ColorMap"/>.
        /// </summary>
        [Test]
        public void TestMetricColorMapZeroElements()
        {
            string filename = Path.GetTempFileName();
            const string label = "metricMap";

            try
            {
                ColorMap saved = new();
                {
                    using ConfigWriter writer = new(filename);
                    saved.Save(writer, label);
                }
                ColorMap loaded = new();
                {
                    using ConfigReader stream = new(filename);
                    loaded.Restore(stream.Read(), label);
                }
                AreEqualMetricColorMap(saved, loaded);
            }
            finally
            {
                FileIO.DeleteIfExists(filename);
            }
        }

        /// <summary>
        /// Test for <see cref="ColorMap"/> with only one element.
        /// </summary>
        [Test]
        public void TestMetricColorMapOneElement()
        {
            string filename = Path.GetTempFileName();
            const string label = "metricMap";

            try
            {
                ColorMap saved = new();
                ColorRange colorRange = NewColorRange(Color.green, Color.cyan, 5);
                saved["metricX"] = colorRange;
                {
                    using ConfigWriter writer = new(filename);
                    saved.Save(writer, label);
                }
                ColorMap loaded = new();
                {
                    using ConfigReader stream = new(filename);
                    loaded.Restore(stream.Read(), label);
                }
                AreEqualMetricColorMap(saved, loaded);
            }
            finally
            {
                FileIO.DeleteIfExists(filename);
            }
        }

        private static ColorRange NewColorRange(Color lower, Color upper, uint numberOfColors)
        {
            ColorRange colorRange = new()
            {
                Lower = lower,
                Upper = upper,
                NumberOfColors = numberOfColors
            };
            return colorRange;
        }

        /// <summary>
        /// Test for <see cref="ColorMap"/> with two elements.
        /// </summary>
        [Test]
        public void TestMetricColorMapTwoElements()
        {
            string filename = Path.GetTempFileName();
            const string label = "metricMap";

            try
            {
                ColorMap saved = new();
                saved["metricX"] = NewColorRange(Color.white, Color.grey, 10);
                saved["metricY"] = NewColorRange(Color.grey, Color.black, 3);
                {
                    using ConfigWriter writer = new(filename);
                    saved.Save(writer, label);
                }
                ColorMap loaded = new();
                {
                    using ConfigReader stream = new(filename);
                    loaded.Restore(stream.Read(), label);
                }
                AreEqualMetricColorMap(saved, loaded);
            }
            finally
            {
                FileIO.DeleteIfExists(filename);
            }
        }

        private void AreEqualMetricColorMap(ColorMap saved, ColorMap loaded)
        {
            Assert.That(loaded.Count, Is.EqualTo(saved.Count));
            foreach (var entry in saved)
            {
                Assert.That(loaded[entry.Key], Is.EqualTo(entry.Value));
            }
        }

        /// <summary>
        /// Test for <see cref="AntennaAttributes"/>.
        /// </summary>
        [Test]
        public void TestAntennaAttributes()
        {
            AntennaAttributes saved = new();
            saved.AntennaSections.Add("metricA");
            saved.AntennaSections.Add("metricB");

            string filename = Path.GetTempFileName();
            try
            {
                const string label = "Antenna";
                {
                    using ConfigWriter writer = new(filename);
                    saved.Save(writer, label);
                }
                AntennaAttributes loaded = new();
                {
                    using ConfigReader stream = new(filename);
                    loaded.Restore(stream.Read(), label);
                }
                AreEqualAntennaSettings(saved, loaded);
            }
            finally
            {
                FileIO.DeleteIfExists(filename);
            }
        }

        /// <summary>
        /// Test for <see cref="SEECity"/>.
        /// </summary>
        [Test]
        public void TestSEECity()
        {
            string filename = Path.GetTempFileName();
            // First save a new city with all its default values.
            SEECity savedCity = NewVanillaSEECity<SEECity>();
            // FIXME: We need tests for the antenna settings
            //savedCity.LeafNodeSettings.AntennaSettings.AntennaSections.Add(new AntennaSection("leafmetric", Color.white));
            //savedCity.InnerNodeSettings.AntennaSettings.AntennaSections.Add(new AntennaSection("innermetric", Color.black));
            VisualNodeAttributes function = new()
            {
                IsRelevant = true
            };
            VisualNodeAttributes file = new()
            {
                IsRelevant = false
            };
            try
            {
                savedCity.NodeTypes = new NodeTypeVisualsMap();
                savedCity.NodeTypes["Function"] = function;
                savedCity.NodeTypes["File"] = file;
                CSVGraphProvider csvProvider = new();
                csvProvider.Path.AbsolutePath = "mydir/myfile.csv";
                savedCity.DataProvider.Add(csvProvider);
                savedCity.Save(filename);

                // Create a new city with all its default values and then
                // wipe out all its attributes to see whether they are correctly
                // restored from the saved configuration file.
                SEECity loadedCity = NewVanillaSEECity<SEECity>();
                WipeOutSEECityAttributes(loadedCity);
                // Load the saved attributes from the configuration file.
                loadedCity.Load(filename);

                SEECityAttributesAreEqual(savedCity, loadedCity);
            }
            finally
            {
                FileIO.DeleteIfExists(filename);
            }
        }

        /// <summary>
        /// Test for <see cref="CommitCity"/>.
        /// </summary>
        /// <remarks>We test only the attributes specific to <see cref="CommitCity"/>
        /// excluding those just inherited. We trust that the inherited attributes
        /// are tested by <see cref="TestSEECity"/>.</remarks>
        [Test]
        public void TestCommitCity()
        {
            string filename = Path.GetTempFileName();
            string vcsPath = "/c/mypath/myvcs";

            try
            {
                // First save a new city with all its default values.
                CommitCity savedCity = NewVanillaSEECity<CommitCity>();
                savedCity.VCSPath = new(vcsPath);
                savedCity.Save(filename);

                // Create a new city with all its default values and then
                // wipe out all its attributes to see whether they are correctly
                // restored from the saved configuration file.
                CommitCity loadedCity = NewVanillaSEECity<CommitCity>();
                WipeOutCommitCityAttributes(loadedCity);
                // Load the saved attributes from the configuration file.
                loadedCity.Load(filename);

                CommitCityAttributesAreEqual(savedCity, loadedCity);
            }
            finally
            {
                FileIO.DeleteIfExists(filename);
            }
        }

        /// <summary>
        /// Test for SEEEvolutionCity.
        /// </summary>
        [Test]
        public void TestSEEEvolutionCity()
        {
            string filename = Path.GetTempFileName();
            try
            {
                // First save a new city with all its default values.
                SEECityEvolution savedCity = NewVanillaSEECity<SEECityEvolution>();
                savedCity.Save(filename);

                // Create a new city with all its default values and then
                // wipe out all its attributes to see whether they are correctly
                // restored from the saved configuration file.
                SEECityEvolution loadedCity = NewVanillaSEECity<SEECityEvolution>();
                WipeOutSEEEvolutionCityAttributes(loadedCity);
                // Load the saved attributes from the configuration file.
                loadedCity.Load(filename);

                SEEEvolutionCityAttributesAreEqual(savedCity, loadedCity);
            }
            finally
            {
                FileIO.DeleteIfExists(filename);
            }
        }

        /// <summary>
        /// Test for SEERandomCity.
        /// </summary>
        [Test]
        public void TestSEERandomCity()
        {
            string filename = Path.GetTempFileName();
            try
            {
                // First save a new city with all its default values.
                SEECityRandom savedCity = NewVanillaSEECity<SEECityRandom>();
                savedCity.Save(filename);

                // Create a new city with all its default values and then
                // wipe out all its attributes to see whether they are correctly
                // restored from the saved configuration file.
                SEECityRandom loadedCity = NewVanillaSEECity<SEECityRandom>();
                WipeOutSEERandomCityAttributes(loadedCity);
                // Load the saved attributes from the configuration file.
                loadedCity.Load(filename);

                SEERandomCityAttributesAreEqual(savedCity, loadedCity);
            }
            finally
            {
                FileIO.DeleteIfExists(filename);
            }
        }

        //--------------------------------------------------------
        // AreEqual comparisons
        //--------------------------------------------------------

        /// <summary>
        /// Checks whether the configuration attributes of <paramref name="expected"/> and
        /// <paramref name="actual"/> are equal.
        /// </summary>
        /// <param name="expected">expected settings</param>
        /// <param name="actual">actual settings</param>
        private static void SEECityAttributesAreEqual(SEECity expected, SEECity actual)
        {
            AbstractSEECityAttributesAreEqual(expected, actual);
            TestGraphProviderIO.AreEqual(expected.DataProvider, actual.DataProvider);
        }

        /// <summary>
        /// Checks whether the configuration attributes of <paramref name="expected"/> and
        /// <paramref name="actual"/> are equal.
        /// </summary>
        /// <param name="expected">expected settings</param>
        /// <param name="actual">actual settings</param>
        private static void CommitCityAttributesAreEqual(CommitCity expected, CommitCity actual)
        {
            SEECityAttributesAreEqual(expected, actual);
            AreEqual(expected.VCSPath, actual.VCSPath);
            Assert.That(actual.OldRevision, Is.EqualTo(expected.OldRevision));
            Assert.That(actual.NewRevision, Is.EqualTo(expected.NewRevision));
        }

        /// <summary>
        /// Checks whether the configuration attributes of <paramref name="expected"/> and
        /// <paramref name="actual"/> are equal.
        /// </summary>
        /// <param name="expected">expected settings</param>
        /// <param name="actual">actual settings</param>
        private static void SEERandomCityAttributesAreEqual(SEECityRandom expected, SEECityRandom actual)
        {
            SEECityAttributesAreEqual(expected, actual);
            AreEqual(expected.LeafConstraint, actual.LeafConstraint);
            AreEqual(expected.InnerNodeConstraint, actual.InnerNodeConstraint);
            AreEqual(expected.LeafAttributes, actual.LeafAttributes);
        }

        /// <summary>
        /// Checks whether the two lists <paramref name="expected"/> and <paramref name="actual"/>
        /// are equal (by value).
        /// </summary>
        /// <param name="expected">expected list</param>
        /// <param name="actual">actual list</param>
        private static void AreEqual(IList<RandomAttributeDescriptor> expected, IList<RandomAttributeDescriptor> actual)
        {
            Assert.That(actual.Count, Is.EqualTo(expected.Count));
            foreach (RandomAttributeDescriptor outer in expected)
            {
                bool found = false;
                foreach (RandomAttributeDescriptor inner in actual)
                {
                    if (outer.Name == inner.Name)
                    {
                        Assert.That(inner.Mean, Is.EqualTo(outer.Mean));
                        Assert.That(inner.StandardDeviation, Is.EqualTo(outer.StandardDeviation));
                        Assert.That(inner.Minimum, Is.EqualTo(outer.Minimum));
                        Assert.That(inner.Maximum, Is.EqualTo(outer.Maximum));
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    Assert.Fail($"Expected RandomAttributeDescriptor {outer.Name} not found.");
                }
            }
        }

        /// <summary>
        /// Checks whether the two constraints <paramref name="expected"/> and <paramref name="actual"/>
        /// are equal (by value).
        /// </summary>
        /// <param name="expected">expected constraint</param>
        /// <param name="actual">actual constraint</param>
        private static void AreEqual(Constraint expected, Constraint actual)
        {
            Assert.That(actual.NodeType, Is.EqualTo(expected.NodeType));
            Assert.That(actual.EdgeType, Is.EqualTo(expected.EdgeType));
            Assert.That(actual.NodeNumber, Is.EqualTo(expected.NodeNumber));
            Assert.That(actual.EdgeDensity, Is.EqualTo(expected.EdgeDensity));
        }

        /// <summary>
        /// Checks whether the configuration attributes of <paramref name="expected"/> and
        /// <paramref name="actual"/> are equal.
        /// </summary>
        /// <param name="expected">expected settings</param>
        /// <param name="actual">actual settings</param>
        private static void SEEEvolutionCityAttributesAreEqual(SEECityEvolution expected, SEECityEvolution actual)
        {
            AbstractSEECityAttributesAreEqual(expected, actual);
            TestGraphProviderIO.AreEqual(expected.DataProvider, actual.DataProvider);
        }

        /// <summary>
        /// Checks whether the configuration attributes of <paramref name="expected"/> and
        /// <paramref name="actual"/> are equal.
        /// </summary>
        /// <param name="expected">expected settings</param>
        /// <param name="actual">actual settings</param>
        private static void AbstractSEECityAttributesAreEqual(AbstractSEECity expected, AbstractSEECity actual)
        {
            AreEqualSharedAttributes(expected, actual);
            Assert.That(actual.NodeTypes.Count, Is.EqualTo(expected.NodeTypes.Count));
            AreEqualNodeTypes(expected, actual);
            AreEqualMetricToColor(expected, actual);
            AreEqualNodeLayoutSettings(expected.NodeLayoutSettings, actual.NodeLayoutSettings);
            AreEqualEdgeLayoutSettings(expected.EdgeLayoutSettings, actual.EdgeLayoutSettings);
            AreEqualEdgeSelectionSettings(expected.EdgeSelectionSettings, actual.EdgeSelectionSettings);
            AreEqualErosionSettings(expected.ErosionSettings, actual.ErosionSettings);
            AreEqual(expected.MarkerAttributes, actual.MarkerAttributes);
            AreEqual(expected.LabelSettings, actual.LabelSettings);
            AreEqual(expected.TooltipSettings, actual.TooltipSettings);
        }

        /// <summary>
        /// Checks whether the two tooltip settings <paramref name="expected"/> and <paramref name="actual"/>
        /// are equal (by value).
        /// </summary>
        /// <param name="expected">expected tooltip settings</param>
        /// <param name="actual">actual tooltip settings</param>
        private static void AreEqual(TooltipSettings expected, TooltipSettings actual)
        {
            Assert.That(actual.ShowName, Is.EqualTo(expected.ShowName));
            Assert.That(actual.ShowType, Is.EqualTo(expected.ShowType));
            Assert.That(actual.ShowIncomingEdges, Is.EqualTo(expected.ShowIncomingEdges));
            Assert.That(actual.ShowOutgoingEdges, Is.EqualTo(expected.ShowOutgoingEdges));
            Assert.That(actual.ShowNodeKind, Is.EqualTo(expected.ShowNodeKind));
            Assert.That(actual.ShownMetrics, Is.EqualTo(expected.ShownMetrics));
        }

        /// <summary>
        /// Checks whether <paramref name="actual"/> has the same values as <paramref name="expected"/>.
        /// </summary>
        /// <param name="expected">expected values</param>
        /// <param name="actual">actual values</param>
        private static void AreEqual(MarkerAttributes expected, MarkerAttributes actual)
        {
            Assert.That(actual.MarkerHeight, Is.EqualTo(expected.MarkerHeight));
            Assert.That(actual.MarkerWidth, Is.EqualTo(expected.MarkerWidth));
            AreEqual(expected.AdditionBeamColor, actual.AdditionBeamColor);
            AreEqual(expected.ChangeBeamColor, actual.ChangeBeamColor);
            AreEqual(expected.DeletionBeamColor, actual.DeletionBeamColor);
        }

        /// <summary>
        /// Checks whether the <see cref="AbstractSEECity.MetricToColor"/> attributes of <paramref name="expected"/>
        /// and equal <paramref name="actual"/>.
        /// </summary>
        /// <param name="expected">expected value</param>
        /// <param name="actual">actual value</param>
        private static void AreEqualMetricToColor(AbstractSEECity expected, AbstractSEECity actual)
        {
            Assert.That(actual.MetricToColor.Count, Is.EqualTo(expected.MetricToColor.Count));
            foreach (var entry in expected.MetricToColor)
            {
                ColorRange actualColorRange = actual.MetricToColor[entry.Key];
                AreEqual(entry.Value, actualColorRange);
            }
        }

        /// <summary>
        /// Checks whether the <see cref="AbstractSEECity.NodeTypes"/> of <paramref name="expected"/>
        /// and equal <paramref name="actual"/>.
        /// </summary>
        /// <param name="expected">expected value</param>
        /// <param name="actual">actual value</param>
        private static void AreEqualNodeTypes(AbstractSEECity expected, AbstractSEECity actual)
        {
            foreach (var entry in expected.NodeTypes)
            {
                Assert.That(actual.NodeTypes.TryGetValue(entry.Key, out VisualNodeAttributes actualSetting),
                            Is.True, $"There is no node type {entry.Key} in the actual settings.");
                AreEqualNodeSettings(entry.Value, actualSetting);
            }
        }

        /// <summary>
        /// Checks whether the two label settings <paramref name="expected"/> and <paramref name="actual"/>
        /// are equal (by value).
        /// </summary>
        /// <param name="expected">expected label setting</param>
        /// <param name="actual">actual label setting</param>
        private static void AreEqual(LabelAttributes expected, LabelAttributes actual)
        {
            Assert.That(actual.Show, Is.EqualTo(expected.Show));
            Assert.That(actual.Distance, Is.EqualTo(expected.Distance).Within(0.001f));
            Assert.That(actual.FontSize, Is.EqualTo(expected.FontSize).Within(0.001f));
            AreEqual(expected.FontColor, actual.FontColor);
            Assert.That(actual.AnimationFactor, Is.EqualTo(expected.AnimationFactor).Within(0.001f));
            Assert.That(actual.LabelAlpha, Is.EqualTo(expected.LabelAlpha).Within(0.001f));
        }

        /// <summary>
        /// Checks whether the two color ranges <paramref name="expected"/> and <paramref name="actual"/>
        /// are equal (by value).
        /// </summary>
        /// <param name="expected">expected color range</param>
        /// <param name="actual">actual color range</param>
        private static void AreEqual(ColorRange expected, ColorRange actual)
        {
            AreEqual(expected.Lower, actual.Lower);
            AreEqual(expected.Upper, actual.Upper);
            Assert.That(actual.NumberOfColors, Is.EqualTo(expected.NumberOfColors));
        }

        /// <summary>
        /// Checks whether the two colors <paramref name="expected"/> and <paramref name="actual"/>
        /// are equal (by value).
        /// </summary>
        /// <param name="expected">expected color</param>
        /// <param name="actual">actual color</param>
        private static void AreEqual(Color expected, Color actual)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.001f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.001f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.001f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.001f));
        }

        //--------------------------------------------------------
        // attribute modifiers
        //--------------------------------------------------------

        // A general note on the following methods wiping out cities:
        // "Wiping out" means in those cases just that a value different from the
        // default or from a previously set value is assigned so that we
        // could notice any difference between the "wiped out" and loaded values.

        /// <summary>
        /// Assigns all attributes of given <paramref name="city"/> to arbitrary values
        /// different from their default values.
        /// </summary>
        /// <param name="city">the city whose attributes are to be re-assigned</param>
        private static void WipeOutSEECityAttributes(SEECity city)
        {
            WipeOutAbstractSEECityAttributes(city);
            city.DataProvider = new SingleGraphPipelineProvider();
        }

        /// <summary>
        /// Wipes out all attributes of <paramref name="markerAttributes"/>.
        /// </summary>
        /// <param name="markerAttributes">to be wiped out</param>
        private static void WipeOutMarkerAttributes(MarkerAttributes markerAttributes)
        {
            markerAttributes.MarkerHeight++;
            markerAttributes.MarkerWidth++;
            markerAttributes.AdditionBeamColor = Color.clear;
            markerAttributes.ChangeBeamColor = Color.clear;
            markerAttributes.DeletionBeamColor = Color.clear;
        }

        /// <summary>
        /// Assigns all attributes of given <paramref name="city"/> to arbitrary values
        /// different from their default values.
        /// </summary>
        /// <param name="city">the city whose attributes are to be re-assigned</param>
        private static void WipeOutCommitCityAttributes(CommitCity city)
        {
            WipeOutSEECityAttributes(city);
            city.VCSPath.Path = "C:/MyAbsoluteDirectory/MyVCSDirectory";
            city.OldRevision = "myOldRevisionYYY";
            city.NewRevision = "myNewRevisionXXX";
        }

        /// <summary>
        /// Assigns all attributes of given <paramref name="city"/> to arbitrary values
        /// different from their default values.
        /// </summary>
        /// <param name="city">the city whose attributes are to be re-assigned</param>
        private void WipeOutSEERandomCityAttributes(SEECityRandom city)
        {
            WipeOutSEECityAttributes(city);
            city.LeafConstraint = new Constraint(nodeType: "X", edgeType: "Y", nodeNumber: 5, edgeDensity: 0);
            city.InnerNodeConstraint = new Constraint(nodeType: "N", edgeType: "T", nodeNumber: 1, edgeDensity: 1);
            city.LeafAttributes = new List<RandomAttributeDescriptor>();
        }

        /// <summary>
        /// Assigns all attributes of given <paramref name="city"/> to arbitrary values
        /// different from their default values.
        /// </summary>
        /// <param name="city">the city whose attributes are to be re-assigned</param>
        private static void WipeOutSEEEvolutionCityAttributes(SEECityEvolution city)
        {
            WipeOutAbstractSEECityAttributes(city);
        }

        /// <summary>
        /// Assigns all attributes of given <paramref name="city"/> to arbitrary values
        /// different from their default values.
        /// </summary>
        /// <param name="city">the city whose attributes are to be re-assigned</param>
        private static void WipeOutAbstractSEECityAttributes(AbstractSEECity city)
        {
            WipeOutSharedAttributes(city);
            WipeOutNodeTypes(city);
            WipeOutMetricToColor(city);
            WipeOutNodeLayoutSettings(city);
            WipeOutEdgeLayoutSettings(city);
            WipeOutEdgeSelectionSettings(city.EdgeSelectionSettings);
            WipeOutErosionSettings(city);
            WipeOutMarkerAttributes(city.MarkerAttributes);
            WipeOutLabelSettings(ref city.LabelSettings);
            WipeOutTooltipSettings(city.TooltipSettings);
        }

        /// <summary>
        /// Wipes out all attributes of <paramref name="tooltipSettings"/>.
        /// </summary>
        /// <param name="tooltipSettings">to be wiped out</param>
        private static void WipeOutTooltipSettings(TooltipSettings tooltipSettings)
        {
            tooltipSettings.ShowName = false;
            tooltipSettings.ShowType = false;
            tooltipSettings.ShowIncomingEdges = true;
            tooltipSettings.ShowOutgoingEdges = true;
            tooltipSettings.ShowNodeKind = true;
            tooltipSettings.ShownMetrics = new();
        }

        /// <summary>
        /// Resets the <see cref="AbstractSEECity.MetricToColor"/> of <paramref name="city"/>
        /// to an empty mapping.
        /// </summary>
        /// <param name="city">the city whose <see cref="AbstractSEECity.MetricToColor"/> is to be wiped out</param>
        private static void WipeOutMetricToColor(AbstractSEECity city)
        {
            city.MetricToColor.Clear();
        }

        /// <summary>
        /// Wipes out <see cref="AbstractSEECity.NodeTypes"/> of <paramref name="city"/>.
        /// </summary>
        /// <param name="city">the city whose attributes are to be re-assigned</param>
        private static void WipeOutNodeTypes(AbstractSEECity city)
        {
            foreach (VisualNodeAttributes settings in city.NodeTypes.Values)
            {
                WipeOutNodeSettings(settings);
            }
        }

        private static void WipeOutErosionSettings(AbstractSEECity city)
        {
            city.ErosionSettings.ShowInnerErosions = !city.ErosionSettings.ShowInnerErosions;
            city.ErosionSettings.ShowLeafErosions = !city.ErosionSettings.ShowLeafErosions;
            city.ErosionSettings.ShowDashboardIssuesInCodeWindow = !city.ErosionSettings.ShowDashboardIssuesInCodeWindow;
            city.ErosionSettings.ErosionScalingFactor++;

            city.ErosionSettings.StyleIssue = "X";
            city.ErosionSettings.UniversalIssue = "X";
            city.ErosionSettings.MetricIssue = "X";
            city.ErosionSettings.DeadCodeIssue = "X";
            city.ErosionSettings.CycleIssue = "X";
            city.ErosionSettings.CloneIssue = "X";
            city.ErosionSettings.ArchitectureIssue = "X";
            city.ErosionSettings.LspHint = "X";
            city.ErosionSettings.LspInfo = "X";
            city.ErosionSettings.LspWarning = "X";
            city.ErosionSettings.LspError = "X";

            city.ErosionSettings.StyleIssueSum = "X";
            city.ErosionSettings.UniversalIssueSum = "X";
            city.ErosionSettings.MetricIssueSum = "X";
            city.ErosionSettings.DeadCodeIssueSum = "X";
            city.ErosionSettings.CycleIssueSum = "X";
            city.ErosionSettings.CloneIssueSum = "X";
            city.ErosionSettings.ArchitectureIssueSum = "X";
        }

        private static void AreEqualErosionSettings(ErosionAttributes expected, ErosionAttributes actual)
        {
            Assert.That(actual.ShowInnerErosions, Is.EqualTo(expected.ShowInnerErosions));
            Assert.That(actual.ShowLeafErosions, Is.EqualTo(expected.ShowLeafErosions));
            Assert.That(actual.ShowDashboardIssuesInCodeWindow, Is.EqualTo(expected.ShowDashboardIssuesInCodeWindow));
            Assert.That(actual.ErosionScalingFactor, Is.EqualTo(expected.ErosionScalingFactor));

            Assert.That(actual.StyleIssue, Is.EqualTo(expected.StyleIssue));
            Assert.That(actual.UniversalIssue, Is.EqualTo(expected.UniversalIssue));
            Assert.That(actual.MetricIssue, Is.EqualTo(expected.MetricIssue));
            Assert.That(actual.DeadCodeIssue, Is.EqualTo(expected.DeadCodeIssue));
            Assert.That(actual.CycleIssue, Is.EqualTo(expected.CycleIssue));
            Assert.That(actual.CloneIssue, Is.EqualTo(expected.CloneIssue));
            Assert.That(actual.ArchitectureIssue, Is.EqualTo(expected.ArchitectureIssue));
            Assert.That(actual.LspHint, Is.EqualTo(expected.LspHint));
            Assert.That(actual.LspInfo, Is.EqualTo(expected.LspInfo));
            Assert.That(actual.LspWarning, Is.EqualTo(expected.LspWarning));
            Assert.That(actual.LspError, Is.EqualTo(expected.LspError));

            Assert.That(actual.StyleIssueSum, Is.EqualTo(expected.StyleIssueSum));
            Assert.That(actual.UniversalIssueSum, Is.EqualTo(expected.UniversalIssueSum));
            Assert.That(actual.MetricIssueSum, Is.EqualTo(expected.MetricIssueSum));
            Assert.That(actual.DeadCodeIssueSum, Is.EqualTo(expected.DeadCodeIssueSum));
            Assert.That(actual.CycleIssueSum, Is.EqualTo(expected.CycleIssueSum));
            Assert.That(actual.CloneIssueSum, Is.EqualTo(expected.CloneIssueSum));
            Assert.That(actual.ArchitectureIssueSum, Is.EqualTo(expected.ArchitectureIssueSum));
        }

        private static void WipeOutEdgeLayoutSettings(AbstractSEECity city)
        {
            city.EdgeLayoutSettings.Kind = EdgeLayoutKind.Straight;
            city.EdgeLayoutSettings.ShowEdges = ShowEdgeStrategy.OnHoverOnly;
            city.EdgeLayoutSettings.AnimateEdgeFlow = !city.EdgeLayoutSettings.AnimateEdgeFlow;
            city.EdgeLayoutSettings.AnimationKind = EdgeAnimationKind.Fading;
            city.EdgeLayoutSettings.AnimateTransitiveSourceEdges = !city.EdgeLayoutSettings.AnimateTransitiveSourceEdges;
            city.EdgeLayoutSettings.AnimateTransitiveTargetEdges = !city.EdgeLayoutSettings.AnimateTransitiveTargetEdges;
            city.EdgeLayoutSettings.EdgeWidth++;
            city.EdgeLayoutSettings.Tension = 0;
        }

        private static void WipeOutEdgeSelectionSettings(EdgeSelectionAttributes edgeSelectionSettings)
        {
            edgeSelectionSettings.TubularSegments = 0;
            edgeSelectionSettings.Radius = 0;
            edgeSelectionSettings.RadialSegments = 0;
            edgeSelectionSettings.AreSelectable = !edgeSelectionSettings.AreSelectable;
        }

        private static void AreEqualEdgeLayoutSettings(EdgeLayoutAttributes expected, EdgeLayoutAttributes actual)
        {
            Assert.That(actual.Kind, Is.EqualTo(expected.Kind));
            Assert.That(actual.ShowEdges, Is.EqualTo(expected.ShowEdges));
            Assert.That(actual.AnimateEdgeFlow, Is.EqualTo(expected.AnimateEdgeFlow));
            Assert.That(actual.AnimationKind, Is.EqualTo(expected.AnimationKind));
            Assert.That(actual.AnimateTransitiveSourceEdges, Is.EqualTo(expected.AnimateTransitiveSourceEdges));
            Assert.That(actual.AnimateTransitiveTargetEdges, Is.EqualTo(expected.AnimateTransitiveTargetEdges));
            Assert.That(actual.EdgeWidth, Is.EqualTo(expected.EdgeWidth));
            Assert.That(actual.Tension, Is.EqualTo(expected.Tension));
        }

        private static void AreEqualEdgeSelectionSettings(EdgeSelectionAttributes expected, EdgeSelectionAttributes actual)
        {
            Assert.That(actual.TubularSegments, Is.EqualTo(expected.TubularSegments));
            Assert.That(actual.Radius, Is.EqualTo(expected.Radius));
            Assert.That(actual.RadialSegments, Is.EqualTo(expected.RadialSegments));
            Assert.That(actual.AreSelectable, Is.EqualTo(expected.AreSelectable));
        }

        private static void WipeOutNodeLayoutSettings(AbstractSEECity city)
        {
            city.NodeLayoutSettings.Kind = NodeLayoutKind.Balloon;
            city.NodeLayoutSettings.LayoutPath.Path = "no path found";
        }

        private static void AreEqualNodeLayoutSettings(NodeLayoutAttributes expected, NodeLayoutAttributes actual)
        {
            Assert.That(actual.Kind, Is.EqualTo(expected.Kind));
            AreEqual(expected.LayoutPath, actual.LayoutPath);
        }

        private static void WipeOutNodeSettings(VisualNodeAttributes settings)
        {
            settings.Shape = NodeShapes.Blocks;
            settings.IsRelevant = false;
            settings.MetricToLength = new List<string> { "0.001", SEE.DataModel.DG.Metrics.Prefix + "LOC" };
            settings.ColorProperty.ColorMetric = "X";
            settings.MinimalBlockLength = 90000;
            settings.MaximalBlockLength = 1000000;
            settings.OutlineWidth = 99999;
            WipeOutAntennaSettings(ref settings.AntennaSettings);
            settings.ShowNames = true;
        }

        private static void AreEqualNodeSettings(VisualNodeAttributes expected, VisualNodeAttributes actual)
        {
            Assert.That(actual.Shape, Is.EqualTo(expected.Shape));
            Assert.That(actual.IsRelevant, Is.EqualTo(expected.IsRelevant));
            AreEqual(expected.MetricToLength, actual.MetricToLength);
            Assert.That(actual.ColorProperty.ColorMetric, Is.EqualTo(expected.ColorProperty.ColorMetric));
            Assert.That(actual.MinimalBlockLength, Is.EqualTo(expected.MinimalBlockLength));
            Assert.That(actual.MaximalBlockLength, Is.EqualTo(expected.MaximalBlockLength));
            Assert.That(actual.OutlineWidth, Is.EqualTo(expected.OutlineWidth));
            AreEqualAntennaSettings(expected.AntennaSettings, actual.AntennaSettings);
            Assert.That(actual.ShowNames, Is.EqualTo(expected.ShowNames));
        }

        private static void AreEqual(IList<string> expected, IList<string> actual)
        {
            Assert.That(actual.Count, Is.EqualTo(expected.Count));
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.That(actual[i], Is.EqualTo(expected[i]));
            }
        }

        private static void WipeOutAntennaSettings(ref AntennaAttributes antennaAttributes)
        {
            antennaAttributes.AntennaSections.Clear();
        }

        private static void AreEqualAntennaSettings(AntennaAttributes expected, AntennaAttributes actual)
        {
            Assert.That(actual.AntennaSections.Count, Is.EqualTo(expected.AntennaSections.Count));
            for (int i = 0; i < expected.AntennaSections.Count; i++)
            {
                Assert.That(actual.AntennaSections[i], Is.EqualTo(expected.AntennaSections[i]));
            }
        }

        private static void WipeOutSharedAttributes(AbstractSEECity city)
        {
            city.LODCulling++;
            city.HierarchicalEdges = new HashSet<string>() { "Nonsense", "Whatever" };
            city.NodeTypes = new NodeTypeVisualsMap();
            city.ConfigurationPath.Path = "C:/MyAbsoluteDirectory/config.cfg";
            city.SourceCodeDirectory.Path = "C:/MyAbsoluteDirectory";
            city.SolutionPath.Path = "C:/MyAbsoluteDirectory/mysolution.sln";
            city.ZScoreScale = !city.ZScoreScale;
            city.ScaleOnlyLeafMetrics = !city.ScaleOnlyLeafMetrics;
        }

        private static void AreEqualSharedAttributes(AbstractSEECity expected, AbstractSEECity actual)
        {
            Assert.That(actual.LODCulling, Is.EqualTo(expected.LODCulling));
            Assert.That(actual.HierarchicalEdges, Is.EquivalentTo(expected.HierarchicalEdges));
            AreEquivalent(expected.NodeTypes, actual.NodeTypes);
            AreEqual(expected.ConfigurationPath, actual.ConfigurationPath);
            AreEqual(expected.SourceCodeDirectory, actual.SourceCodeDirectory);
            AreEqual(expected.SolutionPath, actual.SolutionPath);
            Assert.That(actual.ZScoreScale, Is.EqualTo(expected.ZScoreScale));
            Assert.That(actual.ScaleOnlyLeafMetrics, Is.EqualTo(expected.ScaleOnlyLeafMetrics));
        }

        private static void AreEquivalent(NodeTypeVisualsMap expected, NodeTypeVisualsMap actual)
        {
            Assert.That(actual.Count, Is.EqualTo(expected.Count));
            foreach (var entry in expected)
            {
                if (actual.TryGetValue(entry.Key, out VisualNodeAttributes entryInActual))
                {
                    AreEqualNodeSettings(entry.Value, entryInActual);
                }
                else
                {
                    Assert.Fail($"{entry.Key} not contained in actual");
                }
            }
        }

        /// <summary>
        /// Modifies all attributes of <paramref name="settings"/>.
        /// </summary>
        /// <param name="settings">settings whose attributes are to be modified</param>
        private static void WipeOutLabelSettings(ref LabelAttributes settings)
        {
            settings.Show = !settings.Show;
            settings.Distance++;
            settings.FontSize++;
            settings.FontColor = settings.FontColor.Invert();
            settings.AnimationFactor++;
            settings.LabelAlpha = 0;
        }

        //--------------------------------------------------------
        // new instances
        //--------------------------------------------------------

        /// <summary>
        /// Returns a new game object with a SEECity component T with all its default values.
        /// </summary>
        /// <returns>new game object with a SEECity component T</returns>
        private static T NewVanillaSEECity<T>() where T : Component
        {
            return new GameObject().AddComponent<T>();
        }
    }
}

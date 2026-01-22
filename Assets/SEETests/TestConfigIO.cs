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
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("label : 0;\n"));
        }

        [Test]
        public void TestConfigParseInteger2()
        {
            Dictionary<string, object> expected = new()
            {
                { "l", -1 }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("l : -1;"));
        }

        [Test]
        public void TestConfigParseInteger3()
        {
            Dictionary<string, object> expected = new()
            {
                { "label", 123 }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("label : +123;"));
        }

        [Test]
        public void TestConfigParseFloat1()
        {
            Dictionary<string, object> expected = new()
            {
                { "label", 123.0f }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("label: +123.0;"));
        }

        [Test]
        public void TestConfigParseFloat2()
        {
            Dictionary<string, object> expected = new()
            {
                { "label", -1234.0f }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("label : -1,234.00;"));
        }

        [Test]
        public void TestConfigParseFloat3()
        {
            Dictionary<string, object> expected = new()
            {
                { "label", 1.234567E-06f }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("label : 1.234567E-06 ;"));
        }

        [Test]
        public void TestConfigParseFloat4()
        {
            Dictionary<string, object> expected = new()
            {
                { "label", -1.234567e-1f }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("label\t: -1.234567e-1;\r"));
        }

        [Test]
        public void TestConfigParseInfinity()
        {
            const float value = float.PositiveInfinity;
            Dictionary<string, object> expected = new()
            {
                { "label", value }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse($"label\t: {value.ToString("F8", System.Globalization.CultureInfo.InvariantCulture)};\r"));
        }

        [Test]
        public void TestConfigParseNegativeInfinity()
        {
            const float value = float.NegativeInfinity;
            Dictionary<string, object> expected = new()
            {
                { "label", value }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse($"label\t: {value.ToString("F8", System.Globalization.CultureInfo.InvariantCulture)};\r"));
        }

        [Test]
        public void TestConfigParseString1()
        {
            Dictionary<string, object> expected = new()
            {
                { "label", "hello" }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("label : \"hello\";"));
        }

        [Test]
        public void TestConfigParseString3()
        {
            Dictionary<string, object> expected = new()
            {
                { "label", "" }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("label : \"\";"));
        }

        [Test]
        public void TestConfigParseString4()
        {
            Dictionary<string, object> expected = new()
            {
                { "label", "\"" }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("label : \"\"\"\";"));
        }

        [Test]
        public void TestConfigParseString2()
        {
            Dictionary<string, object> expected = new()
            {
                { "label", "\"hello, world\"" }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("label : \"\"\"hello, world\"\"\";"));
        }

        [Test]
        public void TestConfigParseTrue()
        {
            Dictionary<string, object> expected = new()
            {
                { "label", true }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("label : true;"));
        }

        [Test]
        public void TestConfigParseFalse()
        {
            Dictionary<string, object> expected = new()
            {
                { "label", false }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("label : false;"));
        }

        [Test]
        public void TestConfigParseAttribute1()
        {
            Dictionary<string, object> expected = new()
            {
                { "attr", new Dictionary<string, object>() { { "int", 1 } } }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("attr : { int: 1; };"));
        }

        [Test]
        public void TestConfigParseAttribute2()
        {
            Dictionary<string, object> expected = new()
            {
                { "attr", new Dictionary<string, object>() }
            };
            Dictionary<string, object> actual = ConfigReader.Parse("attr : { };");
            CollectionAssert.AreEquivalent(expected, actual);
        }

        [Test]
        public void TestConfigParseAttribute3()
        {
            Dictionary<string, object> expected = new()
            {
                { "attr", new Dictionary<string, object>() { { "int", 1 }, { "x", "hello" } } }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("attr : { int: 1; x : \"hello\"; };"));
        }


        [Test]
        public void TestConfigParseAttribute4()
        {
            Dictionary<string, object> expected = new()
            {
                { "attr", new Dictionary<string, object>() { { "x", new Dictionary<string, object>() } } }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("attr : { x: {}; };"));
        }

        [Test]
        public void TestConfigParseAttribute5()
        {
            Dictionary<string, object> expected = new()
            {
                { "attr", new Dictionary<string, object>() { { "a", 1 }, { "b", 2 }, { "x", new Dictionary<string, object>() { { "y", true }, { "z", false } } } } }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("attr : { a: 1; b: 2; x: {y : true; z : false;}; };"));
        }

        [Test]
        public void TestConfigParseList1()
        {
            Dictionary<string, object> expected = new()
            {
                { "list", new List<object>() { } }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("list : [];"));
        }

        [Test]
        public void TestConfigParseList2()
        {
            Dictionary<string, object> expected = new()
            {
                { "list", new List<object>() { 1, 2, 3 } }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("list : [ 1; 2; 3;];"));
        }

        [Test]
        public void TestConfigParseList3()
        {
            Dictionary<string, object> expected = new()
            {
                { "list", new List<object>() { true} }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("list : [ true; ];"));
        }

        [Test]
        public void TestConfigParseList4()
        {
            Dictionary<string, object> expected = new()
            {
                { "list", new List<object>() { new List<object>(), new List<object>() { 1 }, new List<object>() { 1, 2 } } }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("list : [ []; [1;]; [1; 2;];];"));
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
            Assert.AreEqual(saved.Count, loaded.Count);
            foreach (var entry in saved)
            {
                Assert.AreEqual(entry.Value, loaded[entry.Key]);
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
            Assert.AreEqual(expected.OldRevision, actual.OldRevision);
            Assert.AreEqual(expected.NewRevision, actual.NewRevision);
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
            Assert.AreEqual(expected.Count, actual.Count);
            foreach (RandomAttributeDescriptor outer in expected)
            {
                bool found = false;
                foreach (RandomAttributeDescriptor inner in actual)
                {
                    if (outer.Name == inner.Name)
                    {
                        Assert.AreEqual(outer.Mean, inner.Mean);
                        Assert.AreEqual(outer.StandardDeviation, inner.StandardDeviation);
                        Assert.AreEqual(outer.Minimum, inner.Minimum);
                        Assert.AreEqual(outer.Maximum, inner.Maximum);
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
            Assert.AreEqual(expected.NodeType, actual.NodeType);
            Assert.AreEqual(expected.EdgeType, actual.EdgeType);
            Assert.AreEqual(expected.NodeNumber, actual.NodeNumber);
            Assert.AreEqual(expected.EdgeDensity, actual.EdgeDensity);
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
            Assert.AreEqual(expected.NodeTypes.Count, actual.NodeTypes.Count);
            AreEqualNodeTypes(expected, actual);
            AreEqualMetricToColor(expected, actual);
            AreEqualNodeLayoutSettings(expected.NodeLayoutSettings, actual.NodeLayoutSettings);
            AreEqualEdgeLayoutSettings(expected.EdgeLayoutSettings, actual.EdgeLayoutSettings);
            AreEqualEdgeSelectionSettings(expected.EdgeSelectionSettings, actual.EdgeSelectionSettings);
            AreEqualErosionSettings(expected.ErosionSettings, actual.ErosionSettings);
            AreEqual(expected.MarkerAttributes, actual.MarkerAttributes);
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
            Assert.AreEqual(expected.ShowName, actual.ShowName);
            Assert.AreEqual(expected.ShowType, actual.ShowType);
            Assert.AreEqual(expected.ShowIncomingEdges, actual.ShowIncomingEdges);
            Assert.AreEqual(expected.ShowOutgoingEdges, actual.ShowOutgoingEdges);
            Assert.AreEqual(expected.ShowNodeKind, actual.ShowNodeKind);
            Assert.AreEqual(expected.ShowLinesOfCode, actual.ShowLinesOfCode);
            Assert.AreEqual(expected.Separator, actual.Separator);
            Assert.AreEqual(expected.NameFormat, actual.NameFormat);
            Assert.AreEqual(expected.TypeFormat, actual.TypeFormat);
            Assert.AreEqual(expected.IncomingEdgesFormat, actual.IncomingEdgesFormat);
            Assert.AreEqual(expected.OutgoingEdgesFormat, actual.OutgoingEdgesFormat);
            Assert.AreEqual(expected.NodeKindFormat, actual.NodeKindFormat);
            Assert.AreEqual(expected.LinesOfCodeFormat, actual.LinesOfCodeFormat);
        }

        /// <summary>
        /// Checks whether <paramref name="actual"/> has the same values as <paramref name="expected"/>.
        /// </summary>
        /// <param name="expected">expected values</param>
        /// <param name="actual">actual values</param>
        private static void AreEqual(MarkerAttributes expected, MarkerAttributes actual)
        {
            Assert.AreEqual(expected.MarkerHeight, actual.MarkerHeight);
            Assert.AreEqual(expected.MarkerWidth, actual.MarkerWidth);
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
            Assert.AreEqual(expected.MetricToColor.Count, actual.MetricToColor.Count);
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
                Assert.IsTrue(actual.NodeTypes.TryGetValue(entry.Key, out VisualNodeAttributes actualSetting));
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
            Assert.AreEqual(expected.Show, actual.Show);
            Assert.AreEqual(expected.FontSize, actual.FontSize, 0.001f);
            Assert.AreEqual(expected.Distance, actual.Distance, 0.001f);
            Assert.AreEqual(expected.AnimationFactor, actual.AnimationFactor, 0.001f);
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
            Assert.AreEqual(expected.NumberOfColors, actual.NumberOfColors);
        }

        /// <summary>
        /// Checks whether the two colors <paramref name="expected"/> and <paramref name="actual"/>
        /// are equal (by value).
        /// </summary>
        /// <param name="expected">expected color</param>
        /// <param name="actual">actual color</param>
        private static void AreEqual(Color expected, Color actual)
        {
            Assert.AreEqual(expected.r, actual.r, 0.001f);
            Assert.AreEqual(expected.g, actual.g, 0.001f);
            Assert.AreEqual(expected.b, actual.b, 0.001f);
            Assert.AreEqual(expected.a, actual.a, 0.001f);
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
            tooltipSettings.ShowLinesOfCode = true;
            tooltipSettings.Separator = "###";
            tooltipSettings.NameFormat = "Name={0}";
            tooltipSettings.TypeFormat = "NodeType={0}";
            tooltipSettings.IncomingEdgesFormat = "InEdges={0}";
            tooltipSettings.OutgoingEdgesFormat = "OutEdges={0}";
            tooltipSettings.NodeKindFormat = "Kind={0}";
            tooltipSettings.LinesOfCodeFormat = "Lines={0}";
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
            Assert.AreEqual(expected.ShowInnerErosions, actual.ShowInnerErosions);
            Assert.AreEqual(expected.ShowLeafErosions, actual.ShowLeafErosions);
            Assert.AreEqual(expected.ShowDashboardIssuesInCodeWindow, actual.ShowDashboardIssuesInCodeWindow);
            Assert.AreEqual(expected.ErosionScalingFactor, actual.ErosionScalingFactor);

            Assert.AreEqual(expected.StyleIssue, actual.StyleIssue);
            Assert.AreEqual(expected.UniversalIssue, actual.UniversalIssue);
            Assert.AreEqual(expected.MetricIssue, actual.MetricIssue);
            Assert.AreEqual(expected.DeadCodeIssue, actual.DeadCodeIssue);
            Assert.AreEqual(expected.CycleIssue, actual.CycleIssue);
            Assert.AreEqual(expected.CloneIssue, actual.CloneIssue);
            Assert.AreEqual(expected.ArchitectureIssue, actual.ArchitectureIssue);
            Assert.AreEqual(expected.LspHint, actual.LspHint);
            Assert.AreEqual(expected.LspInfo, actual.LspInfo);
            Assert.AreEqual(expected.LspWarning, actual.LspWarning);
            Assert.AreEqual(expected.LspError, actual.LspError);

            Assert.AreEqual(expected.StyleIssueSum, actual.StyleIssueSum);
            Assert.AreEqual(expected.UniversalIssueSum, actual.UniversalIssueSum);
            Assert.AreEqual(expected.MetricIssueSum, actual.MetricIssueSum);
            Assert.AreEqual(expected.DeadCodeIssueSum, actual.DeadCodeIssueSum);
            Assert.AreEqual(expected.CycleIssueSum, actual.CycleIssueSum);
            Assert.AreEqual(expected.CloneIssueSum, actual.CloneIssueSum);
            Assert.AreEqual(expected.ArchitectureIssueSum, actual.ArchitectureIssueSum);
        }

        private static void WipeOutEdgeLayoutSettings(AbstractSEECity city)
        {
            city.EdgeLayoutSettings.Kind = EdgeLayoutKind.None;
            city.EdgeLayoutSettings.EdgeWidth++;
            city.EdgeLayoutSettings.EdgesAboveBlocks = !city.EdgeLayoutSettings.EdgesAboveBlocks;
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
            Assert.AreEqual(expected.Kind, actual.Kind);
            Assert.AreEqual(expected.EdgeWidth, actual.EdgeWidth);
            Assert.AreEqual(expected.EdgesAboveBlocks, actual.EdgesAboveBlocks);
            Assert.AreEqual(expected.Tension, actual.Tension);
        }

        private static void AreEqualEdgeSelectionSettings(EdgeSelectionAttributes expected, EdgeSelectionAttributes actual)
        {
            Assert.AreEqual(expected.TubularSegments, actual.TubularSegments);
            Assert.AreEqual(expected.Radius, actual.Radius);
            Assert.AreEqual(expected.RadialSegments, actual.RadialSegments);
            Assert.AreEqual(expected.AreSelectable, actual.AreSelectable);
        }

        private static void WipeOutNodeLayoutSettings(AbstractSEECity city)
        {
            city.NodeLayoutSettings.Kind = NodeLayoutKind.Balloon;
            city.NodeLayoutSettings.LayoutPath.Path = "no path found";
        }

        private static void AreEqualNodeLayoutSettings(NodeLayoutAttributes expected, NodeLayoutAttributes actual)
        {
            Assert.AreEqual(expected.Kind, actual.Kind);
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
            WipeOutLabelSettings(ref settings.LabelSettings);
            settings.ShowNames = true;
        }

        private static void AreEqualNodeSettings(VisualNodeAttributes expected, VisualNodeAttributes actual)
        {
            Assert.AreEqual(expected.Shape, actual.Shape);
            Assert.AreEqual(expected.IsRelevant, actual.IsRelevant);
            AreEqual(expected.MetricToLength, actual.MetricToLength);
            Assert.AreEqual(expected.ColorProperty.ColorMetric, actual.ColorProperty.ColorMetric);
            Assert.AreEqual(expected.MinimalBlockLength, actual.MinimalBlockLength);
            Assert.AreEqual(expected.MaximalBlockLength, actual.MaximalBlockLength);
            Assert.AreEqual(expected.OutlineWidth, actual.OutlineWidth);
            AreEqualAntennaSettings(expected.AntennaSettings, actual.AntennaSettings);
            AreEqual(expected.LabelSettings, actual.LabelSettings);
            Assert.AreEqual(expected.ShowNames, actual.ShowNames);
        }

        private static void AreEqual(IList<string> expected, IList<string> actual)
        {
            Assert.AreEqual(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        private static void WipeOutAntennaSettings(ref AntennaAttributes antennaAttributes)
        {
            antennaAttributes.AntennaSections.Clear();
        }

        private static void AreEqualAntennaSettings(AntennaAttributes expected, AntennaAttributes actual)
        {
            Assert.AreEqual(expected.AntennaSections.Count, actual.AntennaSections.Count);
            for (int i = 0; i < expected.AntennaSections.Count; i++)
            {
                Assert.AreEqual(expected.AntennaSections[i], actual.AntennaSections[i]);
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
            Assert.AreEqual(expected.LODCulling, actual.LODCulling);
            CollectionAssert.AreEquivalent(expected.HierarchicalEdges, actual.HierarchicalEdges);
            AreEquivalent(expected.NodeTypes, actual.NodeTypes);
            AreEqual(expected.ConfigurationPath, actual.ConfigurationPath);
            AreEqual(expected.SourceCodeDirectory, actual.SourceCodeDirectory);
            AreEqual(expected.SolutionPath, actual.SolutionPath);
            Assert.AreEqual(expected.ZScoreScale, actual.ZScoreScale);
            Assert.AreEqual(expected.ScaleOnlyLeafMetrics, actual.ScaleOnlyLeafMetrics);
        }

        private static void AreEquivalent(NodeTypeVisualsMap expected, NodeTypeVisualsMap actual)
        {
            Assert.AreEqual(expected.Count, actual.Count);
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
            settings.AnimationFactor++;
            settings.Show = !settings.Show;
            settings.FontSize++;
            settings.Distance++;
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

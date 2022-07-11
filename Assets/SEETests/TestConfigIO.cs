using System;
using System.Collections.Generic;
using NUnit.Framework;
using SEE.Game;
using SEE.Game.City;
using SEE.Layout.NodeLayouts.Cose;
using SEE.Tools;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Test cases for ConfigIO.
    /// </summary>
    internal class TestConfigIO
    {
        [Test]
        public void TestConfigParseInteger1()
        {
            Dictionary<string, object> expected = new Dictionary<string, object>()
            {
                { "label", 0 }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("label : 0;\n"));
        }

        [Test]
        public void TestConfigParseInteger2()
        {
            Dictionary<string, object> expected = new Dictionary<string, object>()
            {
                { "l", -1 }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("l : -1;"));
        }

        [Test]
        public void TestConfigParseInteger3()
        {
            Dictionary<string, object> expected = new Dictionary<string, object>()
            {
                { "label", 123 }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("label : +123;"));
        }

        [Test]
        public void TestConfigParseFloat1()
        {
            Dictionary<string, object> expected = new Dictionary<string, object>()
            {
                { "label", 123.0f }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("label: +123.0;"));
        }

        [Test]
        public void TestConfigParseFloat2()
        {
            Dictionary<string, object> expected = new Dictionary<string, object>()
            {
                { "label", -1234.0f }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("label : -1,234.00;"));
        }

        [Test]
        public void TestConfigParseFloat3()
        {
            Dictionary<string, object> expected = new Dictionary<string, object>()
            {
                { "label", 1.234567E-06f }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("label : 1.234567E-06 ;"));
        }

        [Test]
        public void TestConfigParseFloat4()
        {
            Dictionary<string, object> expected = new Dictionary<string, object>()
            {
                { "label", -1.234567e-1f }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("label\t: -1.234567e-1;\r"));
        }

        [Test]
        public void TestConfigParseString1()
        {
            Dictionary<string, object> expected = new Dictionary<string, object>()
            {
                { "label", "hello" }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("label : \"hello\";"));
        }

        [Test]
        public void TestConfigParseString3()
        {
            Dictionary<string, object> expected = new Dictionary<string, object>()
            {
                { "label", "" }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("label : \"\";"));
        }

        [Test]
        public void TestConfigParseString4()
        {
            Dictionary<string, object> expected = new Dictionary<string, object>()
            {
                { "label", "\"" }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("label : \"\"\"\";"));
        }

        [Test]
        public void TestConfigParseString2()
        {
            Dictionary<string, object> expected = new Dictionary<string, object>()
            {
                { "label", "\"hello, world\"" }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("label : \"\"\"hello, world\"\"\";"));
        }

        [Test]
        public void TestConfigParseTrue()
        {
            Dictionary<string, object> expected = new Dictionary<string, object>()
            {
                { "label", true }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("label : true;"));
        }

        [Test]
        public void TestConfigParseFalse()
        {
            Dictionary<string, object> expected = new Dictionary<string, object>()
            {
                { "label", false }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("label : false;"));
        }

        [Test]
        public void TestConfigParseAttribute1()
        {
            Dictionary<string, object> expected = new Dictionary<string, object>()
            {
                { "attr", new Dictionary<string, object>() { { "int", 1 } } }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("attr : { int: 1; };"));
        }

        [Test]
        public void TestConfigParseAttribute2()
        {
            Dictionary<string, object> expected = new Dictionary<string, object>()
            {
                { "attr", new Dictionary<string, object>() }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("attr : { };"));
        }

        [Test]
        public void TestConfigParseAttribute3()
        {
            Dictionary<string, object> expected = new Dictionary<string, object>()
            {
                { "attr", new Dictionary<string, object>() { { "int", 1 }, { "x", "hello" } } }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("attr : { int: 1; x : \"hello\"; };"));
        }


        [Test]
        public void TestConfigParseAttribute4()
        {
            Dictionary<string, object> expected = new Dictionary<string, object>()
            {
                { "attr", new Dictionary<string, object>() { { "x", new Dictionary<string, object>() } } }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("attr : { x: {}; };"));
        }

        [Test]
        public void TestConfigParseAttribute5()
        {
            Dictionary<string, object> expected = new Dictionary<string, object>()
            {
                { "attr", new Dictionary<string, object>() { { "a", 1 }, { "b", 2 }, { "x", new Dictionary<string, object>() { { "y", true }, { "z", false } } } } }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("attr : { a: 1; b: 2; x: {y : true; z : false;}; };"));
        }

        [Test]
        public void TestConfigParseList1()
        {
            Dictionary<string, object> expected = new Dictionary<string, object>()
            {
                { "list", new List<object>() { } }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("list : [];"));
        }

        [Test]
        public void TestConfigParseList2()
        {
            Dictionary<string, object> expected = new Dictionary<string, object>()
            {
                { "list", new List<object>() { 1, 2, 3 } }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("list : [ 1; 2; 3;];"));
        }

        [Test]
        public void TestConfigParseList3()
        {
            Dictionary<string, object> expected = new Dictionary<string, object>()
            {
                { "list", new List<object>() { true} }
            };
            CollectionAssert.AreEquivalent(expected, ConfigReader.Parse("list : [ true; ];"));
        }

        [Test]
        public void TestConfigParseList4()
        {
            Dictionary<string, object> expected = new Dictionary<string, object>()
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
            const string filename = "metricmap.cfg";
            const string label = "metricMap";

            ColorMap saved = new ColorMap();
            {
                using ConfigWriter writer = new ConfigWriter(filename);
                saved.Save(writer, label);
            }
            ColorMap loaded = new ColorMap();
            {
                using ConfigReader stream = new ConfigReader(filename);
                loaded.Restore(stream.Read(), label);
            }
            AreEqualMetricColorMap(saved, loaded);
        }

        /// <summary>
        /// Test for <see cref="ColorMap"/> with only one element.
        /// </summary>
        [Test]
        public void TestMetricColorMapOneElement()
        {
            const string filename = "metricmap.cfg";
            const string label = "metricMap";

            ColorMap saved = new ColorMap();
            ColorRange colorRange = NewColorRange(Color.green, Color.cyan, 5);
            saved["metricX"] = colorRange;
            {
                using ConfigWriter writer = new ConfigWriter(filename);
                saved.Save(writer, label);
            }
            ColorMap loaded = new ColorMap();
            {
                using ConfigReader stream = new ConfigReader(filename);
                loaded.Restore(stream.Read(), label);
            }
            AreEqualMetricColorMap(saved, loaded);
        }

        private static ColorRange NewColorRange(Color lower, Color upper, uint numberOfColors)
        {
            ColorRange colorRange = new ColorRange();
            colorRange.Lower = lower;
            colorRange.Upper = upper;
            colorRange.NumberOfColors = numberOfColors;
            return colorRange;
        }

        /// <summary>
        /// Test for <see cref="ColorMap"/> with two elements.
        /// </summary>
        [Test]
        public void TestMetricColorMapTwoElements()
        {
            const string filename = "metricmap.cfg";
            const string label = "metricMap";

            ColorMap saved = new ColorMap();
            saved["metricX"] = NewColorRange(Color.white, Color.grey, 10);
            saved["metricY"] = NewColorRange(Color.grey, Color.black, 3);
            {
                using ConfigWriter writer = new ConfigWriter(filename);
                saved.Save(writer, label);
            }
            ColorMap loaded = new ColorMap();
            {
                using ConfigReader stream = new ConfigReader(filename);
                loaded.Restore(stream.Read(), label);
            }
            AreEqualMetricColorMap(saved, loaded);
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
            AntennaAttributes saved = new AntennaAttributes();
            saved.AntennaSections.Add(new AntennaSection("metricA", Color.white));
            saved.AntennaSections.Add(new AntennaSection("metricB", Color.black));
            saved.AntennaWidth = 2.0f;

            const string filename = "antenna.cfg";
            const string label = "Antenna";
            {
                using ConfigWriter writer = new ConfigWriter(filename);
                saved.Save(writer, label);
            }
            AntennaAttributes loaded = new AntennaAttributes();
            {
                using ConfigReader stream = new ConfigReader(filename);
                loaded.Restore(stream.Read(), label);
            }
            AreEqualAntennaSettings(saved, loaded);
        }

        /// <summary>
        /// Test for SEECity.
        /// </summary>
        [Test]
        public void TestSEECity()
        {
            string filename = "seecity.cfg";
            // First save a new city with all its default values.
            SEECity savedCity = NewVanillaSEECity<SEECity>();
            // FIXME: We need tests for the antenna settings
            //savedCity.LeafNodeSettings.AntennaSettings.AntennaSections.Add(new AntennaSection("leafmetric", Color.white));
            //savedCity.InnerNodeSettings.AntennaSettings.AntennaSections.Add(new AntennaSection("innermetric", Color.black));
            VisualNodeAttributes function = new VisualNodeAttributes("Function")
            {
                IsRelevant = true
            };
            VisualNodeAttributes file = new VisualNodeAttributes("File")
            {
                IsRelevant = false
            };
            savedCity.NodeTypes = new Dictionary<string, VisualNodeAttributes>() { { function.NodeType, function }, { file.NodeType, file } };
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

        /// <summary>
        /// Test for SEEEvolutionCity.
        /// </summary>
        [Test]
        public void TestSEEEvolutionCity()
        {
            string filename = "seerandomcity.cfg";
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

        /// <summary>
        /// Test for SEERandomCity.
        /// </summary>
        [Test]
        public void TestSEERandomCity()
        {
            string filename = "seerandomcity.cfg";
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

        /// <summary>
        /// Test for SEEJlgCity.
        /// </summary>
        [Test]
        public void TestSEEJlgCity()
        {
            string filename = "seejlgcity.cfg";
            // First save a new city with all its default values.
            SEEJlgCity savedCity = NewVanillaSEECity<SEEJlgCity>();
            savedCity.Save(filename);

            // Create a new city with all its default values and then
            // wipe out all its attributes to see whether they are correctly
            // restored from the saved configuration file.
            SEEJlgCity loadedCity = NewVanillaSEECity<SEEJlgCity>();
            WipeOutSEEJlgCityAttributes(loadedCity);
            // Load the saved attributes from the configuration file.
            loadedCity.Load(filename);

            SEEJlgCityAttributesAreEqual(savedCity, loadedCity);
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
            AreEqual(expected.GXLPath, actual.GXLPath);
            AreEqual(expected.CSVPath, actual.CSVPath);
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
        /// Checks whether the configuration attributes of <paramref name="expected"/> and
        /// <paramref name="actual"/> are equal.
        /// </summary>
        /// <param name="expected">expected settings</param>
        /// <param name="actual">actual settings</param>
        private static void SEEJlgCityAttributesAreEqual(SEEJlgCity expected, SEEJlgCity actual)
        {
            SEECityAttributesAreEqual(expected, actual);
            AreEqual(expected.JLGPath, actual.JLGPath);
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
            AreEqual(expected.GXLDirectory, actual.GXLDirectory);
            Assert.AreEqual(expected.MaxRevisionsToLoad, actual.MaxRevisionsToLoad);
            Assert.AreEqual(expected.MarkerHeight, actual.MarkerHeight);
            Assert.AreEqual(expected.MarkerWidth, actual.MarkerWidth);
            AreEqual(expected.AdditionBeamColor, actual.AdditionBeamColor);
            AreEqual(expected.ChangeBeamColor, actual.ChangeBeamColor);
            AreEqual(expected.DeletionBeamColor, actual.DeletionBeamColor);
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
            AreEqualCoseGraphSettings(expected.CoseGraphSettings, actual.CoseGraphSettings);
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
            Assert.AreEqual(expected.AnimationDuration, actual.AnimationDuration, 0.001f);
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

        /// <summary>
        /// Checks whether the two data paths <paramref name="expected"/> and <paramref name="actual"/>
        /// are equal (by value).
        /// </summary>
        /// <param name="expected">expected data path</param>
        /// <param name="actual">actual data path</param>
        private static void AreEqual(DataPath expected, DataPath actual)
        {
            Assert.AreEqual(expected.Root, actual.Root);
            Assert.AreEqual(expected.RelativePath, actual.RelativePath);
            Assert.AreEqual(expected.AbsolutePath, actual.AbsolutePath);
        }

        //--------------------------------------------------------
        // attribute modifiers
        //--------------------------------------------------------

        /// <summary>
        /// Assigns all attributes of given <paramref name="city"/> to arbitrary values
        /// different from their default values.
        /// </summary>
        /// <param name="city">the city whose attributes are to be re-assigned</param>
        private static void WipeOutSEECityAttributes(SEECity city)
        {
            WipeOutAbstractSEECityAttributes(city);
            city.GXLPath.Set("C:/MyAbsoluteDirectory/MyAbsoluteFile.gxl");
            city.CSVPath.Set("C:/MyAbsoluteDirectory/MyAbsoluteFile.csv");
        }

        /// <summary>
        /// Assigns all attributes of given <paramref name="city"/> to arbitrary values
        /// different from their default values.
        /// </summary>
        /// <param name="city">the city whose attributes are to be re-assigned</param>
        private void WipeOutSEERandomCityAttributes(SEECityRandom city)
        {
            WipeOutSEECityAttributes(city);
            city.LeafConstraint = new Tools.Constraint(nodeType: "X", edgeType: "Y", nodeNumber: 5, edgeDensity: 0);
            city.InnerNodeConstraint = new Tools.Constraint(nodeType: "N", edgeType: "T", nodeNumber: 1, edgeDensity: 1);
            city.LeafAttributes = new List<Tools.RandomAttributeDescriptor>();
        }

        /// <summary>
        /// Assigns all attributes of given <paramref name="city"/> to arbitrary values
        /// different from their default values.
        /// </summary>
        /// <param name="city">the city whose attributes are to be re-assigned</param>
        private void WipeOutSEEJlgCityAttributes(SEEJlgCity city)
        {
            WipeOutSEECityAttributes(city);
            city.JLGPath = new FilePath("C:/MyAbsoluteDirectory/MyAbsoluteFile.jlg");
        }

        /// <summary>
        /// Assigns all attributes of given <paramref name="city"/> to arbitrary values
        /// different from their default values.
        /// </summary>
        /// <param name="city">the city whose attributes are to be re-assigned</param>
        private static void WipeOutSEEEvolutionCityAttributes(SEECityEvolution city)
        {
            WipeOutAbstractSEECityAttributes(city);
            city.GXLDirectory.Set("C:/MyAbsoluteDirectory/MyAbsoluteFile.gxl");
            city.MaxRevisionsToLoad++;
            city.MarkerHeight++;
            city.MarkerWidth++;
            city.AdditionBeamColor = Color.clear;
            city.ChangeBeamColor = Color.clear;
            city.DeletionBeamColor = Color.clear;
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
            WipeOutCoseGraphSettings(city);
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
            foreach (var settings in city.NodeTypes.Values)
            {
                WipeOutNodeSettings(settings);
            }
        }

        private static void WipeOutCoseGraphSettings(AbstractSEECity city)
        {
            // CoseGraphSettings
            city.CoseGraphSettings.EdgeLength++;
            city.CoseGraphSettings.UseSmartIdealEdgeCalculation = !city.CoseGraphSettings.UseSmartIdealEdgeCalculation;
            city.CoseGraphSettings.UseSmartMultilevelScaling = !city.CoseGraphSettings.UseSmartMultilevelScaling;
            city.CoseGraphSettings.PerLevelIdealEdgeLengthFactor++;
            city.CoseGraphSettings.UseSmartRepulsionRangeCalculation = !city.CoseGraphSettings.UseSmartRepulsionRangeCalculation;
            city.CoseGraphSettings.GravityStrength++;
            city.CoseGraphSettings.CompoundGravityStrength++;
            city.CoseGraphSettings.RepulsionStrength++;
            city.CoseGraphSettings.MultiLevelScaling = !city.CoseGraphSettings.MultiLevelScaling;
            city.CoseGraphSettings.ListInnerNodeToggle = new Dictionary<string, bool>() { { "ID1", true }, { "ID2", false } };
            city.CoseGraphSettings.InnerNodeLayout = new Dictionary<string, NodeLayoutKind>() { { "ID1", NodeLayoutKind.Manhattan }, { "ID2", NodeLayoutKind.Balloon } };
            city.CoseGraphSettings.InnerNodeShape = new Dictionary<string, NodeShapes>() { { "ID1", NodeShapes.Blocks }, { "ID2", NodeShapes.Cylinders } };
            city.CoseGraphSettings.LoadedForNodeTypes = new Dictionary<string, bool>() { { "ID1", false }, { "ID2", true } };
            city.CoseGraphSettings.UseCalculationParameter = !city.CoseGraphSettings.UseCalculationParameter;
            city.CoseGraphSettings.UseIterativeCalculation = !city.CoseGraphSettings.UseIterativeCalculation;
        }

        private static void AreEqualCoseGraphSettings(CoseGraphAttributes expected, CoseGraphAttributes actual)
        {
            // CoseGraphSettings
            Assert.AreEqual(expected.EdgeLength, actual.EdgeLength);
            Assert.AreEqual(expected.UseSmartIdealEdgeCalculation, actual.UseSmartIdealEdgeCalculation);
            Assert.AreEqual(expected.UseSmartMultilevelScaling, actual.UseSmartMultilevelScaling);
            Assert.AreEqual(expected.PerLevelIdealEdgeLengthFactor, actual.PerLevelIdealEdgeLengthFactor);
            Assert.AreEqual(expected.UseSmartRepulsionRangeCalculation, actual.UseSmartRepulsionRangeCalculation);
            Assert.AreEqual(expected.GravityStrength, actual.GravityStrength);
            Assert.AreEqual(expected.CompoundGravityStrength, actual.CompoundGravityStrength);
            Assert.AreEqual(expected.RepulsionStrength, actual.RepulsionStrength);
            Assert.AreEqual(expected.MultiLevelScaling, actual.MultiLevelScaling);
            CollectionAssert.AreEquivalent(expected.ListInnerNodeToggle, actual.ListInnerNodeToggle);
            CollectionAssert.AreEquivalent(expected.InnerNodeLayout, actual.InnerNodeLayout);
            CollectionAssert.AreEquivalent(expected.InnerNodeShape, actual.InnerNodeShape);
            CollectionAssert.AreEquivalent(expected.LoadedForNodeTypes, actual.LoadedForNodeTypes);
            Assert.AreEqual(expected.UseCalculationParameter, actual.UseCalculationParameter);
            Assert.AreEqual(expected.UseIterativeCalculation, actual.UseIterativeCalculation);
        }

        private static void WipeOutErosionSettings(AbstractSEECity city)
        {
            city.ErosionSettings.ShowInnerErosions = !city.ErosionSettings.ShowInnerErosions;
            city.ErosionSettings.ShowLeafErosions = !city.ErosionSettings.ShowLeafErosions;
            city.ErosionSettings.LoadDashboardMetrics = !city.ErosionSettings.LoadDashboardMetrics;
            city.ErosionSettings.IssuesAddedFromVersion = "XXX";
            city.ErosionSettings.OverrideMetrics = !city.ErosionSettings.OverrideMetrics;
            city.ErosionSettings.ShowIssuesInCodeWindow = !city.ErosionSettings.ShowIssuesInCodeWindow;
            city.ErosionSettings.ErosionScalingFactor++;

            city.ErosionSettings.StyleIssue = "X";
            city.ErosionSettings.UniversalIssue = "X";
            city.ErosionSettings.MetricIssue = "X";
            city.ErosionSettings.Dead_CodeIssue = "X";
            city.ErosionSettings.CycleIssue = "X";
            city.ErosionSettings.CloneIssue = "X";
            city.ErosionSettings.ArchitectureIssue = "X";

            city.ErosionSettings.StyleIssue_SUM = "X";
            city.ErosionSettings.UniversalIssue_SUM = "X";
            city.ErosionSettings.MetricIssue_SUM = "X";
            city.ErosionSettings.Dead_CodeIssue_SUM = "X";
            city.ErosionSettings.CycleIssue_SUM = "X";
            city.ErosionSettings.CloneIssue_SUM = "X";
            city.ErosionSettings.ArchitectureIssue_SUM = "X";
        }

        private static void AreEqualErosionSettings(ErosionAttributes expected, ErosionAttributes actual)
        {
            Assert.AreEqual(expected.ShowInnerErosions, actual.ShowInnerErosions);
            Assert.AreEqual(expected.ShowLeafErosions, actual.ShowLeafErosions);
            Assert.AreEqual(expected.LoadDashboardMetrics, actual.LoadDashboardMetrics);
            Assert.AreEqual(expected.IssuesAddedFromVersion, actual.IssuesAddedFromVersion);
            Assert.AreEqual(expected.OverrideMetrics, actual.OverrideMetrics);
            Assert.AreEqual(expected.ShowIssuesInCodeWindow, actual.ShowIssuesInCodeWindow);
            Assert.AreEqual(expected.ErosionScalingFactor, actual.ErosionScalingFactor);

            Assert.AreEqual(expected.StyleIssue, actual.StyleIssue);
            Assert.AreEqual(expected.UniversalIssue, actual.UniversalIssue);
            Assert.AreEqual(expected.MetricIssue, actual.MetricIssue);
            Assert.AreEqual(expected.Dead_CodeIssue, actual.Dead_CodeIssue);
            Assert.AreEqual(expected.CycleIssue, actual.CycleIssue);
            Assert.AreEqual(expected.CloneIssue, actual.CloneIssue);
            Assert.AreEqual(expected.ArchitectureIssue, actual.ArchitectureIssue);

            Assert.AreEqual(expected.StyleIssue_SUM, actual.StyleIssue_SUM);
            Assert.AreEqual(expected.UniversalIssue_SUM, actual.UniversalIssue_SUM);
            Assert.AreEqual(expected.MetricIssue_SUM, actual.MetricIssue_SUM);
            Assert.AreEqual(expected.Dead_CodeIssue_SUM, actual.Dead_CodeIssue_SUM);
            Assert.AreEqual(expected.CycleIssue_SUM, actual.CycleIssue_SUM);
            Assert.AreEqual(expected.CloneIssue_SUM, actual.CloneIssue_SUM);
            Assert.AreEqual(expected.ArchitectureIssue_SUM, actual.ArchitectureIssue_SUM);
        }

        private static void WipeOutEdgeLayoutSettings(AbstractSEECity city)
        {
            city.EdgeLayoutSettings.Kind = EdgeLayoutKind.None;
            city.EdgeLayoutSettings.EdgeWidth++;
            city.EdgeLayoutSettings.EdgesAboveBlocks = !city.EdgeLayoutSettings.EdgesAboveBlocks;
            city.EdgeLayoutSettings.Tension = 0;
            city.EdgeLayoutSettings.RDP = 0;
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
            Assert.AreEqual(expected.RDP, actual.RDP);
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
            city.NodeLayoutSettings.Kind = NodeLayoutKind.CompoundSpringEmbedder;
            city.NodeLayoutSettings.LayoutPath.Set("no path found");
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
            settings.HeightMetric = "X";
            settings.WidthMetric = "X";
            settings.DepthMetric = "X";
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
            Assert.AreEqual(expected.HeightMetric, actual.HeightMetric);
            Assert.AreEqual(expected.WidthMetric, actual.WidthMetric);
            Assert.AreEqual(expected.DepthMetric, actual.DepthMetric);
            Assert.AreEqual(expected.ColorProperty.ColorMetric, actual.ColorProperty.ColorMetric);
            Assert.AreEqual(expected.MinimalBlockLength, actual.MinimalBlockLength);
            Assert.AreEqual(expected.MaximalBlockLength, actual.MaximalBlockLength);
            Assert.AreEqual(expected.OutlineWidth, actual.OutlineWidth);
            AreEqualAntennaSettings(expected.AntennaSettings, actual.AntennaSettings);
            AreEqual(expected.LabelSettings, actual.LabelSettings);
            Assert.AreEqual(expected.ShowNames, actual.ShowNames);
        }

        private static void WipeOutAntennaSettings(ref AntennaAttributes antennaAttributes)
        {
            antennaAttributes.AntennaWidth = 999;
            antennaAttributes.AntennaSections.Clear();
        }

        private static void AreEqualAntennaSettings(AntennaAttributes expected, AntennaAttributes actual)
        {
            Assert.AreEqual(expected.AntennaWidth, actual.AntennaWidth);
            Assert.AreEqual(expected.AntennaSections.Count, actual.AntennaSections.Count);
            for (int i = 0; i < expected.AntennaSections.Count; i++)
            {
                Assert.AreEqual(expected.AntennaSections[i].Metric, actual.AntennaSections[i].Metric);
                Assert.AreEqual(expected.AntennaSections[i].Color, actual.AntennaSections[i].Color);
            }
        }

        private static void WipeOutSharedAttributes(AbstractSEECity city)
        {
            city.LODCulling++;
            city.HierarchicalEdges = new HashSet<string>() { "Nonsense", "Whatever" };
            city.NodeTypes = new Dictionary<string, VisualNodeAttributes>();
            city.ConfigurationPath.Set("C:/MyAbsoluteDirectory/config.cfg");
            city.SourceCodeDirectory.Set("C:/MyAbsoluteDirectory");
            city.SolutionPath.Set("C:/MyAbsoluteDirectory/mysolution.sln");
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

        private static void AreEquivalent(Dictionary<string, VisualNodeAttributes> expected, Dictionary<string, VisualNodeAttributes> actual)
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
            settings.AnimationDuration++;
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

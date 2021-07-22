using System.Collections.Generic;
using NUnit.Framework;
using SEE.Game;
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
        /// Test for SEECity.
        /// </summary>
        [Test]
        public void TestSEECity()
        {
            string filename = "seecity.cfg";
            // First save a new city with all its default values.
            SEECity savedCity = NewVanillaSEECity<SEECity>();
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
        /// Test for SEEDynCity.
        /// </summary>
        [Test]
        public void TestSEEDynCity()
        {
            string filename = "seedyncity.cfg";
            // First save a new city with all its default values.
            SEEDynCity savedCity = NewVanillaSEECity<SEEDynCity>();
            savedCity.Save(filename);

            // Create a new city with all its default values and then 
            // wipe out all its attributes to see whether they are correctly
            // restored from the saved configuration file.
            SEEDynCity loadedCity = NewVanillaSEECity<SEEDynCity>();
            WipeOutSEEDynCityAttributes(loadedCity);
            // Load the saved attributes from the configuration file.
            loadedCity.Load(filename);

            SEEDynCityAttributesAreEqual(savedCity, loadedCity);
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
        private void SEECityAttributesAreEqual(SEECity expected, SEECity actual)
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
        private void SEERandomCityAttributesAreEqual(SEECityRandom expected, SEECityRandom actual)
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
        private void SEEDynCityAttributesAreEqual(SEEDynCity expected, SEEDynCity actual)
        {
            SEECityAttributesAreEqual(expected, actual);
            AreEqual(expected.DYNPath, actual.DYNPath);
        }

        /// <summary>
        /// Checks whether the configuration attributes of <paramref name="expected"/> and 
        /// <paramref name="actual"/> are equal.
        /// </summary>
        /// <param name="expected">expected settings</param>
        /// <param name="actual">actual settings</param>
        private void SEEJlgCityAttributesAreEqual(SEEJlgCity expected, SEEJlgCity actual)
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
        private void AreEqual(IList<RandomAttributeDescriptor> expected, IList<RandomAttributeDescriptor> actual)
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
        private void AreEqual(Constraint expected, Constraint actual)
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
        private void SEEEvolutionCityAttributesAreEqual(SEECityEvolution expected, SEECityEvolution actual)
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
        private void AbstractSEECityAttributesAreEqual(AbstractSEECity expected, AbstractSEECity actual)
        {
            // FIXME reintroduce tests
#if false
            Assert.AreEqual(expected.LODCulling, actual.LODCulling);
            AreEqual(expected.LayoutPath, actual.LayoutPath);
            CollectionAssert.AreEquivalent(expected.HierarchicalEdges, actual.HierarchicalEdges);
            CollectionAssert.AreEquivalent(expected.SelectedNodeTypes, actual.SelectedNodeTypes);
            AreEqual(expected.CityPath, actual.CityPath);
            AreEqual(expected.LeafNodeColorRange, actual.LeafNodeColorRange);
            AreEqual(expected.InnerNodeColorRange, actual.InnerNodeColorRange);
            Assert.AreEqual(expected.WidthMetric, actual.WidthMetric);
            Assert.AreEqual(expected.HeightMetric, actual.HeightMetric);
            Assert.AreEqual(expected.DepthMetric, actual.DepthMetric);
            Assert.AreEqual(expected.LeafStyleMetric, actual.LeafStyleMetric);
            AreEqual(expected.LeafLabelSettings, actual.LeafLabelSettings);
            AreEqual(expected.InnerNodeLabelSettings, actual.InnerNodeLabelSettings);

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

            Assert.AreEqual(expected.InnerDonutMetric, actual.InnerDonutMetric);
            Assert.AreEqual(expected.InnerNodeHeightMetric, actual.InnerNodeHeightMetric);

            Assert.AreEqual(expected.MinimalBlockLength, actual.MinimalBlockLength);
            Assert.AreEqual(expected.MaximalBlockLength, actual.MaximalBlockLength);

            Assert.AreEqual(expected.LeafObjects, actual.LeafObjects);
            Assert.AreEqual(expected.InnerNodeObjects, actual.InnerNodeObjects);

            Assert.AreEqual(expected.NodeLayout, actual.NodeLayout);
            Assert.AreEqual(expected.EdgeLayout, actual.EdgeLayout);

            Assert.AreEqual(expected.ZScoreScale, actual.ZScoreScale);
            Assert.AreEqual(expected.EdgeWidth, actual.EdgeWidth);
            Assert.AreEqual(expected.ShowErosions, actual.ShowErosions);
            Assert.AreEqual(expected.MaxErosionWidth, actual.MaxErosionWidth); //FIXME: Name changed
            Assert.AreEqual(expected.EdgesAboveBlocks, actual.EdgesAboveBlocks);
            Assert.AreEqual(expected.Tension, actual.Tension);
            Assert.AreEqual(expected.RDP, actual.RDP);

            // CoseGraphSettings
            Assert.AreEqual(expected.coseGraphSettings.EdgeLength, actual.coseGraphSettings.EdgeLength);
            Assert.AreEqual(expected.coseGraphSettings.UseSmartIdealEdgeCalculation, actual.coseGraphSettings.UseSmartIdealEdgeCalculation);
            Assert.AreEqual(expected.coseGraphSettings.UseSmartMultilevelScaling, actual.coseGraphSettings.UseSmartMultilevelScaling);
            Assert.AreEqual(expected.coseGraphSettings.PerLevelIdealEdgeLengthFactor, actual.coseGraphSettings.PerLevelIdealEdgeLengthFactor);
            Assert.AreEqual(expected.coseGraphSettings.UseSmartRepulsionRangeCalculation, actual.coseGraphSettings.UseSmartRepulsionRangeCalculation);
            Assert.AreEqual(expected.coseGraphSettings.GravityStrength, actual.coseGraphSettings.GravityStrength);
            Assert.AreEqual(expected.coseGraphSettings.CompoundGravityStrength, actual.coseGraphSettings.CompoundGravityStrength);
            Assert.AreEqual(expected.coseGraphSettings.RepulsionStrength, actual.coseGraphSettings.RepulsionStrength);
            Assert.AreEqual(expected.coseGraphSettings.MultiLevelScaling, actual.coseGraphSettings.MultiLevelScaling);
            Assert.AreEqual(expected.coseGraphSettings.MultiLevelScaling, actual.coseGraphSettings.MultiLevelScaling);
            CollectionAssert.AreEquivalent(expected.coseGraphSettings.ListInnerNodeToggle, actual.coseGraphSettings.ListInnerNodeToggle);
            CollectionAssert.AreEquivalent(expected.coseGraphSettings.InnerNodeLayout, actual.coseGraphSettings.InnerNodeLayout);
            CollectionAssert.AreEquivalent(expected.coseGraphSettings.InnerNodeShape, actual.coseGraphSettings.InnerNodeShape);
            CollectionAssert.AreEquivalent(expected.coseGraphSettings.LoadedForNodeTypes, actual.coseGraphSettings.LoadedForNodeTypes);
            Assert.AreEqual(expected.coseGraphSettings.UseCalculationParameter, actual.coseGraphSettings.UseCalculationParameter);
            Assert.AreEqual(expected.coseGraphSettings.UseIterativeCalculation, actual.coseGraphSettings.UseIterativeCalculation);
#endif
        }

        /// <summary>
        /// Checks whether the two label settings <paramref name="expected"/> and <paramref name="actual"/>
        /// are equal (by value).
        /// </summary>
        /// <param name="expected">expected label setting</param>
        /// <param name="actual">actual label setting</param>
        private void AreEqual(LabelSettings expected, LabelSettings actual)
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
        private void AreEqual(ColorRange expected, ColorRange actual)
        {
            AreEqual(expected.lower, actual.lower);
            AreEqual(expected.upper, actual.upper);
            Assert.AreEqual(expected.NumberOfColors, actual.NumberOfColors);
        }

        /// <summary>
        /// Checks whether the two colors <paramref name="expected"/> and <paramref name="actual"/>
        /// are equal (by value).
        /// </summary>
        /// <param name="expected">expected color</param>
        /// <param name="actual">actual color</param>
        private void AreEqual(Color expected, Color actual)
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
        private void AreEqual(DataPath expected, DataPath actual)
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
        private void WipeOutSEEDynCityAttributes(SEEDynCity city)
        {
            WipeOutSEECityAttributes(city);
            city.DYNPath = new DataPath("C:/MyAbsoluteDirectory/MyAbsoluteFile.dyn");
        }

        /// <summary>
        /// Assigns all attributes of given <paramref name="city"/> to arbitrary values
        /// different from their default values.
        /// </summary>
        /// <param name="city">the city whose attributes are to be re-assigned</param>
        private void WipeOutSEEJlgCityAttributes(SEEJlgCity city)
        {
            WipeOutSEECityAttributes(city);
            city.JLGPath = new DataPath("C:/MyAbsoluteDirectory/MyAbsoluteFile.jlg");
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
            // FIXME reintroduce tests
#if false
            city.LODCulling++;
            city.LayoutPath.Set("C:/MyAbsoluteDirectory/MyAbsoluteFile.gvl");
            city.HierarchicalEdges = new HashSet<string>() { "Nonsense", "Whatever" };
            city.SelectedNodeTypes = new Dictionary<string, bool>() { { "Routine", true }, { "Class", false } };
            city.CityPath.Set("C:/MyAbsoluteDirectory/config.cfg");
            city.LeafNodeColorRange = new ColorRange(Color.clear, Color.clear, 2);
            city.InnerNodeColorRange = new ColorRange(Color.clear, Color.clear, 10);
            city.WidthMetric = "M1";
            city.HeightMetric = "M2";
            city.DepthMetric = "M3";
            city.LeafStyleMetric = "M4";

            city.StyleIssue = "X";
            city.UniversalIssue = "X";
            city.MetricIssue = "X";
            city.Dead_CodeIssue = "X";
            city.CycleIssue = "X";
            city.CloneIssue = "X";
            city.ArchitectureIssue = "X";

            city.StyleIssue_SUM = "X";
            city.UniversalIssue_SUM = "X";
            city.MetricIssue_SUM = "X";
            city.Dead_CodeIssue_SUM = "X";
            city.CycleIssue_SUM = "X";
            city.CloneIssue_SUM = "X";
            city.ArchitectureIssue_SUM = "X";

            city.InnerDonutMetric = "X";
            city.InnerNodeHeightMetric = "X";
            city.InnerNodeStyleMetric = "X";

            city.MinimalBlockLength++;
            city.MaximalBlockLength++;
            city.LeafObjects = AbstractSEECity.LeafNodeKinds.Blocks;
            city.InnerNodeObjects = AbstractSEECity.InnerNodeKinds.Empty;

            city.NodeLayout = NodeLayoutKind.CompoundSpringEmbedder;
            city.EdgeLayout = Layout.EdgeLayouts.EdgeLayoutKind.Straight;

            city.ZScoreScale = !city.ZScoreScale;
            city.EdgeWidth++;
            city.ShowErosions = !city.ShowErosions;
            city.MaxErosionWidth++; //FIXME: Name changed
            city.EdgesAboveBlocks = !city.EdgesAboveBlocks;
            city.Tension = 0.0f;
            city.RDP = 10;

            WipeOutLabelSettings(ref city.LeafLabelSettings);
            WipeOutLabelSettings(ref city.InnerNodeLabelSettings);

            // CoseGraphSettings
            city.coseGraphSettings.EdgeLength++;
            city.coseGraphSettings.UseSmartIdealEdgeCalculation = !city.coseGraphSettings.UseSmartIdealEdgeCalculation;
            city.coseGraphSettings.UseSmartMultilevelScaling = !city.coseGraphSettings.UseSmartMultilevelScaling;
            city.coseGraphSettings.PerLevelIdealEdgeLengthFactor++;
            city.coseGraphSettings.UseSmartRepulsionRangeCalculation = !city.coseGraphSettings.UseSmartRepulsionRangeCalculation;
            city.coseGraphSettings.GravityStrength++;
            city.coseGraphSettings.CompoundGravityStrength++;
            city.coseGraphSettings.RepulsionStrength++;
            city.coseGraphSettings.MultiLevelScaling = !city.coseGraphSettings.MultiLevelScaling;
            city.coseGraphSettings.ListInnerNodeToggle = new Dictionary<string, bool>() { { "ID1", true }, { "ID2", false } };
            city.coseGraphSettings.InnerNodeLayout = new Dictionary<string, NodeLayoutKind>() { { "ID1", NodeLayoutKind.Manhattan }, { "ID2", NodeLayoutKind.Balloon } };
            city.coseGraphSettings.InnerNodeShape = new Dictionary<string, AbstractSEECity.InnerNodeKinds>() { { "ID1", AbstractSEECity.InnerNodeKinds.Blocks }, { "ID2", AbstractSEECity.InnerNodeKinds.Circles } };
            city.coseGraphSettings.LoadedForNodeTypes = new Dictionary<string, bool>() { { "ID1", false }, { "ID2", true } };
            city.coseGraphSettings.UseCalculationParameter = !city.coseGraphSettings.UseCalculationParameter;
            city.coseGraphSettings.UseIterativeCalculation = !city.coseGraphSettings.UseIterativeCalculation;
#endif
        }

        /// <summary>
        /// Modifies all attributes of <paramref name="settings"/>.
        /// </summary>
        /// <param name="settings">settings whose attributes are to be modified</param>
        private static void WipeOutLabelSettings(ref LabelSettings settings)
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
            GameObject go = new GameObject();
            T city = go.AddComponent<T>();
            return city;
        }
    }
}

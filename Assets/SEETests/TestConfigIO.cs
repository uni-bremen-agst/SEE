using NUnit.Framework;
using SEE.Game;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Test cases for ConfigIO.
    /// </summary>
    class TestConfigIO
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

        private const string filename = "seecity.cfg";

        [Test]
        public void TestSEECity()
        {
            SEECity savedCity = NewSEECity();
            savedCity.Save(filename);

            SEECity loadedCity = NewVanillaSEECity();
            loadedCity.Load(filename);

            Assert.AreEqual(savedCity.LODCulling, loadedCity.LODCulling);
            AreEqual(savedCity.LayoutPath, loadedCity.LayoutPath);
            CollectionAssert.AreEquivalent(savedCity.HierarchicalEdges, loadedCity.HierarchicalEdges);
            CollectionAssert.AreEquivalent(savedCity.SelectedNodeTypes, loadedCity.SelectedNodeTypes);
            AreEqual(savedCity.CityPath, loadedCity.CityPath);
            AreEqual(savedCity.LeafNodeColorRange, loadedCity.LeafNodeColorRange);
            AreEqual(savedCity.InnerNodeColorRange, loadedCity.InnerNodeColorRange);
            Assert.AreEqual(savedCity.WidthMetric, loadedCity.WidthMetric);
            Assert.AreEqual(savedCity.HeightMetric, loadedCity.HeightMetric);
            Assert.AreEqual(savedCity.DepthMetric, loadedCity.DepthMetric);
            Assert.AreEqual(savedCity.LeafStyleMetric, loadedCity.LeafStyleMetric);
            AreEqual(savedCity.LeafLabelSettings, loadedCity.LeafLabelSettings);
            AreEqual(savedCity.InnerNodeLabelSettings, loadedCity.InnerNodeLabelSettings);
            
            // Assert.AreEqual(savedCity., loadedCity.);
            //Assert.AreEqual(savedCity, loadedCity);
        }

        private void AreEqual(LabelSettings expected, LabelSettings actual)
        {
            Assert.AreEqual(expected.Show, actual.Show);
            Assert.AreEqual(expected.FontSize, actual.FontSize, 0.001f);
            Assert.AreEqual(expected.Distance, actual.Distance, 0.001f);
        }

        private void AreEqual(ColorRange expected, ColorRange actual)
        {
            AreEqual(expected.lower, actual.lower);
            AreEqual(expected.upper, actual.upper);
            Assert.AreEqual(expected.NumberOfColors, actual.NumberOfColors);
        }

        private void AreEqual(Color expected, Color actual)
        {
            Assert.AreEqual(expected.r, actual.r, 0.001f);
            Assert.AreEqual(expected.g, actual.g, 0.001f);
            Assert.AreEqual(expected.b, actual.b, 0.001f);
            Assert.AreEqual(expected.a, actual.a, 0.001f);
        }

        private void AreEqual(DataPath expected, DataPath actual)
        {
            Assert.AreEqual(expected.Root, actual.Root);
            Assert.AreEqual(expected.RelativePath, actual.RelativePath);
            Assert.AreEqual(expected.AbsolutePath, actual.AbsolutePath);            
        }

        private static SEECity NewSEECity()
        {
            SEECity city = NewVanillaSEECity();
            city.LayoutPath.Set("C:/MyAbsoluteDirectory/MyAbsoluteFile.gvl");
            city.LODCulling = 1.0f;
            city.SelectedNodeTypes = new Dictionary<string, bool>() { { "Routine", true }, { "Class", false } };
            return city;
        }

        private static SEECity NewVanillaSEECity()
        {
            GameObject go = new GameObject();
            SEECity city = go.AddComponent<SEECity>();
            return city;
        }
    }
}

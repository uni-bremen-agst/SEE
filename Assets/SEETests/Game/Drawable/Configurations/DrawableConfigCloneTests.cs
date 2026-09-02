using NUnit.Framework;
using System.Collections.Generic;

namespace SEE.Game.Drawable.Configurations
{
    /// <summary>
    /// Tests the cloning semantics of <see cref="DrawableConfig"/>.
    /// </summary>
    [TestFixture]
    public class DrawableConfigCloneTests
    {
        /// <summary>
        /// Verifies that line configurations contained in a drawable configuration are cloned independently.
        /// </summary>
        [Test]
        public void CloneCopiesLineConfigurations()
        {
            LineConf line = new()
            {
                ID = "Line"
            };

            DrawableConfig original = new();
            original.LineConfigs.Add(line);

            DrawableConfig clone = original.Clone();

            Assert.That(clone.LineConfigs, Is.Not.SameAs(original.LineConfigs));
            Assert.That(clone.LineConfigs[0], Is.Not.SameAs(original.LineConfigs[0]));

            clone.LineConfigs[0].ID = "Changed";

            Assert.That(original.LineConfigs[0].ID, Is.EqualTo("Line"));
        }

        /// <summary>
        /// Verifies that text configurations contained in a drawable configuration are cloned independently.
        /// </summary>
        [Test]
        public void CloneCopiesTextConfigurations()
        {
            TextConf text = new()
            {
                ID = "Text",
                Text = "Original"
            };

            DrawableConfig original = new();
            original.TextConfigs.Add(text);

            DrawableConfig clone = original.Clone();

            Assert.That(clone.TextConfigs, Is.Not.SameAs(original.TextConfigs));
            Assert.That(clone.TextConfigs[0], Is.Not.SameAs(original.TextConfigs[0]));

            clone.TextConfigs[0].Text = "Changed";

            Assert.That(original.TextConfigs[0].Text, Is.EqualTo("Original"));
        }

        /// <summary>
        /// Verifies that image configurations contained in a drawable configuration are cloned independently.
        /// </summary>
        [Test]
        public void CloneCopiesImageConfigurations()
        {
            ImageConf image = new()
            {
                ID = "Image",
                FileData = new byte[] { 1, 2, 3 }
            };

            DrawableConfig original = new();
            original.ImageConfigs.Add(image);

            DrawableConfig clone = original.Clone();

            Assert.That(clone.ImageConfigs, Is.Not.SameAs(original.ImageConfigs));
            Assert.That(clone.ImageConfigs[0], Is.Not.SameAs(original.ImageConfigs[0]));

            clone.ImageConfigs[0].ID = "Changed";

            Assert.That(original.ImageConfigs[0].ID, Is.EqualTo("Image"));
        }

        /// <summary>
        /// Verifies that mind map node configurations contained in a drawable configuration are cloned independently.
        /// </summary>
        [Test]
        public void CloneCopiesMindMapNodeConfigurations()
        {
            MindMapNodeConf node = new()
            {
                ID = "Node",
                BorderConf = new LineConf(),
                TextConf = new TextConf(),
                BranchLineConf = new LineConf(),
                Children = new Dictionary<UnityEngine.GameObject, UnityEngine.GameObject>()
            };

            DrawableConfig original = new();
            original.MindMapNodeConfigs.Add(node);

            DrawableConfig clone = original.Clone();

            Assert.That(clone.MindMapNodeConfigs, Is.Not.SameAs(original.MindMapNodeConfigs));
            Assert.That(clone.MindMapNodeConfigs[0], Is.Not.SameAs(original.MindMapNodeConfigs[0]));

            clone.MindMapNodeConfigs[0].ID = "Changed";

            Assert.That(original.MindMapNodeConfigs[0].ID, Is.EqualTo("Node"));
        }
    }
}

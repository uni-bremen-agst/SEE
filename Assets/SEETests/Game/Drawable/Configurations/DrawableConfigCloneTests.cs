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

        /// <summary>
        /// Verifies that cloning only the surface configuration copies its values
        /// without copying any contained drawable type configurations.
        /// </summary>
        [Test]
        public void CloneWithoutDrawableTypesCopiesOnlySurfaceConfiguration()
        {
            DrawableConfig original = new()
            {
                ID = "Surface",
                ParentID = "Parent",
                Position = new UnityEngine.Vector3(1f, 2f, 3f),
                Rotation = new UnityEngine.Vector3(4f, 5f, 6f),
                Scale = new UnityEngine.Vector3(7f, 8f, 9f),
                Color = UnityEngine.Color.red,
                Order = 2,
                Lighting = true,
                OrderInLayer = 3,
                Description = "Description",
                Visibility = true,
                CurrentPage = 4,
                MaxPageSize = 5
            };

            original.LineConfigs.Add(new LineConf());
            original.TextConfigs.Add(new TextConf());
            original.ImageConfigs.Add(new ImageConf());
            original.MindMapNodeConfigs.Add(new MindMapNodeConf());

            DrawableConfig clone = original.CloneWithoutDrawableTypes();

            Assert.That(clone.ID, Is.EqualTo(original.ID));
            Assert.That(clone.ParentID, Is.EqualTo(original.ParentID));
            Assert.That(clone.Position, Is.EqualTo(original.Position));
            Assert.That(clone.Rotation, Is.EqualTo(original.Rotation));
            Assert.That(clone.Scale, Is.EqualTo(original.Scale));
            Assert.That(clone.Color, Is.EqualTo(original.Color));
            Assert.That(clone.Order, Is.EqualTo(original.Order));
            Assert.That(clone.Lighting, Is.EqualTo(original.Lighting));
            Assert.That(clone.OrderInLayer, Is.EqualTo(original.OrderInLayer));
            Assert.That(clone.Description, Is.EqualTo(original.Description));
            Assert.That(clone.Visibility, Is.EqualTo(original.Visibility));
            Assert.That(clone.CurrentPage, Is.EqualTo(original.CurrentPage));
            Assert.That(clone.MaxPageSize, Is.EqualTo(original.MaxPageSize));

            Assert.That(clone.LineConfigs, Is.Empty);
            Assert.That(clone.TextConfigs, Is.Empty);
            Assert.That(clone.ImageConfigs, Is.Empty);
            Assert.That(clone.MindMapNodeConfigs, Is.Empty);

            Assert.That(clone.LineConfigs, Is.Not.SameAs(original.LineConfigs));
            Assert.That(clone.TextConfigs, Is.Not.SameAs(original.TextConfigs));
            Assert.That(clone.ImageConfigs, Is.Not.SameAs(original.ImageConfigs));
            Assert.That(clone.MindMapNodeConfigs, Is.Not.SameAs(original.MindMapNodeConfigs));
        }
    }
}

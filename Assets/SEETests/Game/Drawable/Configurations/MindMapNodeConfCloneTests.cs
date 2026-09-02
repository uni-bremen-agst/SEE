using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SEE.Game.Drawable.Configurations
{
    /// <summary>
    /// Tests the cloning semantics of <see cref="MindMapNodeConf"/>.
    /// </summary>
    [TestFixture]
    public class MindMapNodeConfCloneTests
    {
        /// <summary>
        /// Verifies that nested configuration objects are cloned independently.
        /// </summary>
        [Test]
        public void CloneCopiesNestedConfigurations()
        {
            MindMapNodeConf original = new()
            {
                BorderConf = new LineConf
                {
                    ID = "Border"
                },
                TextConf = new TextConf
                {
                    ID = "Text",
                    Text = "Original"
                },
                BranchLineConf = new LineConf
                {
                    ID = "Branch"
                }
            };

            MindMapNodeConf clone = original.Clone();

            Assert.That(clone.BorderConf, Is.Not.Null);
            Assert.That(clone.TextConf, Is.Not.Null);
            Assert.That(clone.BranchLineConf, Is.Not.Null);

            Assert.That(clone.BorderConf, Is.Not.SameAs(original.BorderConf));
            Assert.That(clone.TextConf, Is.Not.SameAs(original.TextConf));
            Assert.That(clone.BranchLineConf, Is.Not.SameAs(original.BranchLineConf));

            clone.BorderConf.ID = "ChangedBorder";
            clone.TextConf.Text = "ChangedText";
            clone.BranchLineConf.ID = "ChangedBranch";

            Assert.That(original.BorderConf.ID, Is.EqualTo("Border"));
            Assert.That(original.TextConf.Text, Is.EqualTo("Original"));
            Assert.That(original.BranchLineConf.ID, Is.EqualTo("Branch"));
        }

        /// <summary>
        /// Verifies that the children collection is copied independently while retaining its contained game objects.
        /// </summary>
        [Test]
        public void CloneCopiesChildrenCollection()
        {
            GameObject child = new("Child");
            GameObject branch = new("Branch");

            try
            {
                MindMapNodeConf original = new()
                {
                    Children = new Dictionary<GameObject, GameObject>
                    {
                        [child] = branch
                    }
                };

                MindMapNodeConf clone = original.Clone();

                Assert.That(clone.Children, Is.Not.Null);
                Assert.That(clone.Children, Is.Not.SameAs(original.Children));
                Assert.That(clone.Children.Count, Is.EqualTo(1));
                Assert.That(clone.Children[child], Is.SameAs(branch));

                clone.Children.Clear();

                Assert.That(original.Children.Count, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(child);
                Object.DestroyImmediate(branch);
            }
        }

        /// <summary>
        /// Verifies that the serialized child name mapping is copied independently.
        /// </summary>
        [Test]
        public void CloneCopiesChildrenNamesCollection()
        {
            MindMapNodeConf original = new();
            Dictionary<string, string> originalChildrenNames = GetChildrenNames(original);
            originalChildrenNames.Add("Child", "Branch");

            MindMapNodeConf clone = original.Clone();
            Dictionary<string, string> cloneChildrenNames = GetChildrenNames(clone);

            Assert.That(cloneChildrenNames, Is.Not.SameAs(originalChildrenNames));
            Assert.That(cloneChildrenNames, Is.EqualTo(originalChildrenNames));

            cloneChildrenNames.Clear();

            Assert.That(originalChildrenNames.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// Returns the private child name mapping of the given configuration.
        /// </summary>
        /// <param name="configuration">The configuration whose child name mapping should be returned.</param>
        /// <returns>The child name mapping.</returns>
        private static Dictionary<string, string> GetChildrenNames(MindMapNodeConf configuration)
        {
            FieldInfo field = typeof(MindMapNodeConf).GetField("childrenNames",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new AssertionException("Could not find field 'childrenNames'.");

            return field.GetValue(configuration) as Dictionary<string, string>
                ?? throw new AssertionException("Field 'childrenNames' does not contain a dictionary.");
        }
    }
}

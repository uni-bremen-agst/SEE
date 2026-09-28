using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using SEE.Utils;

namespace SEE.Controls.ReversibleActions
{
    /// <summary>
    /// Tests for the <see cref="AbstractActionStateType"/> class and its subclasses
    /// <see cref="ActionStateType"/> and <see cref="ActionStateTypeGroup"/>.
    /// </summary>
    internal class TestActionStateType
    {
        private Forest<AbstractActionStateType> allRootTypes;

        [SetUp]
        public void SetUp()
        {
            allRootTypes = ActionStateTypes.AllRootTypes;
        }

        /// <summary>
        /// Returns the types of all public static fields of <see cref="ActionStateTypes"/>
        /// whose type is a subclass of <see cref="AbstractActionStateType"/>. This method
        /// basically returns the types of "public static readonly ActionStateType Move" and
        /// its siblings.
        /// </summary>
        /// <returns>uses of <see cref="AbstractActionStateType"/> in <see cref="ActionStateTypes"/></returns>
        private static IEnumerable<AbstractActionStateType> GetAbstractActionStateTypes()
        {
            return typeof(ActionStateTypes).GetFields(BindingFlags.Public | BindingFlags.Static)
                                           .Where(f => f.FieldType.IsSubclassOf(typeof(AbstractActionStateType)))
                                           .Select(x => (AbstractActionStateType)x.GetValue(null));
        }

        private static List<AbstractActionStateType> GetAllRootTypes()
        {
            return GetAbstractActionStateTypes().Where(a => a.Parent == null).ToList();
        }

        private static List<AbstractActionStateType> GetAllTypes()
        {
            return GetAbstractActionStateTypes().ToList();
        }

        [Test]
        public void AllActionsPresent()
        {
            Assert.That(allRootTypes.AllElements(), Is.EqualTo(GetAllTypes()),
                        "ActionStateTypes.AllRootTypes.AllElements() must contain all action types and only those!\n"
                        + " And the order must be preserved.\n"
                        + DumpDiff(GetAllTypes(), allRootTypes.AllElements())
                        + "\n");
        }

        private string DumpDiff(List<AbstractActionStateType> expected, IList<AbstractActionStateType> actual)
        {
            bool areDifferent = false;
            StringBuilder sb = new();
            foreach (AbstractActionStateType type in expected.Except(actual))
            {
                sb.AppendLine($"Expected {type.Name} not found!\n");
                areDifferent = true;
            }
            foreach (AbstractActionStateType type in actual.Except(expected))
            {
                sb.AppendLine($"Actual {type.Name} not expected!\n");
                areDifferent = true;
            }
            if (areDifferent)
            {
                return sb.ToString();
            }
            // The difference lies in the order.
            for (int i = 0; i < expected.Count; i++)
            {
                if (!expected[i].Equals(actual[i]))
                {
                    return $"Expected {expected[i].Name} at index {i}, but got {actual[i].Name}!\n";
                }
            }
            return string.Empty;
        }

        [Test]
        public void ActionStateTypesAllRootTypesJustContainsAllRoots()
        {
            Assert.That(allRootTypes.ToList(), Is.EqualTo(GetAllRootTypes()),
                        "ActionStateTypes.AllRootTypes must contain all of its root types and only those!"
                        + " And the order must be preserved.");
        }

        [Test]
        public void TestNoAttributeNull()
        {
            Assert.That(allRootTypes.AllElements()
                                    .Where(x => x.Description == null || x.Name == null || x.Icon == default),
                        Is.Empty,
                        "No attribute of an AbstractActionStateType may be null or default!");
        }

        [Test]
        public void TestNameUnique()
        {
            Assert.That(allRootTypes.AllElements().Select(x => x.Name).Distinct().Count(),
                        Is.EqualTo(allRootTypes.AllElements().Count),
                        "Names of AbstractActionStateType must be unique!");
        }

        public static IEnumerable<TestCaseData> AllTypeSupplier()
        {
            return ActionStateTypes.AllRootTypes.AllElements().Select(type => new TestCaseData(type));
        }

        [Test, TestCaseSource(nameof(AllTypeSupplier))]
        public void TestEquality(AbstractActionStateType type)
        {
            Assert.That(type, Is.Not.Null, "Type must not be null.");
            // We redefined `Equals`. The purpose of this test is to check
            // whether the redefinition yields true when comparing the type with itself.
            Assert.That(type.Equals(type), Is.True, $"{type.Name} must be equal to itself.");
            // We redefined `Equals`. The purpose of this test is to check whether
            // the redefinition yields false when comparing against null.
            Assert.That(type.Equals(null), Is.False, $"{type.Name} must not be equal to null.");
            Assert.That(allRootTypes.AllElements().Where(type.Equals).Count(), Is.EqualTo(1),
                        "An ActionStateType must only be equal to itself!");
        }
    }
}

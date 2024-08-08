using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SEE.Utils;

namespace SEE.Controls.Actions
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
            Assert.AreEqual(GetAllTypes(), allRootTypes.AllElements(),
                            "ActionStateTypes.AllRootTypes.AllElements() must contain all action types and only those!"
                            + " And the order must be preserved.");
        }

        [Test]
        public void ActionStateTypesAllRootTypesJustContainsAllRoots()
        {
            Assert.AreEqual(GetAllRootTypes(), allRootTypes.ToList(),
                            "ActionStateTypes.AllRootTypes must contain all of its root types and only those!"
                            + " And the order must be preserved.");
        }

        [Test]
        public void TestNoAttributeNull()
        {
            Assert.IsEmpty(allRootTypes.AllElements().Where(x => x.Description == null || x.Name == null || x.Icon == default),
                "No attribute of an AbstractActionStateType may be null or default!");
        }

        [Test]
        public void TestNameUnique()
        {
            Assert.AreEqual(allRootTypes.AllElements().Count, allRootTypes.AllElements().Select(x => x.Name).Distinct().Count(),
                            "Names of AbstractActionStateType must be unique!");
        }

        public static IEnumerable<TestCaseData> AllTypeSupplier()
        {
            return ActionStateTypes.AllRootTypes.AllElements().Select(type => new TestCaseData(type));
        }

        [Test, TestCaseSource(nameof(AllTypeSupplier))]
        public void TestEquality(AbstractActionStateType type)
        {
            Assert.IsTrue(type.Equals(type));
            Assert.IsFalse(type.Equals(null));
            Assert.AreEqual(1, allRootTypes.AllElements().Where(type.Equals).Count(),
                            "An ActionStateType must only be equal to itself!");
        }
    }
}

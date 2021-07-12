using System;
using System.Collections.Generic;
using System.Linq;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.UI.PropertyDialog
{
    /// <summary>
    /// Represents a logical unit of properties that can be changed in a dialog.
    /// </summary>
    /// <seealso cref="Property"/>
    public class PropertyGroup : PlatformDependentComponent
    {
        /// <summary>
        /// Name of the property group.
        /// </summary>
        public string Name;
        /// <summary>
        /// Icon for the property group.
        /// TODO: This is unused.
        /// </summary>
        public Sprite Icon;

        /// <summary>
        /// The game object representing the group of properties. It is an instantiation
        /// of the prefab <see cref="configurationGroupPrefabPath"/>. It will be an
        /// ascendant of the UI canvas. Its children will be the input fields for the
        /// <see cref="Properties"/>.
        /// </summary>
        public GameObject PropertyGroupUIObject;

        /// <summary>
        /// The input fields for the properties in this property group.
        /// </summary>
        private readonly IList<Property> Properties = new List<Property>();

        /// <summary>
        /// The path of the prefab for a PropertyGroup.
        /// </summary>
        private const string configurationGroupPrefabPath = "Prefabs/UI/PropertyGroup";

        /// <summary>
        /// Makes <paramref name="parent"/> the parent of <see cref="PropertyGroupUIObject"/>.
        /// If <see cref="PropertyGroupUIObject"/> is null, it will be created first.
        /// </summary>
        /// <param name="parent">parent of <see cref="PropertyGroupUIObject"/></param>
        public void SetParent(GameObject parent)
        {
            if (PropertyGroupUIObject == null)
            {
                PropertyGroupUIObject = CreatePropertyUIObject(Name);
            }
            // Reset scale to (1,1,1), otherwise it might be changed
            PropertyGroupUIObject.transform.SetParent(parent.transform);
            PropertyGroupUIObject.transform.localScale = Vector3.one;
            ((RectTransform)PropertyGroupUIObject.transform).sizeDelta = Vector2.zero;
            PropertyGroupUIObject.transform.localPosition = Vector3.zero;
        }

        /// <summary>
        /// Adds a <paramref name="property"/> to this group's <see cref="Properties"/>.
        /// The names of the properties must be unique within the group's <see cref="Properties"/>.
        /// If a name exists already, an exception will be thrown.
        /// </summary>
        /// <param name="property">The property to add to this group.</param>
        public void AddProperty(Property property)
        {
            if (Properties.Any(x => x.Name == property.Name))
            {
                throw new InvalidOperationException($"Property with the given name '{property.Name}' already exists!\n");
            }
            Properties.Add(property);
        }

        /// <summary>
        /// Creates <see cref="PropertyGroupUIObject"/> and makes all properties in
        /// <see cref="Properties"/> its children.
        /// </summary>
        protected override void StartDesktop()
        {
            if (PropertyGroupUIObject == null)
            {
                PropertyGroupUIObject = CreatePropertyUIObject(Name);
            }
            foreach (Property property in Properties)
            {
                property.SetParent(PropertyGroupUIObject);
            }
        }

        /// <summary>
        /// Returns a new instantiation of prefab <see cref="configurationGroupPrefabPath"/>
        /// with given <paramref name="name"/>.
        /// </summary>
        /// <param name="name">name of the instantiated object</param>
        /// <returns>instantiation of prefab <see cref="configurationGroupPrefabPath"/></returns>
        private static GameObject CreatePropertyUIObject(string name)
        {
            GameObject result = PrefabInstantiator.InstantiatePrefab(configurationGroupPrefabPath, instantiateInWorldSpace: false);
            result.name = name;
            return result;
        }
    }
}

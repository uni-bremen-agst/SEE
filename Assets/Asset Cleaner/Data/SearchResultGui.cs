using System.Collections.Generic;
using Leopotam.Ecs;
using UnityEditor;
using UnityEngine;

namespace Asset_Cleaner {
    class SearchResultGui : IEcsAutoReset {
        public SerializedObject SerializedObject;
        public List<PropertyData> Properties;
        public GUIContent Label;
        public string TransformPath;

        public void Reset() {
            SerializedObject?.Dispose();
            SerializedObject = default;

            if (Properties != default)
                foreach (var propertyData in Properties) {
                    propertyData.Property.Dispose();
                }

            Properties = default;
            Label = default;
            TransformPath = default;
        }

        public class PropertyData {
            public SerializedProperty Property;
            public GUIContent Content;
        }
    }
}
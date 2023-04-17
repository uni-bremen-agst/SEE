using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Asset_Cleaner {
    static class IgnoreTypes {
        public static bool Check(string path, out Type type) {
            type = AssetDatabase.GetMainAssetTypeAtPath(path);
            if (type == null) return false;
            var conf = Globals<Config>.Value;
            if (conf == null) return false;
            
            if (type.IsAssignableFromInverse(typeof(MonoScript))) return true;
            if (type.IsAssignableFromInverse(typeof(DefaultAsset))) return true;
            if (conf.IgnoreScriptable && type.IsAssignableFromInverse(typeof(ScriptableObject))) return true;

            if (type.IsAssignableFromInverse(typeof(Shader))) return true;
            if (type.IsAssignableFromInverse(typeof(ComputeShader))) return true;
            if (type.IsAssignableFromInverse(typeof(ShaderVariantCollection))) return true;
#if UNITY_2019_3_OR_NEWER
			if (type.IsAssignableFromInverse(typeof(UnityEngine.Experimental.Rendering.RayTracingShader))) return true; // todo: track of Experimental namespace
#endif

            if (type.IsAssignableFromInverse(typeof(TextAsset))) return true;
            if (type.IsAssignableFromInverse(typeof(AssemblyDefinitionAsset))) return true;

            if (type.IsAssignableFromInverse(typeof(UnityEngine.U2D.SpriteAtlas))) return true;

            if (conf.IgnoreMaterial && type.IsAssignableFromInverse(typeof(Material))) {
                return true;
            }

            if (conf.IgnoreSprite && type.Name.Contains("Texture2D")) {
                return true;
            }

            return false;
        }
    }
}
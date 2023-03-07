using Autohand;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Autohand
{
    public static class AutoHandExtensions
    {
        
        static Transform _transformRuler = null;
        //This is a "ruler" tool used to help calculate parent child calculations without doing parenting/childing
        public static Transform transformRuler
        {
            get {
                if(_transformRuler == null)
                    _transformRuler = new GameObject() { name = "Ruler" }.transform;

                if (_transformRuler.parent != transformParent)
                    _transformRuler.parent = transformParent;

                if (_transformRuler.localScale != Vector3.one)
                    _transformRuler.localScale = Vector3.one;

                if (IsPositionNan(_transformRuler.position))
                    _transformRuler.position = Vector3.zero;
                if (IsRotationNan(_transformRuler.rotation))
                    _transformRuler.rotation = Quaternion.identity;



                return _transformRuler;
            }
        }

        static Transform _transformRulerChild = null;
        //This is a "ruler" tool used to help calculate parent child calculations without doing parenting/childing
        public static Transform transformRulerChild
        {
            get {
                if (_transformRulerChild == null)
                {
                    _transformRulerChild = new GameObject() { name = "RulerChild" }.transform;
                    _transformRulerChild.parent = _transformRuler;
                }

                if (_transformRulerChild.parent != _transformRuler)
                    _transformRulerChild.parent = _transformRuler;

                if (_transformRulerChild.localScale != Vector3.one)
                    _transformRulerChild.localScale = Vector3.one;

                if (IsPositionNan(_transformRulerChild.position))
                    _transformRulerChild.position = Vector3.zero;
                if (IsRotationNan(_transformRulerChild.rotation))
                    _transformRulerChild.rotation = Quaternion.identity;

                return _transformRulerChild;
            }
        }

        static bool IsPositionNan(Vector3 pos)
        {
            return float.IsNaN(pos.x) || float.IsNaN(pos.y) || float.IsNaN(pos.z);
        }
        
        static bool IsRotationNan(Quaternion rot)
        {
            return float.IsNaN(rot.x) || float.IsNaN(rot.y) || float.IsNaN(rot.z) || float.IsNaN(rot.w);
        }

        static Transform _transformParent = null;

        // Holds all the Auto Hand Generated GameObjects
        public static Transform transformParent {
            get {
                if(Application.isEditor)
                    return null;

                if(_transformParent == null)
                    _transformParent = new GameObject() { name="Auto Hand Generated" }.transform;

                return _transformParent;
            }
        }

        public static void RotateAround(this Transform target, Transform center, Quaternion deltaRotation) {
            transformRuler.SetPositionAndRotation(center.position, center.rotation);
            transformRulerChild.SetPositionAndRotation(target.position, target.rotation);
            transformRuler.rotation *= deltaRotation;
            target.SetPositionAndRotation(transformRulerChild.position, transformRulerChild.rotation);
        }

        public static float Round(this float value, int digits)
        {
            float mult = Mathf.Pow(10.0f, (float)digits);
            return Mathf.Round(value * mult) / mult;
        }

        /// <summary>Returns true if there is a grabbable or link, out null if there is none</summary>
        public static bool HasGrabbable(this Hand hand, GameObject obj, out Grabbable grabbable)
        {
            return HasGrabbable(obj, out grabbable);
        }

        /// <summary>Returns true if there is a grabbable or link, out null if there is none</summary>
        public static bool HasGrabbable(this GameObject obj, out Grabbable grabbable)
        {
            if (obj == null)
            {
                grabbable = null;
                return false;
            }

            if (obj.CanGetComponent(out grabbable))
            {
                return true;
            }

            GrabbableChild grabChild;
            if (obj.CanGetComponent(out grabChild))
            {
                grabbable = grabChild.grabParent;
                return true;
            }

            grabbable = null;
            return false;
        }









        public static T GetCopyOf<T>(this Component comp, T other) where T : Component{
            Type type = comp.GetType();
            if (type != other.GetType()) return null; // type mis-match
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
            PropertyInfo[] pinfos = type.GetProperties(flags);
            foreach (var pinfo in pinfos)
            {
                if (pinfo.CanWrite)
                {
                    try
                    {
                        pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                    }
                    catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
                }
            }
            FieldInfo[] finfos = type.GetFields(flags);
            foreach (var finfo in finfos)
            {
                finfo.SetValue(comp, finfo.GetValue(other));
            }
            return comp as T;
        }



        /// <summary>Autohand extension method, used so I can use TryGetComponent for newer versions and GetComponent for older versions</summary>
        public static bool CanGetComponent<T>(this Component componentClass, out T component)
        {
#if UNITY_2019_1 || UNITY_2018 || UNITY_2017
       var tempComponent = componentClass.GetComponent<T>();
        if(tempComponent != null){
            component = tempComponent;
            return true;
        }
        else {
            component = tempComponent;
            return false;
        }
#else
            var value = componentClass.TryGetComponent(out component);
            return value;
#endif
        }

        /// <summary>Autohand extension method, used so I can use TryGetComponent for newer versions and GetComponent for older versions</summary>
        public static bool CanGetComponent<T>(this GameObject componentClass, out T component)
        {
#if UNITY_2019_1 || UNITY_2018 || UNITY_2017
       var tempComponent = componentClass.GetComponent<T>();
        if(tempComponent != null){
            component = tempComponent;
            return true;
        }
        else {
            component = tempComponent;
            return false;
        }
#else
            var value = componentClass.TryGetComponent(out component);
            return value;
#endif
        }



#if UNITY_EDITOR

        public static void TextDebug(this Vector3 vector3, string name = "") {
            Debug.Log(name + ": " + vector3.x + ", " + vector3.y + ", " + vector3.z);
        }

        public static GUIStyle LabelStyle(TextAnchor textAnchor = TextAnchor.MiddleLeft, FontStyle fontStyle = FontStyle.Normal, int fontSize = 13) {
            var style = new GUIStyle(GUI.skin.label);
            style.font = (Font)Resources.Load("Righteous-Regular", typeof(Font));
            style.fontSize = fontSize;
            style.alignment = textAnchor;
            style.fontStyle = fontStyle;
            return style;
        }
        public static GUIStyle LabelStyle(Color textColor, TextAnchor textAnchor = TextAnchor.MiddleLeft, FontStyle fontStyle = FontStyle.Normal, int fontSize = 13) {
            var style = new GUIStyle(GUI.skin.label);
            style.font = (Font)Resources.Load("Righteous-Regular", typeof(Font));
            style.fontSize = fontSize;
            style.alignment = textAnchor;
            style.fontStyle = fontStyle;
            style.normal.textColor = textColor;
            return style;
        }

        public static GUIStyle LabelStyleB(Color textColor, TextAnchor textAnchor = TextAnchor.MiddleLeft, FontStyle fontStyle = FontStyle.Normal, int fontSize = 13) {
            var style = new GUIStyle(GUI.skin.toggle);
            style.font = (Font)Resources.Load("Righteous-Regular", typeof(Font));
            style.fontSize = fontSize;
            style.alignment = textAnchor;
            style.fontStyle = fontStyle;
            style.normal.textColor = textColor;
            return style;
        }
#endif

        public static LayerMask GetPhysicsLayerMask(int currentLayer) {
            int finalMask = 0;
            for (int i=0; i<32; i++) {
                if (!Physics.GetIgnoreLayerCollision(currentLayer, i)) finalMask = finalMask | (1 << i);
            }
            return finalMask;
        }
    }
}
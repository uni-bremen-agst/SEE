// Copyright 2020 Lennart Kipka
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
// IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

#if UNITY_EDITOR

using UnityEditor;
using SEE.Game;
using SEE.Utils;

namespace SEEEditor
{    
        /// <summary>
        /// A custom editor for instances of SEEJlgCity as an extension of the SEECityEditor.
        /// </summary>
        [CustomEditor(typeof(SEEJlgCity))]
        [CanEditMultipleObjects]
        public class SEEJlgCityEditor : SEECityEditor
        {
        /// <summary>
        /// Whether the animation-attribute foldout should be expanded.
        /// </summary>
        private bool showAnimationAttributes = false;

        /// <summary>
        /// Adds the JLGPath value of SEEJlgCity to the inherited settings.
        /// </summary>
        protected override void Attributes()
        {
            base.Attributes();
            SEEJlgCity city = target as SEEJlgCity;
            city.JLGPath = GetDataPath("JLG file", city.JLGPath, Filenames.ExtensionWithoutPeriod(Filenames.JLGExtension));
            AnimationAttributes();
        }

        /// <summary>
        /// Renders the GUI for attributes of the execution animation.
        /// </summary>
        private void AnimationAttributes()
        {
            showAnimationAttributes =
                EditorGUILayout.Foldout(showAnimationAttributes, "Attributes of the execution animation", true, EditorStyles.foldoutHeader);
            if (showAnimationAttributes)
            {
                SEEJlgCity city = target as SEEJlgCity;
                city.BreakpointClass = EditorGUILayout.TextField("Class of the breakpoint", city.BreakpointClass);
                city.BreakpointLine = EditorGUILayout.IntField("Source line of the breakpoint ", city.BreakpointLine);
                city.DistanceAboveCity = EditorGUILayout.FloatField("Distance of source viewer above city", city.DistanceAboveCity);
                city.DistanceBehindCity = EditorGUILayout.FloatField("Distance of source viewer behind city", city.DistanceBehindCity);
                city.LineWidth = EditorGUILayout.FloatField("Width of line connecting source viewer", city.LineWidth);
            }
        }
    }
}
#endif
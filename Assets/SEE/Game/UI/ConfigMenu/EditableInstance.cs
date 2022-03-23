// Copyright 2021 Ruben Smidt
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

using SEE.DataModel;
using SEE.GO;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.UI.ConfigMenu
{
    /// <summary>
    /// Enumerates the list of SEECity instances the config menu can manipulate.
    /// </summary>
    public class EditableInstance
    {
        private EditableInstance(string displayValue, string gameObjectName)
        {
            DisplayValue = displayValue;
            GameObjectName = gameObjectName;
        }

        /// <summary>
        /// The name that should be used to display this instance inside the menu.
        /// </summary>
        public string DisplayValue { get; }

        /// <summary>
        /// The full name of the GameObject the SEECity is attached to.
        /// </summary>
        public string GameObjectName { get; }

        /// <summary>
        /// Returns an EditableInstance for each game object in the scene that is
        /// tagged as <see cref="Tags.CodeCity"/> and has a <see cref="City.SEECity"/>
        /// component attached to it. The <see cref="EditableInstance.DisplayValue"/>
        /// and <see cref="EditableInstance.GameObjectName"/> of these are the name
        /// of the found game object.
        /// </summary>
        /// <returns>all current instances of <see cref="City.SEECity"/> in the scene</returns>
        public static List<EditableInstance> AllEditableCodeCities()
        {
            List<EditableInstance> result = new List<EditableInstance>();

            foreach (GameObject city in GameObject.FindGameObjectsWithTag(Tags.CodeCity))
            {
                if (city.TryGetComponent(out City.SEECity _))
                {
                    result.Add(new EditableInstance(city.name, city.FullName()));
                }
            }
            return result;
        }

        protected bool Equals(EditableInstance other)
        {
            return GameObjectName == other.GameObjectName;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((EditableInstance)obj);
        }

        public override int GetHashCode()
        {
            return (GameObjectName != null ? GameObjectName.GetHashCode() : 0);
        }

        /// <summary>
        /// Yields this instance as a string providing the <see cref="DisplayValue"/>
        /// and <see cref="GameObjectName"/>.
        /// </summary>
        /// <returns>instance as a string</returns>
        public override string ToString()
        {
            return "[" + DisplayValue + ", " + GameObjectName + "]";
        }
    }
}

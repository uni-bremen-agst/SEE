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
        /// The name of the GameObject the SEECity is attached to.
        /// </summary>
        public string GameObjectName { get; }

        /// <summary>
        /// References the architecture SEECity.
        ///
        /// FIXME: We must not make any assumptions about the name of cities. We need
        /// to support all cities in a scene, not just this one, if it exists at all.
        /// </summary>
        public static EditableInstance Architecture => new EditableInstance("Arch", "Architecture");

        /// <summary>
        /// References the implementation SEECity.
        ///
        /// FIXME: We must not make any assumptions about the name of cities. We need
        /// to support all cities in a scene, not just this one, if it exists at all.
        /// </summary>
        public static EditableInstance Implementation => new EditableInstance("Impl", "Implementation");

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
    }
}

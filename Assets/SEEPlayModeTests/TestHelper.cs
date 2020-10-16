// Copyright 2020 Robert Bohnsack
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be included
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
// CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.Charts
{
    /// <summary>
    ///     This class is used to put into a working scene using charts. It eases access to specific components
    ///     and <see cref="GameObject" />s during testing.
    /// </summary>
    public class TestHelper : MonoBehaviour
    {
        public GameObject charts;

        /// <summary>
        ///     Fill this with prefabs of cities in the following order:
        ///     1. A city with multiple buildings
        ///     2. A city with one building
        ///     3. A city with zero buildings
        ///     They might have to be regenerated after changes to the software.
        /// </summary>
        public GameObject[] cityPrefabs;

        public Button closeChartsButton;
        public Button createChartButton;
        public ChartCreator creator;
        public ChartManager manager;
    }
}
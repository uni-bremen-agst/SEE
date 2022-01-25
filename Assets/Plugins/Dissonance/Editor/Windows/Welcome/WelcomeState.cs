using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Dissonance.Editor.Windows.Welcome
{
    [Serializable]
    internal class WelcomeState
    {
        [SerializeField, UsedImplicitly] private string _shownForVersion;

        public string ShownForVersion
        {
            get { return _shownForVersion; }
        }

        public WelcomeState(string version)
        {
            _shownForVersion = version;
        }

        public override string ToString()
        {
            return _shownForVersion;
        }
    }
}

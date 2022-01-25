using System.IO;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Dissonance.Editor.Windows.Welcome
{
    [InitializeOnLoad]
    public class WelcomeLauncher
    {
        private static readonly string StatePath = Path.Combine(DissonanceRootPath.BaseResourcePath, ".WelcomeState.json");

        internal static void Startup()
        {
            bool newState;
            var state = GetWelcomeState(out newState);

            if (!state.ShownForVersion.Equals(DissonanceComms.Version.ToString()))
            {
                SetWelcomeState(new WelcomeState(DissonanceComms.Version.ToString()));
                WelcomeWindow.ShowWindow();
            }
        }

        [NotNull] private static WelcomeState GetWelcomeState(out bool newState)
        {
            if (!File.Exists(StatePath))
            {
                // State path does not exist at all so create the default
                var state = new WelcomeState("");
                SetWelcomeState(state);
                newState = true;
                return state;
            }
            else
            {
                //Read the state from the file
                newState = false;
                using (var reader = File.OpenText(StatePath))
                    return JsonUtility.FromJson<WelcomeState>(reader.ReadToEnd());
            }
        }

        private static void SetWelcomeState([CanBeNull] WelcomeState state)
        {
            if (state == null)
            {
                //Clear installer state
                File.Delete(StatePath);
            }
            else
            {
                using (var writer = File.CreateText(StatePath))
                    writer.Write(JsonUtility.ToJson(state));
            }
        }
    }
}
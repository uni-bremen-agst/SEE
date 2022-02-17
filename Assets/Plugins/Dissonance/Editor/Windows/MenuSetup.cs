#if !NCRUNCH

using Dissonance.Editor.Windows.Welcome;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Dissonance.Editor.Windows
{
    public static class MenuSetup
    {
        /*
         * Manu structure in `Window > Dissonance`:
         *
         * Release Notes
         * Integrations
         * Support
         * > Discord Server
         * > Online Documentation
         * > Report A Bug
         * Review Dissonance
         * ----------------------
         * Quality Settings
         * Diagnostic Settings
         * Room Settings
         */

        [MenuItem("Window/Dissonance/Release Notes", priority = 1), UsedImplicitly]
        public static void ReleaseNotes()
        {
            var query = EditorMetadata.GetQueryString("menu");
            Application.OpenURL(string.Format("https://placeholder-software.co.uk/dissonance/releases/{0}.html{1}#vn", DissonanceComms.Version, query));
        }

        [MenuItem("Window/Dissonance/Download Integrations", priority = 2), UsedImplicitly]
        private static void DownloadIntegrations()
        {
            WelcomeWindow.ShowWindow();
        }

        [MenuItem("Window/Dissonance/Support/Discord Server", priority = 3), UsedImplicitly]
        private static void DiscordServer()
        {
            Application.OpenURL("https://placeholder-software.co.uk/discord");
        }

        [MenuItem("Window/Dissonance/Support/Online Documentation", priority = 4), UsedImplicitly]
        private static void Documentation()
        {
            Application.OpenURL("https://placeholder-software.co.uk/dissonance/docs");
        }

        [MenuItem("Window/Dissonance/Support/Report A Bug", priority = 5), UsedImplicitly]
        private static void IssueTracker()
        {
            Application.OpenURL("https://github.com/Placeholder-Software/Dissonance/issues");
        }

        [MenuItem("Window/Dissonance/Rate And Review", priority = 6), UsedImplicitly]
        private static void RateAndReview()
        {
            Application.OpenURL("http://u3d.as/za2?aid=1100lJDF");
        }

        [MenuItem("Window/Dissonance/Quality Settings", priority = 101), UsedImplicitly]
        private static void ShowQualitySettings()
        {
            VoiceSettingsEditor.GoToSettings();
        }

        [MenuItem("Window/Dissonance/Diagnostic Settings", priority = 102), UsedImplicitly]
        private static void ShowDiagnosticSettings()
        {
            DebugSettingsEditor.GoToSettings();
        }

        [MenuItem("Window/Dissonance/Room Settings", priority = 103), UsedImplicitly]
        private static void Show()
        {
            ChatRoomSettingsEditor.GoToSettings();
        }

        
    }
}

#endif
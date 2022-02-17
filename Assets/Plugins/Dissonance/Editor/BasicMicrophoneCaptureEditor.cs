using Dissonance.Audio.Capture;
using UnityEditor;

namespace Dissonance.Editor
{
    [CustomEditor(typeof(BasicMicrophoneCapture))]
    public class BasicMicrophoneCaptureEditor
        : BaseIMicrophoneCaptureEditor<BasicMicrophoneCapture>
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var capture = (BasicMicrophoneCapture)target;
            DrawMicSelectorGui(capture);
        }
    }
}

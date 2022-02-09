#if !UNITY_ANDROID
using UnityEditor;
using SEE.Game.Avatars;
using SEE.Utils;

#if UNITY_EDITOR

namespace SEEEditor
{
    /// <summary>
    /// Editor for PersonalAssistantSpeechInput.
    /// </summary>
    [CustomEditor(typeof(PersonalAssistantSpeechInput))]
    class PersonalAssistantSpeechInputEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            PersonalAssistantSpeechInput editedTarget = target as PersonalAssistantSpeechInput;
            editedTarget.GrammarFilePath = DataPathEditor.GetDataPath
                                              ("SRGS file", 
                                              editedTarget.GrammarFilePath, 
                                              Filenames.ExtensionWithoutPeriod(Filenames.GrammarExtension));
        }
    }
}

#endif
#endif
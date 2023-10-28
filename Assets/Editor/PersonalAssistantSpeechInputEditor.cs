using UnityEditor;
using SEE.Game.Avatars;
using SEE.Utils;
using UnityEngine;
using SEE.Utils.Paths;

#if UNITY_EDITOR

namespace SEEEditor
{
    /// <summary>
    /// Editor for PersonalAssistantSpeechInput.
    /// </summary>
    [CustomEditor(typeof(PersonalAssistantSpeechInput))]
    internal class PersonalAssistantSpeechInputEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Render default fields
            if (target is PersonalAssistantSpeechInput editedTarget)
            {
                editedTarget.UseChatGPT = EditorGUILayout.Toggle(new GUIContent("Use ChatGPT"), 
                                                                 editedTarget.UseChatGPT);
                if (editedTarget.UseChatGPT)
                {
                    editedTarget.OpenAiApiKey = EditorGUILayout.PasswordField(new GUIContent("OpenAI API Key"), 
                                                                              editedTarget.OpenAiApiKey);
                } 
                else
                {
                    string grammarExtension = Filenames.ExtensionWithoutPeriod(Filenames.GrammarExtension);
                    editedTarget.GrammarFilePath = DataPathEditor.GetDataPath("SRGS file",
                                                                              editedTarget.GrammarFilePath,
                                                                              grammarExtension) as FilePath;
                }
            }
        }
    }
}

#endif
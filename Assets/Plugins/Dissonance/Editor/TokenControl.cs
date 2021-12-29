using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Dissonance.Editor
{
    public class TokenControl
    {
        private readonly string _hint;

        private string _proposedToken = "New Token";

        public TokenControl(string hint, bool foldout = true)
        {
            _hint = hint;
        }

        public void DrawInspectorGui(Object parent, [NotNull] IAccessTokenCollection receiver)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.HelpBox(_hint, MessageType.Info);

                var tokensToRemove = new List<string>();
                foreach (var token in receiver.Tokens)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(token);
                        if (GUILayout.Button("Delete", GUILayout.MaxWidth(50)))
                        {
                            tokensToRemove.Add(token);
                            _proposedToken = "New Token";
                        }
                    }
                }

                foreach (var token in tokensToRemove)
                {
                    Undo.RecordObject(parent, "Removed Dissonance Access Token");
                    receiver.RemoveToken(token);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _proposedToken = EditorGUILayout.TextField(_proposedToken);
                    if (GUILayout.Button("Add Token"))
                    {
                        Undo.RecordObject(parent, "Added Dissonance Access Token");

                        receiver.AddToken(_proposedToken);
                        _proposedToken = "New Token";
                    }
                }
            }
        }
    }
}

// C# 9 only. Braced namespace, no file-scoped namespaces, no records.

using UnityEditor;
using UnityEngine;

namespace Yogabase.Editor
{
    /// <summary>
    /// Draws the developer-token field that deliberately is not a field on
    /// <see cref="YogabaseSettings"/>.
    /// </summary>
    /// <remarks>
    /// The token is written straight to <see cref="EditorPrefs"/> and never touches the serialized
    /// asset, so it cannot be committed by accident. The inspector shows only whether one is
    /// present, never the value it holds.
    /// </remarks>
    [CustomEditor(typeof(YogabaseSettings))]
    public sealed class YogabaseSettingsEditor : UnityEditor.Editor
    {
        private string pasted = string.Empty;

        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var settings = (YogabaseSettings)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Developer token", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "A project-scoped developer token from the console's Settings page. It is stored in "
                + "this machine's EditorPrefs, never in this asset, so it is not committed and not "
                + "shared with anyone who clones the project. It is a secret: it authorizes uploading "
                + "builds to this project.",
                MessageType.Info);

            var stored = settings.GetDeveloperToken();
            EditorGUILayout.LabelField(
                "Status",
                string.IsNullOrEmpty(stored)
                    ? "No token on this machine."
                    : "A token is stored on this machine (" + stored.Length + " characters).");

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(settings.projectId)))
            {
                pasted = EditorGUILayout.PasswordField("Paste token", pasted);
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(pasted)))
                    {
                        if (GUILayout.Button("Save to this machine"))
                        {
                            settings.SetDeveloperToken(pasted);
                            pasted = string.Empty;
                            GUI.FocusControl(null);
                        }
                    }

                    using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(stored)))
                    {
                        if (GUILayout.Button("Forget"))
                        {
                            settings.ClearDeveloperToken();
                            pasted = string.Empty;
                            GUI.FocusControl(null);
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(settings.projectId))
            {
                EditorGUILayout.HelpBox(
                    "Set the project id first: the token is stored per project.", MessageType.Warning);
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Open Yogabase ▸ Publish Server"))
            {
                YogabasePublishServerWindow.Open();
            }
        }
    }
}

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;

[CustomEditor(typeof(DocumentData))]
public class DocumentDataEditor : Editor
{
    private const string TableCollectionName = "Documents";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(12);

        DocumentData document =
            (DocumentData)target;

        EditorGUILayout.LabelField(
            "Localization Tools",
            EditorStyles.boldLabel
        );

        bool hasLocalizationId =
            !string.IsNullOrWhiteSpace(
                document.LocalizationId
            );

        if (!hasLocalizationId)
        {
            EditorGUILayout.HelpBox(
                "Укажите Localization ID, " +
                "например document_01.",
                MessageType.Warning
            );
        }

        GUI.enabled = hasLocalizationId;

        if (GUILayout.Button(
                "SETUP LOCALIZATION",
                GUILayout.Height(36)
            ))
        {
            SetupLocalization(document);
        }

        GUI.enabled = true;
    }

    private void SetupLocalization(
        DocumentData document
    )
    {
        var collection =
            LocalizationEditorSettings
                .GetStringTableCollection(
                    TableCollectionName
                );

        if (collection == null)
        {
            Debug.LogError(
                $"Не найдена String Table Collection " +
                $"'{TableCollectionName}'.",
                document
            );

            return;
        }

        string id =
            document.LocalizationId.Trim();

        List<string> requiredKeys =
            new List<string>
            {
                $"{id}_number",
                $"{id}_title",
                $"{id}_text",
                $"{id}_briefing"
            };

        Undo.RecordObject(
            collection.SharedData,
            "Setup Document Localization"
        );

        int createdCount = 0;

        foreach (string key in requiredKeys)
        {
            if (collection.SharedData
                    .GetEntry(key) != null)
            {
                continue;
            }

            collection.SharedData.AddKey(key);
            createdCount++;
        }

        EditorUtility.SetDirty(
            collection.SharedData
        );

        Undo.RecordObject(
            document,
            "Setup Document Localization"
        );

        document.AutoBindLocalization();

        EditorUtility.SetDirty(document);

        AssetDatabase.SaveAssets();

        serializedObject.Update();

        Debug.Log(
            $"Localization setup завершён для '{id}'. " +
            $"Создано новых ключей: {createdCount}.",
            document
        );
    }
}

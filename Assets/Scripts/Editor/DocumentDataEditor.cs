using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DocumentCatalog))]
public sealed class DocumentCatalogEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        if (GUILayout.Button(
                "Sync All Documents"))
        {
            SyncDocuments();
        }
    }

    private void SyncDocuments()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:DocumentData",
                new[] { "Assets/Documents" }
            );

        List<DocumentData> documents =
            new List<DocumentData>();

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid
                );

            DocumentData document =
                AssetDatabase.LoadAssetAtPath<
                    DocumentData
                >(path);

            if (document == null ||
                string.IsNullOrWhiteSpace(
                    document.DocumentId))
            {
                continue;
            }

            documents.Add(document);
        }

        documents.Sort(
            (a, b) =>
                string.CompareOrdinal(
                    a.DocumentId,
                    b.DocumentId
                )
        );

        serializedObject.Update();

        SerializedProperty documentsProperty =
            serializedObject.FindProperty(
                "documents"
            );

        documentsProperty.arraySize =
            documents.Count;

        for (int i = 0;
             i < documents.Count;
             i++)
        {
            documentsProperty
                .GetArrayElementAtIndex(i)
                .objectReferenceValue =
                    documents[i];
        }

        serializedObject.ApplyModifiedProperties();

        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssets();

        Debug.Log(
            $"DocumentCatalog: добавлено " +
            $"{documents.Count} документов."
        );
    }
}
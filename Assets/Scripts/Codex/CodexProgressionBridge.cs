using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Временный мост между текущей системой документов
/// и кодексом. Позволяет внедрить кодекс без изменения
/// DocumentRedactor.
///
/// Список Documents должен повторять порядок документов
/// в DocumentRedactor.
/// </summary>
public sealed class CodexProgressionBridge :
    MonoBehaviour
{
    [SerializeField]
    private CodexManager codexManager;

    [SerializeField]
    private List<DocumentData> documents =
        new List<DocumentData>();

    private int currentDocumentIndex;

    private void Start()
    {
        currentDocumentIndex = 0;
        UnlockCurrentDocumentEntries();
    }

    public void AdvanceDocument()
    {
        if (currentDocumentIndex >=
            documents.Count - 1)
        {
            return;
        }

        currentDocumentIndex++;
        UnlockCurrentDocumentEntries();
    }

    public void RestartSession()
    {
        currentDocumentIndex = 0;
        UnlockCurrentDocumentEntries();
    }

    private void UnlockCurrentDocumentEntries()
    {
        if (codexManager == null ||
            currentDocumentIndex < 0 ||
            currentDocumentIndex >= documents.Count)
        {
            return;
        }

        DocumentData document =
            documents[currentDocumentIndex];

        if (document == null)
        {
            return;
        }

        codexManager.UnlockEntries(
            document.CodexEntriesToUnlock
        );
    }
}

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "DocumentCatalog",
    menuName = "Classified/Document Catalog"
)]
public sealed class DocumentCatalog : ScriptableObject
{
    [SerializeField]
    private List<DocumentData> documents =
        new List<DocumentData>();

    public DocumentData FindById(
        string documentId)
    {
        if (string.IsNullOrWhiteSpace(documentId))
        {
            return null;
        }

        foreach (DocumentData document in documents)
        {
            if (document != null &&
                document.DocumentId ==
                documentId)
            {
                return document;
            }
        }

        return null;
    }
}

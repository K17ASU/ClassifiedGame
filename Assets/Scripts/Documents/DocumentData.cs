using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(
    fileName = "NewDocument",
    menuName = "Classified/Document Data"
)]
public class DocumentData : ScriptableObject
{
    [Header("Save")]

    [SerializeField]
    private string documentId;

    [Header("Document")]

    [SerializeField]
    private string documentNumber;

    [SerializeField]
    private string documentTitle;

    [TextArea(10, 30)]
    [SerializeField]
    private string documentText;

    [SerializeField]
    private bool isTutorial;

    [Header("Progression")]

    [SerializeField]
    private RevealMethod unlocksAfterCompletion =
        RevealMethod.None;

    [Header("Codex")]

    [SerializeField]
    private List<CodexEntry> codexEntriesToUnlock =
        new List<CodexEntry>();

    [Header("Localization")]

    [SerializeField]
    private string localizationId;

    [SerializeField]
    private LocalizedString localizedDocumentNumber;

    [SerializeField]
    private LocalizedString localizedDocumentTitle;

    [SerializeField]
    private LocalizedString localizedDocumentText;

    [SerializeField]
    private LocalizedString localizedBriefing;

    public string DocumentId => documentId;

    public string DocumentNumber => documentNumber;
    public string DocumentTitle => documentTitle;
    public string DocumentText => documentText;
    public bool IsTutorial => isTutorial;

    public RevealMethod UnlocksAfterCompletion =>
        unlocksAfterCompletion;

    public IReadOnlyList<CodexEntry> CodexEntriesToUnlock =>
        codexEntriesToUnlock;

    public string LocalizationId => localizationId;

    public LocalizedString LocalizedDocumentNumber =>
        localizedDocumentNumber;

    public LocalizedString LocalizedDocumentTitle =>
        localizedDocumentTitle;

    public LocalizedString LocalizedDocumentText =>
        localizedDocumentText;

    public LocalizedString LocalizedBriefing =>
        localizedBriefing;

    public void AutoBindLocalization()
    {
        if (string.IsNullOrWhiteSpace(localizationId))
        {
            Debug.LogWarning(
                $"Localization ID не указан у {name}.",
                this
            );

            return;
        }

        const string tableName = "Documents";

        BindLocalizedString(
            ref localizedDocumentNumber,
            tableName,
            $"{localizationId}_number"
        );

        BindLocalizedString(
            ref localizedDocumentTitle,
            tableName,
            $"{localizationId}_title"
        );

        BindLocalizedString(
            ref localizedDocumentText,
            tableName,
            $"{localizationId}_text"
        );

        BindLocalizedString(
            ref localizedBriefing,
            tableName,
            $"{localizationId}_briefing"
        );

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif

        Debug.Log(
            $"Localization привязана: {localizationId}",
            this
        );
    }

    private void OnValidate()
    {
        if (documentId != null)
        {
            documentId = documentId.Trim();
        }
    }

    private void BindLocalizedString(
        ref LocalizedString localizedString,
        string tableName,
        string entryKey
    )
    {
        if (localizedString == null)
        {
            localizedString =
                new LocalizedString();
        }

        localizedString.TableReference =
            tableName;

        localizedString.TableEntryReference =
            entryKey;
    }
}

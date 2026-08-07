using UnityEngine;
using UnityEngine.Localization;

/// <summary>
/// Хранит содержимое и настройки одного документа.
/// </summary>
[CreateAssetMenu(
    fileName = "NewDocument",
    menuName = "Document Redactor/Document"
)]
public class DocumentData : ScriptableObject
{
    [Header("Основная информация")]

    [SerializeField]
    private string documentNumber = "ДОКУМЕНТ № 001";

    [SerializeField]
    private string documentTitle = "Название документа";

    [SerializeField]
    [TextArea(8, 25)]
    private string documentText;

    [Header("Обучение")]

    [SerializeField]
    private bool isTutorial;

    [SerializeField]
    [TextArea(2, 5)]
    private string instructionText =
        "Найдите и засекретьте служебные сведения.";

    public string DocumentNumber => documentNumber;

    public string DocumentTitle => documentTitle;

    public string DocumentText => documentText;

    public bool IsTutorial => isTutorial;

    public string InstructionText => instructionText;

    [Header("Локализация")]

    [SerializeField]
    private LocalizedString localizedDocumentNumber;

    [SerializeField]
    private LocalizedString localizedDocumentTitle;

    [SerializeField]
    private LocalizedString localizedDocumentText;

    [SerializeField]
    private LocalizedString localizedInstructionText;

    public LocalizedString LocalizedDocumentNumber =>
    localizedDocumentNumber;

    public LocalizedString LocalizedDocumentTitle =>
        localizedDocumentTitle;

    public LocalizedString LocalizedDocumentText =>
        localizedDocumentText;

    public LocalizedString LocalizedInstructionText =>
        localizedInstructionText;
}

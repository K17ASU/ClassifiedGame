using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Показывает документы, обрабатывает засекречивание
/// и переключает игрока между документами.
/// </summary>
public class DocumentRedactor : MonoBehaviour, IPointerClickHandler
{
    [Header("Объекты интерфейса")]

    [SerializeField]
    private TMP_Text documentTitleText;

    [SerializeField]
    private TMP_Text documentText;

    [SerializeField]
    private TMP_Text progressText;

    [SerializeField]
    private TMP_Text completionText;

    [SerializeField]
    private GameObject winPanel;

    [SerializeField]
    private GameObject nextDocumentButton;

    [Header("Документы")]

    [SerializeField]
    private List<DocumentData> documents =
        new List<DocumentData>();

    [Header("Внешний вид")]

    [SerializeField]
    private string secretTextColor = "#FF5656";

    [SerializeField]
    private string secretHighlightColor = "#67292966";

    private readonly List<SecretFragment> secretFragments =
        new List<SecretFragment>();

    private readonly List<DocumentPart> documentParts =
        new List<DocumentPart>();

    private int currentDocumentIndex;
    private int redactedCount;
    private bool documentCompleted;

    private void Start()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        if (documents.Count == 0)
        {
            Debug.LogError(
                "В списке Documents нет ни одного документа."
            );

            enabled = false;
            return;
        }

        currentDocumentIndex = 0;
        LoadCurrentDocument();
    }

    /// <summary>
    /// Проверяет обязательные ссылки из Inspector.
    /// </summary>
    private bool ValidateReferences()
    {
        if (documentText == null)
        {
            Debug.LogError(
                "Не назначено поле Document Text."
            );

            return false;
        }

        if (progressText == null)
        {
            Debug.LogError(
                "Не назначено поле Progress Text."
            );

            return false;
        }

        if (winPanel == null)
        {
            Debug.LogError(
                "Не назначено поле Win Panel."
            );

            return false;
        }

        return true;
    }

    /// <summary>
    /// Загружает документ, соответствующий текущему индексу.
    /// </summary>
    private void LoadCurrentDocument()
    {
        if (currentDocumentIndex < 0 ||
            currentDocumentIndex >= documents.Count)
        {
            Debug.LogError(
                "Индекс документа находится за пределами списка."
            );

            return;
        }

        DocumentData currentDocument =
            documents[currentDocumentIndex];

        if (currentDocument == null)
        {
            Debug.LogError(
                $"Документ под индексом {currentDocumentIndex} не назначен."
            );

            return;
        }

        documentCompleted = false;
        redactedCount = 0;

        winPanel.SetActive(false);

        UpdateDocumentTitle(currentDocument);
        ParseDocument(currentDocument.DocumentText);
        RefreshDocument();
    }

    /// <summary>
    /// Обновляет название и номер дела.
    /// </summary>
    private void UpdateDocumentTitle(DocumentData document)
    {
        if (documentTitleText == null)
        {
            return;
        }

        documentTitleText.text =
            $"{document.DocumentNumber}\n" +
            $"{document.DocumentTitle}";
    }

    /// <summary>
    /// Разделяет документ на обычные и секретные части.
    /// </summary>
    private void ParseDocument(string sourceDocument)
    {
        documentParts.Clear();
        secretFragments.Clear();

        if (string.IsNullOrWhiteSpace(sourceDocument))
        {
            Debug.LogWarning(
                "Текст текущего документа пуст."
            );

            return;
        }

        Regex secretPattern = new Regex(
            @"\[\[(.*?)\]\]",
            RegexOptions.Singleline
        );

        MatchCollection matches =
            secretPattern.Matches(sourceDocument);

        int currentPosition = 0;
        int secretIndex = 0;

        foreach (Match match in matches)
        {
            if (match.Index > currentPosition)
            {
                string normalText = sourceDocument.Substring(
                    currentPosition,
                    match.Index - currentPosition
                );

                documentParts.Add(
                    DocumentPart.CreateNormal(normalText)
                );
            }

            string secretText = match.Groups[1].Value;

            SecretFragment fragment = new SecretFragment
            {
                id = secretIndex,
                originalText = secretText,
                isRedacted = false
            };

            secretFragments.Add(fragment);

            documentParts.Add(
                DocumentPart.CreateSecret(secretIndex)
            );

            secretIndex++;
            currentPosition = match.Index + match.Length;
        }

        if (currentPosition < sourceDocument.Length)
        {
            string remainingText =
                sourceDocument.Substring(currentPosition);

            documentParts.Add(
                DocumentPart.CreateNormal(remainingText)
            );
        }

        if (secretFragments.Count == 0)
        {
            Debug.LogWarning(
                "В документе нет секретных фрагментов [[...]]."
            );
        }
    }

    /// <summary>
    /// Обрабатывает клик по секретному фрагменту.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (documentCompleted)
        {
            return;
        }

        Camera eventCamera = eventData.pressEventCamera;

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(
            documentText,
            eventData.position,
            eventCamera
        );

        if (linkIndex == -1)
        {
            return;
        }

        TMP_LinkInfo linkInfo =
            documentText.textInfo.linkInfo[linkIndex];

        string linkId = linkInfo.GetLinkID();

        if (!int.TryParse(linkId, out int secretId))
        {
            Debug.LogWarning(
                $"Некорректный ID секретного фрагмента: {linkId}"
            );

            return;
        }

        RedactFragment(secretId);
    }

    /// <summary>
    /// Засекречивает выбранный фрагмент.
    /// </summary>
    private void RedactFragment(int secretId)
    {
        if (secretId < 0 ||
            secretId >= secretFragments.Count)
        {
            Debug.LogWarning(
                $"Фрагмент с ID {secretId} не найден."
            );

            return;
        }

        SecretFragment fragment =
            secretFragments[secretId];

        if (fragment.isRedacted)
        {
            return;
        }

        fragment.isRedacted = true;
        redactedCount++;

        RefreshDocument();

        if (redactedCount >= secretFragments.Count)
        {
            CompleteDocument();
        }
    }

    /// <summary>
    /// Заново создаёт отображаемый текст.
    /// </summary>
    private void RefreshDocument()
    {
        StringBuilder result = new StringBuilder();

        foreach (DocumentPart part in documentParts)
        {
            if (!part.isSecret)
            {
                result.Append(part.normalText);
                continue;
            }

            SecretFragment fragment =
                secretFragments[part.secretId];

            if (fragment.isRedacted)
            {
                result.Append(
                    CreateRedactedText(fragment.originalText)
                );
            }
            else
            {
                result.Append(
                    CreateClickableSecretText(fragment)
                );
            }
        }

        documentText.text = result.ToString();

        documentText.ForceMeshUpdate();

        UpdateProgress();
    }

    /// <summary>
    /// Создаёт выделенный кликабельный фрагмент.
    /// </summary>
    private string CreateClickableSecretText(
        SecretFragment fragment
    )
    {
        return
            $"<link=\"{fragment.id}\">" +
            $"<mark={secretHighlightColor}>" +
            $"<color={secretTextColor}>" +
            fragment.originalText +
            "</color>" +
            "</mark>" +
            "</link>";
    }

    /// <summary>
    /// Создаёт чёрную цензурную полосу.
    /// </summary>
    private string CreateRedactedText(string originalText)
    {
        return
            "<mark=#000000FF>" +
            "<color=#00000000>" +
            originalText +
            "</color>" +
            "</mark>";
    }

    private void UpdateProgress()
    {
        int documentNumber = currentDocumentIndex + 1;

        progressText.text =
            $"Документ: {documentNumber} / {documents.Count}\n" +
            $"Засекречено: {redactedCount} / {secretFragments.Count}";
    }

    /// <summary>
    /// Завершает текущий документ.
    /// </summary>
    private void CompleteDocument()
    {
        documentCompleted = true;
        winPanel.SetActive(true);

        bool hasNextDocument =
            currentDocumentIndex < documents.Count - 1;

        if (completionText != null)
        {
            completionText.text = hasNextDocument
                ? "ДОКУМЕНТ ЗАСЕКРЕЧЕН"
                : "ВСЕ ДОКУМЕНТЫ ОБРАБОТАНЫ";
        }

        if (nextDocumentButton != null)
        {
            nextDocumentButton.SetActive(hasNextDocument);
        }

        Debug.Log(
            $"Документ {currentDocumentIndex + 1} завершён."
        );
    }

    /// <summary>
    /// Вызывается кнопкой «Следующий документ».
    /// </summary>
    public void NextDocument()
    {
        if (!documentCompleted)
        {
            return;
        }

        if (currentDocumentIndex >= documents.Count - 1)
        {
            Debug.Log(
                "Следующего документа нет."
            );

            return;
        }

        currentDocumentIndex++;
        LoadCurrentDocument();
    }

    private class SecretFragment
    {
        public int id;
        public string originalText;
        public bool isRedacted;
    }

    private class DocumentPart
    {
        public bool isSecret;
        public string normalText;
        public int secretId;

        public static DocumentPart CreateNormal(string text)
        {
            return new DocumentPart
            {
                isSecret = false,
                normalText = text,
                secretId = -1
            };
        }

        public static DocumentPart CreateSecret(int id)
        {
            return new DocumentPart
            {
                isSecret = true,
                normalText = string.Empty,
                secretId = id
            };
        }
    }
}
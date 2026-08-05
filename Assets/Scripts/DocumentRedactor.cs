using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Управляет документом и обработкой кликов по секретным фрагментам.
/// Секретный текст в исходном документе записывается внутри [[двойных скобок]].
/// </summary>
public class DocumentRedactor : MonoBehaviour, IPointerClickHandler
{
    [Header("Объекты интерфейса")]

    [SerializeField]
    private TMP_Text documentText;

    [SerializeField]
    private TMP_Text progressText;

    [SerializeField]
    private GameObject winPanel;

    [Header("Содержимое документа")]

    [TextArea(15, 35)]
    [SerializeField]
    private string sourceDocument =
        "ДОКЛАД № 17-Б\n\n" +
        "Объект был замечен в районе [[озера Лох-Несс]] " +
        "в 03:42 по местному времени.\n\n" +
        "Свидетель утверждает, что существо достигало " +
        "[[примерно двенадцати метров в длину]].\n\n" +
        "Материалы были переданы сотруднику [[агенту Харперу]].\n\n" +
        "Официальная версия: ошибка наблюдения.";

    [Header("Внешний вид")]

    [SerializeField]
    private string secretTextColor = "#FF5656";

    [SerializeField]
    private string secretHighlightColor = "#67292966";

    // Список всех секретных фрагментов документа.
    private readonly List<SecretFragment> secretFragments =
        new List<SecretFragment>();

    // Части документа: обычный текст и секретные фрагменты.
    private readonly List<DocumentPart> documentParts =
        new List<DocumentPart>();

    private int redactedCount;

    private void Start()
    {
        if (documentText == null)
        {
            Debug.LogError(
                "В DocumentRedactor не назначен объект Document Text."
            );

            enabled = false;
            return;
        }

        ParseDocument();
        RefreshDocument();
    }

    /// <summary>
    /// Ищет текст внутри [[двойных скобок]] и превращает его
    /// в отдельные секретные фрагменты.
    /// </summary>
    private void ParseDocument()
    {
        documentParts.Clear();
        secretFragments.Clear();
        redactedCount = 0;

        if (string.IsNullOrWhiteSpace(sourceDocument))
        {
            Debug.LogWarning("Исходный документ пуст.");
            return;
        }

        Regex secretPattern = new Regex(
            @"\[\[(.*?)\]\]",
            RegexOptions.Singleline
        );

        MatchCollection matches = secretPattern.Matches(sourceDocument);

        int currentPosition = 0;
        int secretIndex = 0;

        foreach (Match match in matches)
        {
            // Добавляем обычный текст перед секретным фрагментом.
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

        // Добавляем оставшийся обычный текст после последнего секрета.
        if (currentPosition < sourceDocument.Length)
        {
            string remainingText =
                sourceDocument.Substring(currentPosition);

            documentParts.Add(
                DocumentPart.CreateNormal(remainingText)
            );
        }
    }

    /// <summary>
    /// Вызывается Unity при клике по объекту с текстом.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        Camera eventCamera = eventData.pressEventCamera;

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(
            documentText,
            eventData.position,
            eventCamera
        );

        // Значение -1 означает, что игрок нажал не на ссылку.
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
                $"Не удалось определить ID секретного фрагмента: {linkId}"
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
        if (secretId < 0 || secretId >= secretFragments.Count)
        {
            Debug.LogWarning(
                $"Секретный фрагмент с ID {secretId} не найден."
            );

            return;
        }

        SecretFragment fragment = secretFragments[secretId];

        // Не засчитываем повторное нажатие.
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
    /// Заново формирует отображаемый текст документа.
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
                result.Append(CreateRedactedText(fragment.originalText));
            }
            else
            {
                result.Append(CreateClickableSecretText(fragment));
            }
        }

        documentText.text = result.ToString();

        UpdateProgress();
    }

    /// <summary>
    /// Создаёт кликабельный выделенный текст.
    /// </summary>
    private string CreateClickableSecretText(
        SecretFragment fragment
    )
    {
        return
            $"<link=\"{fragment.id}\">" +
            $"<mark={secretHighlightColor}>" +
            $"<color={secretTextColor}>" +
            $"{fragment.originalText}" +
            $"</color>" +
            $"</mark>" +
            $"</link>";
    }

    /// <summary>
    /// Создаёт чёрную полосу вместо обработанного текста.
    /// Сам текст становится прозрачным, но сохраняет свою длину.
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
        if (progressText == null)
        {
            return;
        }

        progressText.text =
            $"Засекречено: {redactedCount} / {secretFragments.Count}";
    }

    private void CompleteDocument()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }

        Debug.Log("Документ полностью засекречен.");
    }

    /// <summary>
    /// Информация об одном секретном фрагменте.
    /// </summary>
    private class SecretFragment
    {
        public int id;
        public string originalText;
        public bool isRedacted;
    }

    /// <summary>
    /// Часть документа: обычный текст или ссылка на секретный фрагмент.
    /// </summary>
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
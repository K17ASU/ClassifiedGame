using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Управляет документами и позволяет игроку
/// засекречивать любое слово кликом или движением мыши.
///
/// Правильные слова задаются внутри [[двойных скобок]].
/// Проверка выполняется после нажатия
/// кнопки «Передать документ».
/// </summary>
public class DocumentRedactor :
    MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [Header("Основной интерфейс")]

    [SerializeField]
    private TMP_Text documentTitleText;

    [SerializeField]
    private TMP_Text documentText;

    [SerializeField]
    private TMP_Text progressText;

    [SerializeField]
    private TMP_Text statusText;

    [SerializeField]
    private GameObject submitButton;

    [Header("Панель завершения")]

    [SerializeField]
    private GameObject winPanel;

    [SerializeField]
    private TMP_Text completionText;

    [SerializeField]
    private GameObject nextDocumentButton;

    [Header("Документы")]

    [SerializeField]
    private List<DocumentData> documents =
        new List<DocumentData>();

    [Header("Обычный текст")]

    [Tooltip("Цвет обычного текста и знаков препинания.")]
    [SerializeField]
    private string normalTextColor = "#24211C";

    [Header("Слабая подсказка")]

    [Tooltip("Цвет правильных слов до засекречивания.")]
    [SerializeField]
    private string secretTextColor = "#4B4438";

    [Tooltip("Едва заметный фон правильных слов.")]
    [SerializeField]
    private string secretHighlightColor = "#8E713018";

    [Tooltip("Размер правильных слов относительно обычных.")]
    [Range(95, 105)]
    [SerializeField]
    private int secretTextSizePercent = 99;

    [Header("Цензурная плашка")]

    [SerializeField]
    private string redactionColor = "#000000FF";

    // Все слова текущего документа.
    private readonly List<WordData> words =
        new List<WordData>();

    // Последовательность слов, пробелов,
    // переносов строк и знаков препинания.
    private readonly List<TextPart> textParts =
        new List<TextPart>();

    // Слова, обработанные во время текущего
    // движения мыши. Нужны, чтобы одно слово
    // не переключалось несколько раз.
    private readonly HashSet<int> processedDragWords =
        new HashSet<int>();

    private int currentDocumentIndex;
    private bool documentFinished;

    // true — игрок сейчас удерживает кнопку мыши.
    private bool isDragging;

    // true — текущее движение ставит плашки.
    // false — текущее движение снимает плашки.
    private bool dragRedactionState;

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
    /// На случай, если объект отключится
    /// во время удержания кнопки мыши.
    /// </summary>
    private void OnDisable()
    {
        StopDragging();
    }

    /// <summary>
    /// Проверяет обязательные ссылки из Inspector.
    /// </summary>
    private bool ValidateReferences()
    {
        bool referencesAreValid = true;

        if (documentText == null)
        {
            Debug.LogError(
                "Не назначено поле Document Text."
            );

            referencesAreValid = false;
        }

        if (progressText == null)
        {
            Debug.LogError(
                "Не назначено поле Progress Text."
            );

            referencesAreValid = false;
        }

        if (statusText == null)
        {
            Debug.LogError(
                "Не назначено поле Status Text."
            );

            referencesAreValid = false;
        }

        if (submitButton == null)
        {
            Debug.LogError(
                "Не назначено поле Submit Button."
            );

            referencesAreValid = false;
        }

        if (winPanel == null)
        {
            Debug.LogError(
                "Не назначено поле Win Panel."
            );

            referencesAreValid = false;
        }

        return referencesAreValid;
    }

    /// <summary>
    /// Загружает текущий документ
    /// и очищает предыдущий выбор игрока.
    /// </summary>
    private void LoadCurrentDocument()
    {
        if (currentDocumentIndex < 0 ||
            currentDocumentIndex >= documents.Count)
        {
            Debug.LogError(
                "Некорректный индекс документа."
            );

            return;
        }

        DocumentData currentDocument =
            documents[currentDocumentIndex];

        if (currentDocument == null)
        {
            Debug.LogError(
                $"Документ под индексом " +
                $"{currentDocumentIndex} не назначен."
            );

            return;
        }

        StopDragging();

        documentFinished = false;

        winPanel.SetActive(false);
        submitButton.SetActive(true);

        UpdateDocumentTitle(currentDocument);
        ParseDocument(currentDocument.DocumentText);
        RefreshDocument();

        SetStatus(
            "Выберите сведения для засекречивания."
        );
    }

    /// <summary>
    /// Показывает номер и название документа.
    /// </summary>
    private void UpdateDocumentTitle(
        DocumentData document
    )
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
    /// Подготавливает текст документа.
    /// </summary>
    private void ParseDocument(string sourceText)
    {
        words.Clear();
        textParts.Clear();

        if (string.IsNullOrWhiteSpace(sourceText))
        {
            Debug.LogWarning(
                "Текст текущего документа пуст."
            );

            return;
        }

        ParsedSource parsedSource =
            RemoveSecretMarkers(sourceText);

        CreateWordsAndTextParts(
            parsedSource.cleanText,
            parsedSource.secretCharacters
        );

        int secretWordCount = 0;

        foreach (WordData word in words)
        {
            if (word.isSecret)
            {
                secretWordCount++;
            }
        }

        if (secretWordCount == 0)
        {
            Debug.LogWarning(
                "В документе нет секретных слов [[...]]."
            );
        }
    }

    /// <summary>
    /// Удаляет [[служебные скобки]],
    /// но запоминает положение секретных символов.
    /// </summary>
    private ParsedSource RemoveSecretMarkers(
        string sourceText
    )
    {
        StringBuilder cleanText =
            new StringBuilder();

        List<bool> secretCharacters =
            new List<bool>();

        bool insideSecretFragment = false;
        int position = 0;

        while (position < sourceText.Length)
        {
            bool startsSecretFragment =
                position + 1 < sourceText.Length &&
                sourceText[position] == '[' &&
                sourceText[position + 1] == '[';

            bool endsSecretFragment =
                position + 1 < sourceText.Length &&
                sourceText[position] == ']' &&
                sourceText[position + 1] == ']';

            if (startsSecretFragment)
            {
                if (insideSecretFragment)
                {
                    Debug.LogWarning(
                        "Обнаружены вложенные скобки [[...]]."
                    );
                }

                insideSecretFragment = true;
                position += 2;
                continue;
            }

            if (endsSecretFragment)
            {
                if (!insideSecretFragment)
                {
                    Debug.LogWarning(
                        "Обнаружены закрывающие скобки " +
                        "без открывающих."
                    );
                }

                insideSecretFragment = false;
                position += 2;
                continue;
            }

            cleanText.Append(sourceText[position]);
            secretCharacters.Add(insideSecretFragment);

            position++;
        }

        if (insideSecretFragment)
        {
            Debug.LogWarning(
                "В документе не закрыта пара скобок [[...]]."
            );
        }

        return new ParsedSource
        {
            cleanText = cleanText.ToString(),
            secretCharacters = secretCharacters
        };
    }

    /// <summary>
    /// Разделяет документ на слова и промежутки.
    /// </summary>
    private void CreateWordsAndTextParts(
        string cleanText,
        List<bool> secretCharacters
    )
    {
        Regex wordPattern = new Regex(
            @"[\p{L}\p{N}]+" +
            @"(?:[-–—'][\p{L}\p{N}]+)*"
        );

        MatchCollection matches =
            wordPattern.Matches(cleanText);

        int currentPosition = 0;
        int wordId = 0;

        foreach (Match match in matches)
        {
            if (match.Index > currentPosition)
            {
                string separator =
                    cleanText.Substring(
                        currentPosition,
                        match.Index - currentPosition
                    );

                textParts.Add(
                    TextPart.CreateSeparator(separator)
                );
            }

            bool isSecret = IsWordSecret(
                match.Index,
                match.Length,
                secretCharacters
            );

            WordData word = new WordData
            {
                id = wordId,
                originalText = match.Value,
                isSecret = isSecret,
                isRedacted = false
            };

            words.Add(word);

            textParts.Add(
                TextPart.CreateWord(wordId)
            );

            wordId++;

            currentPosition =
                match.Index + match.Length;
        }

        if (currentPosition < cleanText.Length)
        {
            string remainingText =
                cleanText.Substring(currentPosition);

            textParts.Add(
                TextPart.CreateSeparator(remainingText)
            );
        }
    }

    /// <summary>
    /// Слово считается секретным,
    /// если хотя бы один его символ
    /// находился внутри [[...]].
    /// </summary>
    private bool IsWordSecret(
        int startIndex,
        int length,
        List<bool> secretCharacters
    )
    {
        int endIndex = startIndex + length;

        for (int i = startIndex;
             i < endIndex &&
             i < secretCharacters.Count;
             i++)
        {
            if (secretCharacters[i])
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Начало клика или движения мыши.
    /// Первое слово определяет режим:
    /// ставить плашки или снимать их.
    /// </summary>
    public void OnPointerDown(
        PointerEventData eventData
    )
    {
        if (documentFinished)
        {
            return;
        }

        if (eventData.button !=
            PointerEventData.InputButton.Left)
        {
            return;
        }

        int wordId = GetWordIdAtPosition(
            eventData.position,
            eventData.pressEventCamera
        );

        if (wordId == -1)
        {
            return;
        }

        isDragging = true;
        processedDragWords.Clear();

        WordData firstWord = words[wordId];

        // Если слово открыто — начинаем ставить плашки.
        // Если слово закрыто — начинаем снимать их.
        dragRedactionState = !firstWord.isRedacted;

        ApplyDragState(wordId);
    }

    /// <summary>
    /// Обрабатывает движение мыши
    /// при зажатой левой кнопке.
    /// </summary>
    public void OnDrag(
        PointerEventData eventData
    )
    {
        if (documentFinished || !isDragging)
        {
            return;
        }

        int wordId = GetWordIdAtPosition(
            eventData.position,
            eventData.pressEventCamera
        );

        if (wordId == -1)
        {
            return;
        }

        ApplyDragState(wordId);
    }

    /// <summary>
    /// Завершает движение мыши.
    /// </summary>
    public void OnPointerUp(
        PointerEventData eventData
    )
    {
        if (eventData.button !=
            PointerEventData.InputButton.Left)
        {
            return;
        }

        StopDragging();
    }

    /// <summary>
    /// Возвращает ID слова под курсором.
    /// </summary>
    private int GetWordIdAtPosition(
        Vector2 screenPosition,
        Camera eventCamera
    )
    {
        Camera cameraForText = eventCamera;

        if (documentText.canvas != null &&
            documentText.canvas.renderMode ==
            RenderMode.ScreenSpaceOverlay)
        {
            cameraForText = null;
        }

        int linkIndex =
            TMP_TextUtilities.FindIntersectingLink(
                documentText,
                screenPosition,
                cameraForText
            );

        if (linkIndex == -1)
        {
            return -1;
        }

        if (linkIndex >=
            documentText.textInfo.linkCount)
        {
            return -1;
        }

        TMP_LinkInfo linkInfo =
            documentText.textInfo.linkInfo[linkIndex];

        string linkId = linkInfo.GetLinkID();

        if (!int.TryParse(linkId, out int wordId))
        {
            Debug.LogWarning(
                $"Не удалось определить ID слова: {linkId}"
            );

            return -1;
        }

        if (wordId < 0 || wordId >= words.Count)
        {
            return -1;
        }

        return wordId;
    }

    /// <summary>
    /// Применяет к слову состояние текущего движения.
    /// </summary>
    private void ApplyDragState(int wordId)
    {
        if (wordId < 0 || wordId >= words.Count)
        {
            return;
        }

        if (processedDragWords.Contains(wordId))
        {
            return;
        }

        processedDragWords.Add(wordId);

        WordData word = words[wordId];

        if (word.isRedacted == dragRedactionState)
        {
            return;
        }

        word.isRedacted = dragRedactionState;

        RefreshDocument();

        SetStatus(
            "Документ изменён. " +
            "Результат ещё не проверен."
        );
    }

    /// <summary>
    /// Завершает текущее движение мыши.
    /// </summary>
    private void StopDragging()
    {
        isDragging = false;
        processedDragWords.Clear();
    }

    /// <summary>
    /// Полностью перестраивает отображаемый текст.
    /// </summary>
    private void RefreshDocument()
    {
        StringBuilder result =
            new StringBuilder();

        foreach (TextPart part in textParts)
        {
            if (!part.isWord)
            {
                // Красим пробелы и знаки препинания
                // тем же цветом, что и основной текст.
                result.Append(
                    $"<color={normalTextColor}>" +
                    part.separatorText +
                    "</color>"
                );

                continue;
            }

            WordData word = words[part.wordId];

            result.Append(
                CreateWordMarkup(word)
            );
        }

        documentText.text = result.ToString();
        documentText.ForceMeshUpdate();

        UpdateProgress();
    }

    /// <summary>
    /// Создаёт TMP-разметку одного слова.
    /// Каждое слово получает уникальную ссылку.
    /// </summary>
    private string CreateWordMarkup(WordData word)
    {
        string visibleWord;

        if (word.isRedacted)
        {
            visibleWord =
                CreateRedactedWord(
                    word.originalText
                );
        }
        else if (word.isSecret)
        {
            visibleWord =
                CreateSubtlyHighlightedWord(
                    word.originalText
                );
        }
        else
        {
            visibleWord =
                $"<color={normalTextColor}>" +
                word.originalText +
                "</color>";
        }

        return
            $"<link=\"{word.id}\">" +
            visibleWord +
            "</link>";
    }

    /// <summary>
    /// Слабо выделяет правильное слово.
    /// </summary>
    private string CreateSubtlyHighlightedWord(
        string originalText
    )
    {
        return
            $"<mark={secretHighlightColor}>" +
            $"<size={secretTextSizePercent}%>" +
            $"<color={secretTextColor}>" +
            originalText +
            "</color>" +
            "</size>" +
            "</mark>";
    }

    /// <summary>
    /// Создаёт цензурную плашку,
    /// сохраняя ширину исходного слова.
    /// </summary>
    private string CreateRedactedWord(
        string originalText
    )
    {
        return
            $"<mark={redactionColor}>" +
            "<color=#00000000>" +
            originalText +
            "</color>" +
            "</mark>";
    }

    /// <summary>
    /// Показывает количество выбранных слов,
    /// не раскрывая их правильность.
    /// </summary>
    private void UpdateProgress()
    {
        int redactedWordCount = 0;

        foreach (WordData word in words)
        {
            if (word.isRedacted)
            {
                redactedWordCount++;
            }
        }

        progressText.text =
            $"Документ: {currentDocumentIndex + 1}" +
            $" / {documents.Count}\n" +
            $"Засекречено слов: {redactedWordCount}";
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    /// <summary>
    /// Вызывается кнопкой «Передать документ».
    /// </summary>
    public void SubmitDocument()
    {
        if (documentFinished)
        {
            return;
        }

        StopDragging();

        int missedSecretWords = 0;
        int extraRedactedWords = 0;

        foreach (WordData word in words)
        {
            // Секретное слово осталось открытым.
            if (word.isSecret &&
                !word.isRedacted)
            {
                missedSecretWords++;
            }

            // Обычное слово было скрыто.
            if (!word.isSecret &&
                word.isRedacted)
            {
                extraRedactedWords++;
            }
        }

        bool documentIsCorrect =
            missedSecretWords == 0 &&
            extraRedactedWords == 0;

        if (documentIsCorrect)
        {
            CompleteDocument();
        }
        else
        {
            RejectDocument(
                missedSecretWords,
                extraRedactedWords
            );
        }
    }

    /// <summary>
    /// Сообщает количество ошибок,
    /// но не раскрывает их расположение.
    /// </summary>
    private void RejectDocument(
        int missedSecretWords,
        int extraRedactedWords
    )
    {
        StringBuilder message =
            new StringBuilder();

        message.AppendLine(
            "ДОКУМЕНТ ОТКЛОНЁН"
        );

        if (missedSecretWords > 0)
        {
            message.AppendLine(
                $"Пропущено слов: {missedSecretWords}"
            );
        }

        if (extraRedactedWords > 0)
        {
            message.AppendLine(
                $"Лишних засекречиваний: " +
                $"{extraRedactedWords}"
            );
        }

        message.Append(
            "Исправьте документ и отправьте снова."
        );

        SetStatus(message.ToString());
    }

    /// <summary>
    /// Завершает текущий документ.
    /// </summary>
    private void CompleteDocument()
    {
        documentFinished = true;
        StopDragging();

        submitButton.SetActive(false);
        winPanel.SetActive(true);

        bool hasNextDocument =
            currentDocumentIndex <
            documents.Count - 1;

        if (completionText != null)
        {
            completionText.text = hasNextDocument
                ? "ДОКУМЕНТ ПРИНЯТ"
                : "ВСЕ ДОКУМЕНТЫ ОБРАБОТАНЫ";
        }

        if (nextDocumentButton != null)
        {
            nextDocumentButton.SetActive(
                hasNextDocument
            );
        }

        SetStatus(
            "Проверка завершена. Документ принят."
        );
    }

    /// <summary>
    /// Вызывается кнопкой следующего документа.
    /// </summary>
    public void NextDocument()
    {
        if (!documentFinished)
        {
            return;
        }

        if (currentDocumentIndex >=
            documents.Count - 1)
        {
            return;
        }

        currentDocumentIndex++;
        LoadCurrentDocument();
    }

    /// <summary>
    /// Данные одного слова.
    /// </summary>
    private class WordData
    {
        public int id;
        public string originalText;
        public bool isSecret;
        public bool isRedacted;
    }

    /// <summary>
    /// Часть документа:
    /// слово или разделитель.
    /// </summary>
    private class TextPart
    {
        public bool isWord;
        public int wordId;
        public string separatorText;

        public static TextPart CreateWord(int id)
        {
            return new TextPart
            {
                isWord = true,
                wordId = id,
                separatorText = string.Empty
            };
        }

        public static TextPart CreateSeparator(
            string text
        )
        {
            return new TextPart
            {
                isWord = false,
                wordId = -1,
                separatorText = text
            };
        }
    }

    /// <summary>
    /// Результат удаления служебных скобок.
    /// </summary>
    private class ParsedSource
    {
        public string cleanText;
        public List<bool> secretCharacters;
    }
}
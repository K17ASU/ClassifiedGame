using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Управляет документами, засекречиванием слов,
/// проверками, начислением очков и переходом
/// между документами.
///
/// Правильные слова задаются внутри [[двойных скобок]].
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
    private TMP_Text inspectionsText;

    [SerializeField]
    private GameObject submitButton;

    [Header("Панель результата")]

    [SerializeField]
    private GameObject winPanel;

    [SerializeField]
    private TMP_Text completionText;

    [SerializeField]
    private TMP_Text resultScoreText;

    [SerializeField]
    private GameObject nextDocumentButton;

    [SerializeField]
    private GameObject restartButton;

    [Header("Документы")]

    [SerializeField]
    private List<DocumentData> documents =
        new List<DocumentData>();

    [Header("Проверки")]

    [SerializeField]
    [Min(1)]
    private int maximumInspections = 3;

    [Header("Очки")]

    [SerializeField]
    [Min(0)]
    private int firstTryScore = 100;

    [SerializeField]
    [Min(0)]
    private int secondTryScore = 75;

    [SerializeField]
    [Min(0)]
    private int thirdTryScore = 50;

    [Header("Обычный текст")]

    [SerializeField]
    private string normalTextColor = "#24211C";

    [Header("Слабая подсказка")]

    [SerializeField]
    private string secretTextColor = "#4B4438";

    [SerializeField]
    private string secretHighlightColor = "#8E713018";

    [Range(95, 105)]
    [SerializeField]
    private int secretTextSizePercent = 99;

    [Header("Цензурная плашка")]

    [SerializeField]
    private string redactionColor = "#000000FF";

    private readonly List<WordData> words =
        new List<WordData>();

    private readonly List<TextPart> textParts =
        new List<TextPart>();

    private readonly HashSet<int> processedDragWords =
        new HashSet<int>();

    private int currentDocumentIndex;
    private int inspectionsRemaining;
    private int totalScore;

    private bool documentFinished;
    private bool isDragging;
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
        totalScore = 0;

        LoadCurrentDocument();
    }

    private void OnDisable()
    {
        StopDragging();
    }

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

        if (inspectionsText == null)
        {
            Debug.LogError(
                "Не назначено поле Inspections Text."
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

        if (completionText == null)
        {
            Debug.LogError(
                "Не назначено поле Completion Text."
            );

            referencesAreValid = false;
        }

        if (resultScoreText == null)
        {
            Debug.LogError(
                "Не назначено поле Result Score Text."
            );

            referencesAreValid = false;
        }

        if (restartButton == null)
        {
            Debug.LogError(
                "Не назначено поле Restart Button."
            );

            referencesAreValid = false;
        }

        return referencesAreValid;
    }

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
        inspectionsRemaining = maximumInspections;

        winPanel.SetActive(false);
        submitButton.SetActive(true);
        restartButton.SetActive(false);

        UpdateDocumentTitle(currentDocument);
        ParseDocument(currentDocument.DocumentText);
        RefreshDocument();
        UpdateInspectionsDisplay();

        SetStatus(
            "Выберите сведения для засекречивания."
        );
    }

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
                insideSecretFragment = true;
                position += 2;
                continue;
            }

            if (endsSecretFragment)
            {
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
            textParts.Add(TextPart.CreateWord(wordId));

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

        dragRedactionState =
            !firstWord.isRedacted;

        ApplyDragState(wordId);
    }

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
            return -1;
        }

        if (wordId < 0 || wordId >= words.Count)
        {
            return -1;
        }

        return wordId;
    }

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

    private void StopDragging()
    {
        isDragging = false;
        processedDragWords.Clear();
    }

    private void RefreshDocument()
    {
        StringBuilder result =
            new StringBuilder();

        foreach (TextPart part in textParts)
        {
            if (!part.isWord)
            {
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

    private void UpdateInspectionsDisplay()
    {
        StringBuilder result =
            new StringBuilder("ПРОВЕРКИ: ");

        for (int i = 0;
             i < maximumInspections;
             i++)
        {
            if (i < inspectionsRemaining)
            {
                result.Append("● ");
            }
            else
            {
                result.Append("○ ");
            }
        }

        inspectionsText.text = result.ToString();
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

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
            if (word.isSecret &&
                !word.isRedacted)
            {
                missedSecretWords++;
            }

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
            CompleteDocumentSuccessfully();
            return;
        }

        inspectionsRemaining--;
        UpdateInspectionsDisplay();

        if (inspectionsRemaining <= 0)
        {
            FailDocument();
        }
        else
        {
            RejectDocument(
                missedSecretWords,
                extraRedactedWords
            );
        }
    }

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

    private void CompleteDocumentSuccessfully()
    {
        int documentScore =
            CalculateDocumentScore();

        totalScore += documentScore;

        ShowResultPanel(
            documentPassed: true,
            documentScore: documentScore
        );
    }

    private int CalculateDocumentScore()
    {
        int failedInspections =
            maximumInspections -
            inspectionsRemaining;

        if (failedInspections <= 0)
        {
            return firstTryScore;
        }

        if (failedInspections == 1)
        {
            return secondTryScore;
        }

        return thirdTryScore;
    }

    private void FailDocument()
    {
        ShowResultPanel(
            documentPassed: false,
            documentScore: 0
        );
    }

    private void ShowResultPanel(
        bool documentPassed,
        int documentScore
    )
    {
        documentFinished = true;
        StopDragging();

        submitButton.SetActive(false);
        winPanel.SetActive(true);

        bool hasNextDocument =
            currentDocumentIndex <
            documents.Count - 1;

        bool isLastDocument =
            currentDocumentIndex ==
            documents.Count - 1;

        if (documentPassed)
        {
            if (isLastDocument)
            {
                completionText.text =
                    "ВСЕ ДОКУМЕНТЫ ОБРАБОТАНЫ";
            }
            else
            {
                completionText.text =
                    "ДОКУМЕНТ ПРИНЯТ";
            }
        }
        else
        {
            completionText.text =
                isLastDocument
                    ? "ОБРАБОТКА ЗАВЕРШЕНА"
                    : "УТЕЧКА ИНФОРМАЦИИ";
        }

        if (isLastDocument)
        {
            resultScoreText.text =
                $"НАГРАДА: {documentScore}\n" +
                $"ИТОГОВЫЙ СЧЁТ: {totalScore}" +
                $" / {GetMaximumTotalScore()}";
        }
        else
        {
            resultScoreText.text =
                $"НАГРАДА: {documentScore}\n" +
                $"ОБЩИЙ СЧЁТ: {totalScore}";
        }

        if (nextDocumentButton != null)
        {
            nextDocumentButton.SetActive(
                hasNextDocument
            );
        }

        if (restartButton != null)
        {
            restartButton.SetActive(
                isLastDocument
            );
        }

        SetStatus(string.Empty);

        SetStatus(string.Empty);
    }

    private int GetMaximumTotalScore()
    {
        return documents.Count * firstTryScore;
    }
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

    public void RestartGame()
    {
        StopDragging();

        currentDocumentIndex = 0;
        totalScore = 0;
        documentFinished = false;

        LoadCurrentDocument();
    }

    private class WordData
    {
        public int id;
        public string originalText;
        public bool isSecret;
        public bool isRedacted;
    }

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

    private class ParsedSource
    {
        public string cleanText;
        public List<bool> secretCharacters;
    }
}
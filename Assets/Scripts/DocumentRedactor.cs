using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

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
    private TMP_Text briefingText;

    [SerializeField]
    private GameObject submitButton;

    [Header("Ультрафиолетовая лампа")]

    [SerializeField]
    private Button ultravioletButton;

    [SerializeField]
    private TMP_Text ultravioletButtonText;

    [SerializeField]
    private RectTransform ultravioletCursor;

    [SerializeField]
    private LocalizedString ultravioletInactiveText;

    [SerializeField]
    private LocalizedString ultravioletActiveText;

    [SerializeField]
    private string ultravioletSecretTextColor =
        "#6B35A8";

    [SerializeField]
    private string ultravioletSecretHighlightColor =
        "#8A4DFF35";

    [SerializeField]
    [Min(10f)]
    private float ultravioletRevealRadius = 110f;

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
    private TMP_Text nextDocumentButtonText;

    [SerializeField]
    private LocalizedString nextDocumentText;

    [SerializeField]
    private LocalizedString startWorkText;

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

    [Header("Цензурная плашка")]

    [SerializeField]
    private string redactionColor = "#000000FF";

    [Header("Локализация интерфейса")]

    [SerializeField]
    private LocalizedString statusSelectInfo;

    [SerializeField]
    private LocalizedString statusDocumentChanged;

    [SerializeField]
    private LocalizedString statusUvOn;

    [SerializeField]
    private LocalizedString statusUvOff;

    [SerializeField]
    private LocalizedString tutorialModeText;

    [SerializeField]
    private LocalizedString tutorialReadyText;

    [SerializeField]
    private LocalizedString tutorialExtraText;

    [SerializeField]
    private LocalizedString tutorialContinueText;

    [SerializeField]
    private LocalizedString progressTutorialText;

    [SerializeField]
    private LocalizedString progressDocumentText;

    [SerializeField]
    private LocalizedString resultTutorialCompleteText;

    [SerializeField]
    private LocalizedString resultAllCompleteText;

    [SerializeField]
    private LocalizedString resultDocumentAcceptedText;

    [SerializeField]
    private LocalizedString resultProcessingCompleteText;

    [SerializeField]
    private LocalizedString resultDocumentFailedText;

    [SerializeField]
    private LocalizedString resultTutorialScoreText;

    [SerializeField]
    private LocalizedString resultScoreLocalizedText;

    [SerializeField]
    private LocalizedString resultFinalScoreLocalizedText;

    [SerializeField]
    private LocalizedString errorMissedText;

    [SerializeField]
    private LocalizedString errorExtraText;

    [SerializeField]
    private LocalizedString inspectionsLocalizedText;

    [SerializeField]
    private LocalizedString documentRejectedText;

    [SerializeField]
    private LocalizedString documentFixRetryText;

    private DocumentData CurrentDocument
    {
        get
        {
            return IsValidDocumentIndex(currentDocumentIndex)
                ? documents[currentDocumentIndex]
                : null;
        }
    }

    private readonly List<DocumentWord> words =
        new List<DocumentWord>();

    private readonly List<DocumentTextPart> textParts =
        new List<DocumentTextPart>();

    private readonly HashSet<int> processedDragWords =
        new HashSet<int>();

    private readonly DocumentParser documentParser =
        new DocumentParser();

    private readonly DocumentEvaluator documentEvaluator =
        new DocumentEvaluator();

    private int currentDocumentIndex;
    private int inspectionsRemaining;
    private int totalScore;

    private bool documentFinished;
    private bool isDragging;
    private bool dragRedactionState;
    private bool ultravioletModeActive;
    private bool isInitialized;

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
        isInitialized = true;
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged +=
            OnSelectedLocaleChanged;
    }
    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -=
            OnSelectedLocaleChanged;

        StopDragging();
    }

    private bool ValidateReferences()
    {
        bool referencesAreValid = true;

        if (documentTitleText == null)
        {
            Debug.LogError(
                "Не назначено поле Document Title Text."
            );

            referencesAreValid = false;
        }

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

        if (nextDocumentButton == null)
        {
            Debug.LogError(
                "Не назначено поле Next Document Button."
            );

            referencesAreValid = false;
        }

        if (nextDocumentButtonText == null)
        {
            Debug.LogError(
                "Не назначено поле Next Document Button Text."
            );

            referencesAreValid = false;
        }

        if (ultravioletButton == null)
        {
            Debug.LogError(
                "Не назначено поле Ultraviolet Button."
            );

            referencesAreValid = false;
        }

        if (ultravioletButtonText == null)
        {
            Debug.LogError(
                "Не назначено поле Ultraviolet Button Text."
            );

            referencesAreValid = false;
        }

        if (ultravioletCursor == null)
        {
            Debug.LogError(
                "Не назначено поле Ultraviolet Cursor."
            );

            referencesAreValid = false;
        }

        return referencesAreValid;
    }
    private bool IsValidDocumentIndex(int index)
    {
        return documents != null &&
               index >= 0 &&
               index < documents.Count;
    }

    private bool IsCurrentDocumentTutorial()
    {
        return CurrentDocument != null &&
               CurrentDocument.IsTutorial;
    }

    private void LoadCurrentDocument()
    {
        if (!IsValidDocumentIndex(currentDocumentIndex))
        {
            Debug.LogError(
                $"Некорректный индекс документа: {currentDocumentIndex}."
            );
            return;
        }

        DocumentData currentDocument = CurrentDocument;

        if (currentDocument == null)
        {
            Debug.LogError(
                $"Документ под индексом {currentDocumentIndex} не назначен."
            );
            return;
        }

        StopDragging();
        DisableUltravioletMode();

        documentFinished = false;
        inspectionsRemaining = maximumInspections;

        winPanel.SetActive(false);
        submitButton.SetActive(true);
        restartButton.SetActive(false);

        UpdateDocumentTitle(currentDocument);
        string localizedDocumentText =
            GetLocalizedDocumentString(
                currentDocument.LocalizedDocumentText,
                currentDocument.DocumentText
         );

        ParseDocument(
            localizedDocumentText
        );
        RefreshDocument();
        UpdateInspectionsDisplay();

        SetStatus(
            Localize(statusSelectInfo)
        );

        UpdateDynamicButtonTexts();
        UpdateBriefing();
    }

    private void UpdateDocumentTitle(
        DocumentData document
    )
    {
        if (documentTitleText == null)
        {
            return;
        }

        string documentNumber =
            GetLocalizedDocumentString(
                document.LocalizedDocumentNumber,
                document.DocumentNumber
            );

        string documentTitle =
            GetLocalizedDocumentString(
                document.LocalizedDocumentTitle,
                document.DocumentTitle
            );

        documentTitleText.text =
            $"{documentNumber}\n" +
            $"{documentTitle}";
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

        DocumentParseResult result =
            documentParser.Parse(sourceText);

        words.AddRange(result.words);
        textParts.AddRange(result.textParts);

        if (result.hasUnclosedSecretMarker)
        {
            Debug.LogWarning(
                "В документе не закрыта пара скобок [[...]]."
            );
        }

        if (result.secretWordCount == 0)
        {
            Debug.LogWarning(
                "В документе нет секретных слов [[...]]."
            );
        }
    }

    public void OnPointerDown(
        PointerEventData eventData
    )
    {
        if (documentFinished ||
     ultravioletModeActive)
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

        DocumentWord firstWord = words[wordId];

        dragRedactionState =
            !firstWord.isRedacted;

        ApplyDragState(wordId);
    }

    public void OnDrag(
        PointerEventData eventData
    )
    {
        if (documentFinished ||
            ultravioletModeActive ||
            !isDragging)
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

        DocumentWord word = words[wordId];

        if (word.isRedacted == dragRedactionState)
        {
            return;
        }

        word.isRedacted = dragRedactionState;

        RefreshDocument();

        if (IsCurrentDocumentTutorial())
        {
            UpdateTutorialStatus();
        }
        else
        {
            SetStatus(
               Localize(statusDocumentChanged)
            );
        }
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

        foreach (DocumentTextPart part in textParts)
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

            DocumentWord word = words[part.wordId];

            result.Append(
                CreateWordMarkup(word)
            );
        }

        documentText.text = result.ToString();
        documentText.ForceMeshUpdate();

        UpdateProgress();
    }

    private string CreateWordMarkup(DocumentWord word)
    {
        string visibleWord;

        if (word.isRedacted)
        {
            visibleWord =
                CreateRedactedWord(
                    word.originalText
                );
        }
        else if (word.isUltravioletRevealed)
        {
            visibleWord =
                CreateUltravioletWord(
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

    private string CreateUltravioletWord(
     string originalText
 )
    {
        return
            $"<mark={ultravioletSecretHighlightColor}>" +
            $"<color={ultravioletSecretTextColor}>" +
            originalText +
            "</color>" +
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

        foreach (DocumentWord word in words)
        {
            if (word.isRedacted)
            {
                redactedWordCount++;
            }
        }

        if (IsCurrentDocumentTutorial())
        {
            progressText.text =
                Localize(
                    progressTutorialText,
                    redactedWordCount
                );

            return;
        }

        int currentPlayableDocument =
            GetCurrentPlayableDocumentNumber();

        int playableDocumentCount =
            GetPlayableDocumentCount();

        progressText.text =
            Localize(
                progressDocumentText,
                currentPlayableDocument,
                playableDocumentCount,
                redactedWordCount
            );
    }

    private void UpdateInspectionsDisplay()
    {
        if (IsCurrentDocumentTutorial())
        {
            inspectionsText.text =
                Localize(tutorialModeText);

            return;
        }

        StringBuilder marks =
            new StringBuilder();

        for (int i = 0;
             i < maximumInspections;
             i++)
        {
            marks.Append(
                i < inspectionsRemaining
                    ? "● "
                    : "○ "
            );
        }

        inspectionsText.text =
            Localize(
                inspectionsLocalizedText,
                marks.ToString()
            );
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void CountDocumentErrors(
        out int missedSecretWords,
        out int extraRedactedWords
    )
    {
        missedSecretWords = 0;
        extraRedactedWords = 0;

        foreach (DocumentWord word in words)
        {
            if (word.requiresRedaction && !word.isRedacted)
            {
                missedSecretWords++;
            }

            if (!word.requiresRedaction && word.isRedacted)
            {
                extraRedactedWords++;
            }
        }
    }

    public void SubmitDocument()
    {
        if (documentFinished)
        {
            return;
        }

        StopDragging();

        DocumentEvaluationResult evaluation =
            documentEvaluator.Evaluate(words);

        int missedSecretWords =
            evaluation.missedWords;

        int extraRedactedWords =
            evaluation.extraRedactions;

        if (evaluation.IsCorrect)
        {
            CompleteDocumentSuccessfully();
            return;
        }

        if (IsCurrentDocumentTutorial())
        {
            RejectTutorialDocument(
                missedSecretWords,
                extraRedactedWords
            );

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
            Localize(documentRejectedText)
        );

        if (missedSecretWords > 0)
        {
            message.AppendLine(
                Localize(
                    errorMissedText,
                    missedSecretWords
                )
            );
        }

        if (extraRedactedWords > 0)
        {
            message.AppendLine(
                Localize(
                    errorExtraText,
                    extraRedactedWords
                )
            );
        }

        message.Append(
            Localize(documentFixRetryText)
        );

        SetStatus(message.ToString());
    }

    private void CompleteDocumentSuccessfully()
    {
        if (IsCurrentDocumentTutorial())
        {
            ShowResultPanel(
                documentPassed: true,
                documentScore: 0
            );

            return;
        }

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
        DisableUltravioletMode();

        submitButton.SetActive(false);
        winPanel.SetActive(true);

        bool hasNextDocument =
            currentDocumentIndex <
            documents.Count - 1;

        bool isLastDocument =
            currentDocumentIndex ==
            documents.Count - 1;
        bool isTutorial =
            IsCurrentDocumentTutorial();

        if (documentPassed)
        {
            if (isTutorial)
            {
                completionText.text =
                    Localize(
                        resultTutorialCompleteText
                    );
            }
            else if (isLastDocument)
            {
                completionText.text =
                    Localize(
                        resultAllCompleteText
                    );
            }
            else
            {
                completionText.text =
                    Localize(
                        resultDocumentAcceptedText
                    );
            }
        }
        else
        {
            completionText.text =
                isLastDocument
                    ? Localize(
                        resultProcessingCompleteText
                    )
                    : Localize(
                        resultDocumentFailedText
                    );
        }

        if (isTutorial)
        {
            resultScoreText.text =
                Localize(
                    resultTutorialScoreText
                );
        }
        else if (isLastDocument)
        {
            resultScoreText.text =
                Localize(
                    resultFinalScoreLocalizedText,
                    documentScore,
                    totalScore,
                    GetMaximumTotalScore()
                );
        }
        else
        {
            resultScoreText.text =
                Localize(
                    resultScoreLocalizedText,
                    documentScore,
                    totalScore
                );
        }

        UpdateNextDocumentButtonText();

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
    }

    private int GetMaximumTotalScore()
    {
        return GetPlayableDocumentCount() *
               firstTryScore;
    }
    public void NextDocument()
    {

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

    private int GetPlayableDocumentCount()
    {
        int count = 0;

        foreach (DocumentData document in documents)
        {
            if (document != null &&
                !document.IsTutorial)
            {
                count++;
            }
        }

        return count;
    }

    private int GetCurrentPlayableDocumentNumber()
    {
        int number = 0;

        for (int i = 0;
             i <= currentDocumentIndex &&
             i < documents.Count;
             i++)
        {
            DocumentData document = documents[i];

            if (document != null &&
                !document.IsTutorial)
            {
                number++;
            }
        }

        return number;
    }

    private void UpdateTutorialStatus()
    {
        DocumentEvaluationResult evaluation =
            documentEvaluator.Evaluate(words);

        int missedSecretWords =
            evaluation.missedWords;

        int extraRedactedWords =
            evaluation.extraRedactions;

        if (evaluation.IsCorrect)
        {
            SetStatus(
                Localize(tutorialReadyText)
            );

            return;
        }

        if (extraRedactedWords > 0)
        {
            SetStatus(
                Localize(tutorialExtraText)
            );

            return;
        }

        SetStatus(
            Localize(tutorialContinueText)
        );
    }

    private void RejectTutorialDocument(
    int missedSecretWords,
    int extraRedactedWords
)
    {
        StringBuilder message =
            new StringBuilder();

        message.AppendLine(
            "УЧЕБНЫЙ ДОКУМЕНТ НЕ ГОТОВ"
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
                $"Лишних плашек: {extraRedactedWords}"
            );
        }

        message.Append(
            "Исправьте выделение и попробуйте снова. " +
            "Проверка не была потрачена."
        );

        SetStatus(message.ToString());
    }

    private void Update()
    {
        if (!ultravioletModeActive ||
            ultravioletCursor == null ||
            Mouse.current == null)
        {
            return;
        }

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        ultravioletCursor.position =
            mousePosition;

        UpdateUltravioletReveal(
            mousePosition
        );
    }

    public void ToggleUltravioletMode()
    {
        if (documentFinished)
        {
            return;
        }

        ultravioletModeActive =
            !ultravioletModeActive;

        StopDragging();

        if (ultravioletCursor == null)
        {
            Debug.LogError(
                "Не назначен Ultraviolet Cursor."
            );

            ultravioletModeActive = false;
            return;
        }

        ultravioletCursor.gameObject.SetActive(
            ultravioletModeActive
        );

        if (ultravioletModeActive)
        {
            ultravioletCursor.position =
                Mouse.current != null
                    ? Mouse.current.position.ReadValue()
                    : Vector2.zero;
        }
        else
        {
            ClearUltravioletReveal();
        }

        UpdateUltravioletButtonText();

        if (ultravioletModeActive)
        {
            SetStatus(
                Localize(statusUvOn)
            );
        }
        else
        {
            SetStatus(
                Localize(statusUvOff)
            );
        }
    }

    private void UpdateUltravioletReveal(
    Vector2 lampScreenPosition
)
    {
        if (documentText == null)
        {
            return;
        }

        documentText.ForceMeshUpdate();

        TMP_TextInfo textInfo =
            documentText.textInfo;

        bool displayChanged = false;

        for (int linkIndex = 0;
             linkIndex < textInfo.linkCount;
             linkIndex++)
        {
            TMP_LinkInfo linkInfo =
                textInfo.linkInfo[linkIndex];

            if (!int.TryParse(
                    linkInfo.GetLinkID(),
                    out int wordId))
            {
                continue;
            }

            if (wordId < 0 ||
                wordId >= words.Count)
            {
                continue;
            }

            DocumentWord word = words[wordId];

            bool shouldBeRevealed =
                word.CanBeRevealedBy(
                    RevealMethod.Ultraviolet
                ) &&
                !word.isRedacted &&
                IsLinkInsideUltravioletLight(
                    linkInfo,
                    lampScreenPosition
                );

            if (word.isUltravioletRevealed ==
                shouldBeRevealed)
            {
                continue;
            }

            word.isUltravioletRevealed =
                shouldBeRevealed;

            displayChanged = true;
        }

        if (displayChanged)
        {
            RefreshDocument();
        }
    }

    private bool IsLinkInsideUltravioletLight(
    TMP_LinkInfo linkInfo,
    Vector2 lampScreenPosition
)
    {
        TMP_TextInfo textInfo =
            documentText.textInfo;

        if (linkInfo.linkTextLength <= 0)
        {
            return false;
        }

        Vector2 minimum =
            new Vector2(
                float.PositiveInfinity,
                float.PositiveInfinity
            );

        Vector2 maximum =
            new Vector2(
                float.NegativeInfinity,
                float.NegativeInfinity
            );

        int firstCharacterIndex =
            linkInfo.linkTextfirstCharacterIndex;

        int lastCharacterIndex =
            firstCharacterIndex +
            linkInfo.linkTextLength;

        for (int characterIndex =
                 firstCharacterIndex;
             characterIndex < lastCharacterIndex;
             characterIndex++)
        {
            if (characterIndex < 0 ||
                characterIndex >=
                textInfo.characterCount)
            {
                continue;
            }

            TMP_CharacterInfo characterInfo =
                textInfo.characterInfo[
                    characterIndex
                ];

            if (!characterInfo.isVisible)
            {
                continue;
            }

            Vector3 bottomLeftWorld =
                documentText.transform.TransformPoint(
                    characterInfo.bottomLeft
                );

            Vector3 topRightWorld =
                documentText.transform.TransformPoint(
                    characterInfo.topRight
                );

            Vector2 bottomLeftScreen =
                RectTransformUtility
                    .WorldToScreenPoint(
                        GetDocumentCanvasCamera(),
                        bottomLeftWorld
                    );

            Vector2 topRightScreen =
                RectTransformUtility
                    .WorldToScreenPoint(
                        GetDocumentCanvasCamera(),
                        topRightWorld
                    );

            minimum = Vector2.Min(
                minimum,
                bottomLeftScreen
            );

            maximum = Vector2.Max(
                maximum,
                topRightScreen
            );
        }

        if (float.IsInfinity(minimum.x))
        {
            return false;
        }

        Vector2 closestPoint =
            new Vector2(
                Mathf.Clamp(
                    lampScreenPosition.x,
                    minimum.x,
                    maximum.x
                ),
                Mathf.Clamp(
                    lampScreenPosition.y,
                    minimum.y,
                    maximum.y
                )
            );

        float distance =
            Vector2.Distance(
                lampScreenPosition,
                closestPoint
            );

        return distance <=
               ultravioletRevealRadius;
    }

    private Camera GetDocumentCanvasCamera()
    {
        if (documentText == null ||
            documentText.canvas == null)
        {
            return null;
        }

        Canvas canvas =
            documentText.canvas;

        if (canvas.renderMode ==
            RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return canvas.worldCamera;
    }

    private void ClearUltravioletReveal()
    {
        bool displayChanged = false;

        foreach (DocumentWord word in words)
        {
            if (!word.isUltravioletRevealed)
            {
                continue;
            }

            word.isUltravioletRevealed = false;
            displayChanged = true;
        }

        if (displayChanged)
        {
            RefreshDocument();
        }
    }
    private void DisableUltravioletMode()
    {
        ultravioletModeActive = false;

        StopDragging();
        ClearUltravioletReveal();

        if (ultravioletCursor != null)
        {
            ultravioletCursor.gameObject.SetActive(false);
        }

        UpdateUltravioletButtonText();
    }

    private void OnSelectedLocaleChanged(
     Locale locale
 )
    {
        UpdateDynamicButtonTexts();
        UpdateProgress();
        UpdateInspectionsDisplay();
        UpdateBriefing();
    }

    private void UpdateDynamicButtonTexts()
    {
        UpdateUltravioletButtonText();
        UpdateNextDocumentButtonText();
    }
    private void UpdateUltravioletButtonText()
    {
        if (ultravioletButtonText == null)
        {
            return;
        }

        LocalizedString selectedText =
            ultravioletModeActive
                ? ultravioletActiveText
                : ultravioletInactiveText;

        if (selectedText == null ||
            selectedText.IsEmpty)
        {
            return;
        }

        ultravioletButtonText.text =
            selectedText.GetLocalizedString();
    }
    private void UpdateNextDocumentButtonText()
    {
        if (nextDocumentButtonText == null)
        {
            return;
        }

        LocalizedString selectedText =
            IsCurrentDocumentTutorial()
                ? startWorkText
                : nextDocumentText;

        if (selectedText == null ||
            selectedText.IsEmpty)
        {
            return;
        }

        nextDocumentButtonText.text =
            selectedText.GetLocalizedString();
    }

    private string Localize(
    LocalizedString localizedString,
    params object[] arguments
)
    {
        if (localizedString == null ||
            localizedString.IsEmpty)
        {
            return string.Empty;
        }

        return arguments != null &&
               arguments.Length > 0
            ? localizedString.GetLocalizedString(arguments)
            : localizedString.GetLocalizedString();
    }

    private string GetLocalizedDocumentString(
    LocalizedString localizedString,
    string fallback
)
    {
        if (localizedString == null ||
            localizedString.IsEmpty)
        {
            return fallback;
        }

        string result =
            localizedString.GetLocalizedString();

        return string.IsNullOrWhiteSpace(result)
            ? fallback
            : result;
    }

    private void UpdateBriefing()
    {
        if (briefingText == null)
        {
            return;
        }

        DocumentData document = CurrentDocument;

        if (document == null)
        {
            briefingText.text = string.Empty;
            briefingText.gameObject.SetActive(false);
            return;
        }

        string briefing =
            GetLocalizedDocumentString(
                document.LocalizedBriefing,
                string.Empty
            );

        if (string.IsNullOrWhiteSpace(briefing))
        {
            briefingText.text = string.Empty;
            briefingText.gameObject.SetActive(false);
            return;
        }

        briefingText.gameObject.SetActive(true);
        briefingText.text = briefing;
    }
}

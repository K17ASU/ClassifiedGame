using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

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

    [Header("Инструменты анализа")]

    [SerializeField]
    private UltravioletTool ultravioletTool;

    [SerializeField]
    private MagnifierTool magnifierTool;

    [SerializeField]
    private DecoderTool decoderTool;

    [SerializeField]
    private PencilTool pencilTool;

    [SerializeField]
    private GameObject toolsPanel;

    [SerializeField]
    private GameObject ultravioletButton;

    [SerializeField]
    private GameObject magnifierButton;

    [SerializeField]
    private GameObject decoderButton;

    [SerializeField]
    private GameObject pencilButton;

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

    [Header("Сохранения")]

    [SerializeField]
    private CodexManager codexManager;

    [Header("Проверки")]

    [SerializeField]
    [Min(1)]
    private int maximumInspections = 3;

    [Header("Очки")]

    [SerializeField]
    [Min(1)]
    private int maximumDocumentScore = 100;

    [Header("Обычный текст")]

    [SerializeField]
    private string normalTextColor = "#24211C";

    [Header("Цензурная плашка")]

    [SerializeField]
    private string redactionColor = "#000000FF";

    [Header("Локализация интерфейса")]



    [SerializeField]
    private LocalizedString tutorialModeText;








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

    private RevealMethod unlockedTools =
        RevealMethod.None;

    private bool documentFinished;
    private bool isDragging;
    private bool dragRedactionState;
    private bool dragStrikeThroughState;
    private bool dragUsesPencil;
    private bool isInitialized;

    private bool hasInspectionStatus;
    private int lastMissedInformation;
    private int lastExtraRedactions;

    private IEnumerator Start()
    {
        yield return LocalizationSettings.InitializationOperation;

        yield return LocalizationSettings.StringDatabase
            .GetTableAsync("Documents");

        yield return LocalizationSettings.StringDatabase
            .GetTableAsync("UI");

        if (!ValidateReferences())
        {
            enabled = false;
            yield break;
        }

        if (documents.Count == 0)
        {
            Debug.LogError(
                "В списке Documents нет ни одного документа."
            );

            enabled = false;
            yield break;
        }

        if (!ultravioletTool.Initialize(
                documentText,
                words,
                RefreshDocument,
                StopDragging,
                () => documentFinished,
                _ => { }))
        {
            enabled = false;
            yield break;
        }

        if (!magnifierTool.Initialize(
                documentText,
                words,
                StopDragging,
                () => documentFinished,
                _ => { }))
        {
            enabled = false;
            yield break;
        }

        if (!decoderTool.Initialize(
                documentText,
                words,
                StopDragging,
                () => documentFinished,
                _ => { }))
        {
            enabled = false;
            yield break;
        }

        if (!pencilTool.Initialize(
                StopDragging,
                () => documentFinished))
        {
            enabled = false;
            yield break;
        }

        GameSessionStartMode startMode =
            GameSessionRequest.Consume();

        if (startMode ==
            GameSessionStartMode.Continue)
        {
            if (!CampaignProgress.TryLoadLatestCheckpoint(
                    out CheckpointSaveData checkpoint) ||
                !RestoreCheckpoint(checkpoint))
            {
                Debug.LogError(
                    "Не удалось продолжить сохранённую кампанию."
                );

                enabled = false;
                yield break;
            }
        }
        else
        {
            currentDocumentIndex = 0;
            totalScore = 0;

            unlockedTools = RevealMethod.None;

            codexManager.ResetProgress();

            GetComponent<StoryBranchingController>()
                ?.ResetForNewCampaign();

            LoadCurrentDocument();


            CampaignProgress.StartNewCampaign(
                CurrentDocument != null
                    ? CurrentDocument.DocumentId
                    : string.Empty,
                (int)unlockedTools,
                codexManager.GetUnlockedEntryIds(),
                codexManager.GetReadEntryIds()
            );
        }

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

        ultravioletTool?.DisableMode();
        magnifierTool?.DisableMode();
        decoderTool?.DisableMode();
        pencilTool?.DisableMode();
        StopDragging();
    }

    private void Update()
    {
        if (Mouse.current == null)
        {
            return;
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            DisableActiveTool();
        }
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

        if (ultravioletTool == null)
        {
            Debug.LogError(
                "Не назначено поле Ultraviolet Tool."
            );

            referencesAreValid = false;
        }

        if (magnifierTool == null)
        {
            Debug.LogError(
                "Не назначено поле Magnifier Tool."
            );

            referencesAreValid = false;
        }

        if (decoderTool == null)
        {
            Debug.LogError(
                "Не назначено поле Decoder Tool."
            );

            referencesAreValid = false;
        }

        if (pencilTool == null)
        {
            Debug.LogError(
                "Не назначено поле Pencil Tool."
            );

            referencesAreValid = false;
        }

        if (toolsPanel == null)
        {
            Debug.LogError(
                "Не назначено поле Tools Panel."
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

        if (magnifierButton == null)
        {
            Debug.LogError(
                "Не назначено поле Magnifier Button."
            );

            referencesAreValid = false;
        }

        if (decoderButton == null)
        {
            Debug.LogError(
                "Не назначено поле Decoder Button."
            );

            referencesAreValid = false;
        }

        if (pencilButton == null)
        {
            Debug.LogError(
                "Не назначено поле Pencil Button."
            );

            referencesAreValid = false;
        }
       
        if (codexManager == null)
        {
            Debug.LogError(
                "Не назначено поле Codex Manager."
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

    private bool RestoreCheckpoint(
     CheckpointSaveData checkpoint)
    {
        if (checkpoint == null ||
            string.IsNullOrWhiteSpace(
                checkpoint.currentDocumentId))
        {
            return false;
        }

        int documentIndex =
            FindDocumentIndexById(
                checkpoint.currentDocumentId
            );

        if (documentIndex < 0)
        {
            Debug.LogError(
                $"Не найден документ с Document Id: " +
                $"{checkpoint.currentDocumentId}"
            );

            return false;
        }

        currentDocumentIndex =
            documentIndex;

        totalScore =
            Mathf.Max(
                0,
                checkpoint.totalScore
            );

        unlockedTools =
            (RevealMethod)
            checkpoint.unlockedToolsMask;

        documentFinished = false;

        codexManager.RestoreProgress(
            checkpoint.unlockedCodexEntryIds,
            checkpoint.readCodexEntryIds
        );

        LoadCurrentDocument();

        Debug.Log(
            $"Checkpoint восстановлен. " +
            $"Документ: {checkpoint.currentDocumentId}, " +
            $"счёт: {totalScore}."
        );

        return true;
    }

    private int FindDocumentIndexById(
        string documentId)
    {
        if (string.IsNullOrWhiteSpace(documentId))
        {
            return -1;
        }

        for (int i = 0;
             i < documents.Count;
             i++)
        {
            DocumentData document =
                documents[i];

            if (document != null &&
                document.DocumentId ==
                documentId)
            {
                return i;
            }
        }

        return -1;
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

        GetComponent<StoryBranchingController>()
           ?.ClearResolvedRoute();

        StopDragging();
        ultravioletTool.DisableMode();
        magnifierTool.DisableMode();
        decoderTool.DisableMode();
        pencilTool.DisableMode();

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

        ClearInspectionStatus();

        UpdateDynamicButtonTexts();
        UpdateBriefing();
        RefreshToolAvailability();
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
            ultravioletTool.IsActive ||
            magnifierTool.IsActive ||
            decoderTool.IsActive)
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

        dragUsesPencil = pencilTool.IsActive;

        if (dragUsesPencil)
        {
            dragStrikeThroughState =
                !firstWord.isStruckThrough;
        }
        else
        {
            dragRedactionState =
                !firstWord.isRedacted;
        }

        ApplyDragState(wordId);
    }

    public void OnDrag(
        PointerEventData eventData
    )
    {
        if (documentFinished ||
            ultravioletTool.IsActive ||
            magnifierTool.IsActive ||
            decoderTool.IsActive ||
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

        if (dragUsesPencil)
        {
            if (word.isStruckThrough ==
                dragStrikeThroughState)
            {
                return;
            }

            word.isStruckThrough =
                dragStrikeThroughState;
            DocumentSfxController.PlayPencil(
    dragStrikeThroughState
);
        }
        else
        {
            if (word.isRedacted ==
                dragRedactionState)
            {
                return;
            }

            word.isRedacted =
                dragRedactionState;
            DocumentSfxController.PlayRedaction(
    dragRedactionState
    );
        }

        RefreshDocument();
    }

    private void StopDragging()
    {
        isDragging = false;
        dragUsesPencil = false;
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
                ultravioletTool.CreateRevealedWordMarkup(
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

        if (!word.isRedacted &&
            word.isStruckThrough)
        {
            visibleWord =
                "<u>" +
                visibleWord +
                "</u>";
        }
       
        if (!word.isRedacted &&
             word.isBold)
        {
            visibleWord =
                "<b>" +
                visibleWord +
                "</b>";
        }

        return
            $"<link=\"{word.id}\">" +
            visibleWord +
            "</link>";
    }

    private string CreateRedactedWord(
       string originalText
)
    {
        return
            "<color=#00000000>" +
            originalText +
            "</color>";
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

    private void ClearInspectionStatus()
    {
        hasInspectionStatus = false;
        lastMissedInformation = 0;
        lastExtraRedactions = 0;

        SetStatus(string.Empty);
    }

    private void UpdateInspectionStatus(
        int missedInformation,
        int extraRedactions
    )
    {
        hasInspectionStatus = true;
        lastMissedInformation = missedInformation;
        lastExtraRedactions = extraRedactions;

        RefreshInspectionStatus();
    }

    private void RefreshInspectionStatus()
    {
        if (!hasInspectionStatus)
        {
            SetStatus(string.Empty);
            return;
        }

        string missedText =
            Localize(
                errorMissedText,
                lastMissedInformation
            );

        string extraText =
            Localize(
                errorExtraText,
                lastExtraRedactions
            );

        SetStatus(
            $"{missedText}\n{extraText}"
        );
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

        GameFlowSfxController.PlaySubmit();

        DocumentEvaluationResult evaluation =
            documentEvaluator.Evaluate(words);

        int missedSecretWords =
            evaluation.missedWords;

        int extraRedactedWords =
            evaluation.extraRedactions;

        UpdateInspectionStatus(
            missedSecretWords,
            extraRedactedWords
        );

        if (evaluation.IsCorrect)
        {
            CompleteDocumentSuccessfully();
            return;
        }

        if (IsCurrentDocumentTutorial())
        {
            return;
        }

        inspectionsRemaining--;

        UpdateInspectionsDisplay();

        if (inspectionsRemaining <= 0)
        {
            FailDocument();
        }
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
        int requiredTotal = 0;
        int correctlyRedacted = 0;
        int extraRedactions = 0;

        foreach (DocumentWord word in words)
        {
            if (word.requiresRedaction)
            {
                requiredTotal++;

                if (word.isRedacted)
                {
                    correctlyRedacted++;
                }
            }
            else if (word.isRedacted)
            {
                extraRedactions++;
            }
        }

        if (requiredTotal <= 0)
        {
            return extraRedactions == 0
                ? maximumDocumentScore
                : 0;
        }

        float accuracy =
            (correctlyRedacted - extraRedactions) /
            (float)requiredTotal;

        accuracy = Mathf.Clamp01(accuracy);

        return Mathf.RoundToInt(
            accuracy * maximumDocumentScore
        );
    }

    private void FailDocument()
    {
        int documentScore =
            IsCurrentDocumentTutorial()
                ? 0
                : CalculateDocumentScore();

        totalScore += documentScore;

        ShowResultPanel(
            documentPassed: false,
            documentScore: documentScore
        );
    }

    private void ShowResultPanel(
        bool documentPassed,
        int documentScore
    )
    {
        documentFinished = true;
        StopDragging();

        ultravioletTool.DisableMode();
        magnifierTool.DisableMode();
        decoderTool.DisableMode();
        pencilTool.DisableMode();

        submitButton.SetActive(false);
        winPanel.SetActive(true);

        GameFlowSfxController.PlayResult(
    documentPassed
);

        string linearFallbackNextDocumentId =
           GetLinearNextDocumentId();

        StoryBranchingController storyBranching =
            GetComponent<StoryBranchingController>();

        StoryRouteResult routeResult =
            storyBranching != null
                ? storyBranching.ResolveAfterDocument(
                    CurrentDocument,
                    documentPassed,
                    documentScore,
                    totalScore,
                    maximumDocumentScore,
                    linearFallbackNextDocumentId
                )
                : new StoryRouteResult(
                    linearFallbackNextDocumentId
                );

        bool hasNextDocument =
            !routeResult.EndsCampaign;

        bool isLastDocument =
            routeResult.EndsCampaign;

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

        SaveCheckpointAfterCurrentDocument(
            documentPassed,
            documentScore
        );

        ClearInspectionStatus();
    }

    private void SaveCheckpointAfterCurrentDocument(
        bool documentPassed,
        int documentScore
    )
    {
        if (CurrentDocument == null)
        {
            return;
        }

        string nextDocumentId =
            GetPendingNextDocumentId();

        DocumentData nextDocument =
            FindDocumentById(
                nextDocumentId
            );


        int inspectionsUsed =
            IsCurrentDocumentTutorial()
                ? 0
                : Mathf.Clamp(
                    maximumInspections -
                    inspectionsRemaining,
                    0,
                    maximumInspections
                );

        RevealMethod toolsForNextDocument =
            unlockedTools |
            CurrentDocument.UnlocksAfterCompletion;

        List<string> unlockedCodexIds =
            codexManager.GetUnlockedEntryIds();

        List<string> readCodexIds =
            codexManager.GetReadEntryIds();

        if (nextDocument != null)
        {
            foreach (
                CodexEntry entry
                in nextDocument.CodexEntriesToUnlock
            )
            {
                if (entry == null ||
                    string.IsNullOrWhiteSpace(
                        entry.EntryId
                    ))
                {
                    continue;
                }

                if (!unlockedCodexIds.Contains(
                        entry.EntryId))
                {
                    unlockedCodexIds.Add(
                        entry.EntryId
                    );
                }
            }
        }

        CampaignProgress.RecordDocumentCompletion(
            CurrentDocument.DocumentId,
            documentScore,
            documentPassed,
            inspectionsUsed,
            nextDocumentId,
            totalScore,
            (int)toolsForNextDocument,
            unlockedCodexIds,
            readCodexIds
        );
    }

    private int GetMaximumTotalScore()
    {
        StoryBranchingController storyBranching =
            GetComponent<StoryBranchingController>();

        if (storyBranching != null &&
            storyBranching.HasResolvedRoute &&
            storyBranching.LastResolvedMaximumScore > 0)
        {
            return
                storyBranching
                    .LastResolvedMaximumScore;
        }

        return GetPlayableDocumentCount() *
               maximumDocumentScore;
    }

    public void NextDocument()
    {
        string nextDocumentId =
            GetPendingNextDocumentId();

        if (string.IsNullOrWhiteSpace(
                nextDocumentId))
        {
            return;
        }

        int nextDocumentIndex =
            FindDocumentIndexById(
                nextDocumentId
            );

        if (nextDocumentIndex < 0)
        {
            Debug.LogError(
                $"Не найден следующий документ: " +
                $"{nextDocumentId}"
            );

            return;
        }

        UnlockToolsFromCurrentDocument();

        currentDocumentIndex =
            nextDocumentIndex;

        UnlockCodexEntriesForCurrentDocument();

        LoadCurrentDocument();
    }


    public void RestartGame()
    {
        StopDragging();

        currentDocumentIndex = 0;
        totalScore = 0;
        documentFinished = false;
        unlockedTools = RevealMethod.None;

        codexManager.ResetProgress();

        GetComponent<StoryBranchingController>()
            ?.ResetForNewCampaign();

        LoadCurrentDocument();

        CampaignProgress.StartNewCampaign(
            CurrentDocument != null
                ? CurrentDocument.DocumentId
                : string.Empty,
            (int)unlockedTools,
            codexManager.GetUnlockedEntryIds(),
            codexManager.GetReadEntryIds()
        );
    }

    private int GetPlayableDocumentCount()
    
    {

        StoryBranchingController storyBranching =
    GetComponent<StoryBranchingController>();

        if (storyBranching != null &&
            storyBranching.CampaignPlayableDocumentCount > 0)
        {
            return
                storyBranching
                    .CampaignPlayableDocumentCount;
        }

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

        StoryBranchingController storyBranching =
    GetComponent<StoryBranchingController>();

        if (storyBranching != null)
        {
            return storyBranching
                .GetCurrentPlayableDocumentNumber(
                    documentFinished
                );
        }

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

    private string GetLinearNextDocumentId()
    {
        int nextIndex =
            currentDocumentIndex + 1;

        if (!IsValidDocumentIndex(
                nextIndex))
        {
            return string.Empty;
        }

        DocumentData nextDocument =
            documents[nextIndex];

        return nextDocument != null
            ? nextDocument.DocumentId
            : string.Empty;
    }

    private string GetPendingNextDocumentId()
    {
        StoryBranchingController storyBranching =
            GetComponent<StoryBranchingController>();

        if (storyBranching != null &&
            storyBranching.HasResolvedRoute)
        {
            return
                storyBranching
                    .PendingNextDocumentId;
        }

        return GetLinearNextDocumentId();
    }

    private DocumentData FindDocumentById(
        string documentId)
    {
        int index =
            FindDocumentIndexById(
                documentId
            );

        return IsValidDocumentIndex(index)
            ? documents[index]
            : null;
    }


    public void ToggleUltravioletMode()
    {
        if (!ultravioletTool.IsActive)
        {
            magnifierTool.DisableMode();
            decoderTool.DisableMode();
            pencilTool.DisableMode();
        }

        ultravioletTool.ToggleMode();
    }

    public void ToggleMagnifierMode()
    {
        if (!magnifierTool.IsActive)
        {
            ultravioletTool.DisableMode();
            decoderTool.DisableMode();
            pencilTool.DisableMode();
        }

        magnifierTool.ToggleMode();
    }

    public void ToggleDecoderMode()
    {
        if (!decoderTool.IsActive)
        {
            ultravioletTool.DisableMode();
            magnifierTool.DisableMode();
            pencilTool.DisableMode();
        }

        decoderTool.ToggleMode();
    }

    public void TogglePencilMode()
    {
        if (!pencilTool.IsActive)
        {
            ultravioletTool.DisableMode();
            magnifierTool.DisableMode();
            decoderTool.DisableMode();
        }

        pencilTool.ToggleMode();
    }

    private void DisableActiveTool()
    {
        StopDragging();

        ultravioletTool.DisableMode();
        magnifierTool.DisableMode();
        decoderTool.DisableMode();
        pencilTool.DisableMode();
    }

    private void OnSelectedLocaleChanged(
     Locale locale
 )
    {
        UpdateDynamicButtonTexts();
        UpdateProgress();
        UpdateInspectionsDisplay();
        UpdateBriefing();
        RefreshInspectionStatus();
    }

    private void UpdateDynamicButtonTexts()
    {
        UpdateNextDocumentButtonText();
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
        string result;

        if (localizedString == null ||
            localizedString.IsEmpty)
        {
            result = fallback;
        }
        else
        {
            result = localizedString.GetLocalizedString();

            if (string.IsNullOrWhiteSpace(result))
            {
                result = fallback;
            }
        }

        return PlayerTextResolver.Resolve(result);
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
    private void RefreshToolAvailability()
    {
        bool ultravioletUnlocked =
            (unlockedTools &
             RevealMethod.Ultraviolet) != 0;

        bool magnifierUnlocked =
            (unlockedTools &
             RevealMethod.Magnifier) != 0;

        bool decoderUnlocked =
            (unlockedTools &
             RevealMethod.Decoder) != 0;

        ultravioletButton.SetActive(
            ultravioletUnlocked
        );

        magnifierButton.SetActive(
            magnifierUnlocked
        );

        decoderButton.SetActive(
            decoderUnlocked
        );

        pencilButton.SetActive(true);

        toolsPanel.SetActive(true);
    }
    private void UnlockToolsFromCurrentDocument()
    {
        if (CurrentDocument == null)
        {
            return;
        }

        unlockedTools |=
            CurrentDocument.UnlocksAfterCompletion;
    }
    private void UnlockCodexEntriesForCurrentDocument()
    {
        if (CurrentDocument == null ||
            codexManager == null)
        {
            return;
        }

        codexManager.UnlockEntries(
            CurrentDocument.CodexEntriesToUnlock
        );
    }
}

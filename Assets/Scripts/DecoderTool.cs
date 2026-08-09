using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;

/// <summary>
/// Декодер показывает скрытое значение кодового слова,
/// когда его область анализа находится над таким словом.
/// </summary>
public sealed class DecoderTool : MonoBehaviour
{
    [Header("Интерфейс")]

    [SerializeField]
    private TMP_Text decoderButtonText;

    [SerializeField]
    private RectTransform decoderCursor;

    [SerializeField]
    private GameObject decoderResultContainer;

    [SerializeField]
    private TMP_Text decoderResultText;

    [Header("Локализация")]

    [SerializeField]
    private LocalizedString decoderInactiveText;

    [SerializeField]
    private LocalizedString decoderActiveText;

    [SerializeField]
    private LocalizedString statusDecoderOn;

    [SerializeField]
    private LocalizedString statusDecoderOff;

    [Header("Обнаружение")]

    [SerializeField]
    [Min(10f)]
    private float revealRadius = 75f;

    [Header("Отображение")]

    [SerializeField]
    private string resultFormat = "{0}";

    public bool IsActive { get; private set; }

    private TMP_Text documentText;

    private IReadOnlyList<DocumentWord> words;

    private Action stopDragging;

    private Func<bool> isDocumentFinished;

    private Action<string> setStatus;

    private bool isInitialized;

    public bool Initialize(
        TMP_Text documentText,
        IReadOnlyList<DocumentWord> words,
        Action stopDragging,
        Func<bool> isDocumentFinished,
        Action<string> setStatus
    )
    {
        this.documentText = documentText;
        this.words = words;
        this.stopDragging = stopDragging;
        this.isDocumentFinished =
            isDocumentFinished;
        this.setStatus = setStatus;

        if (!ValidateReferences())
        {
            return false;
        }

        IsActive = false;

        decoderCursor.gameObject.SetActive(false);
        decoderResultContainer.SetActive(false);

        RefreshLocalizedText();

        isInitialized = true;

        return true;
    }

    private bool ValidateReferences()
    {
        bool referencesAreValid = true;

        if (documentText == null)
        {
            Debug.LogError(
                "DecoderTool: не назначен Document Text.",
                this
            );

            referencesAreValid = false;
        }

        if (decoderButtonText == null)
        {
            Debug.LogError(
                "DecoderTool: не назначен Decoder Button Text.",
                this
            );

            referencesAreValid = false;
        }

        if (decoderCursor == null)
        {
            Debug.LogError(
                "DecoderTool: не назначен Decoder Cursor.",
                this
            );

            referencesAreValid = false;
        }

        if (decoderResultContainer == null)
        {
            Debug.LogError(
                "DecoderTool: не назначен Decoder Result Container.",
                this
            );

            referencesAreValid = false;
        }

        if (decoderResultText == null)
        {
            Debug.LogError(
                "DecoderTool: не назначен Decoder Result Text.",
                this
            );

            referencesAreValid = false;
        }

        if (words == null)
        {
            Debug.LogError(
                "DecoderTool: не передан список слов.",
                this
            );

            referencesAreValid = false;
        }

        if (stopDragging == null ||
            isDocumentFinished == null ||
            setStatus == null)
        {
            Debug.LogError(
                "DecoderTool: не переданы необходимые callbacks.",
                this
            );

            referencesAreValid = false;
        }

        return referencesAreValid;
    }

    private void Update()
    {
        if (!isInitialized ||
            !IsActive ||
            Mouse.current == null)
        {
            return;
        }

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        decoderCursor.position =
            mousePosition;

        UpdateDecodedResult(
            mousePosition
        );
    }

    public void ToggleMode()
    {
        if (!isInitialized ||
            isDocumentFinished())
        {
            return;
        }

        IsActive = !IsActive;

        stopDragging();

        decoderCursor.gameObject.SetActive(
            IsActive
        );

        if (IsActive)
        {
            decoderCursor.position =
                Mouse.current != null
                    ? Mouse.current.position.ReadValue()
                    : Vector2.zero;

            decoderResultContainer.SetActive(false);
        }
        else
        {
            HideResult();
        }

        RefreshLocalizedText();

        setStatus(
            Localize(
                IsActive
                    ? statusDecoderOn
                    : statusDecoderOff
            )
        );
    }

    public void DisableMode()
    {
        if (!isInitialized)
        {
            return;
        }

        IsActive = false;

        stopDragging();

        HideResult();

        decoderCursor.gameObject.SetActive(
            false
        );

        RefreshLocalizedText();
    }

    public void RefreshLocalizedText()
    {
        if (decoderButtonText == null)
        {
            return;
        }

        LocalizedString selectedText =
            IsActive
                ? decoderActiveText
                : decoderInactiveText;

        if (selectedText == null ||
            selectedText.IsEmpty)
        {
            return;
        }

        decoderButtonText.text =
            selectedText.GetLocalizedString();
    }

    private void UpdateDecodedResult(
        Vector2 decoderScreenPosition
    )
    {
        documentText.ForceMeshUpdate();

        TMP_TextInfo textInfo =
            documentText.textInfo;

        DocumentWord closestWord = null;
        float closestDistance =
            float.PositiveInfinity;

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

            DocumentWord word =
                words[wordId];

            if (word.isRedacted ||
                !word.CanBeRevealedBy(
                    RevealMethod.Decoder))
            {
                continue;
            }

            if (!word.TryGetAnalysisPayload(
                    RevealMethod.Decoder,
                    out string payload) ||
                string.IsNullOrWhiteSpace(payload))
            {
                continue;
            }

            float distance =
                GetDistanceToLink(
                    linkInfo,
                    decoderScreenPosition
                );

            if (distance > revealRadius ||
                distance >= closestDistance)
            {
                continue;
            }

            closestDistance = distance;
            closestWord = word;
        }

        if (closestWord == null)
        {
            HideResult();
            return;
        }

        closestWord.TryGetAnalysisPayload(
            RevealMethod.Decoder,
            out string decodedText
        );

        ShowResult(decodedText);
    }

    private void ShowResult(
        string decodedText
    )
    {
        decoderResultText.text =
            string.Format(
                resultFormat,
                decodedText
            );

        if (!decoderResultContainer.activeSelf)
        {
            decoderResultContainer.SetActive(
                true
            );
        }
    }

    private void HideResult()
    {
        if (decoderResultContainer != null)
        {
            decoderResultContainer.SetActive(
                false
            );
        }
    }

    private float GetDistanceToLink(
        TMP_LinkInfo linkInfo,
        Vector2 screenPosition
    )
    {
        TMP_TextInfo textInfo =
            documentText.textInfo;

        if (linkInfo.linkTextLength <= 0)
        {
            return float.PositiveInfinity;
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
            return float.PositiveInfinity;
        }

        Vector2 closestPoint =
            new Vector2(
                Mathf.Clamp(
                    screenPosition.x,
                    minimum.x,
                    maximum.x
                ),
                Mathf.Clamp(
                    screenPosition.y,
                    minimum.y,
                    maximum.y
                )
            );

        return Vector2.Distance(
            screenPosition,
            closestPoint
        );
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

    private string Localize(
        LocalizedString localizedString
    )
    {
        if (localizedString == null ||
            localizedString.IsEmpty)
        {
            return string.Empty;
        }

        return localizedString.GetLocalizedString();
    }
}

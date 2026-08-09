using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.UI;

/// <summary>
/// Управляет ультрафиолетовой лампой:
/// курсором лампы, обнаружением слов и UV-визуализацией.
/// </summary>
public sealed class UltravioletTool : MonoBehaviour
{
    [Header("Интерфейс")]

    [SerializeField]
    private TMP_Text ultravioletButtonText;

    [SerializeField]
    private Button toolButton;

    [SerializeField]
    private RectTransform ultravioletCursor;

    [Header("Состояние кнопки")]

    [SerializeField]
    private Color inactiveButtonColor =
        new Color32(52, 46, 63, 255);

    [SerializeField]
    private Color activeButtonColor =
        new Color32(107, 53, 168, 255);

    [Header("Локализация")]

    [SerializeField]
    private LocalizedString ultravioletInactiveText;

    [SerializeField]
    private LocalizedString ultravioletActiveText;

    [SerializeField]
    private LocalizedString statusUvOn;

    [SerializeField]
    private LocalizedString statusUvOff;

    [Header("Внешний вид")]

    [SerializeField]
    private string ultravioletSecretTextColor =
        "#6B35A8";

    [SerializeField]
    private string ultravioletSecretHighlightColor =
        "#8A4DFF35";

    [SerializeField]
    [Min(10f)]
    private float ultravioletRevealRadius = 110f;

    public bool IsActive { get; private set; }

    private TMP_Text documentText;
    private IReadOnlyList<DocumentWord> words;

    private Action refreshDocument;
    private Action stopDragging;
    private Func<bool> isDocumentFinished;
    private Action<string> setStatus;

    private bool isInitialized;

    public bool Initialize(
        TMP_Text documentText,
        IReadOnlyList<DocumentWord> words,
        Action refreshDocument,
        Action stopDragging,
        Func<bool> isDocumentFinished,
        Action<string> setStatus
    )
    {
        this.documentText = documentText;
        this.words = words;
        this.refreshDocument = refreshDocument;
        this.stopDragging = stopDragging;
        this.isDocumentFinished = isDocumentFinished;
        this.setStatus = setStatus;

        if (!ValidateReferences())
        {
            return false;
        }

        IsActive = false;
        ultravioletCursor.gameObject.SetActive(false);
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
                "UltravioletTool: не назначен Document Text.",
                this
            );

            referencesAreValid = false;
        }

        if (ultravioletButtonText == null)
        {
            Debug.LogError(
                "UltravioletTool: не назначен Button Text.",
                this
            );

            referencesAreValid = false;
        }

        if (toolButton == null)
        {
            Debug.LogError(
                "UltravioletTool: не назначен Tool Button.",
                this
            );

            referencesAreValid = false;
        }

        if (ultravioletCursor == null)
        {
            Debug.LogError(
                "UltravioletTool: не назначен Ultraviolet Cursor.",
                this
            );

            referencesAreValid = false;
        }

        if (words == null)
        {
            Debug.LogError(
                "UltravioletTool: не передан список слов.",
                this
            );

            referencesAreValid = false;
        }

        if (refreshDocument == null ||
            stopDragging == null ||
            isDocumentFinished == null ||
            setStatus == null)
        {
            Debug.LogError(
                "UltravioletTool: не переданы необходимые callbacks.",
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

        ultravioletCursor.position =
            mousePosition;

        UpdateReveal(mousePosition);
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

        ultravioletCursor.gameObject.SetActive(
            IsActive
        );

        if (IsActive)
        {
            ultravioletCursor.position =
                Mouse.current != null
                    ? Mouse.current.position.ReadValue()
                    : Vector2.zero;
        }
        else
        {
            ClearReveal();
        }

        RefreshLocalizedText();

        setStatus(
            Localize(
                IsActive
                    ? statusUvOn
                    : statusUvOff
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
        ClearReveal();

        ultravioletCursor.gameObject.SetActive(false);

        RefreshLocalizedText();
    }

    private void RefreshButtonVisual()
    {
        if (toolButton == null)
        {
            return;
        }

        ColorBlock colors =
            toolButton.colors;

        Color stateColor =
            IsActive
                ? activeButtonColor
                : inactiveButtonColor;

        colors.normalColor = stateColor;
        colors.selectedColor = stateColor;

        toolButton.colors = colors;

        if (toolButton.targetGraphic != null)
        {
            toolButton.targetGraphic.color =
                stateColor;
        }
    }

    public void RefreshLocalizedText()
    {
        RefreshButtonVisual();

        if (ultravioletButtonText == null)
        {
            return;
        }

        LocalizedString selectedText =
            IsActive
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

    public string CreateRevealedWordMarkup(
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

    private void UpdateReveal(
        Vector2 lampScreenPosition
    )
    {
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
                IsLinkInsideLight(
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
            refreshDocument();
        }
    }

    private bool IsLinkInsideLight(
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

    private void ClearReveal()
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
            refreshDocument();
        }
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

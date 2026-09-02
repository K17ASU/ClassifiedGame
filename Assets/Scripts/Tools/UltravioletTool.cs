using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;

/// <summary>
/// Управляет ультрафиолетовой лампой:
/// курсором лампы и подсветкой UV-символов внутри круга света.
/// </summary>
public sealed class UltravioletTool : MonoBehaviour
{
    [Header("Интерфейс")]
    [SerializeField] private RectTransform ultravioletCursor;

    [Header("Локализация")]
    [SerializeField] private LocalizedString statusUvOn;
    [SerializeField] private LocalizedString statusUvOff;

    [Header("Внешний вид")]
    [SerializeField] private Color32 ultravioletSecretTextColor =
        new Color32(107, 53, 168, 255);

    [SerializeField, Min(10f)]
    private float ultravioletRevealRadius = 90f;

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
        Action<string> setStatus)
    {
        this.documentText = documentText;
        this.words = words;
        this.refreshDocument = refreshDocument;
        this.stopDragging = stopDragging;
        this.isDocumentFinished = isDocumentFinished;
        this.setStatus = setStatus;

        if (!ValidateReferences())
            return false;

        IsActive = false;
        ultravioletCursor.gameObject.SetActive(false);
        isInitialized = true;
        return true;
    }

    private bool ValidateReferences()
    {
        bool valid = true;

        if (documentText == null)
        {
            Debug.LogError(
                "UltravioletTool: не назначен Document Text.",
                this
            );
            valid = false;
        }

        if (ultravioletCursor == null)
        {
            Debug.LogError(
                "UltravioletTool: не назначен Ultraviolet Cursor.",
                this
            );
            valid = false;
        }

        if (words == null)
        {
            Debug.LogError(
                "UltravioletTool: не передан список слов.",
                this
            );
            valid = false;
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
            valid = false;
        }

        return valid;
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

        ApplyUltravioletEffect(
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
            RestoreTextMesh();
            ClearRevealState();
        }

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
            return;

        IsActive = false;
        stopDragging();
        RestoreTextMesh();
        ClearRevealState();

        ultravioletCursor.gameObject.SetActive(
            false
        );
    }

    // Оставлено временно для совместимости с DocumentRedactor.
    // Старым UI кнопок этот метод больше не управляет.
    public void RefreshLocalizedText()
    {
    }

    // Оставлен для совместимости с текущим DocumentRedactor.
    // Новая UV-визуализация работает напрямую через TMP mesh.
    public string CreateRevealedWordMarkup(
        string originalText)
    {
        return originalText;
    }

    private void ApplyUltravioletEffect(
        Vector2 lampScreenPosition)
    {
        documentText.ForceMeshUpdate();

        TMP_TextInfo textInfo =
            documentText.textInfo;

        float scaledRevealRadius =
    ToolInfluenceScale.ToScreenPixels(
        documentText,
        ultravioletRevealRadius
    );

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
                    RevealMethod.Ultraviolet))
            {
                continue;
            }

            int first =
                linkInfo.linkTextfirstCharacterIndex;

            int last =
                first + linkInfo.linkTextLength;

            for (int characterIndex = first;
                 characterIndex < last;
                 characterIndex++)
            {
                if (characterIndex < 0 ||
                    characterIndex >= textInfo.characterCount)
                {
                    continue;
                }

                TMP_CharacterInfo characterInfo =
                    textInfo.characterInfo[
                        characterIndex
                    ];

                if (!characterInfo.isVisible)
                    continue;

                if (!IsCharacterInsideCircle(
                        characterInfo,
                        lampScreenPosition,
                        scaledRevealRadius))
                {
                    continue;
                }

                ColorCharacter(
                    characterInfo,
                    textInfo,
                    ultravioletSecretTextColor
                );
            }
        }

        documentText.UpdateVertexData(
            TMP_VertexDataUpdateFlags.Colors32
        );
    }

    private void ColorCharacter(
        TMP_CharacterInfo characterInfo,
        TMP_TextInfo textInfo,
        Color32 color)
    {
        int materialIndex =
            characterInfo.materialReferenceIndex;

        int vertexIndex =
            characterInfo.vertexIndex;

        if (materialIndex < 0 ||
            materialIndex >= textInfo.meshInfo.Length)
        {
            return;
        }

        Color32[] colors =
            textInfo.meshInfo[
                materialIndex
            ].colors32;

        if (colors == null ||
            vertexIndex < 0 ||
            vertexIndex + 3 >= colors.Length)
        {
            return;
        }

        colors[vertexIndex + 0] = color;
        colors[vertexIndex + 1] = color;
        colors[vertexIndex + 2] = color;
        colors[vertexIndex + 3] = color;
    }

    private bool IsCharacterInsideCircle(
        TMP_CharacterInfo characterInfo,
        Vector2 circleCenter,
        float radius)
    {
        Vector3 bottomLeftWorld =
            documentText.transform.TransformPoint(
                characterInfo.bottomLeft
            );

        Vector3 topRightWorld =
            documentText.transform.TransformPoint(
                characterInfo.topRight
            );

        Vector2 bottomLeftScreen =
            RectTransformUtility.WorldToScreenPoint(
                GetDocumentCanvasCamera(),
                bottomLeftWorld
            );

        Vector2 topRightScreen =
            RectTransformUtility.WorldToScreenPoint(
                GetDocumentCanvasCamera(),
                topRightWorld
            );

        float minX =
            Mathf.Min(
                bottomLeftScreen.x,
                topRightScreen.x
            );

        float maxX =
            Mathf.Max(
                bottomLeftScreen.x,
                topRightScreen.x
            );

        float minY =
            Mathf.Min(
                bottomLeftScreen.y,
                topRightScreen.y
            );

        float maxY =
            Mathf.Max(
                bottomLeftScreen.y,
                topRightScreen.y
            );

        Vector2 closestPoint =
            new Vector2(
                Mathf.Clamp(
                    circleCenter.x,
                    minX,
                    maxX
                ),
                Mathf.Clamp(
                    circleCenter.y,
                    minY,
                    maxY
                )
            );

        return Vector2.SqrMagnitude(
            circleCenter - closestPoint
        ) <= radius * radius;
    }

    private void RestoreTextMesh()
    {
        if (documentText == null)
            return;

        documentText.ForceMeshUpdate();

        documentText.UpdateVertexData(
            TMP_VertexDataUpdateFlags.Colors32
        );
    }

    private void ClearRevealState()
    {
        foreach (DocumentWord word in words)
        {
            word.isUltravioletRevealed =
                false;
        }
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
        LocalizedString localizedString)
    {
        if (localizedString == null ||
            localizedString.IsEmpty)
        {
            return string.Empty;
        }

        return localizedString
            .GetLocalizedString();
    }
}

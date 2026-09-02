using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

/// <summary>
/// Управляет лупой.
/// Кодовые символы дрожат только внутри круглой области лупы.
/// </summary>
public sealed class MagnifierTool : MonoBehaviour
{
    [Header("Интерфейс")]
    [SerializeField] private RectTransform magnifierCursor;

    [Header("Локализация")]
    [SerializeField] private LocalizedString statusMagnifierOn;
    [SerializeField] private LocalizedString statusMagnifierOff;

    [Header("Обнаружение")]
    [SerializeField, Min(10f)]
    private float revealRadius = 90f;

    [Header("Дрожание текста")]
    [SerializeField, Min(0f)]
    private float shakeAmplitude = 1.5f;

    [SerializeField, Min(0f)]
    private float shakeSpeed = 32f;

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
        Action<string> setStatus)
    {
        this.documentText = documentText;
        this.words = words;
        this.stopDragging = stopDragging;
        this.isDocumentFinished = isDocumentFinished;
        this.setStatus = setStatus;

        if (!ValidateReferences())
            return false;

        IsActive = false;
        magnifierCursor.gameObject.SetActive(false);
        isInitialized = true;
        return true;
    }

    private bool ValidateReferences()
    {
        bool valid = true;

        if (documentText == null)
        {
            Debug.LogError(
                "MagnifierTool: не назначен Document Text.",
                this
            );
            valid = false;
        }

        if (magnifierCursor == null)
        {
            Debug.LogError(
                "MagnifierTool: не назначен Magnifier Cursor.",
                this
            );
            valid = false;
        }

        if (words == null)
        {
            Debug.LogError(
                "MagnifierTool: не передан список слов.",
                this
            );
            valid = false;
        }

        if (stopDragging == null ||
            isDocumentFinished == null ||
            setStatus == null)
        {
            Debug.LogError(
                "MagnifierTool: не переданы необходимые callbacks.",
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
            !ScreenPointer.TryGetPosition(
                out Vector2 pointerPosition))
        {
            return;
        }

        pointerPosition =
    MobileToolPointerOffset.Apply(
        documentText,
        pointerPosition
    );

        magnifierCursor.position =
            pointerPosition;

        ApplyMagnifierEffect(
            pointerPosition
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

        magnifierCursor.gameObject.SetActive(
            IsActive
        );

        if (IsActive)
        {
            if (ScreenPointer.TryGetPosition(
                    out Vector2 pointerPosition))
            {
                magnifierCursor.position =
                    pointerPosition;
            }
        }
        else
        {
            RestoreTextMesh();
        }

        setStatus(
            Localize(
                IsActive
                    ? statusMagnifierOn
                    : statusMagnifierOff
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

        magnifierCursor.gameObject.SetActive(
            false
        );
    }

    // Оставлено временно для совместимости с DocumentRedactor.
    // Старым UI кнопок этот метод больше не управляет.
    public void RefreshLocalizedText()
    {
    }

    private void ApplyMagnifierEffect(
        Vector2 magnifierScreenPosition)
    {
        documentText.ForceMeshUpdate();

        TMP_TextInfo textInfo =
            documentText.textInfo;

        float scaledRevealRadius =
    ToolInfluenceScale.ToScreenPixels(
        documentText,
        revealRadius
    );

        float time =
            Time.unscaledTime *
            shakeSpeed;

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
                    RevealMethod.Magnifier))
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
                    continue;

                if (!IsCharacterInsideCircle(
                        characterInfo,
                        magnifierScreenPosition,
                        scaledRevealRadius))
                {
                    continue;
                }

                ShakeCharacter(
                    characterInfo,
                    textInfo,
                    characterIndex,
                    time
                );
            }
        }

        documentText.UpdateVertexData(
            TMP_VertexDataUpdateFlags.Vertices
        );
    }

    private void ShakeCharacter(
        TMP_CharacterInfo characterInfo,
        TMP_TextInfo textInfo,
        int characterIndex,
        float time)
    {
        int materialIndex =
            characterInfo.materialReferenceIndex;

        int vertexIndex =
            characterInfo.vertexIndex;

        if (materialIndex < 0 ||
            materialIndex >=
            textInfo.meshInfo.Length)
        {
            return;
        }

        Vector3[] vertices =
            textInfo.meshInfo[
                materialIndex
            ].vertices;

        if (vertices == null ||
            vertexIndex < 0 ||
            vertexIndex + 3 >=
            vertices.Length)
        {
            return;
        }

        float phase =
            characterIndex * 1.618f;

        float offsetX =
            Mathf.Sin(
                time +
                phase * 2.17f
            ) * shakeAmplitude;

        float offsetY =
            Mathf.Cos(
                time * 1.31f +
                phase * 1.73f
            ) * shakeAmplitude;

        Vector3 offset =
            new Vector3(
                offsetX,
                offsetY,
                0f
            );

        vertices[vertexIndex + 0] += offset;
        vertices[vertexIndex + 1] += offset;
        vertices[vertexIndex + 2] += offset;
        vertices[vertexIndex + 3] += offset;
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
            circleCenter -
            closestPoint
        ) <= radius * radius;
    }

    private void RestoreTextMesh()
    {
        if (documentText == null)
            return;

        documentText.ForceMeshUpdate();

        documentText.UpdateVertexData(
            TMP_VertexDataUpdateFlags.Vertices
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

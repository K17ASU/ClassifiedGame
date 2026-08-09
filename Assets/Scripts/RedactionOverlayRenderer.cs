using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Рисует непрозрачные UI-плашки поверх засекреченных слов.
/// Засекреченное слово должно оставаться в TMP как полностью
/// прозрачный текст (<color=#00000000>...</color>).
///
/// В отличие от TMP <mark>, плашки:
/// - не меняют размер и переносы текста;
/// - имеют полностью непрозрачный цвет;
/// - имеют одинаковую высоту на всех словах.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public sealed class RedactionOverlayRenderer : MonoBehaviour
{
    [Header("Текст документа")]

    [SerializeField]
    private TMP_Text documentText;

    [Header("Внешний вид")]

    [SerializeField]
    private Color redactionBarColor = Color.black;

    [SerializeField]
    [Min(0f)]
    private float horizontalPadding = 2f;

    [SerializeField]
    [Range(0.1f, 2f)]
    private float barHeightMultiplier = 0.8f;

    [SerializeField]
    private float verticalOffset = 0f;

    private readonly List<Image> redactionBars =
        new List<Image>();

    private string cachedText;
    private Vector2 cachedRectSize;
    private float cachedFontSize;
    private bool layoutDirty = true;

    private void Awake()
    {
        if (documentText == null)
        {
            documentText = GetComponent<TMP_Text>();
        }
    }

    private void OnEnable()
    {
        layoutDirty = true;
        cachedText = null;
    }

    private void OnDisable()
    {
        HideAllBars();
    }

    private void OnRectTransformDimensionsChange()
    {
        layoutDirty = true;
    }

    private void LateUpdate()
    {
        if (documentText == null)
        {
            return;
        }

        Vector2 currentRectSize =
            documentText.rectTransform.rect.size;

        bool textChanged =
            cachedText != documentText.text;

        bool sizeChanged =
            cachedRectSize != currentRectSize;

        bool fontSizeChanged =
            !Mathf.Approximately(
                cachedFontSize,
                documentText.fontSize
            );

        if (!layoutDirty &&
            !textChanged &&
            !sizeChanged &&
            !fontSizeChanged)
        {
            return;
        }

        documentText.ForceMeshUpdate();

        RefreshRedactionBars();

        cachedText = documentText.text;
        cachedRectSize = currentRectSize;
        cachedFontSize = documentText.fontSize;
        layoutDirty = false;
    }

    private void RefreshRedactionBars()
    {
        HideAllBars();

        TMP_TextInfo textInfo =
            documentText.textInfo;

        int nextBarIndex = 0;

        for (int linkIndex = 0;
             linkIndex < textInfo.linkCount;
             linkIndex++)
        {
            TMP_LinkInfo linkInfo =
                textInfo.linkInfo[linkIndex];

            if (!IsRedactedLink(
                    linkInfo,
                    textInfo))
            {
                continue;
            }

            nextBarIndex =
                CreateBarsForLink(
                    linkInfo,
                    textInfo,
                    nextBarIndex
                );
        }
    }

    private bool IsRedactedLink(
        TMP_LinkInfo linkInfo,
        TMP_TextInfo textInfo
    )
    {
        if (linkInfo.linkTextLength <= 0)
        {
            return false;
        }

        int firstCharacterIndex =
            linkInfo.linkTextfirstCharacterIndex;

        int lastCharacterIndex =
            firstCharacterIndex +
            linkInfo.linkTextLength;

        bool foundVisibleCharacter = false;

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

            foundVisibleCharacter = true;

            int materialIndex =
                characterInfo.materialReferenceIndex;

            int vertexIndex =
                characterInfo.vertexIndex;

            if (materialIndex < 0 ||
                materialIndex >=
                textInfo.meshInfo.Length)
            {
                return false;
            }

            Color32[] colors =
                textInfo.meshInfo[
                    materialIndex
                ].colors32;

            if (colors == null ||
                vertexIndex < 0 ||
                vertexIndex >= colors.Length)
            {
                return false;
            }

            // Засекреченный текст в DocumentRedactor
            // полностью прозрачен.
            if (colors[vertexIndex].a > 0)
            {
                return false;
            }
        }

        return foundVisibleCharacter;
    }

    private int CreateBarsForLink(
        TMP_LinkInfo linkInfo,
        TMP_TextInfo textInfo,
        int barIndex
    )
    {
        int firstCharacterIndex =
            linkInfo.linkTextfirstCharacterIndex;

        int lastCharacterIndex =
            firstCharacterIndex +
            linkInfo.linkTextLength;

        int currentLine = -1;

        float minimumX = 0f;
        float maximumX = 0f;

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

            // Используем origin / xAdvance вместо размеров самого глифа.
            // Так длина плашки соответствует реальной ширине слова,
            // но высота не зависит от формы конкретных букв.
            float characterLeft =
                characterInfo.origin;

            float characterRight =
                characterInfo.xAdvance;

            if (currentLine == -1)
            {
                currentLine =
                    characterInfo.lineNumber;

                minimumX =
                    characterLeft;

                maximumX =
                    characterRight;

                continue;
            }

            if (characterInfo.lineNumber !=
                currentLine)
            {
                ShowBar(
                    barIndex,
                    currentLine,
                    minimumX,
                    maximumX,
                    textInfo
                );

                barIndex++;

                currentLine =
                    characterInfo.lineNumber;

                minimumX =
                    characterLeft;

                maximumX =
                    characterRight;

                continue;
            }

            minimumX =
                Mathf.Min(
                    minimumX,
                    characterLeft
                );

            maximumX =
                Mathf.Max(
                    maximumX,
                    characterRight
                );
        }

        if (currentLine != -1)
        {
            ShowBar(
                barIndex,
                currentLine,
                minimumX,
                maximumX,
                textInfo
            );

            barIndex++;
        }

        return barIndex;
    }

    private void ShowBar(
        int barIndex,
        int lineNumber,
        float minimumX,
        float maximumX,
        TMP_TextInfo textInfo
    )
    {
        if (lineNumber < 0 ||
            lineNumber >= textInfo.lineCount)
        {
            return;
        }

        Image bar =
            GetBar(barIndex);

        RectTransform rectTransform =
            bar.rectTransform;

        RectTransform textRect =
            documentText.rectTransform;

        TMP_LineInfo lineInfo =
            textInfo.lineInfo[lineNumber];

        float barHeight =
            documentText.fontSize *
            barHeightMultiplier;

        // Центр берём от середины строки, но высоту всегда
        // задаём одинаковую через fontSize * multiplier.
        float lineCenterY =
            (lineInfo.ascender +
             lineInfo.descender) * 0.5f +
            verticalOffset;

        float left =
            minimumX -
            horizontalPadding;

        float right =
            maximumX +
            horizontalPadding;

        rectTransform.anchorMin =
            textRect.pivot;

        rectTransform.anchorMax =
            textRect.pivot;

        rectTransform.pivot =
            new Vector2(0.5f, 0.5f);

        rectTransform.anchoredPosition =
            new Vector2(
                (left + right) * 0.5f,
                lineCenterY
            );

        rectTransform.sizeDelta =
            new Vector2(
                right - left,
                barHeight
            );

        bar.color =
            redactionBarColor;

        bar.raycastTarget = false;

        bar.gameObject.SetActive(true);
        bar.transform.SetAsLastSibling();
    }

    private Image GetBar(int index)
    {
        while (redactionBars.Count <= index)
        {
            GameObject barObject =
                new GameObject(
                    $"RedactionBar_{redactionBars.Count}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image)
                );

            barObject.transform.SetParent(
                documentText.transform,
                false
            );

            Image image =
                barObject.GetComponent<Image>();

            image.raycastTarget = false;

            redactionBars.Add(image);
        }

        return redactionBars[index];
    }

    private void HideAllBars()
    {
        foreach (Image bar in redactionBars)
        {
            if (bar != null)
            {
                bar.gameObject.SetActive(false);
            }
        }
    }
}

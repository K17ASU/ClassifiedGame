using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

/// <summary>
/// Управляет режимом карандаша.
/// В этом режиме клик и протягивание по словам
/// устанавливают или снимают зачёркивание.
/// </summary>
public sealed class PencilTool : MonoBehaviour
{
    [Header("Интерфейс")]

    [SerializeField]
    private TMP_Text pencilButtonText;

    [SerializeField]
    private Button toolButton;

    [Header("Состояние кнопки")]

    [SerializeField]
    private Color inactiveButtonColor =
        new Color32(52, 46, 63, 255);

    [SerializeField]
    private Color activeButtonColor =
        new Color32(107, 53, 168, 255);

    [Header("Локализация")]

    [SerializeField]
    private LocalizedString pencilInactiveText;

    [SerializeField]
    private LocalizedString pencilActiveText;

    public bool IsActive { get; private set; }

    private Action stopDragging;
    private Func<bool> isDocumentFinished;

    private bool isInitialized;

    public bool Initialize(
        Action stopDragging,
        Func<bool> isDocumentFinished
    )
    {
        this.stopDragging = stopDragging;
        this.isDocumentFinished =
            isDocumentFinished;

        if (!ValidateReferences())
        {
            return false;
        }

        IsActive = false;
        RefreshLocalizedText();

        isInitialized = true;

        return true;
    }

    private bool ValidateReferences()
    {
        bool referencesAreValid = true;

        if (stopDragging == null ||
            isDocumentFinished == null)
        {
            Debug.LogError(
                "PencilTool: не переданы необходимые callbacks.",
                this
            );

            referencesAreValid = false;
        }

        return referencesAreValid;
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

        RefreshLocalizedText();
    }

    public void DisableMode()
    {
        if (!isInitialized)
        {
            return;
        }

        IsActive = false;

        stopDragging();

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

        if (pencilButtonText == null)
        {
            return;
        }

        LocalizedString selectedText =
            IsActive
                ? pencilActiveText
                : pencilInactiveText;

        if (selectedText == null ||
            selectedText.IsEmpty)
        {
            return;
        }

        pencilButtonText.text =
            selectedText.GetLocalizedString();
    }
}

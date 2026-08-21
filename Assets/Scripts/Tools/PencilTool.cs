using System;
using UnityEngine;

/// <summary>
/// Управляет режимом карандаша.
/// В этом режиме клик и протягивание по словам
/// устанавливают или снимают зачёркивание.
/// </summary>
public sealed class PencilTool : MonoBehaviour
{
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
    }

    public void DisableMode()
    {
        if (!isInitialized)
        {
            return;
        }

        IsActive = false;
        stopDragging();
    }

    // Оставлено временно для совместимости с DocumentRedactor.
    // Старым UI кнопок этот метод больше не управляет.
    public void RefreshLocalizedText()
    {
    }
}

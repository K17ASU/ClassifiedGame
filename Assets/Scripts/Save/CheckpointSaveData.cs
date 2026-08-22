using System;
using System.Collections.Generic;

/// <summary>
/// Полное состояние кампании перед началом конкретного документа.
/// Именно к этому состоянию игрок сможет вернуться для переигрывания.
/// </summary>
[Serializable]
public sealed class CheckpointSaveData
{
    public int checkpointIndex;

    public string currentDocumentId = string.Empty;

    public int totalScore;

    /// <summary>
    /// Битовая маска открытых инструментов.
    /// Храним как int, чтобы save-data не зависела напрямую
    /// от сериализации enum RevealMethod.
    /// </summary>
    public int unlockedToolsMask;

    public List<DocumentResultSaveData> completedDocuments =
        new List<DocumentResultSaveData>();

    public List<string> unlockedCodexEntryIds =
        new List<string>();

    public List<string> readCodexEntryIds =
        new List<string>();
}

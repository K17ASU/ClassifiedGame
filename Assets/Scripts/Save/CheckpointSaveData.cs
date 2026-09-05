using System;
using System.Collections.Generic;

/// <summary>
/// Снимок состояния кампании перед началом документа.
/// parentCheckpointId позволяет хранить альтернативные ветки.
/// </summary>
[Serializable]
public sealed class CheckpointSaveData
{
    public int checkpointId;

    public int parentCheckpointId = -1;

    // Legacy-поле saveVersion 1.
    public int checkpointIndex;

    public string currentDocumentId = string.Empty;

    public int totalScore;

    public int unlockedToolsMask;

    public List<DocumentResultSaveData> completedDocuments =
        new List<DocumentResultSaveData>();

    public List<string> unlockedCodexEntryIds =
        new List<string>();

    public List<string> readCodexEntryIds =
        new List<string>();

    // V3: постоянное сюжетное состояние этой ветки.
    public List<StoryStateSaveData> storyState =
        new List<StoryStateSaveData>();
}

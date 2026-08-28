using System;
using System.Collections.Generic;

/// <summary>
/// Корневой объект файла сохранения одной кампании.
/// Содержит историю доступных checkpoint'ов.
/// </summary>
[Serializable]
public sealed class CampaignSaveData
{
    public int saveVersion = 1;

    /// <summary>
    /// Нужен для совместимости со старыми save-файлами,
    /// в которых activeCheckpointIndex ещё не существовал.
    /// </summary>
    public bool hasActiveCheckpointSelection = false;

    /// <summary>
    /// Checkpoint, который сейчас считается активным.
    /// Обычно это последний checkpoint, но при переигрывании
    /// игрок может выбрать более ранний.
    /// </summary>
    public int activeCheckpointIndex = 0;

    public List<CheckpointSaveData> checkpoints =
        new List<CheckpointSaveData>();
}

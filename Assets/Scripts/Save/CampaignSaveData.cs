using System;
using System.Collections.Generic;

/// <summary>
/// Корневой объект сохранения одной кампании.
/// Начиная с saveVersion 2 checkpoint'ы образуют дерево:
/// каждый checkpoint знает своего родителя.
/// </summary>
[Serializable]
public sealed class CampaignSaveData
{
    public int saveVersion = 2;

    public int activeCheckpointId = 0;

    public int nextCheckpointId = 1;

    public List<CheckpointSaveData> checkpoints =
        new List<CheckpointSaveData>();

    // Поля ниже оставлены только для миграции saveVersion 1.
    public bool hasActiveCheckpointSelection = false;
    public int activeCheckpointIndex = 0;
}

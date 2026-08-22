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

    public List<CheckpointSaveData> checkpoints =
        new List<CheckpointSaveData>();
}

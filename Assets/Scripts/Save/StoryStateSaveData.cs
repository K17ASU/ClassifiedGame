using System;

[Serializable]
public sealed class StoryStateSaveData
{
    public string fragmentId = string.Empty;

    // Храним enum как int, чтобы save-слой не зависел
    // от реализации StoryGraph.
    public int state;
}

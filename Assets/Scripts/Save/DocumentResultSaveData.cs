using System;

/// <summary>
/// Итог одного окончательно завершённого документа.
/// </summary>
[Serializable]
public sealed class DocumentResultSaveData
{
    public string documentId = string.Empty;

    public int score;

    public bool passed;

    public int inspectionsUsed;
}

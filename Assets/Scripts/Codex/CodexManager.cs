using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class CodexManager : MonoBehaviour
{
    [Header("Все записи кодекса")]

    [SerializeField]
    private List<CodexEntry> allEntries =
        new List<CodexEntry>();

    public event Action EntriesChanged;

    private readonly HashSet<string> unlockedIds =
        new HashSet<string>();

    private readonly HashSet<string> readIds =
        new HashSet<string>();

    private readonly List<CodexEntry> unlockedEntries =
        new List<CodexEntry>();

    public IReadOnlyList<CodexEntry> UnlockedEntries =>
        unlockedEntries;

    public bool HasUnreadEntries
    {
        get
        {
            foreach (CodexEntry entry in unlockedEntries)
            {
                if (entry != null &&
                    !readIds.Contains(entry.EntryId))
                {
                    return true;
                }
            }

            return false;
        }
    }

    private void Awake()
    {
        ResetProgress();
    }

    public void UnlockEntries(
        IReadOnlyList<CodexEntry> entries
    )
    {
        if (entries == null)
        {
            return;
        }

        bool changed = false;

        foreach (CodexEntry entry in entries)
        {
            if (entry == null ||
                string.IsNullOrWhiteSpace(entry.EntryId))
            {
                continue;
            }

            if (unlockedIds.Add(entry.EntryId))
            {
                changed = true;
            }
        }

        if (!changed)
        {
            return;
        }

        RebuildUnlockedEntries();
        EntriesChanged?.Invoke();
    }

    public void MarkAsRead(CodexEntry entry)
    {
        if (entry == null ||
            string.IsNullOrWhiteSpace(entry.EntryId))
        {
            return;
        }

        if (!unlockedIds.Contains(entry.EntryId))
        {
            return;
        }

        if (!readIds.Add(entry.EntryId))
        {
            return;
        }

        EntriesChanged?.Invoke();
    }

    public bool IsRead(CodexEntry entry)
    {
        return entry != null &&
               readIds.Contains(entry.EntryId);
    }

    /// <summary>
    /// Возвращает копию ID всех открытых записей.
    /// Копия безопасна для записи в save-data.
    /// </summary>
    public List<string> GetUnlockedEntryIds()
    {
        return new List<string>(unlockedIds);
    }

    /// <summary>
    /// Возвращает копию ID всех прочитанных записей.
    /// Копия безопасна для записи в save-data.
    /// </summary>
    public List<string> GetReadEntryIds()
    {
        return new List<string>(readIds);
    }

    /// <summary>
    /// Полностью восстанавливает состояние кодекса по ID.
    /// Неизвестные ID игнорируются.
    /// Прочитанной может быть только уже открытая запись.
    /// </summary>
    public void RestoreProgress(
        IReadOnlyList<string> unlockedEntryIds,
        IReadOnlyList<string> readEntryIds
    )
    {
        unlockedIds.Clear();
        readIds.Clear();

        HashSet<string> validEntryIds =
            BuildValidEntryIdSet();

        if (unlockedEntryIds != null)
        {
            foreach (string entryId in unlockedEntryIds)
            {
                if (string.IsNullOrWhiteSpace(entryId))
                {
                    continue;
                }

                if (validEntryIds.Contains(entryId))
                {
                    unlockedIds.Add(entryId);
                }
            }
        }

        if (readEntryIds != null)
        {
            foreach (string entryId in readEntryIds)
            {
                if (string.IsNullOrWhiteSpace(entryId))
                {
                    continue;
                }

                if (unlockedIds.Contains(entryId))
                {
                    readIds.Add(entryId);
                }
            }
        }

        RebuildUnlockedEntries();
        EntriesChanged?.Invoke();
    }

    public void ResetProgress()
    {
        unlockedIds.Clear();
        readIds.Clear();
        unlockedEntries.Clear();

        EntriesChanged?.Invoke();
    }

    private HashSet<string> BuildValidEntryIdSet()
    {
        HashSet<string> validEntryIds =
            new HashSet<string>();

        foreach (CodexEntry entry in allEntries)
        {
            if (entry == null ||
                string.IsNullOrWhiteSpace(entry.EntryId))
            {
                continue;
            }

            validEntryIds.Add(entry.EntryId);
        }

        return validEntryIds;
    }

    private void RebuildUnlockedEntries()
    {
        unlockedEntries.Clear();

        foreach (CodexEntry entry in allEntries)
        {
            if (entry == null ||
                string.IsNullOrWhiteSpace(entry.EntryId))
            {
                continue;
            }

            if (unlockedIds.Contains(entry.EntryId))
            {
                unlockedEntries.Add(entry);
            }
        }

        unlockedEntries.Sort(
            (a, b) =>
            {
                int orderComparison =
                    a.Order.CompareTo(b.Order);

                if (orderComparison != 0)
                {
                    return orderComparison;
                }

                return string.Compare(
                    a.EntryId,
                    b.EntryId,
                    StringComparison.Ordinal
                );
            }
        );
    }
}

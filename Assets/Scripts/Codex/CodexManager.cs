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

    public void ResetProgress()
    {
        unlockedIds.Clear();
        readIds.Clear();
        unlockedEntries.Clear();

        EntriesChanged?.Invoke();
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

using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class CodexManager : MonoBehaviour
{
    private const string UnlockedKey =
        "Classified.Codex.Unlocked";

    private const string ReadKey =
        "Classified.Codex.Read";

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
        LoadProgress();
        RebuildUnlockedEntries();
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
        SaveProgress();
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

        SaveProgress();
        EntriesChanged?.Invoke();
    }

    public bool IsRead(CodexEntry entry)
    {
        return entry != null &&
               readIds.Contains(entry.EntryId);
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

    private void LoadProgress()
    {
        unlockedIds.Clear();
        readIds.Clear();

        LoadSet(
            PlayerPrefs.GetString(
                UnlockedKey,
                string.Empty
            ),
            unlockedIds
        );

        LoadSet(
            PlayerPrefs.GetString(
                ReadKey,
                string.Empty
            ),
            readIds
        );
    }

    private void SaveProgress()
    {
        PlayerPrefs.SetString(
            UnlockedKey,
            string.Join("|", unlockedIds)
        );

        PlayerPrefs.SetString(
            ReadKey,
            string.Join("|", readIds)
        );

        PlayerPrefs.Save();
    }

    private static void LoadSet(
        string serialized,
        HashSet<string> target
    )
    {
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return;
        }

        string[] ids =
            serialized.Split('|');

        foreach (string id in ids)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                target.Add(id);
            }
        }
    }
}
